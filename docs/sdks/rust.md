# SDK Rust

[![crates.io](https://img.shields.io/crates/v/licensemanager-sdk)](https://crates.io/crates/licensemanager-sdk)

## Instalação

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
        if let Some(sid) = &login.session_id {
            client.heartbeat(sid).await?;
            client.logout(sid).await?;
        }
    }
    Ok(())
}
```
