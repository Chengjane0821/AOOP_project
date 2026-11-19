$unityProcess = Get-Process Unity | Where-Object { $_.MainWindowTitle -like '*AOOP_project*' } | Select-Object -First 1
if (-not $unityProcess) { throw 'AOOP_project Unity window not found' }
$window = New-Object -ComObject WScript.Shell
if ($window.AppActivate($unityProcess.Id)) { Start-Sleep -Milliseconds 500; $window.SendKeys('^r'); Write-Output 'Sent Unity asset refresh' }
