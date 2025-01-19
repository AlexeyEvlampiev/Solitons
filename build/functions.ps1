$version = [Version]::Parse("1.1.0")
$authors = "Alexey Evlampiev"
$company = "Solitons"
$licenseExp = "MPL-2.0"  
$projectUrl = "https://github.com/AlexeyEvlampiev/Solitons"
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

            $projectUrlNode = Ensure-XmlNode -XmlDocument $csproj -ParentXPath '/Project/PropertyGroup' -NodeName 'PackageProjectUrl'
            $projectUrlNode.InnerText = $projectUrl

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
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]]$PackageIds,
        
        [Parameter()]
        [int]$MaxVersionsToUnlist = 10,
        
        [Parameter()]
        [string]$NuGetSource = "https://api.nuget.org/v3/index.json"
    )
    
    Begin {
        if (-not $env:NUGET_API_KEY) {
            throw "NUGET_API_KEY environment variable is not set"
        }
    }
    
    Process {
        foreach ($packageId in $PackageIds) {
            try {
                Write-Verbose "Processing package: $packageId"
                
                # Use NuGet.org search API
                $searchUrl = "https://azuresearch-na.nuget.org/query?q=packageid:$packageId&prerelease=true"
                $searchResult = Invoke-RestMethod -Uri $searchUrl -ErrorAction Stop
                
                if (-not $searchResult.data) {
                    Write-Warning "No packages found for $packageId"
                    continue
                }
                
                $package = $searchResult.data[0]
                $versions = $package.versions | 
                    Where-Object { $_.version -match '-' } | # Only prerelease versions
                    Select-Object -ExpandProperty version
                
                Write-Verbose "Found versions: $($versions -join ', ')"
                
                $prereleaseVersions = $versions |
                    Sort-Object -Descending |
                    Select-Object -First $MaxVersionsToUnlist
                
                if (-not $prereleaseVersions) {
                    Write-Warning "No prerelease versions found for $packageId"
                    continue
                }
                
                Write-Host "Found prerelease versions for $packageId : $($prereleaseVersions -join ', ')"
                
                foreach ($version in $prereleaseVersions) {
                    Write-Host "Unlisting $packageId version $version..."
                    
                    try {
                        $result = dotnet nuget delete $packageId $version `
                            -s $NuGetSource `
                            -k $env:NUGET_API_KEY `
                            --non-interactive `
                            --force-english-output `
                            --verbosity detailed 2>&1
                            
                        if ($LASTEXITCODE -eq 0) {
                            Write-Host "Successfully unlisted $packageId version $version" -ForegroundColor Green
                        } else {
                            Write-Warning "Failed to unlist $packageId version $version. Error: $result"
                            Write-Verbose "Full result: $result"
                            if ($result -match "unauthorized") {
                                throw "Unauthorized access. Please check your NUGET_API_KEY permissions."
                            }
                        }
                    }
                    catch {
                        Write-Error "Error unlisting version $version : $_"
                        continue
                    }
                    
                    Start-Sleep -Milliseconds (Get-Random -Minimum 500 -Maximum 1500)
                }
            }
            catch {
                Write-Warning "Error processing package $packageId : $_"
                Write-Verbose "Full error details: $($_.Exception.Message)"
                continue
            }
        }
    }
}