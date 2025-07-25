Write-Host "Prerequisite to the present script:"
Write-Host "    - WSL must be started."
Write-Host "    - Armonik must be installed on WSL at location ~/ArmoniK"
Write-Host "    - ArmoniK must be already deployed with make deploy"
Write-Host ""

$armonikPath = "~/ArmoniK/infrastructure/quick-deploy/localhost"
wsl test -d $armonikPath
if ($LASTEXITCODE -eq 0) {
} else {
    Write-Host "ArmoniK is not installed on WSL. Expected path is ~/ArmoniK"
	return 1
}

# Set current directory to the current script location
Set-Location $PSScriptRoot

$wslUser = wsl -d Ubuntu -e whoami

$Project = "ArmoniK.EndToEndTests.Worker"
$Version = "1.0.0-700"
$pathToBinaries = "..\publish\$Project\$Version"

if (Test-Path $pathToBinaries)
{
	# Remove binaries from any previous build
	Remove-Item $pathToBinaries\*
}

$zipName = "$Project-v$Version.zip"
$zipPath = "..\packages\$zipName"
if (Test-Path $zipPath) {
	Write-Host "Remove existing zip file"
	Remove-Item $zipPath
}

# Trigger compilation of the worker
Write-Host "Build and publish worker"
dotnet publish --self-contained -c Release -r linux-x64 -f net6.0 .

# Deploy the worker
$destination = "~/ArmoniK/infrastructure/quick-deploy/localhost/data"
Write-Host "Deploying $zipPath to WSL"
Copy-Item $zipPath -Destination "\\wsl$\Ubuntu\home\$wslUser"
wsl -e bash -c "cd ~ && mv $zipName $destination"

# Get the url of the control plane and set it in appSettings.json of the client
Write-Host "Fetching the control plane url"
$armonikOutPut = (wsl cat ~/ArmoniK/infrastructure/quick-deploy/localhost/generated/armonik-output.json) | ConvertFrom-Json
$appSettingsPath = "..\ArmoniK.EndToEndTests.Client\appSettings.json"
try
{
	$appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
	$appSettings.Grpc.EndPoint = $armonikOutPut.armonik.control_plane_url
}
catch{
	Write-Error "Unexpected error (syntax error?) while parsing $appSettingsPath"
	return
}

# Write the url	
Write-Host "Set control plane url to $appSettingsPath"
$appSettings | ConvertTo-Json -Depth 4 | Out-File $appSettingsPath

# Restart computeplane pods
Write-Host "Restarting compute plane pods"
$pods -split "`n" | Where-Object { $_ -like "compute-plane*" } | ForEach-Object {
	$name = ($_ -split "\s+")[0]
	Write-Host "    Restart pod $name"
	wsl kubectl -n armonik delete pod $name
}
