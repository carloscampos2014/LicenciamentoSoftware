<?php

declare(strict_types=1);

namespace LicenseManager\Sdk;

/**
 * Resultado do endpoint de login.
 */
readonly class LoginResult
{
    public function __construct(
        public bool $authorized,
        public ?string $sessionId,
    ) {}
}
