@echo off
title HoloBridge - Public Web Tunnel Launcher (IEEE VR 2027)
color 0B
cls

echo ===============================================================================
echo     HOLOBRIDGE MULTI-DEVICE SPATIAL HUB - 1-CLICK PUBLIC WEB HOSTING
echo ===============================================================================
echo.
echo  [1/3] Checking HoloBridge Unity Web Core on Port 8080...
echo.

powershell -Command "$p = Test-NetConnection -ComputerName 127.0.0.1 -Port 8080 -WarningAction SilentlyContinue; if ($p.TcpTestSucceeded) { Write-Host '[OK] Unity Web Core is RUNNING on port 8080!' -ForegroundColor Green } else { Write-Host '[NOTE] Unity is not yet in Play mode or port 8080 is loading. Make sure Unity is running!' -ForegroundColor Yellow }"

echo.
echo  [2/3] Local Network IP (For devices on the SAME Wi-Fi network):
powershell -Command "$ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254.*' } | Select-Object -First 1).IPAddress; Write-Host ('       http://' + $ip + ':8080/') -ForegroundColor Cyan"

echo.
echo  [3/3] Launching Free Secure Public Cloudflare Tunnel...
echo       (Works worldwide on Mobile 4G/5G, Remote Devices, without Port Forwarding)
echo.

set CLOUDFLARED_EXE=%~dp0cloudflared.exe

if not exist "%CLOUDFLARED_EXE%" (
    echo [Downloading Cloudflare Tunnel CLI (~30MB) - One-time setup...]
    powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; (New-Object Net.WebClient).DownloadFile('https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe', '%CLOUDFLARED_EXE%')"
    if not exist "%CLOUDFLARED_EXE%" (
        echo [Cloudflare download failed. Trying npx localtunnel fallback...]
        npx --yes localtunnel --port 8080
        pause
        exit /b
    )
)

echo.
echo -------------------------------------------------------------------------------
echo  YOUR PUBLIC SECURE HTTPS LINK WILL APPEAR BELOW (Look for .trycloudflare.com):
echo -------------------------------------------------------------------------------
echo.
"%CLOUDFLARED_EXE%" tunnel --url http://127.0.0.1:8080

pause
