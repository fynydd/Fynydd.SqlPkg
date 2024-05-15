if (Test-Path ".\Fynydd.SqlPkg\nupkg") { Remove-Item ".\Fynydd.SqlPkg\nupkg" -Recurse -Force }
. ./clean.ps1
Set-Location Fynydd.SqlPkg
dotnet pack
Set-Location ..
