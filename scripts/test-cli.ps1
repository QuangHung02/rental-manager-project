$ErrorActionPreference = "Stop"

$workspace = "C:\Users\lqh28\Desktop\RentalManagerProject"
$cliExe = "$workspace\RentalManager.Cli\bin\Debug\net8.0-windows\RentalManager.Cli.exe"
$testDb = "$workspace\scripts\test-rental-manager.sqlite"

# Ensure clean state
if (Test-Path $testDb) {
    Remove-Item $testDb -Force
}

$env:RENTALMANAGER_DB_PATH = $testDb

Write-Host "Running CLI Smoke Tests..." -ForegroundColor Cyan

# Ensure project is built
Write-Host "Building project..."
dotnet build "$workspace\RentalManager\RentalManager.sln" --nologo -v q
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

function Run-Command {
    param([string]$Arguments, [string]$ExpectedProperty, [string]$ExpectedValue)
    
    # Run the process, capture stdout and stderr separately
    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = $cliExe
    $pinfo.Arguments = $Arguments
    $pinfo.RedirectStandardOutput = $true
    $pinfo.RedirectStandardError = $true
    $pinfo.UseShellExecute = $false
    
    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $pinfo
    $p.Start() | Out-Null
    
    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
    $p.WaitForExit()
    
    Write-Host "Stderr: $stderr" -ForegroundColor DarkGray
    Write-Host "Stdout: $stdout" -ForegroundColor DarkGray
    
    try {
        $json = $stdout | ConvertFrom-Json
        
        if ($json.$ExpectedProperty -ne $ExpectedValue) {
            Write-Host "FAILED: Expected $ExpectedProperty = $ExpectedValue but got $($json.$ExpectedProperty)" -ForegroundColor Red
            return $null
        }
        
        Write-Host "PASSED" -ForegroundColor Green
        return $json
    }
    catch {
        Write-Host "FAILED to parse JSON from stdout." -ForegroundColor Red
        return $null
    }
}

Write-Host "`n[Seed] Creating test data..."
Run-Command "seed-test" "success" $true | Out-Null

Write-Host "`n[Test 1] Add meter reading"
Run-Command "meter add --property `"Nha Test`" --room `"Phong 101`" --fee `"Dien`" --month `"2026-04`" --current 160" "success" $true | Out-Null

Write-Host "`n[Test 2] Create invoice"
$invRes = Run-Command "invoice create --property `"Nha Test`" --room `"Phong 101`" --month `"2026-04`"" "success" $true
if ($null -eq $invRes) { exit 1 }
$invoiceId = $invRes.data.invoiceId

Write-Host "`n[Test 3] Create invoice again (expect error)"
Run-Command "invoice create --property `"Nha Test`" --room `"Phong 101`" --month `"2026-04`"" "code" "INVOICE_ALREADY_EXISTS" | Out-Null

Write-Host "`n[Test 4] Partial payment (amount: 1,000,000)"
Run-Command "payment add --invoice $invoiceId --amount 1000000 --method `"BankTransfer`" --note `"Thanh toan 1`"" "success" $true | Out-Null

Write-Host "`n[Test 5] Check unpaid invoices (expect invoice to still be there)"
$unpaidRes = Run-Command "invoice unpaid --month `"2026-04`"" "success" $true
$invoiceFound = $false
foreach ($inv in $unpaidRes.data) {
    if ($inv.invoiceId -eq $invoiceId) {
        $invoiceFound = $true
        break
    }
}
if (-not $invoiceFound) {
    Write-Host "FAILED: Invoice not found in unpaid list." -ForegroundColor Red
    exit 1
} else {
    Write-Host "PASSED: Invoice is still unpaid." -ForegroundColor Green
}

Write-Host "`n[Test 6] Pay the remaining amount (amount: 2,210,000)"
# Total amount = 3,000,000 (rent) + (160-100)*3500 (210,000) = 3,210,000. Remainder: 2,210,000.
Run-Command "payment add --invoice $invoiceId --amount 2210000 --method `"Cash`" --note `"Thanh toan 2`"" "success" $true | Out-Null

Write-Host "`n[Test 7] Check unpaid invoices (expect invoice to be fully paid)"
$unpaidRes2 = Run-Command "invoice unpaid --month `"2026-04`"" "success" $true
$invoiceFound2 = $false
foreach ($inv in $unpaidRes2.data) {
    if ($inv.invoiceId -eq $invoiceId) {
        $invoiceFound2 = $true
        break
    }
}
if ($invoiceFound2) {
    Write-Host "FAILED: Invoice still found in unpaid list after full payment." -ForegroundColor Red
    exit 1
} else {
    Write-Host "PASSED: Invoice is fully paid." -ForegroundColor Green
}

Write-Host "`nAll Smoke Tests Passed Successfully!" -ForegroundColor Cyan
