$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    cargo build --release --locked --target-dir artifacts/cargo
    if ($LASTEXITCODE) { throw 'Rust core build failed.' }
} finally { Pop-Location }
