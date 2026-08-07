"""Cliente principal do LicenseManager SDK."""
import hashlib
import hmac
import json
import time
import uuid
from datetime import datetime, timezone
from typing import Optional

import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

from .exceptions import LicenseManagerException
from .models import InstallationResult, LoginResult


class LicenseManagerClient:
    """Cliente para a API de validação do LicenseManager.

    Args:
        base_url:   URL base da API (ex: https://licensemanager-api.enzojb.com.br)
        token:      Token de autenticação da licença
        license_id: GUID da licença
        session:    requests.Session opcional (útil para testes)
    """

    def __init__(
        self,
        base_url: str,
        token: str,
        license_id: str,
        session: Optional[requests.Session] = None,
    ) -> None:
        if not base_url or not base_url.strip():
            raise ValueError("base_url é obrigatório")
        if not token or not token.strip():
            raise ValueError("token é obrigatório")
        if not license_id or not license_id.strip():
            raise ValueError("license_id é obrigatório")

        self._base_url   = base_url.rstrip("/")
        self._token      = token
        self._license_id = license_id

        if session is not None:
            self._session = session
        else:
            self._session = requests.Session()
            retry = Retry(
                total=3,
                backoff_factor=1,
                status_forcelist=[429, 500, 502, 503, 504],
                allowed_methods=["POST"],
            )
            adapter = HTTPAdapter(max_retries=retry)
            self._session.mount("http://", adapter)
            self._session.mount("https://", adapter)

    # -------------------------------------------------------------------------
    # Endpoints públicos
    # -------------------------------------------------------------------------

    def login(self, user_id: str) -> LoginResult:
        """Valida login de um usuário numa licença."""
        body = {"idLicenca": self._license_id, "identificadorUsuario": user_id}
        data = self._post("api/validacao/login", body)
        return LoginResult(
            authorized=data.get("autorizado", False),
            session_id=data.get("idSessao"),
        )

    def heartbeat(self, session_id: str) -> None:
        """Envia heartbeat para manter a sessão ativa."""
        body = {"idLicenca": self._license_id, "idSessao": session_id}
        self._post("api/validacao/heartbeat", body)

    def logout(self, session_id: str) -> None:
        """Encerra a sessão (idempotente)."""
        body = {"idLicenca": self._license_id, "idSessao": session_id}
        self._post("api/validacao/logout", body)

    def validate_installation(self, machine_id: str) -> InstallationResult:
        """Valida ou registra uma instalação da aplicação cliente."""
        body = {"idLicenca": self._license_id, "identificadorMaquina": machine_id}
        data = self._post("api/validacao/instalacao", body)
        return InstallationResult(
            authorized=data.get("autorizado", False),
            installation_id=data.get("idInstalacao"),
            already_registered=data.get("jaRegistrada", False),
        )

    # -------------------------------------------------------------------------
    # Infraestrutura HMAC
    # -------------------------------------------------------------------------

    def _post(self, path: str, body: dict) -> dict:
        body_json = json.dumps(body, separators=(",", ":"))
        timestamp = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
        nonce     = uuid.uuid4().hex
        signature = self._compute_signature(self._license_id, timestamp, body_json)

        headers = {
            "Content-Type": "application/json",
            "X-Token":      self._token,
            "X-Timestamp":  timestamp,
            "X-Nonce":      nonce,
            "X-Signature":  signature,
        }

        url = f"{self._base_url}/{path}"
        response = self._session.post(url, data=body_json, headers=headers, timeout=30)

        if not response.ok:
            raise LicenseManagerException(response.status_code, response.text)

        if response.status_code == 204 or not response.content:
            return {}
        return response.json()

    def _compute_signature(self, license_id: str, timestamp: str, body_json: str) -> str:
        payload   = f"{license_id}:{timestamp}:{body_json}"
        key       = self._token.encode("utf-8")
        data      = payload.encode("utf-8")
        signature = hmac.new(key, data, hashlib.sha256).hexdigest()
        return signature
