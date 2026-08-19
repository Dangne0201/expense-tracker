<#
Organize-Files helper
- Usage examples:
  # Move a single file and auto-create category if unknown:
  PowerShell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tools\organize-files.ps1 -Path "relative\or\absolute\path\to\file.ext" -AutoCreate

  # Move multiple files (comma-separated):
  PowerShell -File .\scripts\tools\organize-files.ps1 -Path "file1.md,file2.ps1" -AutoCreate

Behaviour:
- Reads .file-catalog.json at repo root to find categories and patterns
- If a pattern matches, the file is moved into the category.path (creates folder if needed)
- If no match & -AutoCreate is specified, creates a sensible new category (by extension) and appends to manifest
- If no match & -AutoCreate is NOT specified, prints a message and exits non-zero
Notes:
- Paths may be absolute or relative to repository root (script determines repo root by searching upward from $PSScriptRoot)
- This is a helper for humans and agents. Agents should call it before adding a loose file to repo
#>
param(
    [Parameter(Mandatory=$true)] [string]$Path,
    [switch]$AutoCreate,
    [switch]$VerboseMode
)

function Write-Log([string]$m) { Write-Host "[organize-files] $m" }

# Find repo root: walk up until we find .git or existing .file-catalog.json
$curr = $PSScriptRoot
$repoRoot = $null
while ($curr -and ($curr -ne (Split-Path $curr -Parent))) {
    if (Test-Path (Join-Path $curr '.git') -PathType Container -ErrorAction SilentlyContinue -or Test-Path (Join-Path $curr '.file-catalog.json')) {
        $repoRoot = $curr; break
    }
    $curr = Split-Path $curr -Parent
}
if (-not $repoRoot) { $repoRoot = Get-Location }

$catalogPath = Join-Path $repoRoot '.file-catalog.json'
if (-not (Test-Path $catalogPath)) { Write-Error "Cannot find $catalogPath. Create a .file-catalog.json in repo root."; exit 2 }
$catalog = Get-Content $catalogPath -Raw | ConvertFrom-Json

# Helper to normalize string path for matching
function Normalize-PathForMatch([string]$p) {
    return ($p -replace '/','\\')
}

# Accept multiple comma-separated paths
$paths = $Path.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }

foreach ($raw in $paths) {
    # Resolve to absolute path
    $candidate = $raw
    if (-not ([System.IO.Path]::IsPathRooted($candidate))) { $candidate = Join-Path $repoRoot $candidate }
    if (-not (Test-Path $candidate)) { Write-Log "File not found: $raw (checked: $candidate)"; continue }

    $filename = Split-Path $candidate -Leaf
    $relPath = Resolve-Path -Path $candidate | ForEach-Object { $_.ProviderPath }
    $moved = $false

    foreach ($cat in $catalog.categories) {
        foreach ($pat in $cat.patterns) {
            # Convert glob to -like pattern; support simple ** by replacing with *
            $g = $pat -replace '\*\*','*'
            # If pattern contains path separators, match against the candidate full path; otherwise match file name
            if ($g -match '\\\\') {
                $matchTarget = Normalize-PathForMatch($relPath)
            } else {
                $matchTarget = $filename
            }
            if ($matchTarget -like $g) {
                $destDir = Join-Path $repoRoot $cat.path
                if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir | Out-Null; Write-Log "Created folder: $destDir" }
                $dest = Join-Path $destDir $filename
                if ((Get-Item $candidate).FullName -ieq (Get-Item $dest -ErrorAction SilentlyContinue)?.FullName) { Write-Log "Already in target location: $dest"; $moved = $true; break }
                Move-Item -Path $candidate -Destination $dest -Force
                Write-Log "Moved '$filename' -> '${cat.path}\$filename'"
                $moved = $true; break
            }
        }
        if ($moved) { break }
    }

    if (-not $moved) {
        if ($AutoCreate) {
            $ext = [System.IO.Path]::GetExtension($filename).TrimStart('.').ToLower()
            if (-not $ext) { $ext = 'misc' }
            $newName = "byext_$ext"
            $newPath = Join-Path 'misc' $ext
            $newCat = @{ name = $newName; path = $newPath; patterns = @("*.$ext") }
            # Append to catalog and write back
            $catalog.categories += $newCat | ConvertTo-Json -Compress | ConvertFrom-Json
            $catalog | ConvertTo-Json -Depth 5 | Out-File -FilePath $catalogPath -Encoding UTF8

            $destDir = Join-Path $repoRoot $newPath
            if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir | Out-Null; Write-Log "Created folder: $destDir" }
            $dest = Join-Path $destDir $filename
            Move-Item -Path $candidate -Destination $dest -Force
            Write-Log "No matching category found. Auto-created category '$newName' and moved file to $newPath\$filename"
        } else {
            Write-Log "No matching category found for '$filename'. Run with -AutoCreate to auto-create a category, or update .file-catalog.json manually."; exit 3
        }
    }
}

if ($VerboseMode) { Write-Log "Done." }
exit 0
