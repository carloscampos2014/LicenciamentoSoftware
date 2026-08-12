use chrono::Utc;
use hmac::{Hmac, Mac};
use reqwest::Client;
use serde::{Deserialize, Serialize};
use sha2::Sha256;
use std::time::Duration;
use thiserror::Error;
use uuid::Uuid;

type HmacSha256 = Hmac<Sha256>;

// -------------------------------------------------------------------------
// Erros
// -------------------------------------------------------------------------

#[derive(Debug, Error)]
pub enum LicenseManagerError {
    #[error("LicenseManager API error {status_code}: {body}")]
    ApiError { status_code: u16, body: String },

    #[error("Erro de rede: {0}")]
    NetworkError(#[from] reqwest::Error),

    #[error("Configuração inválida: {0}")]
    ConfigError(String),
}

// -------------------------------------------------------------------------
// Configuração
// -------------------------------------------------------------------------

pub struct LicenseManagerConfig {
    pub base_url:     String,
    pub token:        String,
    pub license_id:   String,
    /// Timeout em segundos (padrão: 30)
    pub timeout_secs: Option<u64>,
}

// -------------------------------------------------------------------------
// Modelos de resposta
// -------------------------------------------------------------------------

#[derive(Debug, Deserialize)]
pub struct LoginResult {
    #[serde(rename = "autorizado")]
    pub authorized: bool,
    #[serde(rename = "idSessao")]
    pub session_id: Option<String>,
}

#[derive(Debug, Deserialize)]
pub struct InstallationResult {
    #[serde(rename = "autorizado")]
    pub authorized: bool,
    #[serde(rename = "idInstalacao")]
    pub installation_id: Option<String>,
    #[serde(rename = "jaRegistrada")]
    pub already_registered: bool,
}

// -------------------------------------------------------------------------
// Cliente
// -------------------------------------------------------------------------

pub struct LicenseManagerClient {
    http:       Client,
    base_url:   String,
    token:      String,
    license_id: String,
}

impl LicenseManagerClient {
    pub fn new(config: LicenseManagerConfig) -> Result<Self, LicenseManagerError> {
        if config.base_url.trim().is_empty() {
            return Err(LicenseManagerError::ConfigError("base_url é obrigatório".into()));
        }
        if config.token.trim().is_empty() {
            return Err(LicenseManagerError::ConfigError("token é obrigatório".into()));
        }
        if config.license_id.trim().is_empty() {
            return Err(LicenseManagerError::ConfigError("license_id é obrigatório".into()));
        }

        let timeout = Duration::from_secs(config.timeout_secs.unwrap_or(30));
        let http    = Client::builder().timeout(timeout).build()?;

        Ok(Self {
            http,
            base_url:   config.base_url.trim_end_matches('/').to_string(),
            token:      config.token,
            license_id: config.license_id,
        })
    }

    // -------------------------------------------------------------------------
    // Endpoints públicos
    // -------------------------------------------------------------------------

    pub async fn login(&self, user_id: &str) -> Result<LoginResult, LicenseManagerError> {
        let body = serde_json::json!({
            "idLicenca": self.license_id,
            "identificadorUsuario": user_id
        });
        let resp = self.post("api/validacao/login", &body).await?;
        Ok(serde_json::from_str(&resp).map_err(|e| {
            LicenseManagerError::ApiError { status_code: 0, body: e.to_string() }
        })?)
    }

    pub async fn heartbeat(&self, session_id: &str) -> Result<(), LicenseManagerError> {
        let body = serde_json::json!({
            "idLicenca": self.license_id,
            "idSessao":  session_id
        });
        self.post("api/validacao/heartbeat", &body).await?;
        Ok(())
    }

    pub async fn logout(&self, session_id: &str) -> Result<(), LicenseManagerError> {
        let body = serde_json::json!({
            "idLicenca": self.license_id,
            "idSessao":  session_id
        });
        self.post("api/validacao/logout", &body).await?;
        Ok(())
    }

    pub async fn validate_installation(
        &self,
        machine_id: &str,
    ) -> Result<InstallationResult, LicenseManagerError> {
        let body = serde_json::json!({
            "idLicenca":           self.license_id,
            "identificadorMaquina": machine_id
        });
        let resp = self.post("api/validacao/instalacao", &body).await?;
        Ok(serde_json::from_str(&resp).map_err(|e| {
            LicenseManagerError::ApiError { status_code: 0, body: e.to_string() }
        })?)
    }

    // -------------------------------------------------------------------------
    // Infraestrutura HMAC
    // -------------------------------------------------------------------------

    async fn post(
        &self,
        path: &str,
        body: &serde_json::Value,
    ) -> Result<String, LicenseManagerError> {
        let body_json = serde_json::to_string(body).unwrap();
        let timestamp = Utc::now().format("%Y-%m-%dT%H:%M:%SZ").to_string();
        let nonce     = Uuid::new_v4().to_string().replace('-', "");
        let signature = self.compute_signature(&self.license_id, &timestamp, &body_json);

        let url = format!("{}/{}", self.base_url, path);

        let mut last_err = None;
        for attempt in 1u32..=3 {
            let response = self
                .http
                .post(&url)
                .header("Content-Type",  "application/json")
                .header("X-Token",       &self.token)
                .header("X-Timestamp",   &timestamp)
                .header("X-Nonce",       &nonce)
                .header("X-Signature",   &signature)
                .body(body_json.clone())
                .send()
                .await;

            match response {
                Err(e) => {
                    last_err = Some(LicenseManagerError::NetworkError(e));
                    if attempt < 3 {
                        tokio::time::sleep(Duration::from_secs(2u64.pow(attempt))).await;
                        continue;
                    }
                }
                Ok(resp) => {
                    let status = resp.status().as_u16();
                    if (status == 429 || status >= 500) && attempt < 3 {
                        tokio::time::sleep(Duration::from_secs(2u64.pow(attempt))).await;
                        continue;
                    }
                    if !resp.status().is_success() {
                        let body = resp.text().await.unwrap_or_default();
                        return Err(LicenseManagerError::ApiError { status_code: status, body });
                    }
                    return Ok(resp.text().await.unwrap_or_default());
                }
            }
        }
        Err(last_err.unwrap())
    }

    pub fn compute_signature(&self, license_id: &str, timestamp: &str, body_json: &str) -> String {
        // Normaliza para lowercase com hífens — igual ao servidor (idLicenca:D)
        let normalized_id = Uuid::parse_str(license_id)
            .map(|u| u.to_string())
            .unwrap_or_else(|_| license_id.to_string());
        let payload = format!("{normalized_id}:{timestamp}:{body_json}");
        let mut mac = HmacSha256::new_from_slice(self.token.as_bytes())
            .expect("HMAC aceita qualquer tamanho de chave");
        mac.update(payload.as_bytes());
        hex::encode(mac.finalize().into_bytes())
    }
}

// -------------------------------------------------------------------------
// Testes
// -------------------------------------------------------------------------

#[cfg(test)]
mod tests {
    use super::*;
    use wiremock::matchers::{method, path};
    use wiremock::{Mock, MockServer, ResponseTemplate};

    fn make_client(base_url: &str) -> LicenseManagerClient {
        LicenseManagerClient::new(LicenseManagerConfig {
            base_url:     base_url.into(),
            token:        "test-secret".into(),
            license_id:   "lic-123".into(),
            timeout_secs: None,
        })
        .unwrap()
    }

    #[test]
    fn compute_signature_mesmo_input_retorna_mesmo_hash() {
        let client = make_client("https://api.example.com");
        let s1 = client.compute_signature("lic", "2026-01-01T00:00:00Z", "{}");
        let s2 = client.compute_signature("lic", "2026-01-01T00:00:00Z", "{}");
        assert_eq!(s1, s2);
    }

    #[test]
    fn compute_signature_input_diferente_retorna_hash_diferente() {
        let client = make_client("https://api.example.com");
        let s1 = client.compute_signature("lic", "2026-01-01T00:00:00Z", "{}");
        let s2 = client.compute_signature("lic", "2026-01-01T00:00:01Z", "{}");
        assert_ne!(s1, s2);
    }

    #[test]
    fn compute_signature_resultado_e_hex_64_chars() {
        let client = make_client("https://api.example.com");
        let sig = client.compute_signature("lic", "2026-01-01T00:00:00Z", "{}");
        assert_eq!(sig.len(), 64);
        assert!(sig.chars().all(|c| c.is_ascii_hexdigit() && !c.is_uppercase()));
    }

    #[test]
    fn new_base_url_vazia_retorna_erro() {
        let result = LicenseManagerClient::new(LicenseManagerConfig {
            base_url: "".into(), token: "tok".into(), license_id: "lic".into(), timeout_secs: None,
        });
        assert!(result.is_err());
    }

    #[test]
    fn new_token_vazio_retorna_erro() {
        let result = LicenseManagerClient::new(LicenseManagerConfig {
            base_url: "https://api.test".into(), token: "".into(), license_id: "lic".into(), timeout_secs: None,
        });
        assert!(result.is_err());
    }

    #[tokio::test]
    async fn login_resposta_autorizada_retorna_session_id() {
        let server = MockServer::start().await;
        Mock::given(method("POST"))
            .and(path("/api/validacao/login"))
            .respond_with(ResponseTemplate::new(200)
                .set_body_json(serde_json::json!({"autorizado": true, "idSessao": "sess-1"})))
            .mount(&server)
            .await;

        let client = make_client(&server.uri());
        let result = client.login("user@test.com").await.unwrap();
        assert!(result.authorized);
        assert_eq!(result.session_id.as_deref(), Some("sess-1"));
    }

    #[tokio::test]
    async fn login_erro_401_retorna_api_error() {
        let server = MockServer::start().await;
        Mock::given(method("POST"))
            .and(path("/api/validacao/login"))
            .respond_with(ResponseTemplate::new(401)
                .set_body_json(serde_json::json!({"erro": "Token inválido"})))
            .mount(&server)
            .await;

        let client = make_client(&server.uri());
        let result = client.login("user@test.com").await;
        assert!(matches!(result, Err(LicenseManagerError::ApiError { status_code: 401, .. })));
    }

    #[tokio::test]
    async fn validate_installation_retorna_installation_id() {
        let server = MockServer::start().await;
        Mock::given(method("POST"))
            .and(path("/api/validacao/instalacao"))
            .respond_with(ResponseTemplate::new(200)
                .set_body_json(serde_json::json!({
                    "autorizado": true,
                    "idInstalacao": "inst-42",
                    "jaRegistrada": false
                })))
            .mount(&server)
            .await;

        let client = make_client(&server.uri());
        let result = client.validate_installation("MACHINE-001").await.unwrap();
        assert!(result.authorized);
        assert_eq!(result.installation_id.as_deref(), Some("inst-42"));
        assert!(!result.already_registered);
    }
}
