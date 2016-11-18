param(
	[Parameter(Position=0,Mandatory=0)]$configPath,
	[Parameter(Position=1,Mandatory=0)]$commandName,
    [Parameter(Position=2,Mandatory=1)]$version
)

if (!$configPath) {
	$configPath = ".\Build\Config.ps1"
}

if (!$commandName) {
	$commandName = "Compile"
}

if (-Not (Test-Path $configPath)) {
	Write-Host "Unable to find $configPath this file is required to execute. Add the file, or pass the -ConfigPath paramter to set it." -ForegroundColor Red
	Exit 1
}

$packages_folder_name = "Packages"
$script_root_path = (Get-Item $PSScriptRoot).Parent.FullName
$source_root_path = "$script_root_path\Source"
$build_directory_name = "build"
$nuget_path = "$script_root_path\$build_directory_name\nuget\nuget.exe"
$build_directory_path = "$script_root_path\$build_directory_name"

. $configPath

$packages_path = "$source_root_path\$packages_folder_name\"
$default_config_path = "$script_root_path\$build_directory_name\default.ps1"
$build_packages_path = "$script_root_path\$build_directory_name\packages.config"
$setup_failure = 0

Write-Host "-----------------------[Build Setup]-----------------------"
Write-Host "Build Directory: " -NoNewLine -ForegroundColor Green
if (Test-Path $build_directory_path) {
	Write-Host $build_directory_path -ForegroundColor DarkGreen
} else {
	Write-Host "FAILED: $build_directory_path" -ForegroundColor Red
	$setup_failure = 1
}

Write-Host "Default Config Path: " -NoNewline -ForegroundColor Green
if (Test-Path $default_config_path) {
	Write-Host $default_config_path -ForegroundColor DarkGreen
} else {
	Write-Host "FAILED: $default_config_path" -ForegroundColor Red
	$setup_failure = 1
}

Write-Host "Build Packages Path: " -NoNewLine -ForegroundColor Green
if (Test-Path $build_packages_path) {
	Write-Host $build_packages_path -ForegroundColor DarkGreen
} else {
	Write-Host "FAILED: $build_packages_path" -ForegroundColor Red
	$setup_failure = 1
}

Write-Host "Nuget Path: " -NoNewLine -ForegroundColor Green
if (Test-Path $nuget_path) {
	Write-Host $nuget_path -ForegroundColor DarkGreen
} else {
	Write-Host "FAILED: $nuget_path" -ForegroundColor Red
	$setup_failure = 1
}

if ($setup_failure -eq 1) {
	Write-Host "Please correct the setup issues and try again." -ForegroundColor Red
	Exit 1
}

$allowedCommands = @("Compile", "Test", "Package", "Restore", "Clean")

# If we don't have a valid command, exit
if ($allowedCommands -notcontains $commandName)
{
	Write-host "Unknown command: $commandName" -ForegroundColor Red
	Write-host "Available commands are: $allowedCommands" -ForegroundColor Red
	Exit 1
}

# This function will execute a nuget restore call if needed
# and then Import the psake module
function Register-PSake {
	Write-Host "Restoring Build Packages" -ForegroundColor DarkYellow
	& $nuget_path restore $build_packages_path -PackagesDirectory $packages_path | Out-Null
	
	Write-Host "Importing Psake" -ForegroundColor DarkYellow
	Remove-Module [p]sake
	Import-Module "$packages_path\psake*\tools\psake.psm1" -Scope Global
}

Register-PSake
Write-Host "Invoking Psake Command: $commandName" -ForegroundColor DarkYellow
Invoke-Psake -framework 4.6.1 $default_config_path $commandName -parameters @{"Version"="$version"}

if ($psake.build_success) {
	exit 0
}
else
{
	exit 1
}
