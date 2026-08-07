# licensemanager-sdk — JavaScript / TypeScript

SDK cliente para a API de validação do LicenseManager. Funciona em Node.js e browser.

## Instalação

```bash
npm install licensemanager-sdk
```

## Uso (TypeScript)

```typescript
import { LicenseManagerClient } from 'licensemanager-sdk';

const client = new LicenseManagerClient({
  baseUrl:   'https://licensemanager-api.enzojb.com.br',
  token:     'seu-token',
  licenseId: 'guid-da-licenca',
});

const login = await client.login('usuario@empresa.com');
if (login.authorized) {
  await client.heartbeat(login.sessionId!);
  await client.logout(login.sessionId!);
}

const inst = await client.validateInstallation('MACHINE-001');
if (inst.authorized) {
  console.log('Instalação:', inst.installationId);
}
```

## Uso (JavaScript ESM)

```js
import { LicenseManagerClient } from 'licensemanager-sdk';
```

## Uso (CommonJS)

```js
const { LicenseManagerClient } = require('licensemanager-sdk');
```

## Testes

```bash
npm test
```
