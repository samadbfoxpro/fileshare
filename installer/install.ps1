$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms

$appName = 'FileShare'
$publisher = 'FileShare'
$version = '1.3'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\FileShare'
$startMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\FileShare'
$desktopShortcut = Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'FileShare.lnk'
$startShortcut = Join-Path $startMenuDir 'FileShare.lnk'
$uninstallShortcut = Join-Path $startMenuDir 'Uninstall FileShare.lnk'
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FileShare'

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
New-Item -ItemType Directory -Force -Path $startMenuDir | Out-Null

Copy-Item -Force -Path (Join-Path $PSScriptRoot 'FileShare.exe') -Destination (Join-Path $installDir 'FileShare.exe')
Copy-Item -Force -ErrorAction SilentlyContinue -Path (Join-Path $PSScriptRoot '*.dll') -Destination $installDir
Copy-Item -Force -Path (Join-Path $PSScriptRoot 'uninstall.ps1') -Destination (Join-Path $installDir 'uninstall.ps1')

$shell = New-Object -ComObject WScript.Shell

$shortcut = $shell.CreateShortcut($desktopShortcut)
$shortcut.TargetPath = Join-Path $installDir 'FileShare.exe'
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = Join-Path $installDir 'FileShare.exe'
$shortcut.Save()

$shortcut = $shell.CreateShortcut($startShortcut)
$shortcut.TargetPath = Join-Path $installDir 'FileShare.exe'
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = Join-Path $installDir 'FileShare.exe'
$shortcut.Save()

$shortcut = $shell.CreateShortcut($uninstallShortcut)
$shortcut.TargetPath = 'powershell.exe'
$shortcut.Arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $installDir 'uninstall.ps1') + '"'
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = Join-Path $installDir 'FileShare.exe'
$shortcut.Save()

New-Item -Force -Path $uninstallKey | Out-Null
Set-ItemProperty -Path $uninstallKey -Name DisplayName -Value $appName
Set-ItemProperty -Path $uninstallKey -Name DisplayVersion -Value $version
Set-ItemProperty -Path $uninstallKey -Name Publisher -Value $publisher
Set-ItemProperty -Path $uninstallKey -Name InstallLocation -Value $installDir
Set-ItemProperty -Path $uninstallKey -Name DisplayIcon -Value (Join-Path $installDir 'FileShare.exe')
Set-ItemProperty -Path $uninstallKey -Name UninstallString -Value ('powershell.exe -NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $installDir 'uninstall.ps1') + '"')
Set-ItemProperty -Path $uninstallKey -Name NoModify -Type DWord -Value 1
Set-ItemProperty -Path $uninstallKey -Name NoRepair -Type DWord -Value 1

[System.Windows.Forms.MessageBox]::Show('FileShare installed successfully.', 'FileShare Setup', 'OK', 'Information') | Out-Null
