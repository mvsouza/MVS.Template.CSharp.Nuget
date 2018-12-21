[CmdletBinding()]
param(
    [parameter(Mandatory=$true, Position=1)]
    [ValidateSet("install", "clean")]
    [string]$action,
    [parameter(Mandatory=$false)]
    [switch]$all
)
process {
    $tasks = @{};
    $tasks.Add("install",@{
        description="Installs the template on dotnet";
        script = {
            if (Test-Path temp) { rm temp -Force }
            git clone https://github.com/mvsouza/MVS.Template.CSharp.git temp;
            dotnet new -u mvs; 
            if (Test-Path .\MVS.Template.CSharp.1.0.0.nupkg) { rm .\MVS.Template.CSharp.1.0.0.nupkg -Force }
            dotnet clean .\content\MVS.Template.CSharp.sln ;
            nuget pack .\MVS.Template.CSharp.nuspec; 
            dotnet new -i .\MVS.Template.CSharp.1.0.0.nupkg
        }
    });
    $tasks.Add("clean",@{
        description="Installs the template on dotnet";
        script = {
            if (Test-Path temp) { rm temp -Force }
            dotnet new -u mvs; 
            if (Test-Path .\MVS.Template.CSharp.1.0.0.nupkg) { rm .\MVS.Template.CSharp.1.0.0.nupkg -Force }
            rm content/* -Exclude .template.config
        }
    });

    $task = $tasks.Get_Item($action)
    if ($task) {
        Invoke-Command $task.script
    }
}
