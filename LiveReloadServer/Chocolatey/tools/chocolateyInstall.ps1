$packageName = 'LiveReloadWebServer'
$url = "https://github.com/RickStrahl/Westwind.AspnetCore.LiveReload/raw/0.2.2/LiveReloadServer/LiveReloadWebServer.zip"
$drop = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$sha = "D5756444911B2C6CE93BBC9B24BB2E71E57416EC4978D532A0D14B3E208ED106"
Install-ChocolateyZipPackage -PackageName $packageName -Url $url -UnzipLocation $drop -checksum "$sha" -checksumtype "sha256" 
