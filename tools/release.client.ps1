$oroginalPath = $pwd;
Set-Location ..;

function askExit() {
    Write-Host "Press any key to exit...";
    Read-Host;
    Set-Location $oroginalPath;
}

dotnet restore;
dotnet publish ./src/Oliver.Client -c Release -r win10-x64 -p:PublishSingleFile=true -o ./release

askExit;