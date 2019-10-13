$packageName = 'LiveReloadWebServer'
$url = 'https://github.com/RickStrahl/Westwind.AspnetCore.LiveReload/raw/0.1.8/LiveReloadServer/LiveReloadWebServer.zip'
$drop = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
Install-ChocolateyZipPackage $packageName $url $drop