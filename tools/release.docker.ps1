$originalLoc = $pwd;
Set-Location ..;

function askExit() {
    Write-Host "Press any key to exit...";
    Read-Host;
    Set-Location $oroginalPath;
}

docker build . -f ./src/Oliver.Api/Dockerfile --tag dockerhub.northeurope.cloudapp.azure.com/oliver;
if ($lastexitcode -ne 0) {
    askExit;
    return $lastexitcode;
}
docker push dockerhub.northeurope.cloudapp.azure.com/oliver;

Write-Host "Press any key to exit...";
Read-Host;

askExit;