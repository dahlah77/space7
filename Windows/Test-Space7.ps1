$ErrorActionPreference="Stop"
Add-Type -AssemblyName System.Windows.Forms
$path="C:\Users\DAHLAH7\Documents\Space7-Windows\Space7.exe"
$a=[Reflection.Assembly]::LoadFrom($path)
$panelType=$a.GetType("Space7Windows.MacPanel")
$p=[Activator]::CreateInstance($panelType)
$p.Show()
foreach($i in 0..7){
    try {
        $p.SelectTab($i)
        $field=$panelType.GetField("sectionTitle",[Reflection.BindingFlags]"Instance,NonPublic")
        $label=$field.GetValue($p)
        Write-Output ("TAB "+$i+": PASS "+$label.Text+" "+$p.Width+"x"+$p.Height)
    } catch {Write-Output ("TAB "+$i+": FAIL "+$_.Exception.ToString())}
}
try{
  $sType=$a.GetType("Space7Windows.Settings7")
  $s=[Activator]::CreateInstance($sType,[Reflection.BindingFlags]"Instance,NonPublic,Public",$null,@($p),$null)
  $s.Show()
  $m=$sType.GetMethod("Select",[Reflection.BindingFlags]"Instance,NonPublic",$null,[Type[]]@([string]),$null)
  foreach($name in @("Fitur","Umum","Energi","Monitor","Tata letak jendela","Pembersih","Audio","Tentang")){
     try{$m.Invoke($s,@($name))|Out-Null;Write-Output ("SETTINGS "+$name+": PASS")}
     catch{Write-Output ("SETTINGS "+$name+": FAIL "+$_.Exception.ToString())}
  }
  $s.Close()
}catch{Write-Output ("SETTINGS_FATAL "+$_.Exception.ToString())}
foreach($name in @("Space7Windows.Layout7","Space7Windows.UrlCleaner7")){
 try{$t=$a.GetType($name);$f=[Activator]::CreateInstance($t);Write-Output ("FORM "+$name+": PASS");$f.Close()}catch{Write-Output ("FORM "+$name+": FAIL "+$_.Exception.ToString())}
}
$p.Dispose()
