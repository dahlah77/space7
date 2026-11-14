# Install Space7 Windows Edition to current-user Programs, no admin necessary.
param([string]$BuiltExecutable)
$ErrorActionPreference="Stop"
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
if(!$BuiltExecutable){$BuiltExecutable=Join-Path $root "Space7.exe"}
if(!(Test-Path $BuiltExecutable)){throw "Space7.exe tidak ditemukan. Jalankan build.ps1 lebih dahulu."}
$destination=Join-Path $env:LOCALAPPDATA "Programs\Space7"
New-Item -ItemType Directory -Force -Path $destination | Out-Null
Get-Process Space7 -ErrorAction SilentlyContinue | ForEach-Object {if($_.Path -eq (Join-Path $destination "Space7.exe")){$_.CloseMainWindow()|Out-Null;Start-Sleep -Milliseconds 500;if(!$_.HasExited){$_.Kill()}}}
Copy-Item -Path $BuiltExecutable -Destination (Join-Path $destination "Space7.exe") -Force
foreach($file in @("LICENSE.txt","uninstall.ps1","brand-logo.png")){if(Test-Path (Join-Path $root $file)){Copy-Item (Join-Path $root $file) -Destination (Join-Path $destination $file) -Force}}
$shell=New-Object -ComObject WScript.Shell
$desktop=Join-Path ([Environment]::GetFolderPath("Desktop")) "Space7.lnk"
$startDirectory=Join-Path ([Environment]::GetFolderPath("StartMenu")) "Programs\Space7"
New-Item -ItemType Directory -Force -Path $startDirectory|Out-Null
foreach($path in @($desktop,(Join-Path $startDirectory "Space7.lnk"))){
  $link=$shell.CreateShortcut($path);$link.TargetPath=Join-Path $destination "Space7.exe";$link.WorkingDirectory=$destination;$link.Description="Space7 Windows Edition";$link.IconLocation=(Join-Path $destination "Space7.exe")+",0";$link.Save()
}
$reg="HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Space7Windows"
New-Item $reg -Force|Out-Null
New-ItemProperty $reg -Name DisplayName -Value "Space7 Windows Edition" -PropertyType String -Force|Out-Null
New-ItemProperty $reg -Name DisplayVersion -Value "0.2.0" -PropertyType String -Force|Out-Null
New-ItemProperty $reg -Name Publisher -Value "Space7 (independent open source)" -PropertyType String -Force|Out-Null
New-ItemProperty $reg -Name InstallLocation -Value $destination -PropertyType String -Force|Out-Null
New-ItemProperty $reg -Name DisplayIcon -Value (Join-Path $destination "Space7.exe") -PropertyType String -Force|Out-Null
$uninstall="powershell.exe -NoProfile -ExecutionPolicy Bypass -File "+'"'+(Join-Path $destination "uninstall.ps1")+'"'
New-ItemProperty $reg -Name UninstallString -Value $uninstall -PropertyType String -Force|Out-Null
New-ItemProperty $reg -Name NoModify -Value 1 -PropertyType DWord -Force|Out-Null
Write-Host "Space7 terpasang: $destination"
Write-Host "Shortcut: $desktop"
Start-Process (Join-Path $destination "Space7.exe")
