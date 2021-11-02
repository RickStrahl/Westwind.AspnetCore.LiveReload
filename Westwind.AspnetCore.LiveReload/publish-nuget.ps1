
foreach ($path in "./nupkg", "./bin", "./obj") {
    if (test-path $path) {
        remove-item $path -Force -Recurse
    }
}

dotnet pack --configuration Release --output ./nupkg -p:ContinuousIntegrationBuild=true

# $filename = 'LiveReloadServer.0.2.4.nupkg'
$filename = gci "./nupkg/*.nupkg" | sort LastWriteTime | select -last 1 | select -ExpandProperty "Name"


$len = $filename.length

if ($len -gt 0) {
    Write-Host "Signing... $filename"
    nuget sign  ".\nupkg\$filename"   -CertificateSubject "West Wind Technologies" -timestamper " http://timestamp.comodoca.com"

    $snufilename = $filename.Replace(".nupkg",".snupkg");

    Write-Host
    Write-Host "Signing... $snufilename"    
    nuget sign  ".\nupkg\$snufilename"   -CertificateSubject "West Wind Technologies" -timestamper " http://timestamp.comodoca.com"

    nuget push  ".\nupkg\$filename" -source nuget.org
}