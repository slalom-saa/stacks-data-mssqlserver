#
# Install.ps1
#
param($installPath, $toolsPath, $package)

$null = [Reflection.Assembly]::LoadFile( (((Get-Item $toolsPath).Parent.FullName) + "\lib\Newtonsoft.Json.dll"))

function GetSolutionProjects(){
    $projects = get-interface $dte.Solution.Projects ([EnvDTE.Projects])
    $result = new-object "System.Collections.Generic.List[EnvDTE.Project]"
    foreach($project in $projects.GetEnumerator()) {
            if($project -eq $null){
                continue;
            }

            if($project.Kind -eq [EnvDTE80.ProjectKinds]::vsProjectKindSolutionFolder){

                foreach($solutionFolderProject in RecurseSolutionFolderProjects($project)){
                    $result+=$solutionFolderProject
                }

            } else {
                $result+=$project
            }
    }
    return $result
}

function RecurseSolutionFolderProjects(){
    param($solutionFolder = $(throw "Please specify a solution folder."))
    $projectList = @()
    for($i = 1; $i -le $solutionFolder.ProjectItems.Count; $i++){
        $subProject = $solutionFolder.ProjectItems.Item($i).subProject
        if($subProject -eq $null){
            continue;
        }

        if($subProject.Kind -eq [EnvDTE80.ProjectKinds]::vsProjectKindSolutionFolder)
        {
            $projectList += RecurseSolutionFolderProjects($subProject)
        } else {
            $projectList += $subProject
        }
    }
    return $projectList
}

function Ensure-RootConfig($path) {
	if (!(Test-Path $path)) {
		"log  : Adding appsettings.json"
		"{}" > $path
	}
	$JObject = [Newtonsoft.Json.JsonConvert]::DeserializeObject((Get-Content $path))
	Ensure-Node $JObject 'Application' "'$projectName'"
	Ensure-Node $JObject 'Environment' "'Local'"
	if ($JObject -ne $null) {
		"log  : Ensuring environment and application name to appsettings.json"
		[Newtonsoft.Json.JsonConvert]::SerializeObject($JObject) > $path
	}
}

function Ensure-Node($root, $names, $value) {
	$current = $root;    
	$names = $names.Split('.');
	for($i = 0; $i -lt $names.Length; $i++) {
		$name = $names[$i];
		if ($i -lt $names.Length - 1) {
			if ($current[$name] -eq $null) {            
				$current[$name] = [Newtonsoft.Json.Linq.JObject]::Parse("{}")
			}
			$current = $current[$name]
		}
		else {
			if ($current[$name] -eq $null) {
				$current[$name] = if ($value -ne $null) { $value } else { $null }
			}
		}
	}
}

function Format-Json($path) {
	$json = Get-Content $path
	$json = [Newtonsoft.Json.JsonConvert]::SerializeObject([Newtonsoft.Json.JsonConvert]::DeserializeObject($json), [Newtonsoft.Json.Formatting]::Indented)
	$json | Set-Content $path
}

function Ensure-Current($path) {
    $JObject = [Newtonsoft.Json.JsonConvert]::DeserializeObject((Get-Content $path))
    Ensure-Node -root $JObject -names 'Stacks.Logging.SQL.ConnectionString'
    
    if ($JObject -ne $null) {
		"log  : Ensuring SQL Logging settings "
        [Newtonsoft.Json.JsonConvert]::SerializeObject($JObject) > $path
    }
}


foreach($project in GetSolutionProjects) {	
	if ($project.FullName -ne $null -and $project.FullName -ne '') {
		
		"log  : Ensuring configuration for " + $project.Name

		$path = (Get-Item $project.FullName).Directory

		Push-Location $path

		Ensure-Current .\appsettings.json
		Format-Json .\appsettings.json

		Pop-Location
	}	
}