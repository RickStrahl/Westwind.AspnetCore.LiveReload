$filename = 'LiveReloadServer.0.2.4.nupkg'


if (test-path ./nupkg) {
    remove-item ./nupkg -Force -Recurse
}   


dotnet build -c Release

$filename = gci "./nupkg" | sort LastWriteTime | select -last 1 | select -ExpandProperty "Name"
Write-host $filename

$len = $filename.length
Write-host $len

if ($len -gt 0) {
    Write-Host "signing..."
    nuget sign  ".\nupkg\$filename"   -CertificateSubject "West Wind Technologies" -timestamper " http://timestamp.comodoca.com"
    nuget push  ".\nupkg\$filename" -source nuget.org
}