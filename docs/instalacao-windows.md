# Instalação do LicenseManager no Windows

## Pré-requisitos

- Windows 10 versão 1809 (build 17763) ou superior
- Windows 11 (qualquer versão)
- Não requer .NET instalado separadamente (o app é self-contained)

## Download

Acesse a aba **Actions** do repositório no GitHub, selecione a execução mais recente do workflow
**"Build MSIX (Windows)"** e baixe o artefato `LicenseManager-Windows-<versão>`.

O arquivo contém dois itens:

| Arquivo | Finalidade |
|---|---|
| `LicenseManager-<versão>-windows.msix` | Instalador do aplicativo |
| `LicenseManager.cer` | Certificado autoassinado (instalar uma única vez) |

---

## Etapa 1 — Instalar o certificado (apenas na primeira instalação)

O certificado precisa ser instalado no repositório **"Computador Local → Pessoas Confiáveis"**
para que o Windows aceite o pacote MSIX assinado.

### Via interface gráfica

1. Clique duas vezes em `LicenseManager.cer`
2. Clique em **Instalar Certificado...**
3. Selecione **Computador Local** → clique em **Avançar**
   - Se solicitado, confirme o UAC (permissão de administrador)
4. Selecione **Colocar todos os certificados no repositório a seguir**
5. Clique em **Procurar...** e escolha **Pessoas Confiáveis**
6. Clique em **Avançar** → **Concluir**
7. Confirme a mensagem "A importação foi bem-sucedida"

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

> **Por que isso é necessário?**
> O MSIX exige que o certificado usado na assinatura seja confiável na máquina.
> Com um certificado de uma Autoridade Certificadora comercial (DigiCert, Sectigo etc.)
> essa etapa não seria necessária, pois o certificado já seria reconhecido pelo Windows.
> Para distribuição direta sem loja, o certificado autoassinado é a opção sem custo.

---

## Etapa 2 — Instalar o aplicativo

1. Clique duas vezes em `LicenseManager-<versão>-windows.msix`
2. O instalador do Windows abrirá automaticamente
3. Clique em **Instalar**
4. Aguarde a conclusão — o app aparecerá no menu Iniciar como **LicenseManager**

### Instalação silenciosa via PowerShell

```powershell
Add-AppxPackage -Path "C:\Caminho\Para\LicenseManager-<versao>-windows.msix"
```

---

## Atualização

Para atualizar para uma versão mais recente:

1. Baixe o novo MSIX do artefato de Actions
2. Clique duas vezes no MSIX (ou use `Add-AppxPackage`)
3. O Windows desinstala a versão anterior e instala a nova automaticamente
4. Reinstalar o certificado **não** é necessário nas atualizações

---

## Desinstalação

**Via Configurações do Windows:**

1. Abrir **Configurações** → **Aplicativos** → **Aplicativos instalados**
2. Localizar **LicenseManager**
3. Clicar nos três pontos → **Desinstalar**

**Via PowerShell:**

```powershell
Get-AppxPackage -Name "*LicenseManager*" | Remove-AppxPackage
```

---

## Solução de problemas

### "O certificado do aplicativo não é confiável"

O certificado não foi instalado ou foi instalado no repositório errado.
Verifique se está em **Computador Local → Pessoas Confiáveis** (não em Usuário Atual).

### "Este pacote de aplicativo requer o Windows 10 versão 1809 ou superior"

Atualize o Windows via Windows Update antes de instalar.

### "Não foi possível instalar o pacote"

Verifique se o certificado foi instalado com sucesso antes de tentar instalar o MSIX.
Se o problema persistir, tente a instalação via PowerShell como Administrador:

```powershell
Add-AppxPackage -Path "LicenseManager-<versao>-windows.msix" -ForceApplicationShutdown
```

### O app não consegue conectar à API

O app precisa de acesso à internet para comunicar com `https://licensemanager-api.enzojb.com.br`.
Verifique se firewalls ou proxies corporativos não estão bloqueando a conexão HTTPS.

---

## Observação sobre certificados comerciais

Para distribuição corporativa em larga escala, recomenda-se substituir o certificado
autoassinado por um certificado de assinatura de código emitido por uma Autoridade
Certificadora reconhecida (ex: DigiCert, Sectigo). Com isso, a Etapa 1 deste guia
não seria necessária e o Windows confiaria no instalador automaticamente.
