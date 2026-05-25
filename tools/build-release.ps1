param(
    [string] $Configuration = "Release",
    [string] $Runtime = "win-x64",
    [switch] $SkipInstaller
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishDir = Join-Path $repoRoot "artifacts\MyristaSwitch-$Runtime-portable"
$installerDir = Join-Path $repoRoot "artifacts\installer"

function Clear-RepoDirectory([string] $Path) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPath = [System.IO.Path]::GetFullPath($repoRoot)
    if (-not $fullPath.StartsWith($rootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clear a path outside the repository: $fullPath"
    }

    if (Test-Path $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }
}

Clear-RepoDirectory $publishDir
Clear-RepoDirectory $installerDir

dotnet build (Join-Path $repoRoot "MyristaSwitch.sln") --configuration $Configuration
dotnet publish (Join-Path $repoRoot "MyristaSwitch.App\MyristaSwitch.App.csproj") `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    --output $publishDir

if ($SkipInstaller) {
    Write-Host "Portable build written to $publishDir"
    return
}

$isccCommand = Get-Command "iscc.exe" -ErrorAction SilentlyContinue
$isccPath = if ($isccCommand) { $isccCommand.Source } else { $null }
if (-not $isccPath) {
    $candidatePaths = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )

    $isccPath = $candidatePaths | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
}

if (-not $isccPath) {
    throw "Inno Setup compiler (iscc.exe) was not found. Install it with: winget install --id JRSoftware.InnoSetup -e"
}

New-Item -ItemType Directory -Force -Path $installerDir | Out-Null
& $isccPath (Join-Path $repoRoot "installer\MyristaSwitch.iss")

Write-Host "Portable build written to $publishDir"
Write-Host "Installer written to $installerDir"
