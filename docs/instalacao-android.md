# Instalação do LicenseManager no Android

## Pré-requisitos

- Android 5.0 (API 21) ou superior
- Espaço livre: aproximadamente 150 MB
- Conexão com a internet para uso do aplicativo

## Download

Acesse a aba **Actions** do repositório no GitHub, selecione a execução mais recente do workflow
**"Build APK (Android)"** e baixe o artefato `LicenseManager-Android-<versão>`.

O arquivo contém:

| Arquivo | Finalidade |
|---|---|
| `LicenseManager-<versão>-android.apk` | Instalador do aplicativo assinado |

---

## Etapa 1 — Habilitar instalação de fontes desconhecidas

O Android bloqueia por padrão a instalação de APKs fora da Google Play Store.
É necessário habilitar essa permissão uma única vez para o gerenciador de arquivos
ou navegador que você usará para instalar o APK.

### Android 8.0 ou superior (recomendado)

1. Abra **Configurações** → **Aplicativos** (ou **Gerenciar aplicativos**)
2. Localize o aplicativo que vai abrir o APK (ex: **Gerenciador de arquivos**, **Chrome**)
3. Toque em **Instalar aplicativos desconhecidos** (ou **Permissões especiais**)
4. Ative a opção **Permitir desta fonte**

> No Android 8+, a permissão é concedida por aplicativo, não globalmente.
> Isso é mais seguro — apenas o app que você autorizou pode instalar APKs.

### Android 7.0 ou inferior

1. Abra **Configurações** → **Segurança**
2. Ative a opção **Fontes desconhecidas**
3. Confirme o aviso de segurança

---

## Etapa 2 — Instalar o APK

### Via gerenciador de arquivos (método mais simples)

1. Transfira o arquivo `.apk` para o dispositivo (via cabo USB, Google Drive, e-mail, etc.)
2. Abra o **Gerenciador de arquivos** no dispositivo
3. Navegue até a pasta onde o APK foi salvo (geralmente **Downloads**)
4. Toque no arquivo `LicenseManager-<versão>-android.apk`
5. Toque em **Instalar** na tela de confirmação
6. Após a instalação, toque em **Abrir** ou encontre o app na gaveta de aplicativos

### Via ADB (para administradores de TI)

Com o dispositivo conectado via USB e depuração USB habilitada:

```bash
adb install LicenseManager-<versao>-android.apk
```

Para reinstalar preservando os dados:

```bash
adb install -r LicenseManager-<versao>-android.apk
```

---

## Atualização

Para atualizar para uma versão mais recente:

1. Baixe o novo APK
2. Siga os mesmos passos da instalação
3. O Android detectará que é uma atualização (mesmo `applicationId`) e preservará os dados do app
4. Não é necessário desinstalar a versão anterior

---

## Desinstalação

**Via configurações do Android:**

1. Abra **Configurações** → **Aplicativos**
2. Localize **LicenseManager**
3. Toque em **Desinstalar** → confirme

**Via ADB:**

```bash
adb uninstall com.licensemanager.app
```

---

## Solução de problemas

### "Análise bloqueada" ou "App pode ser prejudicial"

O Google Play Protect pode exibir um aviso ao instalar APKs fora da Play Store.
Isso é esperado para distribuição direta (sideload).

1. Toque em **Mais detalhes**
2. Toque em **Instalar mesmo assim**

Se preferir desabilitar temporariamente o Play Protect:

1. Abra o **Google Play Store**
2. Toque no ícone do seu perfil → **Play Protect**
3. Toque no ícone de engrenagem → desative **Verificar apps com o Play Protect**
4. Reinstale o APK
5. Reative o Play Protect após a instalação

### "Não instalado" após tentar instalar

Possíveis causas:

- **Espaço insuficiente:** libere pelo menos 200 MB e tente novamente
- **Versão incompatível:** verifique se o Android é 5.0 (API 21) ou superior em **Configurações → Sobre o telefone**
- **APK corrompido:** faça o download novamente

### "Aplicativo não instalado — certificado inválido"

Isso ocorre ao tentar instalar sobre uma versão assinada com outra keystore.
Desinstale a versão anterior antes de instalar:

```bash
adb uninstall com.licensemanager.app
adb install LicenseManager-<versao>-android.apk
```

> **Atenção:** a desinstalação remove os dados locais do app (tokens armazenados).
> Você precisará fazer login novamente após reinstalar.

### O app não consegue conectar à API

O app precisa de acesso à internet para comunicar com `https://licensemanager-api.enzojb.com.br`.
Verifique:
- Conexão Wi-Fi ou dados móveis ativa
- Firewall corporativo não está bloqueando HTTPS (porta 443)
- VPN ativa não está interferindo

---

## Sobre a assinatura do APK

O APK é assinado com uma keystore própria do projeto, gerada com `keytool` e armazenada
como GitHub Secret (`ANDROID_KEYSTORE_BASE64`). A assinatura garante a integridade do
arquivo — qualquer APK modificado após a assinatura será rejeitado pelo Android.

Para verificar a assinatura manualmente:

```bash
apksigner verify --verbose LicenseManager-<versao>-android.apk
```
