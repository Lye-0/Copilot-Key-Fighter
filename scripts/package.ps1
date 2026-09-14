param([ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$')][string]$Version = '0.1.0')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$output = Join-Path $repoRoot 'artifacts'
$staging = Join-Path $output ('stage-' + [Guid]::NewGuid().ToString('N'))
$package = Join-Path $staging 'CopilotKeyFighter'
New-Item -ItemType Directory -Path $package -Force | Out-Null
dotnet publish (Join-Path $repoRoot 'src/CopilotKeyFighter/CopilotKeyFighter.csproj') -c Release -r win-x64 --self-contained true -p:Version=$Version -o $package
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
foreach ($file in @('README.md','LICENSE','NOTICE.md')) { Copy-Item -LiteralPath (Join-Path $repoRoot $file) -Destination $package }
if (Test-Path (Join-Path $repoRoot 'docs/verification.md')) { Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/verification.md') -Destination (Join-Path $package 'VERIFICATION.md') }
$nugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages' }
$assets = Get-Content (Join-Path $repoRoot 'src/CopilotKeyFighter/obj/project.assets.json') -Raw | ConvertFrom-Json
$runtimeLibraries = @($assets.project.frameworks.PSObject.Properties.Value.downloadDependencies | Where-Object { $_.name -match '^Microsoft\.(NETCore|WindowsDesktop)\.App\.Runtime\.win-x64$' } | Sort-Object name -Unique)
foreach ($library in $runtimeLibraries) {
    $runtimeVersion = $library.version.Trim('[',']').Split(',')[0].Trim()
    $path = Join-Path $nugetRoot ($library.name.ToLowerInvariant() + '/' + $runtimeVersion)
    $noticeFolder = Join-Path $package ('licenses/' + $library.name + '-' + $runtimeVersion)
    New-Item -ItemType Directory -Path $noticeFolder -Force | Out-Null
    $notices = @(Get-ChildItem -LiteralPath $path -File | Where-Object { $_.Name -match 'license|third.?party' })
    if ($notices.Count -eq 0) { throw "Runtime notices missing: $($library.Name)" }
    $notices | Copy-Item -Destination $noticeFolder
}
if (@($runtimeLibraries).Count -lt 2) { throw 'Runtime package provenance missing' }
$zip = Join-Path $output "CopilotKeyFighter-$Version-win-x64.zip"
Compress-Archive -LiteralPath $package -DestinationPath $zip -Force
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$zip.sha256" -Value "$hash  $([IO.Path]::GetFileName($zip))" -Encoding ascii
Write-Output $zip
