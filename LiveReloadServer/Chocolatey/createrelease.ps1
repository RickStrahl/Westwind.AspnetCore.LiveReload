# Major version
$release = "v0.1.5" 
$releaseFile = "$PSScriptRoot\..\LiveReloadWebServer.exe"


$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($releaseFile).FileVersion
"Raw version: " + $version
$version = $version.Trim().Replace(".0","") 
"Writing Version File for: " + $version

# Sign the exe
.\signtool.exe sign /v /n "West Wind Technologies" /sm  /tr "http://timestamp.digicert.com" /td SHA256 /fd SHA256 "..\LiveReloadWebServer.exe"

# Commit it and add a tag
git commit ..\LiveReloadWebServer.exe
git push --delete origin $version 
git tag --delete $version
git tag $version
git push origin master

# Write out new NuSpec file with Version
$chocoNuspec = ".\LiveReloadWebServer.template.nuspec"
$content = Get-Content -Path $chocoNuspec
$content = $content.Replace("{{version}}",$version)
Write-Host $content
out-file -filepath $chocoNuSpec.Replace(".template","")  -inputobject $content -Encoding utf8

# Write out Verification.txt
$sha = get-filehash -path "..\LiveReloadWebServer.exe" -Algorithm SHA256  | select -ExpandProperty "Hash"
write-host $sha

$filetext = @"
`VERIFICATION
`LiveReloadWebServer.exe
`SHA256: $sha
`URL   : https://github.com/RickStrahl/Westwind.AspnetCore.LiveReload/tree/$version/LiveReloadServer/LiveReloadWebServer.exe
"@
out-file -filepath .\tools\Verification.txt -inputobject $filetext