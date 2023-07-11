#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Builds the rust dynamic link library for all supported targets.
.PARAMETER Release
    Build in release mode.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
Param(
    [switch]$Release
)

$buildArgs = @()
if ($Release) {
    $buildArgs += '-r'
}

Push-Location $PSScriptRoot

$rustTargets = @(..\..\azure-pipelines\Get-RustTargets.ps1)
$rustTargets | % { $buildArgs += "--target=$_" }

if ($env:BUILD_BUILDID) {
    Write-Host "##[command]cargo build @buildArgs"
}
cargo build @buildArgs

# Special handling for building the wasm32-unknown-unknown target as it requires the nightly build of rust.
if ($env:BUILD_BUILDID) {
    Write-Host "##[command]cargo +nightly build -Zbuild-std --target=wasm32-unknown-unknown"
}
cargo +nightly build -Zbuild-std --target=wasm32-unknown-unknown

$configPathSegment = 'debug'
if ($Release) { $configPathSegment = 'release' }
$rustTargets | % {
    New-Item -ItemType Directory -Path "..\..\obj\src\nerdbank-zcash-rust\$configPathSegment\$_" -Force | Out-Null
    Copy-Item "target/$_/$configPathSegment/*nerdbank_zcash_rust*" "..\..\obj\src\nerdbank-zcash-rust\$configPathSegment\$_"
}

Pop-Location
