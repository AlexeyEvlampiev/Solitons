$version = [Version]::Parse("1.1.1")
$authors = "Alexey Evlampiev"
$company = "Solitons"
$licenseExp = "MPL-2.0"  
$projectUrl = "https://github.com/AlexeyEvlampiev/Solitons"
$ticks = ((New-TimeSpan -Start (Get-Date "1/1/2025") -End (Get-Date)).Ticks)

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

        # Get NuGet service index
        $serviceIndex = Invoke-RestMethod -Uri $NuGetSource -ErrorAction Stop
        $packageBaseAddress = ($serviceIndex.resources | 
            Where-Object { $_.'@type' -eq 'PackageBaseAddress/3.0.0' }).'@id'
    }
    
    Process {
        foreach ($packageId in $PackageIds) {
            Write-Host "Processing package: $packageId"
            
            try {
                # Query package versions
                $versionsUrl = "$($packageBaseAddress)$($packageId.ToLower())/index.json"
                $response = Invoke-RestMethod -Uri $versionsUrl -ErrorAction Stop
                $versions = $response.versions
                
                # Filter for prerelease versions and take top N
                $prereleaseVersions = $versions | 
                    Where-Object { $_ -match '-alpha|-beta|-preview|-rc' } | 
                    Sort-Object -Descending |
                    Select-Object -First $MaxVersionsToUnlist
                
                if (-not $prereleaseVersions) {
                    Write-Host "No prerelease versions found for $packageId"
                    continue
                }
                
                Write-Host "Found $($prereleaseVersions.Count) prerelease versions for $packageId"
                
                foreach ($version in $prereleaseVersions) {
                    $success = $false
                    $attempts = 0
                    $maxAttempts = 3
                    
                    while (-not $success -and $attempts -lt $maxAttempts) {
                        $attempts++
                        Write-Host "Attempt $attempts of $maxAttempts to unlist $packageId $version..."
                        
                        try {
                            $result = dotnet nuget delete $packageId $version `
                                -s $NuGetSource `
                                -k $env:NUGET_API_KEY `
                                --non-interactive 2>&1
                            
                            if ($LASTEXITCODE -eq 0) {
                                Write-Host "Successfully unlisted $packageId $version" -ForegroundColor Green
                                $success = $true
                            }
                            else {
                                $errorMessage = $result -join "`n"
                                
                                if ($errorMessage -match "403.*Quota Exceeded") {
                                    Write-Host "Rate limit hit. Waiting 3 minutes..." -ForegroundColor Yellow
                                    Start-Sleep -Seconds 180
                                    continue
                                }
                                elseif ($errorMessage -match "already unlisted") {
                                    Write-Host "Package already unlisted" -ForegroundColor Blue
                                    $success = $true
                                }
                                elseif ($errorMessage -match "(unauthorized|403)") {
                                    throw "Unauthorized access. Check NUGET_API_KEY permissions."
                                }
                                else {
                                    Write-Host "Failed to unlist. Error: $errorMessage" -ForegroundColor Red
                                    Start-Sleep -Seconds 5
                                }
                            }
                        }
                        catch {
                            Write-Warning "Error unlisting $packageId $version : $_"
                            Start-Sleep -Seconds 5
                        }
                    }
                    
                    # Add delay between versions
                    Start-Sleep -Seconds 2
                }
            }
            catch {
                if ($_.Exception.Response.StatusCode -eq 404) {
                    Write-Warning "Package $packageId not found on NuGet.org"
                }
                else {
                    Write-Error "Error processing package $packageId : $_"
                }
            }
        }
    }
}