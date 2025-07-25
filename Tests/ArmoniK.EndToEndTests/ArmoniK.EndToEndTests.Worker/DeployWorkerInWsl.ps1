Write-Host "Deployment of the Worker on WSL. Prerequisites:"
Write-Host "    - WSL must be started."
Write-Host "    - ArmoniK must be already deployed with make deploy"
Write-Host ""

# Set current directory to the current script location
Set-Location $PSScriptRoot

$wslUser = wsl whoami
Write-Host "WSL user is $wslUser"
$wslDistrib = ((wsl --status) -replace "`0","" -split ":")[1].Trim()
Write-Host "WSL distribution is $wslDistrib"

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

# Find the destination folder (should contain path /ArmoniK/infrastructure/quick-deploy/localhost/data)
$deployment = $(wsl kubectl -n armonik get deployments/compute-plane-default -o json) | ConvertFrom-Json
$sharedVolume = $deployment.spec.template.spec.volumes | Where-Object { $_.name -eq "shared-volume" }
$destinationPath = $sharedVolume.hostPath.path

# Deploy the worker
Write-Host ""
Write-Host "Deploying $zipPath to WSL $destinationPath"
Copy-Item $zipPath -Destination "\\wsl$\$wslDistrib\home\$wslUser"
wsl -e bash -c "cd ~ && mv $zipName $destinationPath"

# Get the url of the control plane and set it in appSettings.json of the client
Write-Host "Fetching the control plane url"
$localhostPath = (wsl dirname $destinationPath)
$armonikOutPut = (wsl cat $localhostPath/generated/armonik-output.json) | ConvertFrom-Json
$appSettingsPath = "..\ArmoniK.EndToEndTests.Client\appSettings.json"
try
{
	$appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
	$appSettings.Grpc.EndPoint = $armonikOutPut.armonik.control_plane_url
}
catch{
	Write-Error "Unexpected error (syntax error?) while parsing $appSettingsPath"
	return 1
}

# Write the url	
$url = $appSettings.Grpc.EndPoint
Write-Host "Set control plane url $url to $appSettingsPath"
$appSettings | ConvertTo-Json -Depth 4 | Out-File $appSettingsPath

# Restart computeplane pods
Write-Host "Restarting compute plane pods"
wsl kubectl -n armonik delete pod -l partition=default
