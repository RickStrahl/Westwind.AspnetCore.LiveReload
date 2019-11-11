$releaseFile = "$PSScriptRoot\..\LiveReloadWebServer.exe"
$releaseZip = "$PSScriptRoot\..\LiveReloadWebServer.zip"

$rawVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($releaseFile).FileVersion

$version = $rawVersion.Trim().Replace(".0","") 
"Writing Version File for: $version ($rawVersion)"

$downloadUrl = "https://github.com/RickStrahl/Westwind.AspnetCore.LiveReload/raw/$version/LiveReloadServer/LiveReloadWebServer.zip"

# Create Zip file
7z a -tzip $releaseZip $releaseFile "..\LiveReloadServer.json" 

# Write out Verification.txt
$sha = get-filehash -path $releaseZip -Algorithm SHA256  | select -ExpandProperty "Hash"
write-host $sha

$filetext = @"
`$packageName = 'LiveReloadWebServer'
`$url = "$downloadUrl"
`$drop = "`$(Split-Path -Parent `$MyInvocation.MyCommand.Definition)"
`$sha = "$sha"
Install-ChocolateyZipPackage -PackageName `$packageName -Url `$url -UnzipLocation `$drop -checksum "`$sha" -checksumtype "sha256" 
"@
out-file -filepath .\tools\chocolateyInstall.ps1 -inputobject $filetext


# Write out new NuSpec file with Version
$chocoNuspec = ".\LiveReloadWebServer.template.nuspec"
$content = Get-Content -Path $chocoNuspec
$content = $content.Replace("{{version}}",$version)
# Write-Host $content
out-file -filepath $chocoNuSpec.Replace(".template","")  -inputobject $content -Encoding utf8

# Commit  current changes and add a tag
git add --all

git tag --delete $version
git push --delete origin $version 
git tag $version

git commit -m "$version" 
git push origin master --tags
