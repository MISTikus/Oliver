$originalLoc = $pwd;
Set-Location ..;
Write-Host "Current location is $pwd";

docker build . -f ./src/Oliver.Api/Dockerfile --tag oliver:dev;

Write-Host "Press any key to exit...";
Read-Host;

Set-Location $originalLoc;