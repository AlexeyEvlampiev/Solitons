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

Write-Host "Preprocessing debian control files"
Update-AllDebianControlFiles -searchRoot './src/'

@"

  _         _ _    _ 
 | |__ _  _(_) |__| |
 | '_ \ || | | / _` |
 |_.__/\_,_|_|_\__,_|
                                         
                       
"@
dotnet restore solitons.sln
if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

dotnet build solitons.sln -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# Build the Linux executable for Solitons.Postgres.PgUp.Native
Write-Host "Publishing Linux executable for Solitons.Postgres.PgUp.Native project..."
dotnet publish ./src/Solitons.Postgres.PgUp.Native/Solitons.Postgres.PgUp.Native.csproj -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
if ($LASTEXITCODE -ne 0) { throw "PgUp publish failed" }
Write-Host "Linux executable build completed successfully!"

# Define variables
$debianDir = "./src/Solitons.Postgres.PgUp.Native/debian"
$outputDir = "./src/Solitons.Postgres.PgUp.Native/bin/Release/linux-x64"

# Create necessary directories
Write-Host "Creating directories for Debian package..."
mkdir "$outputDir/DEBIAN" -Force | Out-Null

# Copy control file
Write-Host "Copying control file..."
Copy-Item "$debianDir/control" "$outputDir/DEBIAN/control"

# Add the built CLI app to the package
Write-Host "Copying built executable to package..."
mkdir "$outputDir/usr/local/bin" -Force | Out-Null
Copy-Item "$outputDir/publish/pgup" "$outputDir/usr/local/bin/pgup"

# Build the .deb package
Write-Host "Building the Debian package..."
$packagePath = "$outputDir/pgup_$($version)_amd64.deb"
dpkg-deb --build "$outputDir" $packagePath
Write-Host "Debian package created at $packagePath"


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


# cloudsmith push deb your-org/your-repo/any-distro/any-version pgup_1.0.0_amd64.deb
