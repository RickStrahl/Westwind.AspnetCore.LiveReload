# Script builds a Chocolatey Package and tests it locally
# 
# This copies the binaries directly into the Tools folder - no ChocolateyInstall.ps1 is used
#
#  Assumes: Uses latest release out of Pre-release folder
#           Release has been checked in to GitHub Repo
#   Builds: ChocolateyInstall.ps1 file with download URL and sha256 embedded


# *** IMPORTANT! *** 
# Make sure you ADD A TAG  with the version number when the zip file is pushed! (ie. 0.2.5)
# or else the zip file will not be found on GitHub

cd "$PSScriptRoot" 

Remove-Item *.nupkg

# Create .nupkg from .nuspec
choco pack LiveReloadWebServer.nuspec

choco uninstall "LiveReloadWebServer" -f

choco install "LiveReloadWebServer" -fd  -y -s ".\"