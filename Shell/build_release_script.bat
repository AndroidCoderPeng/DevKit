@echo off
setlocal

echo.
echo Set variables environment.
echo.
set PROJECT_DIR=D:\Code\RiderProjects\DevKit\DevKit
set INNO_SETUP=E:\Program Files (x86)\Inno Setup 6\ISCC.exe
set ISS_SCRIPT=%PROJECT_DIR%\Shell\DevKit.iss

echo.
echo Packaging with Inno Setup...
echo %INNO_SETUP%
echo %ISS_SCRIPT%
echo.
if not exist "%ISS_SCRIPT%" (
    echo Error: Inno Setup script not found at %ISS_SCRIPT%
    pause
)
"%INNO_SETUP%" "%ISS_SCRIPT%"

if errorlevel 1 (
    echo Error: Inno Setup packaging failed!
    pause
)

echo.
echo Packaging completed successfully! Installer generated on the desktop.
echo.

endlocal