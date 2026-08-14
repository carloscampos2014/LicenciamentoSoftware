"""Cliente principal do LicenseManager SDK."""
import hashlib
import hmac
import json
import time
import uuid
from datetime import datetime, timezone
from typing import Optional

import requests

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
        # Normaliza GUID para lowercase com hífens — igual ao servidor (idLicenca:D)
        try:
            from uuid import UUID
            self._license_id = str(UUID(license_id))
        except ValueError:
            self._license_id = license_id

        if session is not None:
            self._session = session
        else:
            self._session = requests.Session()

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
        url = f"{self._base_url}/{path}"

        for attempt in range(1, 4):
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

            try:
                response = self._session.post(url, data=body_json, headers=headers, timeout=30)
            except requests.RequestException:
                if attempt < 3:
                    time.sleep(2 ** attempt)
                    continue
                raise

            if (response.status_code == 429 or response.status_code >= 500) and attempt < 3:
                time.sleep(2 ** attempt)
                continue

            if not response.ok:
                raise LicenseManagerException(response.status_code, response.text)

            if response.status_code == 204 or not response.content:
                return {}
            return response.json()

        raise LicenseManagerException(0, "Número máximo de tentativas excedido.")

    def _compute_signature(self, license_id: str, timestamp: str, body_json: str) -> str:
        # Normaliza para lowercase com hífens — igual ao servidor (idLicenca:D)
        try:
            from uuid import UUID
            normalized_id = str(UUID(license_id))
        except ValueError:
            normalized_id = license_id
        payload   = f"{normalized_id}:{timestamp}:{body_json}"
        key       = self._token.encode("utf-8")
        data      = payload.encode("utf-8")
        signature = hmac.new(key, data, hashlib.sha256).hexdigest()
        return signature
