[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [ValidateSet("All", "Controls")]
    [string]$Scope = "All",
    [ValidateSet("All", "net48", "net8.0-windows")]
    [string]$Framework = "All",
    [string]$Filter = "",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw "Native WinForms tests require Windows. They cannot be validated on Linux."
}

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot "build.ps1") -Configuration $Configuration -Scope $Scope
}

$projects = @("MyDmsVn.Bootstrap5WinFormUI.Tests")
if ($Scope -eq "All") { $projects += "MyDmsVn.BootstrapSourceGrid.Tests" }
$frameworks = if ($Framework -eq "All") { @("net48", "net8.0-windows") } else { @($Framework) }

Push-Location $PSScriptRoot
try {
    foreach ($project in $projects) {
        foreach ($target in $frameworks) {
            $testArguments = @(
                "test", "tests/$project/$project.csproj", "-c", $Configuration,
                "-f", $target, "--no-build", "--no-restore",
                "--blame-hang", "--blame-hang-timeout", "5m",
                "--results-directory", "artifacts/test-results",
                "--logger", "trx;LogFileName=$project.$target.trx"
            )
            if (-not [string]::IsNullOrWhiteSpace($Filter)) { $testArguments += @("--filter", $Filter) }
            dotnet @testArguments
            if ($LASTEXITCODE -ne 0) { throw "$project ($target) failed with exit code $LASTEXITCODE." }
        }
    }
}
finally {
    Pop-Location
}
