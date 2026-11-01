param([Parameter(Mandatory=$true)][string]$Command, [string]$File)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$descriptor = Get-Content -LiteralPath (Join-Path $workspace 'Library/Pipeline/.unity-pipeline-port') -Raw | ConvertFrom-Json
$request = @{ command = $Command }
if ($File) { $request.parameters = @{ file = $File } }
$headers = @{ Authorization = 'Bearer ' + $descriptor.evalToken }
$result = Invoke-RestMethod -Uri ('http://127.0.0.1:' + $descriptor.port + '/api/exec') -Method Post -Headers $headers -ContentType 'application/json' -Body ($request | ConvertTo-Json -Depth 5) -TimeoutSec 50
$result | ConvertTo-Json -Depth 12
