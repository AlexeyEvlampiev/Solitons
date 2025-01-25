# Get the directory of the script
$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Path

# Change to the script's directory
Set-Location -Path $scriptDirectory

# Clear the screen
Clear-Host

# Publish for Windows (win-x64)
dotnet publish -c Release -r win-x64   --self-contained true /p:PublishSingleFile=true

# Publish for Linux (linux-x64)
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true

# Publish for macOS (osx-x64)
dotnet publish -c Release -r osx-x64 --self-contained true /p:PublishSingleFile=true