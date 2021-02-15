$oroginalPath = $pwd;
Set-Location ..;

function askExit() {
    Write-Host "Press any key to exit...";
    Read-Host;
    Set-Location $oroginalPath;
}

docker build . -f ./src/Oliver.Api/Dockerfile --tag oliver:dev;
if ($lastexitcode -ne 0) {
    askExit;
    return $lastexitcode;
}

docker run -it --rm `
    --env-file ./tools/.env `
    -p 5000:5000 `
    -p 443:5001 `
    -v "$($pwd)\tools:C:\settings" `
    -v "$($env:USERPROFILE)\.aspnet\https:C:\https" `
    oliver:dev

askExit;