@echo off
chcp 65001 >nul
title Audio Mirror - Screenshots fuer die Website

rem Baut die Entwicklungsfassung und zeichnet damit die Bilder fuer die Website.
rem Das Fenster erscheint dabei nicht auf dem Bildschirm (--render, siehe Program.cs).
rem Ergebnis: website\screenshots\*.png - danach committen und pushen.

set "REPO=%~dp0.."
set "EXE=%REPO%\bin\Debug\net8.0-windows\win-x64\AudioMirror.exe"
set "OUT=%~dp0screenshots"

cd /d "%REPO%"

echo Baue ...
dotnet build AudioMirror.csproj -v q --nologo
if errorlevel 1 (
    echo.
    echo Der Bau ist fehlgeschlagen. Die Meldungen stehen oben.
    pause
    exit /b 1
)

if not exist "%OUT%" mkdir "%OUT%"

echo Zeichne ...
"%EXE%" --render "%OUT%\devices-dark.png"   dark  en
"%EXE%" --render "%OUT%\devices-light.png"  light en
"%EXE%" --render "%OUT%\settings-dark.png"  dark  en settings
"%EXE%" --render "%OUT%\settings-light.png" light en settings

echo.
echo Fertig. Die Bilder liegen in:
echo   %OUT%
echo.
pause
