$ErrorActionPreference = 'Stop'
$ProjectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$ProjectFile = Join-Path $ProjectRoot 'src/HoldToPeek/HoldToPeek.csproj'
$localDotnet = Join-Path $ProjectRoot '.dotnet/dotnet.exe'
if (Test-Path -LiteralPath $localDotnet) {
    $Dotnet = $localDotnet
    $env:DOTNET_ROOT = Split-Path -Parent $localDotnet
} else {
    $installedDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $installedDotnet) { throw 'Install the .NET 10 SDK, or place it in the project .dotnet directory.' }
    $Dotnet = $installedDotnet.Source
}
$env:DOTNET_CLI_HOME = Join-Path $ProjectRoot '.dotnet-home'
$env:NUGET_PACKAGES = Join-Path $ProjectRoot '.nuget/packages'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

function Assert-DotnetSuccess {
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}
