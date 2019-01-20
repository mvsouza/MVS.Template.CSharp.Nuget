[CmdletBinding()]
param(
    [parameter(Mandatory=$true, Position=1)]
    [ValidateSet("install")]
    [string]$action,
    [parameter(Mandatory=$false)]
    [switch]$all
)
process {
    $tasks = @{};
    $tasks.Add("install",@{
        description="Installs the template on dotnet";
        script = {
            $task = $tasks.Get_Item("updateContent")
            if($task){
                Invoke-Command $task.script -ArgumentList (,$tasks);
            }
            $task = $tasks.Get_Item("buildInstall")
            if($task){
                Invoke-Command $task.script -ArgumentList (,$tasks);
            }
        }
    });

    $tasks.Add("updateContent",@{
        description="Update the template content";
        script = {
            git clone https://github.com/mvsouza/MVS.Template.CSharp.git temp;
            ls temp;
            $gitversion = ConvertFrom-Json "$(gitversion .\temp\)"
            $global:NuGetVersionV2 = $gitversion.NuGetVersionV2
            ls content  | ? {$_.Name -ne  ".template.config"} | %{rm $_.FullName -Recurse -Force}; #rm content/* -Exclude .template.config/* -Recurse;
            mv .\temp\* .\content\;
            rm temp -Force -Recurse
        }
    });

    $tasks.Add("buildInstall",@{
        description="Build and installs the template on dotnet";
        script = {
            #dotnet clean .\content\MVS.Template.CSharp.sln -ErrorAction SilentlyContinue;
            dotnet new -u mvs; 
            if (Test-Path .\MVS.Template.CSharp.*.nupkg) { rm .\MVS.Template.CSharp.*.nupkg -Force}
            nuget pack .\MVS.Template.CSharp.nuspec -version $global:NuGetVersionV2;
            dotnet new -i .\MVS.Template.CSharp.*.nupkg;
            ls;
        }
    });
    if ($action) {
        $task = $tasks.Get_Item($action)
        if($task){
            Invoke-Command $task.script -ArgumentList (,$tasks);
        }
    }
}
