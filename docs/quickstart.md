# Guia de InÃ­cio RÃ¡pido

## 1. Criar conta

Acesse [licensemanager.enzojb.com.br](https://licensemanager.enzojb.com.br) e clique em **Criar conta**.

Preencha os dados da empresa (razÃ£o social, CPF/CNPJ, e-mail e telefone) e o cadastro do usuÃ¡rio administrador.

## 2. Criar uma aplicaÃ§Ã£o

ApÃ³s o login, acesse **AplicaÃ§Ãµes â†’ Nova AplicaÃ§Ã£o** e informe o nome do seu software.

## 3. Criar um tipo de licenÃ§a

Acesse **Tipos de LicenÃ§a â†’ Novo** e escolha o modelo:

| Tipo | DescriÃ§Ã£o |
|---|---|
| **Permanente** | Sem expiraÃ§Ã£o |
| **Por PerÃ­odo** | Expira em uma data definida |
| **Por UsuÃ¡rios** | Limita o nÃºmero de usuÃ¡rios simultÃ¢neos |
| **Por InstalaÃ§Ã£o** | Limita o nÃºmero de mÃ¡quinas registradas |

## 4. Emitir uma licenÃ§a

Acesse **LicenÃ§as â†’ Nova LicenÃ§a**, selecione o cliente final, a aplicaÃ§Ã£o e o tipo de licenÃ§a.

ApÃ³s salvar, o sistema gera um **token HMAC** que serÃ¡ usado pela aplicaÃ§Ã£o do cliente para validar a licenÃ§a.

## 5. Integrar o SDK

=== "C#"

    ```bash
    dotnet add package LicenseManagerSdk
    ```

    ```csharp
    var client = new LicenseManagerClient(
        baseUrl:   "https://licensemanager-api.enzojb.com.br",
        token:     "seu-token-da-licenca",
        licenseId: "guid-da-licenca"
    );

    var login = await client.LoginAsync("usuario@empresa.com");
    if (login.Authorized)
        await client.HeartbeatAsync(login.SessionId!);
    ```

=== "Python"

    ```bash
    pip install licensemanager-sdk
    ```

    ```python
    from licensemanager_sdk import LicenseManagerClient

    client = LicenseManagerClient(
        base_url   = "https://licensemanager-api.enzojb.com.br",
        token      = "seu-token",
        license_id = "guid-da-licenca"
    )
    login = client.login("usuario@empresa.com")
    if login.authorized:
        client.heartbeat(login.session_id)
    ```

=== "JavaScript"

    ```bash
    npm install licensemanager-sdk
    ```

    ```typescript
    import { LicenseManagerClient } from 'licensemanager-sdk';

    const client = new LicenseManagerClient({
      baseUrl:   'https://licensemanager-api.enzojb.com.br',
      token:     'seu-token',
      licenseId: 'guid-da-licenca',
    });
    const login = await client.login('usuario@empresa.com');
    ```

## 6. Ativar 2FA (recomendado)

No portal, acesse **Minha Conta â†’ AutenticaÃ§Ã£o de dois fatores** e configure o Google Authenticator ou Authy.
