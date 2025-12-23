param(
    [string]$Project = "..\DoleVisitorLogbook\DoleVisitorLogbook.csproj",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [bool]$SelfContained = $true,
    [bool]$PublishSingleFile = $true,
    [bool]$Trim = $false,
    [string]$Output = ".\artifacts\publish\win-x64"
)

Write-Host "Publish script starting..."
Write-Host "Project: $Project"
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $Runtime"
Write-Host "Self-contained: $SelfContained"
Write-Host "Single file: $PublishSingleFile"
Write-Host "Trim: $Trim"
Write-Host "Output: $Output"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet CLI not found in PATH. Install .NET SDK 8 and ensure 'dotnet' is available."
    exit 1
}

# Ensure output folder exists
New-Item -ItemType Directory -Force -Path $Output | Out-Null

# Build publish args
$sc = $SelfContained ? "true" : "false"
$singleFile = $PublishSingleFile ? "true" : "false"
$trim = $Trim ? "true" : "false"

$publishArgs = @(
    $Project,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", $sc,
    "-p:PublishSingleFile=$singleFile",
    "-p:PublishTrimmed=$trim",
    "-o", $Output
)

try {
    Write-Host "Running dotnet publish..."
    dotnet publish @publishArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    Write-Host "Publish succeeded. Output available at: $Output"
    Write-Host "Next steps:"
    Write-Host "- Test the published files on a clean VM."
    Write-Host "- If producing an installer (MSIX/MSI), point your installer project at the folder above."
    Write-Host "- Do NOT embed production DB credentials in published config files. Use environment variables or a secure store."
}
catch {
    Write-Error "Publish failed: $_"
    exit 1
}