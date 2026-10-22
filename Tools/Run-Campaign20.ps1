param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor',
    [ValidateSet('Author', 'GuiAuthor', 'SelectionAuthor', 'TypographyAuthor', 'GuiSmoke', 'Smoke', 'Persistence')][string]$Mode = 'Smoke'
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$validationProject = Join-Path $workspace 'Temp/Campaign20Validation'
New-Item -ItemType Directory -Force -Path $validationProject | Out-Null
Copy-Item -LiteralPath (Join-Path $workspace 'Assets') -Destination $validationProject -Recurse -Force
Copy-Item -LiteralPath (Join-Path $workspace 'ProjectSettings') -Destination $validationProject -Recurse -Force
foreach ($helper in @('UnitySmoke.cs', 'CampaignSmoke.cs', 'World2Authoring.cs', 'GuiAuthoring.cs', 'TypographyAuthoring.cs', 'GuiSmoke.cs', 'Campaign20Smoke.cs')) {
    $source = Join-Path $PSScriptRoot $helper
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $validationProject ('Assets/Editor/' + $helper)) -Force }
}
New-Item -ItemType Directory -Force -Path (Join-Path $validationProject 'Packages') | Out-Null
$dependencies = @{}
$manifest = Get-Content (Join-Path $workspace 'Packages/manifest.json') -Raw | ConvertFrom-Json
foreach ($entry in $manifest.dependencies.PSObject.Properties) {
    if ($entry.Name.StartsWith('com.unity.modules.')) { $dependencies[$entry.Name] = $entry.Value }
}
foreach ($folder in Get-ChildItem (Join-Path $workspace 'Library/PackageCache') -Directory) {
    $packageFile = Join-Path $folder.FullName 'package.json'
    if (Test-Path -LiteralPath $packageFile) {
        $package = Get-Content $packageFile -Raw | ConvertFrom-Json
        $dependencies[$package.name] = 'file:' + $folder.FullName.Replace('\','/')
    }
}
[IO.File]::WriteAllText((Join-Path $validationProject 'Packages/manifest.json'), (@{dependencies=$dependencies} | ConvertTo-Json -Depth 5))
New-Item -ItemType Directory -Force -Path (Join-Path $workspace 'Logs/Validation') | Out-Null
$entryPoint = if ($Mode -eq 'TypographyAuthor') { 'TypographyAuthoring.Run' } elseif ($Mode -eq 'SelectionAuthor') { 'GuiAuthoring.RunLevelSelect' } elseif ($Mode -eq 'GuiSmoke') { 'GuiSmoke.Run' } elseif ($Mode -eq 'GuiAuthor') { 'GuiAuthoring.Run' } elseif ($Mode -eq 'Author') { 'World2Authoring.Run' } elseif ($Mode -eq 'Persistence') { 'Campaign20Smoke.VerifyPersistence' } else { 'Campaign20Smoke.Run' }
$logPath = Join-Path $workspace ('Logs/Validation/campaign20-' + $Mode.ToLowerInvariant() + '.log')
$arguments = '-batchmode -projectPath "' + $validationProject + '" -executeMethod ' + $entryPoint + ' -logFile "' + $logPath + '"'
$process = Start-Process -FilePath (Join-Path $UnityEditor 'Unity.exe') -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Validation Unity process: ' + $process.Id + '; log: ' + $logPath)
