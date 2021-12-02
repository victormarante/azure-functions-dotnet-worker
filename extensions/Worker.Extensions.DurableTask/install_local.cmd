@echo off
set pkgName=Microsoft.Azure.Functions.Worker.Extensions.DurableTask
set pkgVersion=1.0.0-local
rmdir /S /Q %USERPROFILE%\.nuget\packages\%pkgName%\%pkgVersion%
rmdir /S /Q C:\LocalNuGet\%pkgName%\%pkgVersion%
nuget add bin\Debug\%pkgName%.%pkgVersion%.nupkg -Source C:\LocalNuGet