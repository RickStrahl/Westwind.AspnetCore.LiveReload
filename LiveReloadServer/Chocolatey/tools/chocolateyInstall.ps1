$packageName = 'LiveReloadWebServer'
$url = 'https://github.com/RickStrahl/Westwind.AspnetCore.LiveReload/tree/master/LiveReloadServer/raw/v0.1.8/LiveReloadWebServer.exe'
$drop = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

Install-ChocolateyZipPackage $packageName $url $drop