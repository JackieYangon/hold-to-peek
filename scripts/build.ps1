param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')
. (Join-Path $PSScriptRoot 'common.ps1')
Push-Location $ProjectRoot
try {
    & $Dotnet build $ProjectFile -c $Configuration --nologo
    Assert-DotnetSuccess
} finally { Pop-Location }
