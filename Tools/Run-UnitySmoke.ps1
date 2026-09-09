param([string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor', [ValidateSet('Run', 'VerifyPersistence', 'RunCampaign', 'VerifyCampaignPersistence')][string]$Method = 'Run')
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$validationProject = Join-Path $workspace 'Temp/Level1Validation'
New-Item -ItemType Directory -Force -Path $validationProject | Out-Null
Copy-Item -LiteralPath (Join-Path $workspace 'Assets') -Destination $validationProject -Recurse -Force
Copy-Item -LiteralPath (Join-Path $workspace 'ProjectSettings') -Destination $validationProject -Recurse -Force
Copy-Item -LiteralPath (Join-Path $workspace 'Tools/UnitySmoke.cs') -Destination (Join-Path $validationProject 'Assets/Editor/UnitySmoke.cs') -Force
Copy-Item -LiteralPath (Join-Path $workspace 'Tools/CampaignSmoke.cs') -Destination (Join-Path $validationProject 'Assets/Editor/CampaignSmoke.cs') -Force
New-Item -ItemType Directory -Force -Path (Join-Path $validationProject 'Packages') | Out-Null
$localDependencies = @{}
$originalManifest = Get-Content (Join-Path $workspace 'Packages/manifest.json') -Raw | ConvertFrom-Json
foreach ($property in $originalManifest.dependencies.PSObject.Properties) {
    if ($property.Name.StartsWith('com.unity.modules.')) { $localDependencies[$property.Name] = $property.Value }
}
foreach ($packageFolder in Get-ChildItem (Join-Path $workspace 'Library/PackageCache') -Directory) {
    $packageFile = Join-Path $packageFolder.FullName 'package.json'
    if (Test-Path -LiteralPath $packageFile) {
        $packageData = Get-Content -Raw -LiteralPath $packageFile | ConvertFrom-Json
        $localDependencies[$packageData.name] = 'file:' + $packageFolder.FullName.Replace('\','/')
    }
}
[IO.File]::WriteAllText((Join-Path $validationProject 'Packages/manifest.json'), (@{ dependencies = $localDependencies } | ConvertTo-Json -Depth 5))
New-Item -ItemType Directory -Force -Path (Join-Path $workspace 'Logs/Validation') | Out-Null
$logName = if ($Method -eq 'Run') { 'unity-rewards.log' } elseif ($Method -eq 'RunCampaign') { 'unity-campaign.log' } else { 'unity-persistence.log' }
$arguments = '-batchmode -projectPath "' + $validationProject + '" -executeMethod UnitySmoke.' + $Method + ' -logFile "' + (Join-Path $workspace ('Logs/Validation/' + $logName)) + '"'
$process = Start-Process -FilePath (Join-Path $UnityEditor 'Unity.exe') -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Validation Unity process: ' + $process.Id)
