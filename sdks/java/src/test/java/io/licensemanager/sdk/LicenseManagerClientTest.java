package io.licensemanager.sdk;

import mockwebserver3.MockResponse;
import mockwebserver3.MockWebServer;
import org.junit.jupiter.api.*;

import static org.junit.jupiter.api.Assertions.*;

class LicenseManagerClientTest {

    private MockWebServer server;
    private LicenseManagerClient client;

    @BeforeEach
    void setUp() throws Exception {
        server = new MockWebServer();
        server.start();
        client = new LicenseManagerClient(
            server.url("/").toString(), "test-token", "lic-123");
    }

    @AfterEach
    void tearDown() throws Exception {
        server.shutdown();
    }

    @Test
    void computeSignature_mesmoInput_retornaMesmoHash() {
        var s1 = client.computeSignature("lic", "2026-01-01T00:00:00Z", "{}");
        var s2 = client.computeSignature("lic", "2026-01-01T00:00:00Z", "{}");
        assertEquals(s1, s2);
    }

    @Test
    void computeSignature_inputDiferente_retornaHashDiferente() {
        var s1 = client.computeSignature("lic", "2026-01-01T00:00:00Z", "{}");
        var s2 = client.computeSignature("lic", "2026-01-01T00:00:01Z", "{}");
        assertNotEquals(s1, s2);
    }

    @Test
    void computeSignature_resultado_eHexStringMinuscula() {
        var sig = client.computeSignature("lic", "2026-01-01T00:00:00Z", "{}");
        assertTrue(sig.matches("[0-9a-f]{64}"), "Esperado hex lowercase de 64 chars, obtido: " + sig);
    }

    @Test
    void login_respostaAutorizada_retornaSessionId() throws Exception {
        server.enqueue(new MockResponse()
            .setResponseCode(200)
            .setBody("{\"autorizado\":true,\"idSessao\":\"sess-1\"}"));

        var result = client.login("user@test.com");

        assertTrue(result.isAuthorized());
        assertEquals("sess-1", result.getSessionId());
    }

    @Test
    void login_erro401_lancaLicenseManagerException() {
        server.enqueue(new MockResponse()
            .setResponseCode(401)
            .setBody("{\"erro\":\"Token inválido\"}"));

        var ex = assertThrows(LicenseManagerException.class,
            () -> client.login("user@test.com"));
        assertEquals(401, ex.getStatusCode());
    }

    @Test
    void heartbeat_resposta204_naoLancaExcecao() throws Exception {
        server.enqueue(new MockResponse().setResponseCode(204));
        assertDoesNotThrow(() -> client.heartbeat("sess-1"));
    }

    @Test
    void logout_resposta204_naoLancaExcecao() throws Exception {
        server.enqueue(new MockResponse().setResponseCode(204));
        assertDoesNotThrow(() -> client.logout("sess-1"));
    }

    @Test
    void validateInstallation_respostaAutorizada_retornaInstallationId() throws Exception {
        server.enqueue(new MockResponse()
            .setResponseCode(200)
            .setBody("{\"autorizado\":true,\"idInstalacao\":\"inst-42\",\"jaRegistrada\":false}"));

        var result = client.validateInstallation("MACHINE-001");

        assertTrue(result.isAuthorized());
        assertEquals("inst-42", result.getInstallationId());
        assertFalse(result.isAlreadyRegistered());
    }

    @Test
    void constructor_baseUrlVazia_lancaIllegalArgumentException() {
        assertThrows(IllegalArgumentException.class,
            () -> new LicenseManagerClient("", "tok", "lic"));
    }

    @Test
    void constructor_tokenVazio_lancaIllegalArgumentException() {
        assertThrows(IllegalArgumentException.class,
            () -> new LicenseManagerClient("https://api.test", "", "lic"));
    }
}
