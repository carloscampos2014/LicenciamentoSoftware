# SDK PHP

## Instalação

```bash
composer require carloscampos2014/licensemanager-sdk
```

## Requisitos

- PHP 8.1+
- `ext-curl`, `ext-json`

## Uso

```php
<?php
require 'vendor/autoload.php';

use LicenseManager\Sdk\LicenseManagerClient;
use LicenseManager\Sdk\LicenseManagerException;

$client = new LicenseManagerClient(
    baseUrl:   'https://licensemanager-api.enzojb.com.br',
    token:     'seu-token',
    licenseId: 'guid-da-licenca'
);

try {
    $login = $client->login('usuario@empresa.com');
    if ($login->authorized) {
        $client->heartbeat($login->sessionId);
        $client->logout($login->sessionId);
    }

    $inst = $client->validateInstallation(gethostname());
    if ($inst->authorized)
        echo "Instalação: {$inst->installationId}";

} catch (LicenseManagerException $e) {
    echo "Erro {$e->getStatusCode()}: {$e->getResponseBody()}";
}
```
