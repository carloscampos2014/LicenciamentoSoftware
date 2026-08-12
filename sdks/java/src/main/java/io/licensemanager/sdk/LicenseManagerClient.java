package io.licensemanager.sdk;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.PropertyNamingStrategies;
import okhttp3.*;

import javax.crypto.Mac;
import javax.crypto.spec.SecretKeySpec;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.time.Instant;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.util.HexFormat;
import java.util.Map;
import java.util.UUID;

/**
 * Cliente para a API de validação do LicenseManager.
 * Encapsula geração de HMAC-SHA256 e os 4 endpoints de validação.
 */
public class LicenseManagerClient {

    private static final MediaType JSON = MediaType.get("application/json; charset=utf-8");
    private static final DateTimeFormatter ISO_UTC =
        DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ss'Z'").withZone(ZoneOffset.UTC);

    private final OkHttpClient http;
    private final String baseUrl;
    private final String token;
    private final String licenseId;
    private final ObjectMapper mapper;

    public LicenseManagerClient(String baseUrl, String token, String licenseId) {
        this(baseUrl, token, licenseId, new OkHttpClient.Builder().build());
    }

    /** Construtor com HttpClient customizado (útil para testes). */
    public LicenseManagerClient(String baseUrl, String token, String licenseId, OkHttpClient http) {
        if (baseUrl == null || baseUrl.isBlank()) throw new IllegalArgumentException("baseUrl é obrigatório");
        if (token   == null || token.isBlank())   throw new IllegalArgumentException("token é obrigatório");
        if (licenseId == null || licenseId.isBlank()) throw new IllegalArgumentException("licenseId é obrigatório");

        this.baseUrl   = baseUrl.stripTrailing().endsWith("/") ? baseUrl : baseUrl + "/";
        this.token     = token;
        // Normaliza GUID para lowercase com hífens — igual ao servidor (idLicenca:D)
        String normalizedId;
        try {
            normalizedId = UUID.fromString(licenseId).toString();
        } catch (IllegalArgumentException e) {
            normalizedId = licenseId;
        }
        this.licenseId = normalizedId;
        this.http      = http;
        this.mapper    = new ObjectMapper()
            .setPropertyNamingStrategy(PropertyNamingStrategies.LOWER_CAMEL_CASE);
    }

    // -------------------------------------------------------------------------
    // Endpoints públicos
    // -------------------------------------------------------------------------

    public LoginResult login(String userId) throws LicenseManagerException {
        var body = Map.of("idLicenca", licenseId, "identificadorUsuario", userId);
        var json = post("api/validacao/login", body);
        return deserialize(json, LoginResult.class);
    }

    public void heartbeat(String sessionId) throws LicenseManagerException {
        var body = Map.of("idLicenca", licenseId, "idSessao", sessionId);
        post("api/validacao/heartbeat", body);
    }

    public void logout(String sessionId) throws LicenseManagerException {
        var body = Map.of("idLicenca", licenseId, "idSessao", sessionId);
        post("api/validacao/logout", body);
    }

    public InstallationResult validateInstallation(String machineId) throws LicenseManagerException {
        var body = Map.of("idLicenca", licenseId, "identificadorMaquina", machineId);
        var json = post("api/validacao/instalacao", body);
        return deserialize(json, InstallationResult.class);
    }

    // -------------------------------------------------------------------------
    // Infraestrutura HMAC
    // -------------------------------------------------------------------------

    private String post(String path, Object bodyObj) throws LicenseManagerException {
        String bodyJson;
        try {
            bodyJson = mapper.writeValueAsString(bodyObj);
        } catch (IOException e) {
            throw new LicenseManagerException(0, "Erro ao serializar body: " + e.getMessage());
        }

        var timestamp = ISO_UTC.format(Instant.now());
        var nonce     = UUID.randomUUID().toString().replace("-", "");
        var signature = computeSignature(licenseId, timestamp, bodyJson);

        var requestBody = RequestBody.create(bodyJson, JSON);
        var request = new Request.Builder()
            .url(baseUrl + path)
            .post(requestBody)
            .addHeader("X-Token",     token)
            .addHeader("X-Timestamp", timestamp)
            .addHeader("X-Nonce",     nonce)
            .addHeader("X-Signature", signature)
            .build();

        int attempts = 0;
        while (true) {
            attempts++;
            try (Response response = http.newCall(request).execute()) {
                var code = response.code();
                if (code == 429 || code >= 500) {
                    if (attempts < 3) {
                        Thread.sleep((long) Math.pow(2, attempts) * 1000);
                        continue;
                    }
                }
                if (!response.isSuccessful()) {
                    var errorBody = response.body() != null ? response.body().string() : "";
                    throw new LicenseManagerException(code, errorBody);
                }
                return response.body() != null ? response.body().string() : "";
            } catch (IOException e) {
                if (attempts < 3) {
                    try { Thread.sleep((long) Math.pow(2, attempts) * 1000); } catch (InterruptedException ignored) {}
                    continue;
                }
                throw new LicenseManagerException(0, "Erro de rede: " + e.getMessage());
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                throw new LicenseManagerException(0, "Interrompido durante retry");
            }
        }
    }

    String computeSignature(String licenseId, String timestamp, String bodyJson) {
        try {
            // Normaliza para lowercase com hífens — igual ao servidor (idLicenca:D)
            String normalizedId;
            try {
                normalizedId = UUID.fromString(licenseId).toString();
            } catch (IllegalArgumentException e) {
                normalizedId = licenseId;
            }
            var payload = normalizedId + ":" + timestamp + ":" + bodyJson;
            var mac = Mac.getInstance("HmacSHA256");
            mac.init(new SecretKeySpec(token.getBytes(StandardCharsets.UTF_8), "HmacSHA256"));
            var hash = mac.doFinal(payload.getBytes(StandardCharsets.UTF_8));
            return HexFormat.of().formatHex(hash);
        } catch (Exception e) {
            throw new RuntimeException("Erro ao calcular HMAC", e);
        }
    }

    private <T> T deserialize(String json, Class<T> type) throws LicenseManagerException {
        try {
            return mapper.readValue(json, type);
        } catch (IOException e) {
            throw new LicenseManagerException(0, "Erro ao deserializar resposta: " + e.getMessage());
        }
    }
}
