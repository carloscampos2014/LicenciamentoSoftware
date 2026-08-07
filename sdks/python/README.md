# LicenseManagerSdk — Python

SDK cliente para a API de validação do LicenseManager.

## Instalação

```bash
pip install licensemanager-sdk
```

## Uso

```python
from licensemanager_sdk import LicenseManagerClient, LicenseManagerException

client = LicenseManagerClient(
    base_url   = "https://licensemanager-api.enzojb.com.br",
    token      = "seu-token",
    license_id = "guid-da-licenca",
)

try:
    login = client.login("usuario@empresa.com")
    if login.authorized:
        client.heartbeat(login.session_id)
        client.logout(login.session_id)

    inst = client.validate_installation("MACHINE-001")
    if inst.authorized:
        print(f"Instalação: {inst.installation_id}")

except LicenseManagerException as e:
    print(f"Erro {e.status_code}: {e.response_body}")
```

## Requisitos

- Python 3.9+
- requests >= 2.31.0

## Testes

```bash
pip install -r requirements-dev.txt
pytest
```
