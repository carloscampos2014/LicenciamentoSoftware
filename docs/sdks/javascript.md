# SDK JavaScript / TypeScript

[![npm](https://img.shields.io/npm/v/licensemanager-sdk)](https://www.npmjs.com/package/licensemanager-sdk)

## Instalação

```bash
npm install licensemanager-sdk
```

## Uso básico (TypeScript)

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
if (inst.authorized)
  console.log('Instalação:', inst.installationId);
```

## Compatibilidade

- Node.js 18+
- Browser (usa `fetch` nativo)
