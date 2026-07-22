$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms

if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process -FilePath 'powershell.exe' -ArgumentList ('-NoProfile -ExecutionPolicy Bypass -File "' + $PSCommandPath + '"') -Verb RunAs
    exit
}

$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FileShare'
$installDir = (Get-ItemProperty -Path $uninstallKey -ErrorAction SilentlyContinue).InstallLocation
if ([string]::IsNullOrWhiteSpace($installDir)) {
    $installDir = Join-Path $env:LOCALAPPDATA 'Programs\FileShare'
}
$startMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\FileShare'
$desktopShortcut = Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'FileShare.lnk'

Get-Process -Name 'FileShare' -ErrorAction SilentlyContinue | Stop-Process -Force

Remove-Item -Force -ErrorAction SilentlyContinue -Path $desktopShortcut
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue -Path $startMenuDir
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue -Path $uninstallKey
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue -Path $installDir

[System.Windows.Forms.MessageBox]::Show('FileShare uninstalled successfully.', 'FileShare Setup', 'OK', 'Information') | Out-Null
