# LicenseManagerSdk — VB6 / COM

DLL COM para integração com a API de validação do LicenseManager a partir de **VB6** e outras linguagens que suportam COM (Delphi, VBA, Access, Excel).

A DLL é escrita em C# e exposta via COM Interop. Não requer .NET Framework 4.x instalado no cliente — usa .NET 4.8 que já vem no Windows 10/11.

## Download

Baixe `LicenseManagerSdk.dll` e `LicenseManagerSdk.tlb` da aba **Releases**:
https://github.com/carloscampos2014/LicenciamentoSoftware/releases

## Instalação (registro da DLL)

Execute como Administrador:

```cmd
regasm /codebase /tlb LicenseManagerSdk.dll
```

Para desregistrar:

```cmd
regasm /unregister LicenseManagerSdk.dll
```

## Uso no VB6

```vb
' Adicione a referência: Project > References > LicenseManagerSdk
' Ou use CreateObject sem referência estática

Dim client As New LicenseManagerSdk.LicenseManagerClient

' Configurar (necessário antes de usar)
' O ProgID é: LicenseManagerSdk.LicenseManagerClient
Set client = CreateObject("LicenseManagerSdk.LicenseManagerClient")

' Login — retorna JSON string
Dim loginJson As String
loginJson = client.Login("usuario@empresa.com", _
                         "https://licensemanager-api.enzojb.com.br", _
                         "seu-token", _
                         "guid-da-licenca")

' Parsear o JSON com objetos VB6 ou via scripting:
Dim sc As Object
Set sc = CreateObject("MSScriptControl.ScriptControl")
sc.Language = "JScript"
sc.ExecuteStatement "var result = " & loginJson

If sc.Eval("result.autorizado") Then
    Dim sessionId As String
    sessionId = sc.Eval("result.idSessao")
    MsgBox "Autorizado! Sessão: " & sessionId

    ' Heartbeat
    client.Heartbeat sessionId, "https://licensemanager-api.enzojb.com.br", "seu-token", "guid-da-licenca"

    ' Logout
    client.Logout sessionId, "https://licensemanager-api.enzojb.com.br", "seu-token", "guid-da-licenca"
End If
```

## Uso no VBA (Excel/Access)

```vba
Sub ValidarLicenca()
    Dim client As Object
    Set client = CreateObject("LicenseManagerSdk.LicenseManagerClient")

    Dim resultado As String
    resultado = client.Login("usuario@empresa.com", _
                             "https://licensemanager-api.enzojb.com.br", _
                             "seu-token", _
                             "guid-da-licenca")
    MsgBox resultado
End Sub
```

## Compilar a DLL (desenvolvedores)

```bash
dotnet build sdks/vb6/LicenseManagerSdk/LicenseManagerSdk.csproj -c Release -f net48
```

## Testes

```bash
dotnet test sdks/vb6/LicenseManagerSdk.Tests --logger "console;verbosity=minimal"
```
