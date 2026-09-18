@echo off
chcp 65001 >nul
title Audio Mirror - Entwicklungsfassung

rem Baut den aktuellen Stand und startet ihn. Die Vorlage liegt im Projekt unter
rem tools\, auf dem Desktop liegt eine Kopie davon.

set "REPO=C:\Users\yusuf\Documents\audio-mirror"
set "EXE=%REPO%\bin\Debug\net8.0-windows\win-x64\AudioMirror.exe"

cd /d "%REPO%" 2>nul
if errorlevel 1 (
    echo Projektordner nicht gefunden:
    echo   %REPO%
    echo.
    pause
    exit /b 1
)

rem Nur die Entwicklungsfassung beenden, nicht die installierte: sonst waere die
rem Spiegelung, die gerade im Infobereich laeuft, mit einem Schlag weg. Der Vergleich
rem geht ueber den vollen Pfad, nicht ueber den Prozessnamen.
powershell -NoProfile -Command ^
    "Get-Process AudioMirror -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq '%EXE%' } | Stop-Process -Force" >nul 2>&1

echo Baue ...
echo.
dotnet build AudioMirror.csproj -v q --nologo
if errorlevel 1 (
    echo.
    echo ==========================================================
    echo  Der Bau ist fehlgeschlagen. Die Meldungen stehen oben.
    echo ==========================================================
    echo.
    pause
    exit /b 1
)

if not exist "%EXE%" (
    echo.
    echo Der Bau lief durch, aber die Datei fehlt:
    echo   %EXE%
    echo.
    pause
    exit /b 1
)

echo Starte ...
start "" "%EXE%"
exit /b 0
