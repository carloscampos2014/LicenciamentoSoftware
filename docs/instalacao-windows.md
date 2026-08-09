# InstalaÃ§Ã£o do LicenseManager no Windows

## PrÃ©-requisitos

- Windows 10 versÃ£o 1809 (build 17763) ou superior
- Windows 11 (qualquer versÃ£o)
- NÃ£o requer .NET instalado separadamente (o app Ã© self-contained)

## Download

Acesse a aba **Actions** do repositÃ³rio no GitHub, selecione a execuÃ§Ã£o mais recente do workflow
**"Build MSIX (Windows)"** e baixe o artefato `LicenseManager-Windows-<versÃ£o>`.

O arquivo contÃ©m dois itens:

| Arquivo | Finalidade |
|---|---|
| `LicenseManager-<versÃ£o>-windows.msix` | Instalador do aplicativo |
| `LicenseManager.cer` | Certificado autoassinado (instalar uma Ãºnica vez) |

---

## Etapa 1 â€” Instalar o certificado (apenas na primeira instalaÃ§Ã£o)

O certificado precisa ser instalado no repositÃ³rio **"Computador Local â†’ Pessoas ConfiÃ¡veis"**
para que o Windows aceite o pacote MSIX assinado.

### Via interface grÃ¡fica

1. Clique duas vezes em `LicenseManager.cer`
2. Clique em **Instalar Certificado...**
3. Selecione **Computador Local** â†’ clique em **AvanÃ§ar**
   - Se solicitado, confirme o UAC (permissÃ£o de administrador)
4. Selecione **Colocar todos os certificados no repositÃ³rio a seguir**
5. Clique em **Procurar...** e escolha **Pessoas ConfiÃ¡veis**
6. Clique em **AvanÃ§ar** â†’ **Concluir**
7. Confirme a mensagem "A importaÃ§Ã£o foi bem-sucedida"

### Via PowerShell (administrador)

```powershell
# Abrir PowerShell como Administrador e executar:
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
$cert.Import("C:\Caminho\Para\LicenseManager.cer")

$store = New-Object System.Security.Cryptography.X509Certificates.X509Store("TrustedPeople", "LocalMachine")
$store.Open("ReadWrite")
$store.Add($cert)
$store.Close()

Write-Host "Certificado instalado com sucesso."
```

> **Por que isso Ã© necessÃ¡rio?**
> O MSIX exige que o certificado usado na assinatura seja confiÃ¡vel na mÃ¡quina.
> Com um certificado de uma Autoridade Certificadora comercial (DigiCert, Sectigo etc.)
> essa etapa nÃ£o seria necessÃ¡ria, pois o certificado jÃ¡ seria reconhecido pelo Windows.
> Para distribuiÃ§Ã£o direta sem loja, o certificado autoassinado Ã© a opÃ§Ã£o sem custo.

---

## Etapa 2 â€” Instalar o aplicativo

1. Clique duas vezes em `LicenseManager-<versÃ£o>-windows.msix`
2. O instalador do Windows abrirÃ¡ automaticamente
3. Clique em **Instalar**
4. Aguarde a conclusÃ£o â€” o app aparecerÃ¡ no menu Iniciar como **LicenseManager**

### InstalaÃ§Ã£o silenciosa via PowerShell

```powershell
Add-AppxPackage -Path "C:\Caminho\Para\LicenseManager-<versao>-windows.msix"
```

---

## AtualizaÃ§Ã£o

Para atualizar para uma versÃ£o mais recente:

1. Baixe o novo MSIX do artefato de Actions
2. Clique duas vezes no MSIX (ou use `Add-AppxPackage`)
3. O Windows desinstala a versÃ£o anterior e instala a nova automaticamente
4. Reinstalar o certificado **nÃ£o** Ã© necessÃ¡rio nas atualizaÃ§Ãµes

---

## DesinstalaÃ§Ã£o

**Via ConfiguraÃ§Ãµes do Windows:**

1. Abrir **ConfiguraÃ§Ãµes** â†’ **Aplicativos** â†’ **Aplicativos instalados**
2. Localizar **LicenseManager**
3. Clicar nos trÃªs pontos â†’ **Desinstalar**

**Via PowerShell:**

```powershell
Get-AppxPackage -Name "*LicenseManager*" | Remove-AppxPackage
```

---

## SoluÃ§Ã£o de problemas

### "O certificado do aplicativo nÃ£o Ã© confiÃ¡vel"

O certificado nÃ£o foi instalado ou foi instalado no repositÃ³rio errado.
Verifique se estÃ¡ em **Computador Local â†’ Pessoas ConfiÃ¡veis** (nÃ£o em UsuÃ¡rio Atual).

### "Este pacote de aplicativo requer o Windows 10 versÃ£o 1809 ou superior"

Atualize o Windows via Windows Update antes de instalar.

### "NÃ£o foi possÃ­vel instalar o pacote"

Verifique se o certificado foi instalado com sucesso antes de tentar instalar o MSIX.
Se o problema persistir, tente a instalaÃ§Ã£o via PowerShell como Administrador:

```powershell
Add-AppxPackage -Path "LicenseManager-<versao>-windows.msix" -ForceApplicationShutdown
```

### O app nÃ£o consegue conectar Ã  API

O app precisa de acesso Ã  internet para comunicar com `https://licensemanager-api.enzojb.com.br`.
Verifique se firewalls ou proxies corporativos nÃ£o estÃ£o bloqueando a conexÃ£o HTTPS.

---

## ObservaÃ§Ã£o sobre certificados comerciais

Para distribuiÃ§Ã£o corporativa em larga escala, recomenda-se substituir o certificado
autoassinado por um certificado de assinatura de cÃ³digo emitido por uma Autoridade
Certificadora reconhecida (ex: DigiCert, Sectigo). Com isso, a Etapa 1 deste guia
nÃ£o seria necessÃ¡ria e o Windows confiaria no instalador automaticamente.
