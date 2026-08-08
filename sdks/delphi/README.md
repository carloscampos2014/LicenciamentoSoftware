# LicenseManagerSdk — Delphi

SDK cliente para a API de validação do LicenseManager.
Compatível com **Delphi 10.4 Sydney** e superior.
Sem dependências externas — usa apenas a RTL padrão.

## Instalação

Copie o arquivo `LicenseManagerSdk.pas` para o diretório do seu projeto
e adicione ao `uses` da sua unit.

## Uso

```pascal
uses
  LicenseManagerSdk;

procedure TForm1.BtnValidarClick(Sender: TObject);
var
  Client: TLicenseManagerClient;
  Login: TLoginResult;
  Inst: TInstallationResult;
begin
  Client := TLicenseManagerClient.Create(
    'https://licensemanager-api.enzojb.com.br',
    'seu-token-de-licenca',
    'guid-da-licenca'
  );
  try
    // Validar login
    Login := Client.Login('usuario@empresa.com');
    if Login.Authorized then
    begin
      ShowMessage('Login autorizado! Sessão: ' + Login.SessionId);

      // Heartbeat periódico (chamar a cada N minutos)
      Client.Heartbeat(Login.SessionId);

      // Logout ao fechar o app
      Client.Logout(Login.SessionId);
    end
    else
      ShowMessage('Login não autorizado.');

    // Validar instalação
    Inst := Client.ValidateInstallation(GetEnvironmentVariable('COMPUTERNAME'));
    if Inst.Authorized then
      ShowMessage('Instalação autorizada: ' + Inst.InstallationId);

  except
    on E: ELicenseManagerException do
      ShowMessage(Format('Erro %d: %s', [E.StatusCode, E.ResponseBody]));
  end;
  Client.Free;
end;
```

## Uso em serviço Windows (sem VCL)

```pascal
uses
  LicenseManagerSdk, System.SysUtils;

var
  Client: TLicenseManagerClient;
  Login: TLoginResult;
begin
  Client := TLicenseManagerClient.Create(
    'https://licensemanager-api.enzojb.com.br',
    GetEnvironmentVariable('LICENSE_TOKEN'),
    GetEnvironmentVariable('LICENSE_ID')
  );
  try
    Login := Client.Login('service-account');
    if Login.Authorized then
      Writeln('Sessão: ' + Login.SessionId);
  finally
    Client.Free;
  end;
end.
```

## Requisitos

- Delphi 10.4 Sydney ou superior
- Windows (usa `System.Net.HttpClient` que é multiplataforma, mas o SDK foi validado no Windows)
- Sem dependências NuGet/GetIt

## Distribuição

O SDK é uma única unit Pascal (`LicenseManagerSdk.pas`).
Baixe diretamente de:
https://github.com/carloscampos2014/LicenciamentoSoftware/releases
