$packageName = 'LiveReloadWebServer'
$url = "https://github.com/RickStrahl/Westwind.AspnetCore.LiveReload/raw/0.1.10/LiveReloadServer/LiveReloadWebServer.zip"
$drop = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$sha = "08A71395F79AE8386FDFCC24BA83EEAF65A50670A50D41999C7A9AD7D0EC1A9C"
Install-ChocolateyZipPackage -PackageName $packageName -Url $url -UnzipLocation $drop -checksum "$sha" -checksumtype "sha256" 
