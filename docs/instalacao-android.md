# InstalaÃ§Ã£o do LicenseManager no Android

## PrÃ©-requisitos

- Android 5.0 (API 21) ou superior
- EspaÃ§o livre: aproximadamente 150 MB
- ConexÃ£o com a internet para uso do aplicativo

## Download

Acesse a aba **Actions** do repositÃ³rio no GitHub, selecione a execuÃ§Ã£o mais recente do workflow
**"Build APK (Android)"** e baixe o artefato `LicenseManager-Android-<versÃ£o>`.

O arquivo contÃ©m:

| Arquivo | Finalidade |
|---|---|
| `LicenseManager-<versÃ£o>-android.apk` | Instalador do aplicativo assinado |

---

## Etapa 1 â€” Habilitar instalaÃ§Ã£o de fontes desconhecidas

O Android bloqueia por padrÃ£o a instalaÃ§Ã£o de APKs fora da Google Play Store.
Ã‰ necessÃ¡rio habilitar essa permissÃ£o uma Ãºnica vez para o gerenciador de arquivos
ou navegador que vocÃª usarÃ¡ para instalar o APK.

### Android 8.0 ou superior (recomendado)

1. Abra **ConfiguraÃ§Ãµes** â†’ **Aplicativos** (ou **Gerenciar aplicativos**)
2. Localize o aplicativo que vai abrir o APK (ex: **Gerenciador de arquivos**, **Chrome**)
3. Toque em **Instalar aplicativos desconhecidos** (ou **PermissÃµes especiais**)
4. Ative a opÃ§Ã£o **Permitir desta fonte**

> No Android 8+, a permissÃ£o Ã© concedida por aplicativo, nÃ£o globalmente.
> Isso Ã© mais seguro â€” apenas o app que vocÃª autorizou pode instalar APKs.

### Android 7.0 ou inferior

1. Abra **ConfiguraÃ§Ãµes** â†’ **SeguranÃ§a**
2. Ative a opÃ§Ã£o **Fontes desconhecidas**
3. Confirme o aviso de seguranÃ§a

---

## Etapa 2 â€” Instalar o APK

### Via gerenciador de arquivos (mÃ©todo mais simples)

1. Transfira o arquivo `.apk` para o dispositivo (via cabo USB, Google Drive, e-mail, etc.)
2. Abra o **Gerenciador de arquivos** no dispositivo
3. Navegue atÃ© a pasta onde o APK foi salvo (geralmente **Downloads**)
4. Toque no arquivo `LicenseManager-<versÃ£o>-android.apk`
5. Toque em **Instalar** na tela de confirmaÃ§Ã£o
6. ApÃ³s a instalaÃ§Ã£o, toque em **Abrir** ou encontre o app na gaveta de aplicativos

### Via ADB (para administradores de TI)

Com o dispositivo conectado via USB e depuraÃ§Ã£o USB habilitada:

```bash
adb install LicenseManager-<versao>-android.apk
```

Para reinstalar preservando os dados:

```bash
adb install -r LicenseManager-<versao>-android.apk
```

---

## AtualizaÃ§Ã£o

Para atualizar para uma versÃ£o mais recente:

1. Baixe o novo APK
2. Siga os mesmos passos da instalaÃ§Ã£o
3. O Android detectarÃ¡ que Ã© uma atualizaÃ§Ã£o (mesmo `applicationId`) e preservarÃ¡ os dados do app
4. NÃ£o Ã© necessÃ¡rio desinstalar a versÃ£o anterior

---

## DesinstalaÃ§Ã£o

**Via configuraÃ§Ãµes do Android:**

1. Abra **ConfiguraÃ§Ãµes** â†’ **Aplicativos**
2. Localize **LicenseManager**
3. Toque em **Desinstalar** â†’ confirme

**Via ADB:**

```bash
adb uninstall com.licensemanager.app
```

---

## SoluÃ§Ã£o de problemas

### "AnÃ¡lise bloqueada" ou "App pode ser prejudicial"

O Google Play Protect pode exibir um aviso ao instalar APKs fora da Play Store.
Isso Ã© esperado para distribuiÃ§Ã£o direta (sideload).

1. Toque em **Mais detalhes**
2. Toque em **Instalar mesmo assim**

Se preferir desabilitar temporariamente o Play Protect:

1. Abra o **Google Play Store**
2. Toque no Ã­cone do seu perfil â†’ **Play Protect**
3. Toque no Ã­cone de engrenagem â†’ desative **Verificar apps com o Play Protect**
4. Reinstale o APK
5. Reative o Play Protect apÃ³s a instalaÃ§Ã£o

### "NÃ£o instalado" apÃ³s tentar instalar

PossÃ­veis causas:

- **EspaÃ§o insuficiente:** libere pelo menos 200 MB e tente novamente
- **VersÃ£o incompatÃ­vel:** verifique se o Android Ã© 5.0 (API 21) ou superior em **ConfiguraÃ§Ãµes â†’ Sobre o telefone**
- **APK corrompido:** faÃ§a o download novamente

### "Aplicativo nÃ£o instalado â€” certificado invÃ¡lido"

Isso ocorre ao tentar instalar sobre uma versÃ£o assinada com outra keystore.
Desinstale a versÃ£o anterior antes de instalar:

```bash
adb uninstall com.licensemanager.app
adb install LicenseManager-<versao>-android.apk
```

> **AtenÃ§Ã£o:** a desinstalaÃ§Ã£o remove os dados locais do app (tokens armazenados).
> VocÃª precisarÃ¡ fazer login novamente apÃ³s reinstalar.

### O app nÃ£o consegue conectar Ã  API

O app precisa de acesso Ã  internet para comunicar com `https://licensemanager-api.enzojb.com.br`.
Verifique:
- ConexÃ£o Wi-Fi ou dados mÃ³veis ativa
- Firewall corporativo nÃ£o estÃ¡ bloqueando HTTPS (porta 443)
- VPN ativa nÃ£o estÃ¡ interferindo

---

## Sobre a assinatura do APK

O APK Ã© assinado com uma keystore prÃ³pria do projeto, gerada com `keytool` e armazenada
como GitHub Secret (`ANDROID_KEYSTORE_BASE64`). A assinatura garante a integridade do
arquivo â€” qualquer APK modificado apÃ³s a assinatura serÃ¡ rejeitado pelo Android.

Para verificar a assinatura manualmente:

```bash
apksigner verify --verbose LicenseManager-<versao>-android.apk
```
