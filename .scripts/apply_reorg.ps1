$csv = "Assets/Scripts/reorg_map.csv"
if (-not (Test-Path $csv)) {
    Write-Error "CSV not found: $csv"
    exit 1
}

# Start on a new branch
$branch = "reorg/scripts-structure"
$cur = git rev-parse --abbrev-ref HEAD 2>$null
if ($LASTEXITCODE -ne 0) { Write-Error "git not available or not a git repo"; exit 1 }

# Ensure we're on the target branch (create if missing)
$branches = git branch --list $branch
if ($branches) {
    git checkout $branch
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to checkout branch $branch"; exit 1 }
} else {
    git checkout -b $branch
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to create branch $branch"; exit 1 }
}

$lines = Import-Csv $csv
$moved = 0
$skipped = 0
$missing = @()

foreach ($line in $lines) {
    $src = $line.current_path.Trim()
    $dst = $line.proposed_path.Trim()

    if ($src -eq $dst) {
        $skipped++
        continue
    }

    if (-not (Test-Path $src)) {
        $missing += $src
        continue
    }

    $dstDir = Split-Path $dst -Parent
    if (-not (Test-Path $dstDir)) {
        New-Item -ItemType Directory -Force -Path $dstDir | Out-Null
    }

    # Move file and its .meta if present
    git mv -f -- "$src" "$dst"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "git mv failed for $src -> $dst, trying Move-Item fallback"
        Move-Item -Force -LiteralPath $src -Destination $dst
    }

    $srcMeta = "$src.meta"
    $dstMeta = "$dst.meta"
    if (Test-Path $srcMeta) {
        git mv -f -- "$srcMeta" "$dstMeta" 2>$null
        if ($LASTEXITCODE -ne 0) {
            Move-Item -Force -LiteralPath $srcMeta -Destination $dstMeta
        }
    }

    $moved++
}

Write-Host "Moved: $moved, Skipped (same path): $skipped, Missing: $($missing.Count)"
if ($missing.Count -gt 0) {
    Write-Host "Missing files summary (first 20):"
    $missing | Select-Object -First 20 | ForEach-Object { Write-Host " - $_" }
}

# Stage and commit changes
git add -A
if ($LASTEXITCODE -ne 0) { Write-Error "git add failed"; exit 1 }

$commitMsg = "Reorganize scripts per reorg_map.csv: move files to new structure"
git commit -m "$commitMsg"
if ($LASTEXITCODE -ne 0) {
    Write-Host "No changes to commit or commit failed (exit $LASTEXITCODE)"
} else {
    Write-Host "Committed changes on branch $branch"
}

# Show git status short
git status --porcelain

# Print a small sample of moved files
Write-Host "Sample of mapping applied (first 30 entries from CSV):"
$lines | Select-Object -First 30 | ForEach-Object { Write-Host "$_`".current_path` -> $_.proposed_path" }

exit 0
