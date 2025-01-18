
$functionsPath = Join-Path $PSScriptRoot "functions.ps1"
 . $functionsPath

 @"
  _   _       _ _     _   
 | | | |_ __ | (_)___| |_ 
 | | | | '_ \| | / __| __|
 | |_| | | | | | \__ \ |_ 
  \___/|_| |_|_|_|___/\__|
                                                                                      
"@

# Call Unlist-PreviousPrereleases for each package
$packageIds = @(
    "Solitons.Core",
    "Solitons.Azure",
    "Solitons.Postgres",
    "Solitons.Postgres.PgUp"
)

Write-Host "Starting to unlist previous prereleases..."
Unlist-PreviousPrereleases -PackageIds $packageIds
Write-Host "Completed unlisting previous prereleases."