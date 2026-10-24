[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [ValidateSet("All", "Controls")]
    [string]$Scope = "All"
)

$ErrorActionPreference = "Stop"
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw "The supported WinFormsKit build requires Windows. See docs/TESTING.md for cross-compilation limits."
}

$solutionName = if ($Scope -eq "Controls") { "MyDmsVn.WinFormsKit.Controls.sln" } else { "MyDmsVn.WinFormsKit.sln" }
$solution = Join-Path $PSScriptRoot $solutionName
if ($Scope -eq "All" -and -not (Test-Path (Join-Path $PSScriptRoot "vendor/sourcegrid/SourceGrid/SourceGrid.csproj"))) {
    throw "SourceGrid is missing. Run: git submodule update --init --recursive"
}

Push-Location $PSScriptRoot
try {
    dotnet restore $solution --locked-mode
    if ($LASTEXITCODE -ne 0) { throw "Restore failed with exit code $LASTEXITCODE." }
    dotnet build $solution -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE." }
}
finally {
    Pop-Location
}
