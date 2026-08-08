<?php

declare(strict_types=1);

namespace LicenseManager\Sdk;

use RuntimeException;

/**
 * Exceção lançada quando a API retorna um erro HTTP.
 */
class LicenseManagerException extends RuntimeException
{
    public function __construct(
        private readonly int $statusCode,
        private readonly string $responseBody
    ) {
        parent::__construct(
            sprintf('LicenseManager API error %d: %s', $statusCode, $responseBody)
        );
    }

    public function getStatusCode(): int
    {
        return $this->statusCode;
    }

    public function getResponseBody(): string
    {
        return $this->responseBody;
    }
}
