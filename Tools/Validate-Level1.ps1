param([string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor')
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $workspace 'Logs/Validation'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$unityData = Join-Path $UnityEditor 'Data'
$dotnet = Join-Path $unityData 'NetCoreRuntime/dotnet.exe'
$compiler = Join-Path $unityData 'DotNetSdkRoslyn/csc.dll'
$baseArgs = @('-nologo', '-langversion:9', '-nowarn:0649', '-nostdlib+', ('-r:' + (Join-Path $unityData 'NetStandard/ref/2.1.0/netstandard.dll')))
$runtimeRefs = Get-ChildItem (Join-Path $unityData 'Managed/UnityEngine') -Filter '*.dll' | ForEach-Object { '-r:' + $_.FullName }
$runtimeRefs += '-r:' + (Join-Path $workspace 'Library/ScriptAssemblies/UnityEngine.UI.dll')
$runtimeRefs += '-r:' + (Join-Path $workspace 'Library/ScriptAssemblies/Unity.InputSystem.dll')
$sources = Get-ChildItem (Join-Path $workspace 'Assets/Scripts') -Recurse -Filter '*.cs' | ForEach-Object { $_.FullName }
$runtimeOut = Join-Path $reportDir 'TapBlast.Runtime.dll'
& $dotnet $compiler $baseArgs $runtimeRefs '-target:library' "-out:$runtimeOut" $sources
if ($LASTEXITCODE -ne 0) { throw 'Runtime compilation failed' }
$editorRefs = Get-ChildItem (Join-Path $unityData 'Managed') -Filter 'UnityEditor.*.dll' | ForEach-Object { '-r:' + $_.FullName }
$editorSources = Get-ChildItem (Join-Path $workspace 'Assets/Editor') -Recurse -Filter '*.cs' | ForEach-Object { $_.FullName }
& $dotnet $compiler $baseArgs $runtimeRefs $editorRefs '-define:UNITY_EDITOR' '-target:library' ('-out:' + (Join-Path $reportDir 'TapBlast.Editor.dll')) $sources $editorSources
if ($LASTEXITCODE -ne 0) { throw 'Editor compilation failed' }
$framework = Get-ChildItem (Join-Path $unityData 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Select-Object -First 1
$testRefs = @('System.Private.CoreLib.dll', 'System.Runtime.dll', 'System.Console.dll') | ForEach-Object { '-r:' + (Join-Path $framework.FullName $_) }
$testOut = Join-Path $reportDir 'RoundSessionChecks.dll'
& $dotnet $compiler '-nologo' '-langversion:9' '-nostdlib+' $testRefs '-target:exe' "-out:$testOut" (Join-Path $workspace 'Assets/Scripts/Core/RoundSession.cs') (Join-Path $workspace 'Tests/RoundSessionChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Rule checks compilation failed' }
$runtimeConfig = @{ runtimeOptions = @{ tfm = 'net6.0'; framework = @{ name = 'Microsoft.NETCore.App'; version = $framework.Name } } } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $reportDir 'RoundSessionChecks.runtimeconfig.json'), $runtimeConfig)
& $dotnet $testOut | Tee-Object -FilePath (Join-Path $reportDir 'round-rules.txt')
if ($LASTEXITCODE -ne 0) { throw 'Rule checks failed' }
Write-Output 'Runtime + editor compile checks passed.'
