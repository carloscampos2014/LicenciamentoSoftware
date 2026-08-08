<?php

declare(strict_types=1);

namespace LicenseManager\Sdk\Tests;

use LicenseManager\Sdk\LicenseManagerClient;
use LicenseManager\Sdk\LicenseManagerException;
use PHPUnit\Framework\TestCase;

class LicenseManagerClientTest extends TestCase
{
    private const BASE_URL   = 'https://api.example.com';
    private const TOKEN      = 'test-secret';
    private const LICENSE_ID = 'lic-123';

    private function makeClient(): LicenseManagerClient
    {
        return new LicenseManagerClient(self::BASE_URL, self::TOKEN, self::LICENSE_ID);
    }

    // -------------------------------------------------------------------------
    // HMAC
    // -------------------------------------------------------------------------

    public function testComputeSignatureRetornaMesmoHashParaMesmoInput(): void
    {
        $client = $this->makeClient();
        $s1 = $client->computeSignature('lic', '2026-01-01T00:00:00Z', '{}');
        $s2 = $client->computeSignature('lic', '2026-01-01T00:00:00Z', '{}');
        $this->assertSame($s1, $s2);
    }

    public function testComputeSignatureRetornaHashDiferenteParaInputDiferente(): void
    {
        $client = $this->makeClient();
        $s1 = $client->computeSignature('lic', '2026-01-01T00:00:00Z', '{}');
        $s2 = $client->computeSignature('lic', '2026-01-01T00:00:01Z', '{}');
        $this->assertNotSame($s1, $s2);
    }

    public function testComputeSignatureRetornaHexLowercaseDe64Chars(): void
    {
        $client = $this->makeClient();
        $sig = $client->computeSignature('lic', '2026-01-01T00:00:00Z', '{}');
        $this->assertMatchesRegularExpression('/^[0-9a-f]{64}$/', $sig);
    }

    public function testComputeSignatureBateComCalculoManual(): void
    {
        $licenseId = 'abc-123';
        $timestamp = '2026-08-06T12:00:00Z';
        $body      = '{"idLicenca":"abc-123"}';
        $payload   = "{$licenseId}:{$timestamp}:{$body}";
        $expected  = hash_hmac('sha256', $payload, self::TOKEN);

        $client = new LicenseManagerClient(self::BASE_URL, self::TOKEN, $licenseId);
        $this->assertSame($expected, $client->computeSignature($licenseId, $timestamp, $body));
    }

    // -------------------------------------------------------------------------
    // Construtor
    // -------------------------------------------------------------------------

    public function testBaseUrlVaziaLancaInvalidArgumentException(): void
    {
        $this->expectException(\InvalidArgumentException::class);
        new LicenseManagerClient('', self::TOKEN, self::LICENSE_ID);
    }

    public function testTokenVazioLancaInvalidArgumentException(): void
    {
        $this->expectException(\InvalidArgumentException::class);
        new LicenseManagerClient(self::BASE_URL, '', self::LICENSE_ID);
    }

    public function testLicenseIdVazioLancaInvalidArgumentException(): void
    {
        $this->expectException(\InvalidArgumentException::class);
        new LicenseManagerClient(self::BASE_URL, self::TOKEN, '');
    }
}
