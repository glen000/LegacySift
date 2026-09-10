$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

msbuild LegacySift.sln /m /p:Configuration=Release /p:Platform="Any CPU"
& .\tests\LegacySift.Tests\bin\Release\LegacySift.Tests.exe
if ($LASTEXITCODE -ne 0) { throw "LegacySift smoke tests failed." }

Remove-Item -Recurse -Force .\dist -ErrorAction SilentlyContinue
New-Item -ItemType Directory .\dist | Out-Null
Copy-Item .\src\LegacySift\bin\Release\LegacySift.exe .\dist\LegacySift.exe
Copy-Item .\README.md .\dist\README.md
Copy-Item .\LICENSE .\dist\LICENSE.txt
Compress-Archive -Path .\dist\LegacySift.exe,.\dist\README.md,.\dist\LICENSE.txt -DestinationPath .\LegacySift-windows.zip -Force
$hash = (Get-FileHash .\LegacySift-windows.zip -Algorithm SHA256).Hash.ToLower()
"$hash  LegacySift-windows.zip" | Set-Content .\LegacySift-windows.zip.sha256 -Encoding ascii
Write-Host "Built LegacySift-windows.zip"
Write-Host "SHA-256: $hash"
