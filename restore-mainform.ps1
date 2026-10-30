$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

$encodedPath = Join-Path $PSScriptRoot 'Source\MainForm.cs.gz.b64'
$destination = Join-Path $PSScriptRoot 'Source\MainForm.cs'
$expectedSha256 = '0a20c221e75c64091e76660c09161de7e78d19a49c687e3d079730092b336908'

if (-not (Test-Path -LiteralPath $encodedPath)) {
    throw "Missing compressed MainForm source: $encodedPath"
}

$encoded = [IO.File]::ReadAllText($encodedPath)
$compressedBytes = [Convert]::FromBase64String(($encoded -replace '\s', ''))
$inputStream = New-Object IO.MemoryStream(,$compressedBytes)
$outputStream = New-Object IO.MemoryStream
$gzipStream = $null

try {
    $gzipStream = New-Object IO.Compression.GZipStream($inputStream, [IO.Compression.CompressionMode]::Decompress)
    $gzipStream.CopyTo($outputStream)
    $sourceBytes = $outputStream.ToArray()
}
finally {
    if ($gzipStream) { $gzipStream.Dispose() }
    $outputStream.Dispose()
    $inputStream.Dispose()
}

$sha = [Security.Cryptography.SHA256]::Create()
try {
    $actualSha256 = ([BitConverter]::ToString($sha.ComputeHash($sourceBytes))).Replace('-', '').ToLowerInvariant()
}
finally {
    $sha.Dispose()
}
if ($actualSha256 -ne $expectedSha256) {
    throw "Restored MainForm.cs failed SHA-256 validation. Expected $expectedSha256; received $actualSha256."
}

if (Test-Path -LiteralPath $destination) {
    $existingHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($existingHash -ne $expectedSha256) {
        throw 'Source\MainForm.cs exists but its checksum does not match the packaged source. Move it aside before restoring.'
    }
    Write-Host 'MainForm.cs is already restored and verified.' -ForegroundColor Green
    exit 0
}

[IO.File]::WriteAllBytes($destination, $sourceBytes)
Write-Host 'Restored and verified Source\MainForm.cs.' -ForegroundColor Green
