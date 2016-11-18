###############################################################################################
###### Validates a condition and returns the passed message in the event the condition is false
function Validate
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=0)]$preMessage,
        [Parameter(Position=1,Mandatory=1)]$conditionToCheck,
        [Parameter(Position=2,Mandatory=1)]$failureMessage,
        [Parameter(Position=3,Mandatory=0)]$successMessage
    )

	if ($preMessage) {
		Write-Host $preMessage -NoNewline -ForegroundColor Green
	}

    if ($conditionToCheck) {
		if ($successMessage) {
			Write-Host $successMessage -ForegroundColor DarkGreen
		}
		return 0
    } else {
		Write-Host "FAILED: $failureMessage" -ForegroundColor Red
		return 1
	}
}
###############################################################################################


###############################################################################################
###### Validates a condition and returns the passed message in the event the condition is false
function Update-SourceVersion
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)]$file_path,
        [Parameter(Position=1,Mandatory=1)]$version
    )

    $NewVersion = 'AssemblyVersion("' + $Version + '")';
    $NewFileVersion = 'AssemblyFileVersion("' + $Version + '")';
    
    if (Test-Path $file_path)
    {
        $tmpFile = "$file_path.tmp"
        Get-Content $file_path | 
            %{$_ -Replace 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', $NewVersion } |
            %{$_ -Replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', $NewFileVersion }  >   $TmpFile
            
        Move-Item $TmpFile $file_path -Force
    }
}
###############################################################################################



###############################################################################################
###### Finds a specified value in the packages folder
function Find-PackagePath {
	[CmdletBinding()]
	param (
		[Parameter(Position=0, Mandatory=1)]$package_path,
		[Parameter(Position=1, Mandatory=1)]$package_name
	)

	$packages = Get-ChildItem("$package_path\$package_name*") -ErrorAction SilentlyContinue

	if (!$packages) {
		return ""
	}

	return ($packages).FullName | Sort-Object $_ | Select -Last 1 -ErrorAction SilentlyContinue
}
###############################################################################################


###############################################################################################
####### Makes sure the necessary folders are created and returns the paths to known test assemblies
function Prepare-Tests
{
	[CmdletBinding()]
	param(
		[Parameter(Position=0, Mandatory=1)]$testRunnerName,
		[Parameter(Position=1, Mandatory=1)]$publishedTestsDirectory,
		[Parameter(Position=2, Mandatory=1)]$testResultsDirectory
	)

	$projects = Get-ChildItem $publishedTestsDirectory

	if ($projects.Count -eq 1)
	{
		Write-Host "1 $testRunnerName project has been found:"
	} else {
		Write-Host $projects.Count " $testRunnerName projects have been found:"
	}

	Write-Host ($projects | Select $_.Name )

	if (-Not (Test-Path $testResultsDirectory))
	{
		Write-Host "Creating test results directory"
		New-Item -ItemType Directory $testResultsDirectory | Out-Null
	}

	$testAssemblyPaths = $projects | ForEach-Object { 
        $path = $_.FullName
        Get-ChildItem "$path\bin" -Recurse -Filter "*Tests.dll" | % { $_.FullName }
	}

    return $testAssemblyPaths
#	$testAssemblies = [string]::Join(" ", $testAssemblyPaths)
#	return $testAssemblies
}
###############################################################################################


###############################################################################################
####### Compiles the solution
function Compile {
	[CmdletBinding()]
	param(
		[Parameter(Position=0, Mandatory=0)]$mode,
        [Parameter(Position=1, Mandatory=0)]$path,
		[Parameter(Position=2, Mandatory=0)]$output_directory
	)

	Write-Host "Compiling the solution" -ForegroundColor DarkYellow
	if ($output_directory) {
		Exec { & "C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe" $path /t:Build /p:Configuration=$mode /v:m /p:OutDir=$output_directory /p:TargetFrameworkMoniker=""".NETFramework,Version=4.6.1""" } 
		
		foreach ($file in $non_deploy_files) {
			$del_path = "$output_directory\$file"
			Exec { & del $del_path }
		}

	} else {
		Exec { & "C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe" $path /t:Build /p:Configuration=$mode /v:m } 
	}

	# Sleep 2 seconds after build
	Start-Sleep -Seconds 2
}
###############################################################################################


###############################################################################################
####### Specify Default values that can be overridden in the config
$artifacts_directory_name = "Artifacts"
$test_results_directory_name = "TestResults"
$tests_directory_name = "Tests"
$packages_folder_name = "Packages"
$build_directory_name = "Build"
$source_directory_name = "Source"
#$artifacts_folder = "Artifacts"

$test_filter = @("*UnitTest.dll", "*IntegrationTest.dll")
$non_deploy_files = @("*.pdb")
$do_xunit = 0
$do_nunit = 0
$do_dotcover = 0
$nuspec_file_name = ""
# Include the config and execute the init_config method to set values
Include "config.ps1"
properties {
	. init_config
}

# Setup the script variables / paths
$script_root_path = (Get-Item $PSScriptRoot).Parent.FullName
$source_path = "$script_root_path\$source_directory_name"
$solutions = (Get-ChildItem $source_path -Filter "*.sln")
$build_path = "$script_root_path\$build_directory_name"
$artifacts_path = "$script_root_path\$artifacts_directory_name"
$temp_build_path = "$script_root_path\.build"
$temp_package_path = "$script_root_path\.package"

$packages_path = "$source_path\packages"
$nuget_path = "$build_path\nuget\nuget.exe"
$default_config_path = "$build_path\default.ps1"
$build_config_path = "$build_path\config.ps1"
$non_deploy_files = @("*.pdb")
$xunit_path = (Find-PackagePath $packages_path "XUnit.Runner.Console") + "\Tools\xunit.console.exe"
$nunit_path = (Find-PackagePath $packages_path "NUnit.Runners") + "\Tools\nunit.console-x86.exe"
$opencover_path = (Find-PackagePath $packages_path "OpenCover") + "\Tools\OpenCover.Console.exe"
$test_results_directory = "$script_root_path\$test_results_directory_name"
$tests_directory = "$script_root_path\$tests_directory_name"


$solution_name = ""

if ($solutions) {
	$solution_name = $solutions[0]
}

$solution_path = "$source_path\$solution_name"

# Set the format of the task name in Psake
FormatTaskName {
   param($taskName)
   $name = (Get-Culture).TextInfo.ToTitleCase($taskname)
   Write-Host (("-"*25) + "[$name]" + ("-"*25)) -ForegroundColor green
   # write-host "Executing Task: $name" -foregroundcolor green 
}
###############################################################################################

###############################################################################################
# Initialize makes sure that all of the necessary variables are set properly and that 
# it can find the folders and paths needed to run
task Initialize {
	$setup_failure = 0
	$setup_failure += Validate `
						-preMessage "Solution Directory: " `
						-failureMessage $script_root_path `
						-successMessage $script_root_path `
						-conditionToCheck (Test-Path $script_root_path)

	$ignored_value = Validate `
		                  -preMessage "Artifacts Directory: " `
		                  -failureMessage $artifacts_path `
		                  -successMessage $artifacts_path `
		                  -conditionToCheck (Test-Path $artifacts_path)

	$setup_failure += Validate `
						-preMessage "Build Config Path: " `
						-failureMessage $build_config_path `
						-successMessage $build_config_path `
						-conditionToCheck (Test-Path $build_config_path)

	$setup_failure += Validate `
						-preMessage "Solution Name: " `
						-failureMessage $solution_name `
						-successMessage $solution_name `
						-conditionToCheck ($solution_name)
                        
    $setup_failure += Validate `
						-preMessage "Packages Path: " `
						-failureMessage $packages_path `
						-successMessage $packages_path `
						-conditionToCheck ($packages_path)

    $setup_failure += Validate `
						-preMessage "NuGet Path: " `
						-failureMessage $nuget_path `
						-successMessage $nuget_path `
						-conditionToCheck ($nuget_path)

    if (([bool]$nuspec_file_name)) 
    {
        $nuget_spec_path = "$build_path\$nuspec_file_name"
        $setup_failure += Validate `
						-preMessage "Nuspec Path: " `
						-failureMessage $nuget_spec_path `
						-successMessage $nuget_spec_path `
						-conditionToCheck (Test-Path $nuget_spec_path)    
    }
    
        
    $SharedAssemblyPath = "$build_path\SharedAssemblyInfo.cs"
    if (Test-Path $SharedAssemblyPath)
    {
        Write-Host "Updating Assembly Version" -ForegroundColor DarkYellow
        Update-SourceVersion -Version $version -file_path $SharedAssemblyPath
    }
    
	if ($setup_failure -gt 1) {
		Exit 1
	}
}
###############################################################################################


###############################################################################################
####### Restores nuget packages
task Restore -Depends Initialize {
	if (Get-Command "custom_restore" -ErrorAction SilentlyContinue) {
		custom_restore
		return
	}

	Write-Host "Restoring NuGet Packages" -ForegroundColor DarkYellow
	Exec { & $nuget_path restore -PackagesDirectory $packages_path $solution_path }
}
###############################################################################################


###############################################################################################
####### Cleans the artifacts directory and build directories
task Clean `
	 -Depends Restore {
	if (Get-Command "custom_clean" -ErrorAction SilentlyContinue) {
		custom_clean
		return
	}
    
	if (Test-Path $artifacts_path)
	{
		Write-Host "Cleaning Artifacts Directory" -ForegroundColor DarkYellow
		Exec { & del "$artifacts_path\*" -Recurse -Force }
	}
    
    if (Test-Path $temp_build_path)
	{
		Write-Host "Cleaning Temp Build Directory" -ForegroundColor DarkYellow
		Exec { & del "$temp_build_path\*" -Recurse -Force }
	}
    
    if (Test-Path $temp_package_path)
	{
		Write-Host "Cleaning Temp Package Directory" -ForegroundColor DarkYellow
		Exec { & del "$temp_package_path\*" -Recurse -Force }
	}

	Write-Host "Cleaning the build" -ForegroundColor DarkYellow
	Exec { msbuild $solution_path /t:Clean /p:Configuration=Release /v:m }
}
###############################################################################################


###############################################################################################
####### Compiles the code
task Compile `
	-depends Clean, Restore {
	if (Get-Command "custom_compile" -ErrorAction SilentlyContinue) {
		custom_compile
		return
	}

	Compile -mode Release -path $solution_path
    
    if ($deploy_path) {
        Compile -mode Release -output_directory
    }
}
###############################################################################################


###############################################################################################
####### Runs code coverage
task Cover -depends Compile {


}
###############################################################################################


###############################################################################################
####### Runs unit tests
task Test -depends Compile {
	if (Get-Command "custom_test" -ErrorAction SilentlyContinue) {
		custom_test
		return
	}
    
    $unit_tests_run = 0

	# Compile -mode Release

	if ($do_xunit -eq 1) {
		$xunit_found = Validate `
							-failureMessage "XUnit Console could not be found" `
							-conditionToCheck (Test-Path $xunit_path)

		if ($xunit_found -gt 0) {
			return
		}
		
		Write-Host "Running XUnit Tests" -ForegroundColor DarkYellow
		$testAssemblies = Prepare-Tests -testRunnerName "XUnit" `
									-publishedTestsDirectory $tests_directory `
									-testResultsDirectory $test_results_directory

		Exec { & $xunit_path $testAssemblies -Xml "$test_results_directory\XUnit.xml" -nologo -noshadow }
        
        $unit_tests_run = 1
	}

	if ($do_nunit -eq 1) {
		$nunit_found = Validate `
					-failureMessage "NUnit Console could not be found" `
					-conditionToCheck (Test-Path $nunit_path)


		if ($nunit_found -gt 0) {
			return
		}
		Write-Host "Running NUnit Tests" -ForegroundColor DarkYellow
		$testAssemblies = Prepare-Tests -testRunnerName "NUnit" `
									-publishedTestsDirectory $tests_directory `
									-testResultsDirectory $test_results_directory

		Exec { & $nunit_path $testAssemblies.Replace("`"", "") -Xml "$test_results_directory\XUnit.xml" -nologo -noshadow }
        
        $unit_tests_run = 1
	}
    
    if ($unit_tests_run -eq 0)
    {
        Write-Host "No unit tests runners configured." -ForegroundColor DarkYellow
    }
}
###############################################################################################

###############################################################################################
####### Packages the output into a nuget package
task Package -depends Compile {
	if (Get-Command "custom_package" -ErrorAction SilentlyContinue) {
		custom_package
		return
	}
    
    if (!([bool]$nuspec_file_name)) {
        Write-Host "No Nuspec file to package with"
    }

    
    $build_file_path = $temp_build_path
    if ($deploy_project) {
        $build_file_path = "$source_path\$deploy_project\bin\release"
    }
    
    if (-Not (Test-Path $temp_package_path))
	{
		Write-Host "Creating temp package directory"
		New-Item -ItemType Directory $temp_package_path | Out-Null
	}
    
    Write-Host "Copying files to temp package directory" -ForegroundColor DarkYellow
    $skipList = @(".pdb", ".xml")
    Get-ChildItem $build_file_path -File |
    Foreach-object {
        if ($skipList -notcontains $_.Extension)
        {
            Copy-Item $_.FullName $temp_package_path
        }
    }

	if (Test-Path "$build_file_path\x86") {
		Copy-Item "$build_file_path\x86" $temp_package_path -Recurse
	}

	if (Test-Path "$build_file_path\x64") {
		Copy-Item "$build_file_path\x64" $temp_package_path -Recurse
	}
    
	Write-Host "Packaging the project" -ForegroundColor DarkYellow
    if (Test-Path "$build_path\Deploy.ps1")
    {
        Write-Host "Copying Deploy Script" -ForegroundColor DarkYellow
	    copy "$build_path\Deploy.ps1" "$temp_package_path"
    }
    
    if (Test-Path "$build_path\PostDeploy.ps1")
    {
        Write-Host "Copying PostDeploy Script" -ForegroundColor DarkYellow
	    copy "$build_path\PostDeploy.ps1" "$temp_package_path"
    }
    
    if (Test-Path "$build_path\$nuspec_file_name")
    {
        Write-Host "Copying Nuspec File" -ForegroundColor DarkYellow
        copy "$build_path\$nuspec_file_name" "$temp_package_path\$nuspec_file_name"
    }

    if (-Not (Test-Path $artifacts_path))
	{
		Write-Host "Creating artifacts directory"
		New-Item -ItemType Directory $artifacts_path | Out-Null
	}
    
    Exec { & $nuget_path pack "$temp_package_path\$nuspec_file_name" -BasePath $temp_package_path -Output $artifacts_path -Version $version }
}
###############################################################################################
