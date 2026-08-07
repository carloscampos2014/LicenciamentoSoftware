"""Testes do LicenseManager SDK Python."""
import hashlib
import hmac
import json

import pytest
import responses

from licensemanager_sdk import LicenseManagerClient, LicenseManagerException


BASE_URL   = "https://api.example.com"
TOKEN      = "test-secret"
LICENSE_ID = "lic-123"


@pytest.fixture
def client():
    return LicenseManagerClient(BASE_URL, TOKEN, LICENSE_ID)


# -------------------------------------------------------------------------
# Testes de HMAC
# -------------------------------------------------------------------------

class TestHmac:
    def test_mesmo_input_retorna_mesmo_hash(self, client):
        s1 = client._compute_signature("lic", "2026-01-01T00:00:00Z", "{}")
        s2 = client._compute_signature("lic", "2026-01-01T00:00:00Z", "{}")
        assert s1 == s2

    def test_input_diferente_retorna_hash_diferente(self, client):
        s1 = client._compute_signature("lic", "2026-01-01T00:00:00Z", "{}")
        s2 = client._compute_signature("lic", "2026-01-01T00:00:01Z", "{}")
        assert s1 != s2

    def test_resultado_e_hex_lowercase_64_chars(self, client):
        sig = client._compute_signature("lic", "2026-01-01T00:00:00Z", "{}")
        assert len(sig) == 64
        assert sig == sig.lower()
        assert all(c in "0123456789abcdef" for c in sig)

    def test_valor_conhecido_bate_com_calculo_manual(self, client):
        license_id = "abc-123"
        timestamp  = "2026-08-06T12:00:00Z"
        body       = '{"idLicenca":"abc-123"}'
        payload    = f"{license_id}:{timestamp}:{body}"
        expected   = hmac.new(TOKEN.encode(), payload.encode(), hashlib.sha256).hexdigest()

        c = LicenseManagerClient(BASE_URL, TOKEN, license_id)
        assert c._compute_signature(license_id, timestamp, body) == expected


# -------------------------------------------------------------------------
# Testes dos endpoints
# -------------------------------------------------------------------------

class TestLogin:
    @responses.activate
    def test_resposta_autorizada_retorna_session_id(self, client):
        responses.add(responses.POST, f"{BASE_URL}/api/validacao/login",
                      json={"autorizado": True, "idSessao": "sess-1"}, status=200)

        result = client.login("user@test.com")
        assert result.authorized is True
        assert result.session_id == "sess-1"

    @responses.activate
    def test_erro_401_lanca_exception(self, client):
        responses.add(responses.POST, f"{BASE_URL}/api/validacao/login",
                      json={"erro": "Token inválido"}, status=401)

        with pytest.raises(LicenseManagerException) as exc_info:
            client.login("user@test.com")
        assert exc_info.value.status_code == 401


class TestHeartbeat:
    @responses.activate
    def test_resposta_204_nao_lanca_excecao(self, client):
        responses.add(responses.POST, f"{BASE_URL}/api/validacao/heartbeat", status=204)
        client.heartbeat("sess-1")  # não deve lançar


class TestLogout:
    @responses.activate
    def test_resposta_204_nao_lanca_excecao(self, client):
        responses.add(responses.POST, f"{BASE_URL}/api/validacao/logout", status=204)
        client.logout("sess-1")  # não deve lançar


class TestValidateInstallation:
    @responses.activate
    def test_resposta_autorizada_retorna_installation_id(self, client):
        responses.add(responses.POST, f"{BASE_URL}/api/validacao/instalacao",
                      json={"autorizado": True, "idInstalacao": "inst-42", "jaRegistrada": False},
                      status=200)

        result = client.validate_installation("MACHINE-001")
        assert result.authorized is True
        assert result.installation_id == "inst-42"
        assert result.already_registered is False


class TestConstructor:
    def test_base_url_vazia_lanca_valueerror(self):
        with pytest.raises(ValueError):
            LicenseManagerClient("", TOKEN, LICENSE_ID)

    def test_token_vazio_lanca_valueerror(self):
        with pytest.raises(ValueError):
            LicenseManagerClient(BASE_URL, "", LICENSE_ID)

    def test_license_id_vazio_lanca_valueerror(self):
        with pytest.raises(ValueError):
            LicenseManagerClient(BASE_URL, TOKEN, "")
