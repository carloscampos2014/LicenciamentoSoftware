# SDKs de Validação

O LicenseManager oferece SDKs oficiais para 9 linguagens/plataformas.

## Instalação rápida

| Linguagem | Comando | Versão |
|---|---|---|
| C# / .NET | `dotnet add package LicenseManagerSdk` | 1.0.1 |
| Java / Kotlin | Maven: `io.github.carloscampos2014:licensemanager-sdk:1.0.1` | 1.0.1 |
| Python | `pip install licensemanager-sdk` | 1.0.0 |
| JavaScript/TS | `npm install licensemanager-sdk` | 1.0.0 |
| Rust | `cargo add licensemanager-sdk` | 1.0.0 |
| Ruby | `gem install licensemanager-sdk` | 1.0.0 |
| PHP | `composer require carloscampos2014/licensemanager-sdk` | 1.0.0 |
| Delphi | [Download .pas](https://github.com/carloscampos2014/LicenciamentoSoftware/releases/tag/sdk-delphi-v1.0.0) | 1.0.0 |
| VB6/COM | [Download DLL](https://github.com/carloscampos2014/LicenciamentoSoftware/releases/tag/sdk-vb6-v1.0.0) | 1.0.0 |

## O que todos os SDKs implementam

- Geração automática de HMAC-SHA256 (headers `X-Token`, `X-Timestamp`, `X-Nonce`, `X-Signature`)
- 4 endpoints: `login`, `heartbeat`, `logout`, `validateInstallation`
- Retry automático (3 tentativas, backoff exponencial) em erros de rede e 5xx
- Modelos de resposta tipados
