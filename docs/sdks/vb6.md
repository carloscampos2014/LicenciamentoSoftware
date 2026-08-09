# SDK VB6 / COM

DLL COM para integraÃ§Ã£o a partir de **VB6, VBA, Delphi** e qualquer linguagem COM.

## Download

[Download DLL + TLB](https://github.com/carloscampos2014/LicenciamentoSoftware/releases/tag/sdk-vb6-v1.0.0)

## InstalaÃ§Ã£o

Execute como Administrador:

```cmd
regasm /codebase /tlb LicenseManagerSdk.dll
```

## Uso (VB6)

```vb
Set client = CreateObject("LicenseManagerSdk.LicenseManagerClient")

Dim json As String
json = client.Login("usuario@empresa.com")
```

## Uso (VBA â€” Excel/Access)

```vba
Sub ValidarLicenca()
    Dim client As Object
    Set client = CreateObject("LicenseManagerSdk.LicenseManagerClient")
    MsgBox client.Login("usuario@empresa.com")
End Sub
```

## Desregistrar

```cmd
regasm /unregister LicenseManagerSdk.dll
```
