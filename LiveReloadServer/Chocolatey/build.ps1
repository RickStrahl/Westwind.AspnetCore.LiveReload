# Script builds a Chocolatey Package and tests it locally
# 
#  Assumes: Uses latest release out of Pre-release folder
#           Release has been checked in to GitHub Repo
#   Builds: ChocolateyInstall.ps1 file with download URL and sha256 embedded

cd "$PSScriptRoot" 

# Create .nupkg from .nuspec
choco pack

choco uninstall "LiveReloadWebServer" -f

choco install "LiveReloadWebServer" -fd  -y -s ".\"

#choco push