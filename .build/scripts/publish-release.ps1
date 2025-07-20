# This script orchestrates the final packaging and publishing of artifacts.
# It's designed to be called from the publish-release.yml GitHub Actions workflow.

# Define parameters that can be passed from the workflow.
param (
    [string]$TagName
)

# --- Step 1: Load variables from public.env into the shell's environment ---
# This makes variables like $env:PROJECT_TO_PACK available to this script.
Write-Host "--- Loading environment variables from public.env ---"
Get-Content ./public.env | ForEach-Object {
    $name, $value = $_.Split('=')
    Set-Item -Path "env:$name" -Value $value
}

# --- Step 2: Echo variables for debugging ---
# These values come from both public.env and the 'env:' block in the GitHub Actions step.
Write-Host "DEBUG: Value of PROJECT_TO_PACK is '$($env:PROJECT_TO_PACK)'"
Write-Host "DEBUG: Value of PROJECT_TO_PUBLISH is '$($env:PROJECT_TO_PUBLISH)'"
Write-Host "DEBUG: Value of APP_VERSION is '$($env:APP_VERSION)'"

Write-Host "DEBUG: MINVERSION is '$(task get-version)'"
Write-Host "DEBUG: TagName passed to script is '$TagName'"


# --- Step 3: Conditional NuGet push ---
if (-not [string]::IsNullOrEmpty($env:PROJECT_TO_PACK)) {
    Write-Host "--- Project is a library. Pushing to NuGet... ---"
    task push:nuget
}
else {
    Write-Host "--- Skipping NuGet push (PROJECT_TO_PACK is not set) ---"
}

# --- Step 4: Conditional Velopack build and upload ---
if (-not [string]::IsNullOrEmpty($env:PROJECT_TO_PUBLISH)) {
    Write-Host "--- Project is an application. Packing Velopack release... ---"
    # We use environment variables that were set in the GitHub Actions 'env' block.
    # Taskfile will automatically pick up APP_VERSION from the environment.
    task pack:velopack -- --RID win-x64

    Write-Host "--- Uploading Velopack assets to GitHub Release... ---"
    gh release upload $TagName ./dist/releases/*
}
else {
    Write-Host "--- Skipping Velopack steps (PROJECT_TO_PUBLISH is not set) ---"
}

Write-Host "--- Publish script finished. ---"