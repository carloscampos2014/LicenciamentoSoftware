# Instalação — Windows

## Download

[Download MSIX + Certificado](https://github.com/carloscampos2014/LicenciamentoSoftware/releases/tag/windows-v1.0)

## Passo 1 — Instalar o certificado

Execute o arquivo `instalar-certificado.bat` como **Administrador** (clique com botão direito → Executar como administrador).

Ou via PowerShell como Administrador:

```powershell
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
$cert.Import("C:\Caminho\LicenseManager.cer")
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "LocalMachine")
$store.Open("ReadWrite")
$store.Add($cert)
$store.Close()
```

## Passo 2 — Instalar o MSIX

Clique duas vezes em `LicenseManager-1.0-windows.msix` e clique em **Instalar**.

## Requisitos

- Windows 10 versão 1809 (build 17763) ou superior
- Windows 11 (qualquer versão)
