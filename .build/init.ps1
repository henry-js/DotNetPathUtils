# .build/init.ps1
[CmdletBinding()]
param (
    # Use the -Force switch to overwrite existing files in the project root.
    [Switch]$Force
)

# --- Function to safely copy a file ---
function Copy-TemplateFile {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [bool]$ShouldForce
    )
    $FileName = Split-Path -Path $DestinationPath -Leaf
    if ((Test-Path $DestinationPath) -and -not $ShouldForce) {
        Write-Host "File '$FileName' already exists in the project root. Use -Force to overwrite." -ForegroundColor Yellow
    }
    else {
        Write-Host "Creating root '$FileName'..." -ForegroundColor Green
        Copy-Item -Path $SourcePath -Destination $DestinationPath -Force
    }
}

# --- Function to safely copy a directory ---
function Copy-TemplateDirectory {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [bool]$ShouldForce
    )
    $DirName = Split-Path -Path $DestinationPath -Leaf
    if ((Test-Path $DestinationPath) -and -not $ShouldForce) {
        Write-Host "Directory '$DirName' already exists in the project root. Use -Force to overwrite." -ForegroundColor Yellow
    }
    else {
        Write-Host "Creating root '$DirName' directory..." -ForegroundColor Green
        Copy-Item -Path $SourcePath -Destination $DestinationPath -Recurse -Force
    }
}

# Get the directory of the script itself (e.g., '.build/')
$ScriptDir = $PSScriptRoot
# Get the root directory of the repository (which is one level up)
$RepoRoot = Resolve-Path (Join-Path -Path $ScriptDir -ChildPath "..")

Write-Host "Initializing build system in '$RepoRoot'..." -ForegroundColor Cyan

# --- Define Source and Destination Paths ---
$SourceTaskfile = Join-Path -Path $ScriptDir -ChildPath "templates/Taskfile.yml.template"
$SourcePublicEnv = Join-Path -Path $ScriptDir -ChildPath "templates/public.env.example"
$SourceEnvExample = Join-Path -Path $ScriptDir -ChildPath "templates/.env.example"
$SourceBuildProps = Join-Path -Path $ScriptDir -ChildPath "templates/Directory.Build.props.template"
$SourceToolConfig = Join-Path -Path $ScriptDir -ChildPath "dotnet/.config"

$DestTaskfile = Join-Path -Path $RepoRoot -ChildPath "Taskfile.yml"
$DestSecretExample = Join-Path -Path $RepoRoot -ChildPath "public.env"
$DestEnvExample = Join-Path -Path $RepoRoot -ChildPath ".env.example"
$DestBuildProps = Join-Path -Path $RepoRoot -ChildPath "Directory.Build.props"
$DestToolConfig = Join-Path -Path $RepoRoot -ChildPath ".config"

# --- Execute the copy operations ---
Copy-TemplateFile -SourcePath $SourceTaskfile -DestinationPath $DestTaskfile -ShouldForce $Force
Copy-TemplateFile -SourcePath $SourceEnvExample -DestinationPath $DestEnvExample -ShouldForce $Force
Copy-TemplateFile -SourcePath $SourcePublicEnv -DestinationPath $DestSecretExample -ShouldForce $Force
Copy-TemplateFile -SourcePath $SourceBuildProps -DestinationPath $DestBuildProps -ShouldForce $Force # <-- New
Copy-TemplateDirectory -SourcePath $SourceToolConfig -DestinationPath $DestToolConfig -ShouldForce $Force # <-- New

dotnet tool restore

# --- Provide next steps ---
Write-Host "`n✅ Initialization complete." -ForegroundColor White
Write-Host "   Next steps:"
Write-Host "   1. Run 'task setup' to restore local tools." -ForegroundColor Yellow
Write-Host "   2. Copy '.env.example' to '.env' and customize it"
Write-Host "   3. Create your first Git tag (e.g., git tag v0.1.0)"
Write-Host "   4. Run 'task build' to build with the new version"