param(
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot 'bin\Release\net9.0-windows\win-x64\publish'
$distDir = Join-Path $repoRoot 'dist'
$stagingDir = Join-Path $repoRoot 'obj\installer'
$setupProjectDir = Join-Path $PSScriptRoot 'Setup'
$setupPayloadDir = Join-Path $setupProjectDir 'Payload'
$setupPath = Join-Path $distDir 'FileShareSetup.exe'

New-Item -ItemType Directory -Force -Path $distDir | Out-Null
New-Item -ItemType Directory -Force -Path $stagingDir | Out-Null
New-Item -ItemType Directory -Force -Path $setupPayloadDir | Out-Null
Remove-Item -Force -ErrorAction SilentlyContinue -Path $setupPath
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue -Path (Join-Path $setupProjectDir 'bin')
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue -Path (Join-Path $setupProjectDir 'obj')

$publishArgs = @(
    'publish',
    (Join-Path $repoRoot 'FileShareVisualStudio.csproj'),
    '-c', 'Release',
    '-r', 'win-x64',
    '--self-contained', 'true',
    '--no-restore',
    '-p:PublishSingleFile=true',
    '-p:EnableCompressionInSingleFile=true',
    '-p:PublishTrimmed=false'
)

if (-not $SkipPublish) {
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        $existingExePath = Join-Path $publishDir 'FileShare.exe'
        $existingWebViewLoaderPath = Join-Path $publishDir 'WebView2Loader.dll'

        if ((Test-Path $existingExePath) -and (Test-Path $existingWebViewLoaderPath)) {
            Write-Warning "dotnet publish failed with exit code $LASTEXITCODE. Using existing publish output from $publishDir."
        }
        else {
            throw "dotnet publish failed with exit code $LASTEXITCODE"
        }
    }
}
else {
    Write-Host "Skipping publish and using existing output from $publishDir"
}

$exePath = Join-Path $publishDir 'FileShare.exe'
$webViewLoaderPath = Join-Path $publishDir 'WebView2Loader.dll'

if (-not (Test-Path $exePath)) {
    throw "Published executable was not found: $exePath"
}

if (-not (Test-Path $webViewLoaderPath)) {
    throw "WebView2Loader.dll was not found: $webViewLoaderPath"
}

Copy-Item -Force -Path $exePath -Destination (Join-Path $stagingDir 'FileShare.exe')
Copy-Item -Force -Path $webViewLoaderPath -Destination (Join-Path $stagingDir 'WebView2Loader.dll')
Copy-Item -Force -Path (Join-Path $PSScriptRoot 'uninstall.ps1') -Destination (Join-Path $stagingDir 'uninstall.ps1')

Copy-Item -Force -Path (Join-Path $stagingDir 'FileShare.exe') -Destination (Join-Path $setupPayloadDir 'FileShare.exe')
Copy-Item -Force -Path (Join-Path $stagingDir 'WebView2Loader.dll') -Destination (Join-Path $setupPayloadDir 'WebView2Loader.dll')
Copy-Item -Force -Path (Join-Path $stagingDir 'uninstall.ps1') -Destination (Join-Path $setupPayloadDir 'uninstall.ps1')

dotnet publish (Join-Path $setupProjectDir 'FileShareSetup.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false
if ($LASTEXITCODE -ne 0) {
    throw "setup publish failed with exit code $LASTEXITCODE"
}

$builtSetupPath = Join-Path $setupProjectDir 'bin\Release\net9.0-windows\win-x64\publish\FileShareSetup.exe'
if (-not (Test-Path $builtSetupPath)) {
    throw "Built setup executable was not found: $builtSetupPath"
}

Copy-Item -Force -Path $builtSetupPath -Destination $setupPath

if (-not (Test-Path $setupPath)) {
    throw "Installer was not created: $setupPath"
}

Write-Host "Created installer: $setupPath"
