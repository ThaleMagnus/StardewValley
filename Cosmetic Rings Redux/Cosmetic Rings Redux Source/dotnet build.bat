@echo off
setlocal
pushd "%~dp0"

if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
if exist "release" rmdir /s /q "release"

dotnet build "CosmeticRingsRedux.csproj" -c Release
set "BUILD_EXIT=%ERRORLEVEL%"

if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
if exist "release" rmdir /s /q "release"

popd
endlocal & exit /b %BUILD_EXIT%
