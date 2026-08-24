@echo off
chcp 65001 >nul
title WinDeskBox 桌面收納箱 啟動器
cd /d "%~dp0"

:: 檢查本機是否已安裝 .NET 8 Windows Desktop Runtime
dotnet --list-runtimes 2>nul | findstr /i "Microsoft.WindowsDesktop.App 8." >nul
if %errorlevel% equ 0 (
    start "" "WinDeskBox.exe"
    exit /b 0
)

echo ========================================================
echo   📦 WinDeskBox 桌面收納箱 - 首次執行環境檢查
echo ========================================================
echo.
echo 系統偵測到本機尚未安裝 .NET 8 Desktop Runtime (執行環境)。
echo 為確保程式在企業防毒與各電腦環境下 100%% 順暢執行，
echo 正在自動為您自微軟官方下載安裝程式...
echo.

set "INSTALLER_PATH=%TEMP%\windowsdesktop-runtime-8.0-win-x64.exe"

powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; $wc = New-Object System.Net.WebClient; $url = 'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe'; Write-Host '正在自微軟官方 (aka.ms) 下載 .NET 8 執行環境...'; $wc.DownloadFile($url, '%INSTALLER_PATH%')"

if not exist "%INSTALLER_PATH%" (
    echo.
    echo 下載失敗，請手動前往微軟官網下載安裝 .NET 8 Desktop Runtime:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo.
echo 下載完成！正在啟動微軟官方安裝程式，請在彈出的視窗中點擊「安裝」...
start /wait "" "%INSTALLER_PATH%" /passive /norestart

echo.
echo 安裝完成！正在為您啟動 WinDeskBox 桌面收納箱...
start "" "WinDeskBox.exe"
exit /b 0