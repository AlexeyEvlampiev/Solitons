# Assert STAGING_TYPE environment variable is set
if (-not $env:STAGING_TYPE) {
    throw "STAGING_TYPE environment variable must be set"
}

# Execute commands
$functionsPath = Join-Path $PSScriptRoot "functions.ps1"
 . $functionsPath

Set-Location -Path "$PSScriptRoot/.." -ErrorAction Stop
Get-ChildItem -Directory | Select-Object Name

Write-Host "Preprocessing source code for $env:STAGING_TYPE release..."
Config-Packages -staging $env:STAGING_TYPE -searchRoot './src/'

@"

  _         _ _    _ 
 | |__ _  _(_) |__| |
 | '_ \ || | | / _` |
 |_.__/\_,_|_|_\__,_|
                                         
                       
"@
dotnet restore solitons.sln
dotnet build solitons.sln -c Release --no-restore

@"
  _____       _   
 |_   _|__ __| |_ 
   | |/ -_|_-<  _|
   |_|\___/__/\__|
                                                   
"@
dotnet test solitons.sln -c Release --no-build --verbosity normal

@"
  ___         _   
 | _ \__ _ __| |__
 |  _/ _` / _| / /
 |_| \__,_\__|_\_\
                                                                   
"@
$projects = @(
    "src/Solitons.Core/Solitons.Core.csproj",
    "src/Solitons.Postgres/Solitons.Postgres.csproj",
    "src/Solitons.Postgres.PgUp/Solitons.Postgres.PgUp.csproj",
    "src/Solitons.SQLite/Solitons.SQLite.csproj",
    "src/Solitons.Azure/Solitons.Azure.csproj"
)
$projects | ForEach-Object {
    dotnet pack $_ -c Release -o /app/packages
}


@"
  ___         _    
 | _ \_  _ __| |_  
 |  _/ || (_-< ' \ 
 |_|  \_,_/__/_||_|
                                                                                      
"@
$packages = @(
    "/app/packages/Solitons.Core.*.nupkg",
    "/app/packages/Solitons.Azure.*.nupkg",
    "/app/packages/Solitons.Postgres.*.nupkg",
    "/app/packages/Solitons.Postgres.PgUp.*.nupkg"
)
$packages | ForEach-Object {
    dotnet nuget push $_ --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
}




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