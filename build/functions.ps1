$version = [Version]::Parse("1.1.0")
$authors = "Alexey Evlampiev"
$company = "Solitons"
$licenseExp = "MPL-2.0"  
$ticks = ((New-TimeSpan -Start (Get-Date "1/1/2024") -End (Get-Date)).Ticks)

function Ensure-XmlNode {
    param (
        [Parameter(Mandatory=$true)][xml]$XmlDocument,
        [Parameter(Mandatory=$true)][string]$ParentXPath,
        [Parameter(Mandatory=$true)][string]$NodeName,
        [string]$InitialValue = $null  # Optional initial value for the node
    )

    $node = $XmlDocument.SelectSingleNode("$ParentXPath/$NodeName")
    if ($node -eq $null) {
        $parentNode = $XmlDocument.SelectSingleNode($ParentXPath)
        if ($parentNode -eq $null) {
            Write-Error "Parent node '$ParentXPath' not found."
            return $null
        }
        $newNode = $XmlDocument.CreateElement($NodeName, $XmlDocument.DocumentElement.NamespaceURI)
        $node = $parentNode.AppendChild($newNode)
        
        if ($InitialValue -ne $null) {
            $node.InnerText = $InitialValue
        }
    }

    return $node
}






function Config-Packages {
    param (
        [Parameter(Mandatory=$true)]
        [ValidateSet('Alpha', 'PreView', 'Live')]
        [string]$staging,
        [string]$searchRoot = "."
    )

   
    $versionSuffix = switch ($staging) {
        'Alpha'   { "alpha.$ticks" }
        'PreView' { "beta.$ticks" }
        'Live'    { "" }
    }

    # Find all csproj files starting with 'Solitons'
    Get-ChildItem -Path . -Filter "Solitons*.csproj" -Recurse | ForEach-Object {
        [xml]$csproj = Get-Content $_.FullName
        
        # Process only if PackageId is defined
        if ($csproj.Project.PropertyGroup.PackageId) {
            "Processing: $($_.Name)"

            # Ensure required nodes exist and set their values
            $versionPrefixNode = Ensure-XmlNode -XmlDocument $csproj -ParentXPath '/Project/PropertyGroup' -NodeName 'VersionPrefix'
            $versionPrefixNode.InnerText = $version.ToString()


            $versionSuffixNode = Ensure-XmlNode -XmlDocument $csproj -ParentXPath '/Project/PropertyGroup' -NodeName 'VersionSuffix'
            $versionSuffixNode.InnerText = $versionSuffix

            $licenseNode = Ensure-XmlNode -XmlDocument $csproj -ParentXPath '/Project/PropertyGroup' -NodeName 'PackageLicenseExpression'
            $licenseNode.InnerText = $licenseExp

            $authorsNode = Ensure-XmlNode -XmlDocument $csproj -ParentXPath '/Project/PropertyGroup' -NodeName 'Authors'
            $authorsNode.InnerText = $authors

            $companyNode = Ensure-XmlNode -XmlDocument $csproj -ParentXPath '/Project/PropertyGroup' -NodeName 'Company'
            $companyNode.InnerText = $company

            $licenseAcceptanceNode = Ensure-XmlNode -XmlDocument $csproj -ParentXPath '/Project/PropertyGroup' -NodeName 'PackageRequireLicenseAcceptance' -InitialValue "True"
            $licenseAcceptanceNode.InnerText = "True"

  
            $assemblyVersionNode = Ensure-XmlNode -XmlDocument $csproj -ParentXPath '/Project/PropertyGroup' -NodeName 'AssemblyVersion'
            $assemblyVersionNode.InnerText = $version.ToString()

            $fileVersionNode = Ensure-XmlNode -XmlDocument $csproj -ParentXPath '/Project/PropertyGroup' -NodeName 'FileVersion'
            $fileVersionNode.InnerText = $version.ToString()



            if ([string]::IsNullOrWhiteSpace($versionSuffix)) {
                # Remove the node if suffix is empty
                if ($versionSuffixNode -ne $null) {
                    $versionSuffixNode.ParentNode.RemoveChild($versionSuffixNode)
                }
            } else {
                $versionSuffixNode.InnerText = $versionSuffix
            }


            # Save the changes back to the csproj file
            $csproj.Save($_.FullName)
            Write-Host "Updated $($_.Name)"
        } else {
            Write-Host "$($_.Name) does not have a defined PackageId."
        }
    }
}

# Example usage:
#Config-Packages -staging 'Alpha' -searchRoot "." 



function Unlist-PreviousPrereleases {
    param(
        [Parameter(Mandatory=$true)]
        [string[]]$PackageIds
    )
    
    # Only unlist for Alpha or Preview environments
    if ($env:STAGING_TYPE -notin @("Alpha", "Preview")) {
        Write-Host "Skipping prerelease unlisting for staging type: $env:STAGING_TYPE"
        return
    }

    Write-Host "Unlisting previous prereleases for staging type: $env:STAGING_TYPE"
    
    foreach ($packageId in $PackageIds) {
        try {
            # Query NuGet API for package versions
            $versionsUrl = "https://api.nuget.org/v3-flatcontainer/$packageId/index.json"
            $versions = (Invoke-RestMethod -Uri $versionsUrl -ErrorAction Stop).versions
            
            # Filter for prerelease versions
            $prereleaseVersions = $versions | Where-Object { $_ -match '-alpha|-preview' }
            
            foreach ($version in $prereleaseVersions) {
                Write-Host "Unlisting $packageId version $version..."
                
                # Unlist the package version
                $result = dotnet nuget delete $packageId $version --source https://api.nuget.org/v3/index.json --api-key $env:NUGET_API_KEY --non-interactive
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "Successfully unlisted $packageId version $version"
                } else {
                    Write-Warning "Failed to unlist $packageId version $version"
                }
            }
        }
        catch {
            Write-Error "Error processing package $packageId : $_"
        }
    }
}