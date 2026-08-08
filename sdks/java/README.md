# LicenseManagerSdk — Java / Kotlin

SDK cliente para a API de validação do LicenseManager. Encapsula autenticação HMAC-SHA256 e os 4 endpoints de validação de licença.

## Instalação

### Maven

```xml
<dependency>
  <groupId>io.github.carloscampos2014</groupId>
  <artifactId>licensemanager-sdk</artifactId>
  <version>1.0.0</version>
</dependency>
```

### Gradle (Groovy)

```groovy
implementation 'io.github.carloscampos2014:licensemanager-sdk:1.0.0'
```

### Gradle (Kotlin DSL)

```kotlin
implementation("io.github.carloscampos2014:licensemanager-sdk:1.0.0")
```

---

## Uso (Java)

```java
import io.licensemanager.sdk.LicenseManagerClient;
import io.licensemanager.sdk.LoginResult;
import io.licensemanager.sdk.InstallationResult;
import io.licensemanager.sdk.LicenseManagerException;

public class App {
    public static void main(String[] args) {
        LicenseManagerClient client = new LicenseManagerClient(
            "https://licensemanager-api.enzojb.com.br",
            "seu-token-de-licenca",
            "guid-da-licenca"
        );

        try {
            // Validar login do usuário
            LoginResult login = client.login("usuario@empresa.com");
            if (login.isAuthorized()) {
                String sessionId = login.getSessionId();
                System.out.println("Login autorizado, sessão: " + sessionId);

                // Heartbeat periódico para manter a sessão ativa
                client.heartbeat(sessionId);

                // Logout ao encerrar
                client.logout(sessionId);
            }

            // Validar instalação na máquina
            InstallationResult inst = client.validateInstallation(
                System.getenv("COMPUTERNAME") // ou qualquer ID único da máquina
            );
            if (inst.isAuthorized()) {
                System.out.println("Instalação autorizada: " + inst.getInstallationId());
                System.out.println("Já registrada: " + inst.isAlreadyRegistered());
            }

        } catch (LicenseManagerException e) {
            System.err.println("Erro " + e.getStatusCode() + ": " + e.getResponseBody());
        }
    }
}
```

---

## Uso (Kotlin)

```kotlin
import io.licensemanager.sdk.LicenseManagerClient
import io.licensemanager.sdk.LicenseManagerException

fun main() {
    val client = LicenseManagerClient(
        baseUrl   = "https://licensemanager-api.enzojb.com.br",
        token     = "seu-token-de-licenca",
        licenseId = "guid-da-licenca"
    )

    try {
        // Login
        val login = client.login("usuario@empresa.com")
        if (login.isAuthorized) {
            val sessionId = login.sessionId!!
            println("Sessão: $sessionId")

            // Heartbeat
            client.heartbeat(sessionId)

            // Logout
            client.logout(sessionId)
        }

        // Validar instalação
        val inst = client.validateInstallation(
            System.getenv("COMPUTERNAME") ?: "MACHINE-001"
        )
        if (inst.isAuthorized) {
            println("Instalação: ${inst.installationId}")
        }

    } catch (e: LicenseManagerException) {
        System.err.println("Erro ${e.statusCode}: ${e.responseBody}")
    }
}
```

---

## Uso com Spring Boot

```java
@Configuration
public class LicenseManagerConfig {

    @Value("${licensemanager.url}")
    private String baseUrl;

    @Value("${licensemanager.token}")
    private String token;

    @Value("${licensemanager.license-id}")
    private String licenseId;

    @Bean
    public LicenseManagerClient licenseManagerClient() {
        return new LicenseManagerClient(baseUrl, token, licenseId);
    }
}

// application.properties
// licensemanager.url=https://licensemanager-api.enzojb.com.br
// licensemanager.token=seu-token
// licensemanager.license-id=guid-da-licenca
```

---

## Endpoints suportados

| Método | Endpoint | Descrição |
|---|---|---|
| `login(userId)` | `POST /api/validacao/login` | Valida login de um usuário |
| `heartbeat(sessionId)` | `POST /api/validacao/heartbeat` | Mantém a sessão ativa |
| `logout(sessionId)` | `POST /api/validacao/logout` | Encerra a sessão |
| `validateInstallation(machineId)` | `POST /api/validacao/instalacao` | Valida/registra instalação |

## Autenticação

Todos os endpoints usam **HMAC-SHA256** com os headers:

| Header | Descrição |
|---|---|
| `X-Token` | Token da licença |
| `X-Timestamp` | Timestamp UTC (ISO-8601) |
| `X-Nonce` | UUID único por requisição |
| `X-Signature` | HMAC-SHA256 de `{licenseId}:{timestamp}:{body}` |

O SDK gera esses headers automaticamente — você não precisa se preocupar com a assinatura.

## Requisitos

- Java 21+
- OkHttp 4.12.0
- Jackson 2.17.1

## Testes

```bash
mvn test
```
