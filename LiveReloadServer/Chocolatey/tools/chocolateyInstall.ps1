$packageName = 'LiveReloadWebServer'
$url = 'https://github.com/RickStrahl/Westwind.AspnetCore.LiveReload/tree/0.1.10/LiveReloadServer/LiveReloadWebServer.exe'

# Download from an HTTPS location
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
Get-ChocolateyWebFile -PackageName $packageName -FileFullPath "$toolsDir\LiveReloadWebServer.exe" -Url $url