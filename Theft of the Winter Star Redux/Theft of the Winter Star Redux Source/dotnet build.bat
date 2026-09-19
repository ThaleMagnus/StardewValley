@echo off
setlocal
set "GamePath=C:\Steam\steamapps\common\Stardew Valley"
cd /d "%~dp0"

dotnet build "-p:GamePath=%GamePath%"
set "exitCode=%errorlevel%"

for %%D in (bin obj release) do if exist "%%D" rmdir /s /q "%%D"

endlocal & exit /b %exitCode%
