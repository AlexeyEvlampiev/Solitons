

$version = [Version]::Parse("1.1")
$ticks = ((New-TimeSpan -Start (Get-Date "1/1/2024") -End (Get-Date)).Ticks)
$authors = "Alexey Evlampiev"
$company = "Solitons"
$licenseExp = "MPL"

# Function to configure packages
function Config-Packages {
    param (
        [Parameter(Mandatory=$true)]
        [ValidateSet('Alpha', 'PreView', 'Life')]
        [string]$staging
    )

    # Determine version suffix based on staging
    $versionSuffix = ""
    switch ($staging) {
        'Alpha'   { $versionSuffix = "-alpha.$ticks" }
        'PreView' { $versionSuffix = "-beta.$ticks" }
        'Life'    { $versionSuffix = "" }
    }

    # Find all csproj files that start with 'Solitons'
    Get-ChildItem -Path . -Filter "Solitons*.csproj" -Recurse | ForEach-Object {
        [xml]$csproj = Get-Content $_.FullName
        
        # Check if the project is packaged to NuGet
        if ($csproj.Project.PropertyGroup.PackageId) {
            # Set the version prefix and suffix
            $csproj.Project.PropertyGroup.VersionPrefix = $version.ToString()
            $csproj.Project.PropertyGroup.VersionSuffix = $versionSuffix

            # Ensure package license is set to MPL
            $csproj.Project.PropertyGroup.PackageLicenseExpression = $licenseExp

            # Set authors and company
            $csproj.Project.PropertyGroup.Authors = $authors
            $csproj.Project.PropertyGroup.Company = $company

            # Save the changes back to the csproj file
            $csproj.Save($_.FullName)
        }
    }
}

# Example usage:
Config-Packages -staging 'Alpha'
