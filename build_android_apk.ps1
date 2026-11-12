# Holobridge VR - Meta Quest 3 Android APK Builder
param(
    [string]$UnityPath = "",
    [switch]$OpenFolderAfterBuild = $true
)

$Host.UI.RawUI.WindowTitle = "Holobridge VR - Android APK Builder"
Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host "           HOLOBRIDGE VR - META QUEST 3 ANDROID APK BUILDER                   " -ForegroundColor Yellow
Write-Host "          Architecture: ARM64 | Graphics: Vulkan | Runtime: OpenXR             " -ForegroundColor White
Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host ""

$ProjectDir = (Get-Item -Path $PSScriptRoot).FullName

# Detect Unity Editor
if ([string]::IsNullOrWhiteSpace($UnityPath) -or -not (Test-Path $UnityPath)) {
    $Candidates = @(
        "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe",
        "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe"
    )
    foreach ($cand in $Candidates) {
        if (Test-Path $cand) {
            $UnityPath = $cand
            break
        }
    }
    if (-not $UnityPath) {
        $WildcardMatch = Get-ChildItem "C:\Program Files\Unity\Hub\Editor\6000.*\Editor\Unity.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($WildcardMatch) {
            $UnityPath = $WildcardMatch.FullName
        }
    }
}

if (-not (Test-Path $UnityPath)) {
    Write-Host "[ERROR] Unity 6000 Editor not found! Please provide -UnityPath parameter." -ForegroundColor Red
    exit 1
}

$BuildDir = Join-Path $ProjectDir "Build"
if (-not (Test-Path $BuildDir)) {
    New-Item -ItemType Directory -Path $BuildDir | Out-Null
}

$LogFile = Join-Path $BuildDir "Android_Build.log"
$ApkPath = Join-Path $BuildDir "HOLOBRIDGE_VR_Quest3.apk"

Write-Host "[1/3] Using Unity Editor: $UnityPath" -ForegroundColor Green
Write-Host "[2/3] Project Root: $ProjectDir" -ForegroundColor Green
Write-Host "[3/3] Compiling Meta Quest 3 Android APK in Batchmode..." -ForegroundColor Yellow
Write-Host "      Log output: $LogFile" -ForegroundColor Gray
Write-Host "      Target APK: $ApkPath" -ForegroundColor Gray
Write-Host ""
Write-Host "Building... (This will take a few minutes for IL2CPP compilation)" -ForegroundColor Cyan

$process = Start-Process -FilePath $UnityPath -ArgumentList "-quit", "-batchmode", "-projectPath `"$ProjectDir`"", "-executeMethod OmniOS.EditorTools.OmniBuildTool.BuildQuestApk", "-logFile `"$LogFile`"" -PassThru -Wait

if ($process.ExitCode -eq 0 -and (Test-Path $ApkPath)) {
    Write-Host ""
    Write-Host "===============================================================================" -ForegroundColor Green
    Write-Host "[SUCCESS] Meta Quest 3 APK Build Succeeded!" -ForegroundColor Green
    $apkItem = Get-Item $ApkPath
    $sizeMb = [Math]::Round($apkItem.Length / 1MB, 2)
    Write-Host "Artifact: $ApkPath ($sizeMb MB)" -ForegroundColor White
    Write-Host "===============================================================================" -ForegroundColor Green
    if ($OpenFolderAfterBuild) {
        explorer.exe /select,$ApkPath
    }
} else {
    Write-Host ""
    Write-Host "[FINISHED] Process exited with code $($process.ExitCode). Check log: $LogFile" -ForegroundColor Yellow
}
