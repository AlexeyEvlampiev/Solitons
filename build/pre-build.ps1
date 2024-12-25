# Get absolute path of the script's directory
$scriptPath = $PSScriptRoot
if (-not $scriptPath) {
    $scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
}

# Store original location and handle errors
$originalLocation = Get-Location
try {
    # Navigate to script directory then up one level
    Push-Location -Path $scriptPath -ErrorAction Stop
    Push-Location -Path ".." -ErrorAction Stop
}
catch {
    # Restore original location if navigation fails
    Set-Location -Path $originalLocation
    Write-Error "Failed to navigate directories: $_"
    throw
}

#. ".\build\commands.ps1"
#Config-Packages -staging 'Alpha' -searchRoot "." 


docker build -t solitons-build --build-arg SOLITONS_TEST_POSTGRES_SERVER_CONNECTION_STRING=$env:SOLITONS_TEST_POSTGRES_SERVER_CONNECTION_STRING .