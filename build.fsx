#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target 
nuget Fake.DotNet.Paket 
nuget Fake.Runtime 
nuget Fake.Tools.Git
nuget FSharp.Data
nuget Newtonsoft.Json
nuget NuGet //"

#load ".fake/build.fsx/intellisense.fsx"

open Newtonsoft.Json
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.DotNet.NuGet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing
open Fake.IO.Globbing.Operators
open Fake.Tools
open Fake.Tools.Git
open Fake.Tools.Git.CommandHelper
open FSharp.Data
open System
open System.IO

let branch = "develop"
let tempFolder = "temp"

Target.create "Clean" (fun _ ->
    !! "temp/.git"
    ++ "temp"
    ++ "content/"
    |> Shell.cleanDirs 
)

Target.create "ClearPackage" (fun _ ->
    !! "*.nupkg"
    |> File.deleteAll 
)

Target.create "Clone" (fun _ ->
    Repository.cloneSingleBranch "" "https://github.com/mvsouza/MVS.Template.CSharp" branch "temp"
)

Target.create "Copy" (fun _ ->
    !! "temp/**/*"
    |> GlobbingPattern.setBaseDir "temp"
    |> Shell.copyFilesWithSubFolder "content/"
    !! ".template.config/**/*"
    |> Shell.copyFilesWithSubFolder "content/"
    !! "content/.git"
    ++ "content/**/*.suo"
    ++ "content/**/*.csproj.user"
    ++ "content/**/*.gpState"
    ++ "content/**/bin/**"
    ++ "content/**/obj/**"
    |> Shell.cleanDirs 
)

type GitversionParams = {
    ToolPath : string
}

type GitVersionProperties = {
    Major : int
    Minor : int
    Patch : int
    PreReleaseTag : string
    PreReleaseTagWithDash : string
    PreReleaseLabel : string
    PreReleaseNumber : Nullable<int>
    BuildMetaData : string
    BuildMetaDataPadded : string
    FullBuildMetaData : string
    MajorMinorPatch : string
    SemVer : string
    LegacySemVer : string
    LegacySemVerPadded : string
    AssemblySemVer : string
    FullSemVer : string
    InformationalVersion : string
    BranchName : string
    Sha : string
    NuGetVersionV2 : string
    NuGetVersion : string
    CommitsSinceVersionSource : int
    CommitsSinceVersionSourcePadded : string
    CommitDate : string
}

let generateProperties (setParams : GitversionParams -> GitversionParams) =
    let result = CreateProcess.fromRawCommand "dotnet" ["gitversion"]
                |> CreateProcess.redirectOutput
                |> Proc.run
    if result.ExitCode <> 0 then failwithf "GitVersion.exe failed with exit code %i and message %s" result.ExitCode (String.concat "" [result.Result.Output])
    result.Result.Output |> JsonConvert.DeserializeObject<GitVersionProperties>

let getGitHash =
    let _,msg,error = runGitCommand tempFolder "log --oneline -1"
    if error <> "" then failwithf "git log --oneline failed: %s" error
    let log = msg |> Seq.head
    log.Split(' ') |> Seq.head 

Target.create "Pack" (fun _ ->
    let version = generateProperties id
    NuGet.NuGet (fun p -> 
        { p with
            ToolPath = "nuget"
            Version = version.FullSemVer
            OutputPath = "./"
            WorkingDir = "./"
        }
    ) "MVS.Template.CSharp.nuspec"
)

Target.create "NuGetPush" (fun _ ->
    !! "*.nupkg"
    |> Seq.iter (DotNet.nugetPush (fun opt -> 
    opt.WithPushParams(
      { opt.PushParams with 
          ApiKey = Some (Environment.environVarOrFail "NUGET_APIKEY")
          Source = Some "https://api.nuget.org/v3/index.json"
      })
  ))
)

Target.create "All" ignore

"Clean"
   ==> "Clone"
   ==> "Copy"
   ==> "ClearPackage"
   ==> "Pack"
   ==> "NuGetPush"

Target.runOrDefault "All"
