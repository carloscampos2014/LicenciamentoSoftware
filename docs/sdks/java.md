# SDK Java / Kotlin

[![Maven Central](https://img.shields.io/maven-central/v/io.github.carloscampos2014/licensemanager-sdk)](https://central.sonatype.com/artifact/io.github.carloscampos2014/licensemanager-sdk)

## Instalação

=== "Maven"
    ```xml
    <dependency>
      <groupId>io.github.carloscampos2014</groupId>
      <artifactId>licensemanager-sdk</artifactId>
      <version>1.0.1</version>
    </dependency>
    ```

=== "Gradle"
    ```groovy
    implementation 'io.github.carloscampos2014:licensemanager-sdk:1.0.1'
    ```

## Uso (Java)

```java
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
```

## Uso (Kotlin)

```kotlin
val client = LicenseManagerClient(
    baseUrl   = "https://licensemanager-api.enzojb.com.br",
    token     = "seu-token",
    licenseId = "guid-da-licenca"
)
val login = client.login("usuario@empresa.com")
if (login.isAuthorized) client.heartbeat(login.sessionId)
```
