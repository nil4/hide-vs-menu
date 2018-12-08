@echo off

nuget.exe restore
msbuild.exe HideMenu.sln /p:Configuration=Release
