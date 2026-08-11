# Guia de Início Rápido

## 1. Criar conta

Acesse [licensemanager.enzojb.com.br](https://licensemanager.enzojb.com.br) e clique em **Criar conta**.

Preencha os dados da empresa (razão social, CPF/CNPJ, e-mail e telefone) e o cadastro do usuário administrador.

## 2. Criar uma aplicação

Após o login, acesse **Aplicações → Nova Aplicação** e informe o nome do seu software.

## 3. Criar um tipo de licença

Acesse **Tipos de Licença → Novo** e escolha o modelo:

| Tipo | Descrição |
|---|---|
| **Permanente** | Sem expiração |
| **Por Período** | Expira em uma data definida |
| **Por Usuários** | Limita o número de usuários simultâneos |
| **Por Instalação** | Limita o número de máquinas registradas |

## 4. Emitir uma licença

Acesse **Licenças → Nova Licença**, selecione o cliente final, a aplicação e o tipo de licença.

Após salvar, o sistema gera um **token HMAC** que será usado pela aplicação do cliente para validar a licença.

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

No portal, acesse **Minha Conta → Autenticação de dois fatores** e configure o Google Authenticator ou Authy.
