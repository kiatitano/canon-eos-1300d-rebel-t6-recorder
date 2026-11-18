$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$executable=Join-Path $root 'dist\EOS1300DRecorder.exe'
if(-not (Test-Path -LiteralPath $executable)) { throw 'Build the app first with scripts\Build.ps1.' }
$shell=New-Object -ComObject WScript.Shell
$shortcut=$shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'EOS 1300D Recorder.lnk'))
$shortcut.TargetPath=$executable
$shortcut.WorkingDirectory=Split-Path $executable -Parent
$shortcut.IconLocation=$executable+',0'
$shortcut.Description='USB recording, live preview and verified transfers for Canon EOS 1300D / Rebel T6'
$shortcut.Save()
Write-Output 'Desktop shortcut created.'
