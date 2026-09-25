param(
    [Parameter(Mandatory = $true)][string]$Root,
    [Parameter(Mandatory = $true)][string]$Artifacts,
    [Parameter(Mandatory = $true)][string]$Version
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path -LiteralPath $Root).Path
$Artifacts = (Resolve-Path -LiteralPath $Artifacts).Path
$appName = 'ReaperTrayHelper'
$releaseName = "$appName-$Version"
$releaseRoot = Join-Path $Artifacts $releaseName
$sourceRoot = Join-Path $Artifacts "$releaseName-Source"
$zipPath = Join-Path $Artifacts "$releaseName.zip"
$sourceZipPath = Join-Path $Artifacts "$releaseName-Source.zip"

function Get-Sha256([string]$Path) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    $stream = [IO.File]::OpenRead($Path)
    try {
        return ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $stream.Dispose()
        $algorithm.Dispose()
    }
}

New-Item -ItemType Directory -Path $releaseRoot, $sourceRoot -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $Artifacts "$appName.exe"), (Join-Path $Root 'ReaperTrayHelper_ToggleTrackMute.lua'), (Join-Path $Root 'README.md'), (Join-Path $Root 'LICENSE') -Destination $releaseRoot
Copy-Item -LiteralPath (Join-Path $Root '.github'), (Join-Path $Root 'src'), (Join-Path $Root 'tests'), (Join-Path $Root 'scripts'), (Join-Path $Root 'ReaperTrayHelper_ToggleTrackMute.lua'), (Join-Path $Root 'README.md'), (Join-Path $Root 'LICENSE'), (Join-Path $Root 'SECURITY.md'), (Join-Path $Root 'TESTING.md'), (Join-Path $Root 'THIRD_PARTY_NOTICES.md'), (Join-Path $Root 'build.cmd') -Destination $sourceRoot -Recurse

$personalPattern = 'C:\\Users\\ghdwn|REAPER Media|reaper-license|license\.rk|CodexSandbox'
$sourceFiles = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | Where-Object { $_.Name -ne 'Package.ps1' }
foreach ($file in $sourceFiles) {
    if (Select-String -LiteralPath $file.FullName -Pattern $personalPattern -Quiet) {
        throw "Potential personal or licensed content reference found: $($file.FullName)"
    }
}

Compress-Archive -LiteralPath $releaseRoot -DestinationPath $zipPath
Compress-Archive -LiteralPath $sourceRoot -DestinationPath $sourceZipPath

Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach ($path in @($zipPath, $sourceZipPath)) {
    $archive = [IO.Compression.ZipFile]::OpenRead($path)
    try {
        $entries = @($archive.Entries | Where-Object { $_.Name -ne '' })
        if ($entries.Count -eq 0) { throw "Empty archive: $path" }
        if ($path -eq $sourceZipPath -and @($entries | Where-Object { $_.FullName -match '\.(exe|dll|vbs)$' }).Count -gt 0) {
            throw "Source archive contains an executable or VBS script: $path"
        }
        if ($path -eq $zipPath -and @($entries | Where-Object { $_.FullName -match '\.(vbs|cmd|bat|ps1)$' }).Count -gt 0) {
            throw "User archive contains an installer script: $path"
        }
    }
    finally {
        $archive.Dispose()
    }
}

$hashPaths = @(
    (Join-Path $Artifacts "$appName.exe"),
    $zipPath,
    $sourceZipPath
)
$hashes = foreach ($path in $hashPaths) {
    '{0}  {1}' -f (Get-Sha256 $path), (Split-Path -Leaf $path)
}
$hashes | Set-Content -LiteralPath (Join-Path $Artifacts 'SHA256SUMS.txt') -Encoding ascii
Write-Host "Package verification passed."
Get-ChildItem -LiteralPath $Artifacts -File | Select-Object Name, Length
