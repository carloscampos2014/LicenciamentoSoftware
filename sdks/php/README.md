# LicenseManagerSdk — PHP

SDK cliente para a API de validação do LicenseManager. Compatível com PHP 7.4+.

## Instalação

```bash
composer require carloscampos2014/licensemanager-sdk
```

## Uso

```php
<?php
require 'vendor/autoload.php';

use LicenseManager\Sdk\LicenseManagerClient;
use LicenseManager\Sdk\LicenseManagerException;

$client = new LicenseManagerClient(
    baseUrl:   'https://licensemanager-api.enzojb.com.br',
    token:     'seu-token-de-licenca',
    licenseId: 'guid-da-licenca'
);

try {
    // Validar login
    $login = $client->login('usuario@empresa.com');
    if ($login->authorized) {
        echo "Sessão: {$login->sessionId}\n";

        // Heartbeat periódico
        $client->heartbeat($login->sessionId);

        // Logout
        $client->logout($login->sessionId);
    }

    // Validar instalação
    $inst = $client->validateInstallation(gethostname());
    if ($inst->authorized) {
        echo "Instalação: {$inst->installationId}\n";
    }

} catch (LicenseManagerException $e) {
    echo "Erro {$e->getStatusCode()}: {$e->getResponseBody()}\n";
}
```

## Uso com Laravel

```php
// config/services.php
'licensemanager' => [
    'url'        => env('LICENSE_MANAGER_URL'),
    'token'      => env('LICENSE_MANAGER_TOKEN'),
    'license_id' => env('LICENSE_MANAGER_LICENSE_ID'),
],

// AppServiceProvider.php
$this->app->singleton(LicenseManagerClient::class, fn() =>
    new LicenseManagerClient(
        config('services.licensemanager.url'),
        config('services.licensemanager.token'),
        config('services.licensemanager.license_id'),
    )
);
```

## Requisitos

- PHP 7.4+
- Extensões: `ext-curl`, `ext-json`

## Testes

```bash
composer install
composer test
```
