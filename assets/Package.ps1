<#
.SYNOPSIS
    Packages the SQL Server Logging NuGet packages.
#>
param (
    $Configuration = "DEBUG",
    $IncrementVersion = $false,
    $Packages = @("Slalom.Stacks.Logging.SqlServer")
)

function Increment-Version() {
    $jsonpath = 'project.json'
    $json = Get-Content -Raw -Path $jsonpath | ConvertFrom-Json
    $versionString = $json.version
    $patchInt = [convert]::ToInt32($versionString.Split(".")[2].Split("-")[0], 10)
    [int]$incPatch = $patchInt + 1
    $patchUpdate = $versionString.Split(".")[0] + "." + $versionString.Split(".")[1] + "." + ($incPatch -as [string]) + "-*"
    $json.version = $patchUpdate
    $json = ConvertTo-Json $json -Depth 100


    $json = Format-Json $json    
    $json | Out-File  -FilePath $jsonpath
}


function Format-Json([Parameter(Mandatory, ValueFromPipeline)][String] $json) {
  $indent = 0;
  ($json -Split '\n' |
    % {
      if ($_ -match '[\}\]]') { 
        # This line contains  ] or }, decrement the indentation level
        $indent--
      }
      $line = (' ' * $indent * 2) + $_.TrimStart().Replace(':  ', ': ')
      if ($_ -match '[\{\[]') {
        # This line contains [ or {, increment the indentation level
        $indent++
      }
      $line
  }) -Join "`n"
}

function Clear-LocalCache() {
    $paths = nuget locals all -list
    foreach($path in $paths) {
        $path = $path.Substring($path.IndexOf(' ')).Trim()

        if (Test-Path $path) {

            Push-Location $path

            foreach($package in $Packages) {

                foreach($item in Get-ChildItem -Filter "$package" -Recurse) {
                    if (Test-Path $item) {
                        Remove-Item $item.FullName -Recurse -Force
                        Write-Host "Removing $item"
                    }
                }
            }

            Pop-Location
        }
    }
}

function Go ($Path) {
    Push-Location $Path

    Remove-Item .\Bin -Force -Recurse
    if ($IncrementVersion) {
        Increment-Version
    }
    else {
        Clear-LocalCache
    }
    dotnet build
    dotnet pack --no-build --configuration $Configuration
    copy .\bin\$Configuration\*.nupkg c:\nuget\

    Pop-Location
}

Push-Location $PSScriptRoot

foreach($package in $Packages) {
    Go "..\src\$package"
}

Pop-Location



