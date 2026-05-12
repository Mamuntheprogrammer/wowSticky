$certName = "WowSticky Development"
$certFile = Join-Path $PSScriptRoot "installer\WowSticky-dev.pfx"
$exePath = Join-Path $PSScriptRoot "publish\WowSticky.exe"

# Create self-signed code signing cert if it doesn't exist
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq "CN=$certName" } | Select-Object -First 1
if (-not $cert) {
    Write-Host "Creating self-signed code signing certificate..."
    $cert = New-SelfSignedCertificate -Type Custom -Subject "CN=$certName" `
        -KeyUsage DigitalSignature -TextExtension "2.5.29.37={text}1.3.6.1.5.5.7.3.3" `
        -FriendlyName $certName -CertStoreLocation "Cert:\CurrentUser\My"
    Write-Host "Certificate created. Thumbprint: $($cert.Thumbprint)"

    # Export to PFX
    $password = ConvertTo-SecureString -String "WowStickyDev" -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $certFile -Password $password
    Write-Host "Certificate exported to: $certFile (password: WowStickyDev)"
}
else {
    Write-Host "Using existing certificate: $certName ($($cert.Thumbprint))"
}

# Install cert to Trusted Root store so the system trusts it
$rootStore = Get-Item Cert:\CurrentUser\Root
$existing = $rootStore | Where-Object { $_.Thumbprint -eq $cert.Thumbprint }
if (-not $existing) {
    Write-Host "Installing certificate to Trusted Root store (admin may be needed)..."
    try {
        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store "Root", "CurrentUser"
        $store.Open("ReadWrite")
        $store.Add($cert)
        $store.Close()
        Write-Host "Certificate installed. Windows will now trust this publisher."
    }
    catch {
        Write-Host "Could not install to Trusted Root: $_" -ForegroundColor Yellow
        Write-Host "To fix 'Unknown Publisher', run as Administrator:"
        Write-Host "  powershell -Command `"Get-ChildItem Cert:\CurrentUser\My | Where-Object { `$_.Subject -eq 'CN=$certName' } | Select-Object -First 1 | ForEach-Object { `$store = New-Object System.Security.Cryptography.X509Certificates.X509Store 'Root','CurrentUser'; `$store.Open('ReadWrite'); `$store.Add(`$_); `$store.Close() }`"" -ForegroundColor Gray
    }
}

# Sign the executable
if (Test-Path $exePath) {
    try {
        $sig = Set-AuthenticodeSignature -FilePath $exePath -Certificate $cert
        if ($sig.Status -eq "Valid") {
            Write-Host "Signed successfully: $exePath"
        } else {
            Write-Host "Signed (status: $($sig.Status) - expected for self-signed cert before trust is established)"
        }
    }
    catch {
        Write-Host "Signing failed: $_" -ForegroundColor Yellow
    }
}
else {
    Write-Host "Executable not found: $exePath. Run dotnet publish first." -ForegroundColor Red
}
