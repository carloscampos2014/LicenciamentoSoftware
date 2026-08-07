# LicenseManagerSdk — Java / Kotlin

SDK cliente para a API de validação do LicenseManager.

## Maven

```xml
<dependency>
  <groupId>io.github.carloscampos2014</groupId>
  <artifactId>licensemanager-sdk</artifactId>
  <version>1.0.0</version>
</dependency>
```

## Gradle

```groovy
implementation 'io.github.carloscampos2014:licensemanager-sdk:1.0.0'
```

## Uso (Java)

```java
import io.licensemanager.sdk.LicenseManagerClient;
import io.licensemanager.sdk.LoginResult;
import io.licensemanager.sdk.InstallationResult;

LicenseManagerClient client = new LicenseManagerClient(
    "https://licensemanager-api.enzojb.com.br",
    "seu-token",
    "guid-da-licenca"
);

LoginResult login = client.login("usuario@empresa.com");
if (login.isAuthorized()) {
    client.heartbeat(login.getSessionId());
    client.logout(login.getSessionId());
}

InstallationResult inst = client.validateInstallation("MACHINE-001");
```

## Uso (Kotlin)

```kotlin
val client = LicenseManagerClient(
    baseUrl   = "https://licensemanager-api.enzojb.com.br",
    token     = "seu-token",
    licenseId = "guid-da-licenca"
)
val login = client.login("usuario@empresa.com")
```

## Testes

```bash
mvn test
```
