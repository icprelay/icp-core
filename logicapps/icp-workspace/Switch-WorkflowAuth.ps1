#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Switches authentication mode in Logic App workflow.json files between local (Raw) and production (ManagedServiceIdentity).

.DESCRIPTION
    This script modifies workflow.json files to switch between:
    - LOCAL mode: Uses Raw authentication with placeholder "REPLACE" for local development and testing
    - PRODUCTION mode: Uses ManagedServiceIdentity with UAMI for Azure deployment

.PARAMETER Mode
    The target authentication mode: 'local' or 'production'

.PARAMETER WorkflowPath
    Path to the workflow directory or workflow.json file. Defaults to current directory.

.PARAMETER WhatIf
    Shows what changes would be made without actually making them.

.EXAMPLE
    .\Switch-WorkflowAuth.ps1 -Mode local
    Switches all workflows in current directory to local authentication

.EXAMPLE
    .\Switch-WorkflowAuth.ps1 -Mode production -WorkflowPath .\process\workflow.json
    Switches specific workflow to production authentication

.EXAMPLE
    .\Switch-WorkflowAuth.ps1 -Mode production -WhatIf
    Preview changes without modifying files
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('local', 'production')]
    [string]$Mode,

    [Parameter(Mandatory = $false)]
    [string]$WorkflowPath = "."
)

$ErrorActionPreference = 'Stop'

# Authentication patterns
$localAuth = @{
    type  = "Raw"
    value = "REPLACE"
}

$productionAuth = @{
    type     = "ManagedServiceIdentity"
    identity = "@{parameters('LogicAppUamiIdentity')}"
    audience = "api://@{parameters('ControlPlaneApiClientId')}/"
}

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Get-WorkflowFiles {
    param([string]$Path)
    
    if (Test-Path $Path -PathType Leaf) {
        if ($Path -like "*workflow.json") {
            return @(Get-Item $Path)
        }
        else {
            throw "Specified file is not a workflow.json file"
        }
    }
    elseif (Test-Path $Path -PathType Container) {
        return Get-ChildItem -Path $Path -Filter "workflow.json" -Recurse
    }
    else {
        throw "Path not found: $Path"
    }
}

function Update-WorkflowAuthentication {
    param(
        [string]$FilePath,
        [string]$TargetMode
    )
    
    Write-ColorOutput "`nProcessing: $FilePath" -Color Cyan
    
    # Read file content
    $content = Get-Content -Path $FilePath -Raw -Encoding UTF8
    
    # Determine patterns based on mode
    if ($TargetMode -eq 'local') {
        $fromPattern = '"authentication":\s*\{\s*"type":\s*"ManagedServiceIdentity",\s*"identity":\s*"@\{parameters\(''LogicAppUamiIdentity''\)\}",\s*"audience":\s*"api://@\{parameters\(''ControlPlaneApiClientId''\)\}/"\s*\}'
        $toReplacement = '"authentication": {
                        "type": "Raw",
                        "value": "REPLACE"
                    }'
        $fromDesc = "ManagedServiceIdentity (production)"
        $toDesc = "Raw (local)"
    }
    else {
        $fromPattern = '"authentication":\s*\{\s*"type":\s*"Raw",\s*"value":\s*"REPLACE"\s*\}'
        $toReplacement = '"authentication": {
                        "type": "ManagedServiceIdentity",
                        "identity": "@{parameters(''LogicAppUamiIdentity'')}",
                        "audience": "api://@{parameters(''ControlPlaneApiClientId'')}/"
                    }'
        $fromDesc = "Raw (local)"
        $toDesc = "ManagedServiceIdentity (production)"
    }
    
    # Find all matches
    $matches = [regex]::Matches($content, $fromPattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $matchCount = $matches.Count
    
    if ($matchCount -eq 0) {
        Write-ColorOutput "  No $fromDesc authentication blocks found (already in $TargetMode mode or different pattern)" -Color Yellow
        return @{ Changed = $false; Count = 0 }
    }
    
    Write-ColorOutput "  Found $matchCount authentication block(s) to convert" -Color Gray
    Write-ColorOutput "    $fromDesc -> $toDesc" -Color Green    # Replace all occurrences
    if (-not $WhatIfPreference) {
        $newContent = $content -replace $fromPattern, $toReplacement
        # Remove BOM by using UTF8 encoding without BOM
        $utf8NoBom = New-Object System.Text.UTF8Encoding $false
        [System.IO.File]::WriteAllText($FilePath, $newContent, $utf8NoBom)
        Write-ColorOutput "  [OK] Updated $matchCount authentication block(s)" -Color Green
    }
    else {
        Write-ColorOutput "  [WhatIf] Would update $matchCount authentication block(s)" -Color Yellow
    }
    
    return @{ Changed = $true; Count = $matchCount }
}

# Main execution
try {
    Write-ColorOutput "`n=== Logic App Workflow Authentication Switcher ===" -Color Cyan
    Write-ColorOutput "Target Mode: $Mode" -Color White
    Write-ColorOutput "Workflow Path: $WorkflowPath" -Color White
    
    if ($WhatIf) {
        Write-ColorOutput "Mode: DRY RUN (no files will be modified)" -Color Yellow
    }
    
    # Get workflow files
    $workflowFiles = Get-WorkflowFiles -Path $WorkflowPath
    Write-ColorOutput "`nFound $($workflowFiles.Count) workflow file(s)" -Color White
    
    # Process each workflow
    $totalChanged = 0
    $filesModified = 0
    
    foreach ($file in $workflowFiles) {
        $result = Update-WorkflowAuthentication -FilePath $file.FullName -TargetMode $Mode
        
        if ($result.Changed) {
            $filesModified++
            $totalChanged += $result.Count
        }
    }
    
    # Summary
    Write-ColorOutput "`n=== Summary ===" -Color Cyan
    Write-ColorOutput "Files processed: $($workflowFiles.Count)" -Color White
    Write-ColorOutput "Files modified: $filesModified" -Color $(if ($filesModified -gt 0) { "Green" } else { "Yellow" })
    Write-ColorOutput "Authentication blocks updated: $totalChanged" -Color $(if ($totalChanged -gt 0) { "Green" } else { "Yellow" })
    
    if ($WhatIfPreference) {
        Write-ColorOutput "`nWARNING: This was a dry run. Use without -WhatIf to apply changes." -Color Yellow
    }
    elseif ($totalChanged -gt 0) {
        Write-ColorOutput "`nSUCCESS: Successfully switched to $Mode mode" -Color Green
    }
    else {
        Write-ColorOutput "`nSUCCESS: All workflows already in $mode mode" -Color Green
    }
}
catch {
    Write-ColorOutput "`nERROR: $_" -Color Red
    Write-ColorOutput $_.ScriptStackTrace -Color Red
    exit 1
}
