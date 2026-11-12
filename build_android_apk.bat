@echo off
setlocal enabledelayedexpansion
title Holobridge VR - Meta Quest Android APK Builder
color 0b

echo ===============================================================================
echo                HOLOBRIDGE VR - META QUEST 3 ANDROID APK BUILDER
echo               Target: ARM64 ^| Vulkan ^| OpenXR ^| Meta Quest OS 32+
echo ===============================================================================
echo.

set "PROJECT_DIR=%~dp0"
if "%PROJECT_DIR:~-1%"=="\" set "PROJECT_DIR=%PROJECT_DIR:~0,-1%"

:: 1. Search for installed Unity 6000.x Editor
set "UNITY_PATH=C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe"
if not exist "%UNITY_PATH%" (
    set "UNITY_PATH=C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe"
)
if not exist "%UNITY_PATH%" (
    for /d %%E in ("C:\Program Files\Unity\Hub\Editor\6000.*") do (
        if exist "%%E\Editor\Unity.exe" set "UNITY_PATH=%%E\Editor\Unity.exe"
    )
)

if not exist "%UNITY_PATH%" (
    color 0c
    echo [ERROR] Unity 6000 Editor executable not found in default paths!
    echo Please install Unity 6000 LTS or verify your Unity Hub installation.
    pause
    exit /b 1
)

echo [1/3] Located Unity Editor at:
echo       "%UNITY_PATH%"
echo.
echo [2/3] Building Meta Quest 3 Android APK...
echo       Project: "%PROJECT_DIR%"
echo       Log file: "%PROJECT_DIR%\Build\Android_Build.log"
echo       Output target: "%PROJECT_DIR%\Build\HOLOBRIDGE_VR_Quest3.apk"
echo.
echo Please wait, compiling IL2CPP ARM64 binary and bundling assets...
echo.

if not exist "%PROJECT_DIR%\Build" mkdir "%PROJECT_DIR%\Build"

"%UNITY_PATH%" -quit -batchmode -projectPath "%PROJECT_DIR%" -executeMethod OmniOS.EditorTools.OmniBuildTool.BuildQuestApk -logFile "%PROJECT_DIR%\Build\Android_Build.log"
set "BUILD_EXIT_CODE=%ERRORLEVEL%"

if %BUILD_EXIT_CODE% EQU 0 (
    echo.
    echo ===============================================================================
    echo [SUCCESS] Meta Quest 3 APK Build Completed Successfully!
    echo Output APK: "%PROJECT_DIR%\Build\HOLOBRIDGE_VR_Quest3.apk"
    echo ===============================================================================
    echo.
    if exist "%PROJECT_DIR%\Build\HOLOBRIDGE_VR_Quest3.apk" (
        explorer.exe /select,"%PROJECT_DIR%\Build\HOLOBRIDGE_VR_Quest3.apk"
    )
) else (
    color 0c
    echo.
    echo ===============================================================================
    echo [BUILD FINISHED] Process exited with code %BUILD_EXIT_CODE%.
    echo Check log for details: "%PROJECT_DIR%\Build\Android_Build.log"
    echo ===============================================================================
    echo.
)

pause
