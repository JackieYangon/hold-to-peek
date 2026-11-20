param([ValidateSet('win-x64')][string]$Runtime = 'win-x64')
. (Join-Path $PSScriptRoot 'common.ps1')
$version = (Get-Content -LiteralPath (Join-Path $ProjectRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION must contain a.b.c' }
$outputDirectory = Join-Path $ProjectRoot "artifacts/hold-to-peek-$version-$Runtime"
Push-Location $ProjectRoot
try {
    & $Dotnet publish $ProjectFile -c Release -r $Runtime --self-contained true -o $outputDirectory `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false `
        -p:DebugType=None -p:DebugSymbols=false --nologo
    Assert-DotnetSuccess
    Copy-Item -LiteralPath (Join-Path $ProjectRoot 'docs/QUICK_START.md') -Destination (Join-Path $outputDirectory 'README.zh-CN.md')
    Copy-Item -LiteralPath (Join-Path $ProjectRoot 'VERSION') -Destination $outputDirectory
    Copy-Item -LiteralPath (Join-Path $ProjectRoot 'LICENSE') -Destination $outputDirectory
    $archive = "$outputDirectory.zip"
    Compress-Archive -Path (Join-Path $outputDirectory '*') -DestinationPath $archive -Force
    Write-Output "Portable build: $outputDirectory"
    Write-Output "Archive: $archive"
    Get-FileHash -LiteralPath $archive -Algorithm SHA256
} finally { Pop-Location }
