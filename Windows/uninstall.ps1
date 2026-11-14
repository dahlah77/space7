# Remove only Space7 Windows Edition files, shortcuts, and its per-user registry entry.
$ErrorActionPreference="Stop"
$folder=Join-Path $env:LOCALAPPDATA "Programs\Space7"
Get-Process Space7 -ErrorAction SilentlyContinue | ForEach-Object {try{$_.CloseMainWindow()|Out-Null;Start-Sleep -Milliseconds 300;if(!$_.HasExited){$_.Kill()}}catch{}}
Remove-Item (Join-Path ([Environment]::GetFolderPath("Desktop")) "Space7.lnk") -ErrorAction SilentlyContinue
$start=Join-Path ([Environment]::GetFolderPath("StartMenu")) "Programs\Space7"
Remove-Item $start -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Space7Windows" -Recurse -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500
Remove-Item $folder -Recurse -Force -ErrorAction SilentlyContinue
