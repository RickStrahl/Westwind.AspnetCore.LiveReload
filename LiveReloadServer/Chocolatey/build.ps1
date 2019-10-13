# Script builds a Chocolatey Package and tests it locally
# 
#  Assumes: Uses latest release out of Pre-release folder
#           Release has been checked in to GitHub Repo
#   Builds: ChocolateyInstall.ps1 file with download URL and sha256 embedded

cd "$PSScriptRoot" 

.\signtool.exe sign /v /n "West Wind Technologies" /sm  /tr "http://timestamp.digicert.com" /td SHA256 /fd SHA256 "..\LiveReloadWebServer.exe"

7z a -tzip ".\tools\LiveReloadWebServer.zip" "..\LiveReloadWebServer.exe" "..\LiveReloadServer.json" 

$sha = get-filehash -path ".\tools\LiveReloadWebServer.zip" -Algorithm SHA256  | select -ExpandProperty "Hash"
write-host $sha

$filetext = @"
`VERIFICATION
`LiveReloadWebServer.zip
`SHA256 Checksum Value: $sha
"@
out-file -filepath .\tools\Verification.txt -inputobject $filetext

Remove-Item *.nupkg

# Create .nupkg from .nuspec
choco pack

choco uninstall "LiveReloadWebServer" -f

choco install "LiveReloadWebServer" -fd  -y -s ".\"


#choco push
remove-item .\tools\LiveReloadWebServer.zip