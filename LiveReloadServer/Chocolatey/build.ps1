# Script builds a Chocolatey Package and tests it locally
# 
# This copies the binaries directly into the Tools folder - no ChocolateyInstall.ps1 is used
#
#  Assumes: Uses latest release out of Pre-release folder
#           Release has been checked in to GitHub Repo
#   Builds: ChocolateyInstall.ps1 file with download URL and sha256 embedded

cd "$PSScriptRoot" 

Remove-Item *.nupkg

# Create .nupkg from .nuspec
choco pack

choco uninstall "LiveReloadWebServer" -f

choco install "LiveReloadWebServer" -fd  -y -s ".\"


#choco push
remove-item .\tools\LiveReloadWebServer.exe
remove-item .\tools\LiveReloadServer.json