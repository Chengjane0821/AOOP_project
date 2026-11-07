param([string]$UnityData = 'C:/Program Files/Unity/Hub/Editor/6000.0.80f1/Editor/Data')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    $destination = Join-Path $projectRoot 'Temp/VerificationCompile'
    New-Item -ItemType Directory -Force $destination | Out-Null
    foreach ($assembly in @('Assembly-CSharp', 'Assembly-CSharp-Editor')) {
        $source = Get-ChildItem Library/Bee -Recurse -Filter "$assembly.rsp" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if (!$source) { throw 'Open the project in Unity once to generate compiler references.' }
        $lines = Get-Content -LiteralPath $source.FullName | Where-Object { $_ -notmatch '^-(out|refout):|^/additionalfile:|^/analyzer:' }
        $lines = $lines | Where-Object { $_ -notmatch '^"Assets/TheForgottenVillage/.*\.cs"$' }
        $lines = $lines | ForEach-Object { $_ -replace 'Library/Bee/artifacts/[^/]+/Assembly-CSharp.ref.dll', 'Temp/VerificationCompile/Assembly-CSharp.dll' }
        $editor = $assembly.EndsWith('-Editor')
        $sources = Get-ChildItem Assets/TheForgottenVillage -Recurse -Filter '*.cs' | Where-Object { ($_.FullName -match '[\\/]Editor[\\/]') -eq $editor }
        $lines += $sources | ForEach-Object { '"' + $_.FullName.Replace('\','/') + '"' }
        $lines += '-out:"Temp/VerificationCompile/' + $assembly + '.dll"'
        $response = Join-Path $destination "$assembly.rsp"
        $lines | Set-Content -LiteralPath $response -Encoding UTF8
        & "$UnityData/NetCoreRuntime/dotnet.exe" "$UnityData/DotNetSdkRoslyn/csc.dll" "@$response"
        if ($LASTEXITCODE -ne 0) { throw "$assembly compilation failed." }
    }
    Write-Output 'PASS: runtime and Editor scripts compile against installed Unity references.'
} finally { Pop-Location }
