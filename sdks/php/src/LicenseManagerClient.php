<?php

declare(strict_types=1);

namespace LicenseManager\Sdk;

/**
 * Cliente para a API de validação do LicenseManager.
 * Encapsula autenticação HMAC-SHA256 e os 4 endpoints de validação.
 *
 * @example
 * $client = new LicenseManagerClient(
 *     'https://licensemanager-api.enzojb.com.br',
 *     'seu-token',
 *     'guid-da-licenca'
 * );
 * $login = $client->login('usuario@empresa.com');
 */
class LicenseManagerClient
{
    private const MAX_RETRIES = 3;

    public function __construct(
        private readonly string $baseUrl,
        private readonly string $token,
        private readonly string $licenseId,
        private readonly int    $timeoutSeconds = 30,
    ) {
        if (empty(trim($baseUrl)))   throw new \InvalidArgumentException('baseUrl é obrigatório');
        if (empty(trim($token)))     throw new \InvalidArgumentException('token é obrigatório');
        if (empty(trim($licenseId))) throw new \InvalidArgumentException('licenseId é obrigatório');

        $this->baseUrl   = rtrim($baseUrl, '/');
        $this->token     = $token;
        // Normaliza GUID para lowercase com hífens — igual ao servidor (idLicenca:D)
        $this->licenseId = preg_match(
            '/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i',
            $licenseId
        ) ? strtolower($licenseId) : $licenseId;
    }

    // -------------------------------------------------------------------------
    // Endpoints públicos
    // -------------------------------------------------------------------------

    /**
     * Valida login de um usuário numa licença.
     */
    public function login(string $userId): LoginResult
    {
        $data = $this->post('api/validacao/login', [
            'idLicenca'            => $this->licenseId,
            'identificadorUsuario' => $userId,
        ]);

        return new LoginResult(
            authorized: (bool) ($data['autorizado'] ?? false),
            sessionId:  $data['idSessao'] ?? null,
        );
    }

    /**
     * Envia heartbeat para manter a sessão ativa.
     */
    public function heartbeat(string $sessionId): void
    {
        $this->post('api/validacao/heartbeat', [
            'idLicenca' => $this->licenseId,
            'idSessao'  => $sessionId,
        ]);
    }

    /**
     * Encerra a sessão (idempotente).
     */
    public function logout(string $sessionId): void
    {
        $this->post('api/validacao/logout', [
            'idLicenca' => $this->licenseId,
            'idSessao'  => $sessionId,
        ]);
    }

    /**
     * Valida ou registra uma instalação da aplicação cliente.
     */
    public function validateInstallation(string $machineId): InstallationResult
    {
        $data = $this->post('api/validacao/instalacao', [
            'idLicenca'           => $this->licenseId,
            'identificadorMaquina'=> $machineId,
        ]);

        return new InstallationResult(
            authorized:        (bool) ($data['autorizado']    ?? false),
            installationId:    $data['idInstalacao'] ?? null,
            alreadyRegistered: (bool) ($data['jaRegistrada']  ?? false),
        );
    }

    // -------------------------------------------------------------------------
    // Infraestrutura HMAC
    // -------------------------------------------------------------------------

    /**
     * Calcula a assinatura HMAC-SHA256.
     * @internal Público para facilitar testes unitários.
     */
    public function computeSignature(string $licenseId, string $timestamp, string $bodyJson): string
    {
        // Normaliza para lowercase com hífens — igual ao servidor (idLicenca:D)
        if (preg_match('/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i', $licenseId)) {
            $licenseId = strtolower($licenseId);
        }
        $payload = "{$licenseId}:{$timestamp}:{$bodyJson}";
        return hash_hmac('sha256', $payload, $this->token);
    }

    /**
     * @return array<string, mixed>
     */
    private function post(string $path, array $body): array
    {
        $bodyJson  = json_encode($body, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
        $timestamp = gmdate('Y-m-d\TH:i:s\Z');
        $nonce     = bin2hex(random_bytes(16));
        $signature = $this->computeSignature($this->licenseId, $timestamp, $bodyJson);
        $url       = rtrim($this->baseUrl, '/') . '/' . $path;

        $lastError = null;

        for ($attempt = 1; $attempt <= self::MAX_RETRIES; $attempt++) {
            $ch = curl_init($url);
            curl_setopt_array($ch, [
                CURLOPT_POST           => true,
                CURLOPT_POSTFIELDS     => $bodyJson,
                CURLOPT_RETURNTRANSFER => true,
                CURLOPT_TIMEOUT        => $this->timeoutSeconds,
                CURLOPT_HTTPHEADER     => [
                    'Content-Type: application/json',
                    "X-Token: {$this->token}",
                    "X-Timestamp: {$timestamp}",
                    "X-Nonce: {$nonce}",
                    "X-Signature: {$signature}",
                ],
            ]);

            $responseBody = curl_exec($ch);
            $statusCode   = (int) curl_getinfo($ch, CURLINFO_HTTP_CODE);
            $curlError    = curl_error($ch);
            curl_close($ch);

            if ($curlError !== '') {
                $lastError = $curlError;
                if ($attempt < self::MAX_RETRIES) {
                    sleep((int) pow(2, $attempt));
                    continue;
                }
                throw new LicenseManagerException(0, "Erro de rede: {$curlError}");
            }

            if ($statusCode === 429 || $statusCode >= 500) {
                if ($attempt < self::MAX_RETRIES) {
                    sleep((int) pow(2, $attempt));
                    continue;
                }
            }

            if ($statusCode < 200 || $statusCode >= 300) {
                throw new LicenseManagerException($statusCode, (string) $responseBody);
            }

            if ($statusCode === 204 || $responseBody === '' || $responseBody === false) {
                return [];
            }

            return json_decode((string) $responseBody, true) ?? [];
        }

        throw new LicenseManagerException(0, "Erro de rede: {$lastError}");
    }
}
