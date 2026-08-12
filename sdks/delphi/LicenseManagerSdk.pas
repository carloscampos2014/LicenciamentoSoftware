unit LicenseManagerSdk;

{
  LicenseManager SDK para Delphi
  Compatível com: Delphi 10.4 Sydney e superior
  Dependências: apenas RTL padrão (System.Net.HttpClient, System.Hash, System.JSON)

  Uso:
    var Client := TLicenseManagerClient.Create(
      'https://licensemanager-api.enzojb.com.br',
      'seu-token',
      'guid-da-licenca'
    );
    try
      var Login := Client.Login('usuario@empresa.com');
      if Login.Authorized then
      begin
        Client.Heartbeat(Login.SessionId);
        Client.Logout(Login.SessionId);
      end;
    finally
      Client.Free;
    end;
}

interface

uses
  System.SysUtils,
  System.Classes,
  System.Net.HttpClient,
  System.Net.URLClient,
  System.Hash,
  System.JSON,
  System.DateUtils;

type
  ELicenseManagerException = class(Exception)
  private
    FStatusCode: Integer;
    FResponseBody: string;
  public
    constructor Create(AStatusCode: Integer; const AResponseBody: string);
    property StatusCode: Integer read FStatusCode;
    property ResponseBody: string read FResponseBody;
  end;

  TLoginResult = record
    Authorized: Boolean;
    SessionId: string;
  end;

  TInstallationResult = record
    Authorized: Boolean;
    InstallationId: string;
    AlreadyRegistered: Boolean;
  end;

  TLicenseManagerClient = class
  private
    FBaseUrl: string;
    FToken: string;
    FLicenseId: string;
    FHttpClient: THTTPClient;

    function ComputeSignature(const ALicenseId, ATimestamp, ABodyJson: string): string;
    function Post(const APath: string; const ABody: TJSONObject): TJSONObject;
    function NowUTCIso8601: string;
    function NewNonce: string;
  public
    constructor Create(const ABaseUrl, AToken, ALicenseId: string);
    destructor Destroy; override;

    function Login(const AUserId: string): TLoginResult;
    procedure Heartbeat(const ASessionId: string);
    procedure Logout(const ASessionId: string);
    function ValidateInstallation(const AMachineId: string): TInstallationResult;
  end;

implementation

uses
  System.NetEncoding,
  System.SyncObjs;

{ ELicenseManagerException }

constructor ELicenseManagerException.Create(AStatusCode: Integer; const AResponseBody: string);
begin
  inherited CreateFmt('LicenseManager API error %d: %s', [AStatusCode, AResponseBody]);
  FStatusCode   := AStatusCode;
  FResponseBody := AResponseBody;
end;

{ TLicenseManagerClient }

constructor TLicenseManagerClient.Create(const ABaseUrl, AToken, ALicenseId: string);
begin
  if ABaseUrl.IsEmpty then
    raise EArgumentException.Create('ABaseUrl é obrigatório');
  if AToken.IsEmpty then
    raise EArgumentException.Create('AToken é obrigatório');
  if ALicenseId.IsEmpty then
    raise EArgumentException.Create('ALicenseId é obrigatório');

  inherited Create;
  FBaseUrl   := ABaseUrl.TrimRight(['/']);
  FToken     := AToken;
  FLicenseId := ALicenseId;

  FHttpClient := THTTPClient.Create;
  FHttpClient.ConnectionTimeout := 30000;
  FHttpClient.ResponseTimeout   := 30000;
end;

destructor TLicenseManagerClient.Destroy;
begin
  FHttpClient.Free;
  inherited;
end;

function TLicenseManagerClient.NowUTCIso8601: string;
var
  Now: TDateTime;
begin
  Now := TTimeZone.Local.ToUniversalTime(System.SysUtils.Now);
  Result := FormatDateTime('yyyy-mm-dd"T"hh:nn:ss"Z"', Now);
end;

function TLicenseManagerClient.NewNonce: string;
var
  Guid: TGUID;
begin
  CreateGUID(Guid);
  Result := GUIDToString(Guid).Replace('{', '').Replace('}', '').Replace('-', '');
end;

function TLicenseManagerClient.ComputeSignature(
  const ALicenseId, ATimestamp, ABodyJson: string): string;
var
  Payload: string;
  KeyBytes, DataBytes, HashBytes: TBytes;
  I: Integer;
  HexResult: string;
  NormalizedId: string;
begin
  // Normaliza GUID para lowercase com hífens — igual ao servidor (idLicenca:D)
  NormalizedId := ALicenseId.ToLower;

  Payload   := NormalizedId + ':' + ATimestamp + ':' + ABodyJson;
  KeyBytes  := TEncoding.UTF8.GetBytes(FToken);
  DataBytes := TEncoding.UTF8.GetBytes(Payload);

  // HMAC-SHA256 correto usando THashSHA2 com bytes
  HashBytes := THashSHA2.GetHMACAsBytes(DataBytes, KeyBytes, SHA256);

  // Converte para hex lowercase
  HexResult := '';
  for I := 0 to High(HashBytes) do
    HexResult := HexResult + IntToHex(HashBytes[I], 2).ToLower;

  Result := HexResult;
end;

function TLicenseManagerClient.Post(
  const APath: string; const ABody: TJSONObject): TJSONObject;
var
  BodyJson, Timestamp, Nonce, Signature, Url: string;
  RequestBody: TStringStream;
  Response: IHTTPResponse;
  Attempt: Integer;
begin
  BodyJson  := ABody.ToJSON;
  Timestamp := NowUTCIso8601;
  Nonce     := NewNonce;
  Signature := ComputeSignature(FLicenseId, Timestamp, BodyJson);
  Url       := FBaseUrl + '/' + APath;

  for Attempt := 1 to 3 do
  begin
    RequestBody := TStringStream.Create(BodyJson, TEncoding.UTF8);
    try
      FHttpClient.CustomHeaders['X-Token']     := FToken;
      FHttpClient.CustomHeaders['X-Timestamp'] := Timestamp;
      FHttpClient.CustomHeaders['X-Nonce']     := Nonce;
      FHttpClient.CustomHeaders['X-Signature'] := Signature;
      FHttpClient.CustomHeaders['Content-Type']:= 'application/json';

      try
        Response := FHttpClient.Post(Url, RequestBody);
      except
        on E: Exception do
        begin
          if Attempt < 3 then
          begin
            Sleep(Round(Power(2, Attempt)) * 1000);
            Continue;
          end;
          raise;
        end;
      end;

      if (Response.StatusCode = 429) or (Response.StatusCode >= 500) then
      begin
        if Attempt < 3 then
        begin
          Sleep(Round(Power(2, Attempt)) * 1000);
          Continue;
        end;
      end;

      if not (Response.StatusCode in [200, 204]) then
        raise ELicenseManagerException.Create(Response.StatusCode, Response.ContentAsString);

      if Response.StatusCode = 204 then
        Exit(TJSONObject.Create);

      Result := TJSONObject.ParseJSONValue(Response.ContentAsString) as TJSONObject;
      Exit;
    finally
      RequestBody.Free;
    end;
  end;
end;

function TLicenseManagerClient.Login(const AUserId: string): TLoginResult;
var
  Body, Response: TJSONObject;
begin
  Body := TJSONObject.Create;
  try
    Body.AddPair('idLicenca', FLicenseId);
    Body.AddPair('identificadorUsuario', AUserId);
    Response := Post('api/validacao/login', Body);
    try
      Result.Authorized := Response.GetValue<Boolean>('autorizado', False);
      Result.SessionId  := Response.GetValue<string>('idSessao', '');
    finally
      Response.Free;
    end;
  finally
    Body.Free;
  end;
end;

procedure TLicenseManagerClient.Heartbeat(const ASessionId: string);
var
  Body, Response: TJSONObject;
begin
  Body := TJSONObject.Create;
  try
    Body.AddPair('idLicenca', FLicenseId);
    Body.AddPair('idSessao', ASessionId);
    Response := Post('api/validacao/heartbeat', Body);
    Response.Free;
  finally
    Body.Free;
  end;
end;

procedure TLicenseManagerClient.Logout(const ASessionId: string);
var
  Body, Response: TJSONObject;
begin
  Body := TJSONObject.Create;
  try
    Body.AddPair('idLicenca', FLicenseId);
    Body.AddPair('idSessao', ASessionId);
    Response := Post('api/validacao/logout', Body);
    Response.Free;
  finally
    Body.Free;
  end;
end;

function TLicenseManagerClient.ValidateInstallation(
  const AMachineId: string): TInstallationResult;
var
  Body, Response: TJSONObject;
begin
  Body := TJSONObject.Create;
  try
    Body.AddPair('idLicenca', FLicenseId);
    Body.AddPair('identificadorMaquina', AMachineId);
    Response := Post('api/validacao/instalacao', Body);
    try
      Result.Authorized        := Response.GetValue<Boolean>('autorizado', False);
      Result.InstallationId    := Response.GetValue<string>('idInstalacao', '');
      Result.AlreadyRegistered := Response.GetValue<Boolean>('jaRegistrada', False);
    finally
      Response.Free;
    end;
  finally
    Body.Free;
  end;
end;

end.
