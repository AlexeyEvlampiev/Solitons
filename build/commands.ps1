$version = [Version]::Parse("1.1.0")
$authors = "Alexey Evlampiev"
$company = "Solitons"
$licenseExp = "MPL"
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
