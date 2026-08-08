# SDK Delphi

Unit Pascal pura para Delphi 10.4 Sydney e superior. Sem dependências externas.

## Download

[Download LicenseManagerSdk.pas](https://github.com/carloscampos2014/LicenciamentoSoftware/releases/tag/sdk-delphi-v1.0.0)

## Instalação

Copie `LicenseManagerSdk.pas` para o diretório do seu projeto e adicione ao `uses`.

## Uso

```pascal
uses LicenseManagerSdk;

var
  Client: TLicenseManagerClient;
  Login: TLoginResult;
begin
  Client := TLicenseManagerClient.Create(
    'https://licensemanager-api.enzojb.com.br',
    'seu-token',
    'guid-da-licenca'
  );
  try
    Login := Client.Login('usuario@empresa.com');
    if Login.Authorized then
    begin
      Client.Heartbeat(Login.SessionId);
      Client.Logout(Login.SessionId);
    end;
  finally
    Client.Free;
  end;
end;
```
