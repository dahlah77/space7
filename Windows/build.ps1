# Space7 Windows Edition build & user-local install
param([switch]$Install)
$ErrorActionPreference="Stop"
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler=Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if(!(Test-Path $compiler)){throw "Compiler .NET Framework 4.x tidak ditemukan. Instal .NET Framework Developer Pack atau gunakan SDK Windows."}
$out=Join-Path $root "Space7.exe"
$source=Join-Path $root "Space7.cs"
if(!(Test-Path $source)){throw "Source Space7.cs tidak ditemukan."}
$refs=@("/reference:System.dll","/reference:System.Core.dll","/reference:System.Drawing.dll","/reference:System.Windows.Forms.dll","/reference:System.Management.dll")
$flags=@("/nologo","/target:winexe","/optimize+","/platform:x64",("/out:"+$out))
$ico=Join-Path $root "Space7.ico"; if(Test-Path $ico){$flags+=("/win32icon:"+$ico)}
& $compiler @flags @refs $source
if($LASTEXITCODE -ne 0){throw "Build gagal (exit $LASTEXITCODE)."}
Write-Host "Build berhasil: $out"
if($Install){& (Join-Path $root "install.ps1") -BuiltExecutable $out}
