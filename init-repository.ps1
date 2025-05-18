# Initialize Git Repository for Home-Decorator Project
# This script sets up the Git repository with proper hooks and configuration
# to enforce secure development practices as defined in the Week 0 foundation tasks

Write-Host "Initializing repository for Home-Decorator project..." -ForegroundColor Cyan

# 1. Ensure we're in the correct directory
$repoRoot = $PSScriptRoot
Write-Host "Repository root: $repoRoot" -ForegroundColor Yellow

# 2. Initialize Git hooks
$hooksDir = Join-Path $repoRoot ".git\hooks"

# Create pre-commit hook to check for secrets
$preCommitHook = @'
#!/bin/sh
# Pre-commit hook to check for secrets

echo "Running pre-commit hook to check for secrets..."

# Check for potential secrets in staged files
files=$(git diff --cached --name-only)
found_secrets=0

# Patterns to check (simplified - in production use a more robust solution)
for file in $files; do
    if grep -i -E "(secret|password|apikey|connectionstring|token)" "$file" > /dev/null; then
        echo "WARNING: Potential secret found in file: $file"
        found_secrets=1
    fi
done

if [ $found_secrets -eq 1 ]; then
    echo "Secrets detected in staged files. Please review and ensure no real secrets are being committed."
    echo "Use 'git diff --cached' to review changes."
    echo "To bypass this check (for false positives), use 'git commit --no-verify'"
    exit 1
fi

exit 0
'@

$preCommitPath = Join-Path $hooksDir "pre-commit"
Set-Content -Path $preCommitPath -Value $preCommitHook
Write-Host "Pre-commit hook installed to check for secrets" -ForegroundColor Green

# Make hook executable in case this is run on a Unix-like system
if ($IsLinux -or $IsMacOS) {
    Write-Host "Making hook executable..."
    Invoke-Expression "chmod +x $preCommitPath"
}

# 3. Configure Git to use VS Code as the diff tool (to help identify secrets)
git config --local diff.tool vscode
git config --local difftool.vscode.cmd "code --wait --diff `$LOCAL `$REMOTE"

# 4. Configure Git to support long paths (useful for .NET projects)
git config --local core.longpaths true

# 5. Ensure Git ignores file mode changes (useful in cross-platform teams)
git config --local core.fileMode false

# 6. Ensure LF line endings consistent across platforms
git config --local core.autocrlf true

# 7. Enable ignorecase for Windows
git config --local core.ignorecase true

Write-Host "Git repository initialized successfully!" -ForegroundColor Green
Write-Host "Week 0 foundation setup complete. Next step is Week 1: Mobile Test-Harness." -ForegroundColor Cyan
