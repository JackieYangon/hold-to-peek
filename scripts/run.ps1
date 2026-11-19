. (Join-Path $PSScriptRoot 'common.ps1')
Push-Location $ProjectRoot
try {
    & $Dotnet build $ProjectFile -c Debug --nologo
    Assert-DotnetSuccess
    $executable = Join-Path $ProjectRoot 'src/HoldToPeek/bin/Debug/net10.0-windows/HoldToPeek.exe'
    Start-Process -FilePath $executable -WorkingDirectory $ProjectRoot -WindowStyle Hidden
} finally { Pop-Location }
