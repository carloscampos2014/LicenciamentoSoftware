# SDK Python

[![PyPI](https://img.shields.io/pypi/v/licensemanager-sdk)](https://pypi.org/project/licensemanager-sdk/)

## Instalação

```bash
pip install licensemanager-sdk
```

## Requisitos

- Python 3.9+
- `requests` >= 2.31.0

## Uso básico

```python
from licensemanager_sdk import LicenseManagerClient, LicenseManagerException

client = LicenseManagerClient(
    base_url   = "https://licensemanager-api.enzojb.com.br",
    token      = "seu-token",
    license_id = "guid-da-licenca"
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
