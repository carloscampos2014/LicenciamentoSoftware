# licensemanager-sdk — Rust

SDK cliente para a API de validação do LicenseManager.

## Cargo.toml

```toml
[dependencies]
licensemanager-sdk = "1.0"
tokio = { version = "1", features = ["full"] }
```

## Uso

```rust
use licensemanager_sdk::{LicenseManagerClient, LicenseManagerConfig};

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let client = LicenseManagerClient::new(LicenseManagerConfig {
        base_url:   "https://licensemanager-api.enzojb.com.br".into(),
        token:      "seu-token".into(),
        license_id: "guid-da-licenca".into(),
        timeout_secs: None,
    })?;

    let login = client.login("usuario@empresa.com").await?;
    if login.authorized {
        if let Some(session_id) = &login.session_id {
            client.heartbeat(session_id).await?;
            client.logout(session_id).await?;
        }
    }

    let inst = client.validate_installation("MACHINE-001").await?;
    if inst.authorized {
        println!("Instalação: {:?}", inst.installation_id);
    }

    Ok(())
}
```

## Testes

```bash
cargo test
```
