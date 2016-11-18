# init_config sets the config variables to control how the build script behaves
function init_config {
	$non_deploy_files = @("*.pdb")
	$do_xunit = 1
	$do_dotcover = 1
    $nuspec_file_name = "OldSchool.nuspec"
    $deploy_project = "OldSchool.Service"
}

# function custom_clean {
# }

# function custom_compile {
# }

# function custom_test {
# }

# function custom_analyze {
# }

# function custom_document {
# }

# function custom_package {
# }
