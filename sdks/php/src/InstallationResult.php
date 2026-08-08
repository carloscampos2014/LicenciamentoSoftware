<?php

declare(strict_types=1);

namespace LicenseManager\Sdk;

/**
 * Resultado do endpoint de validação de instalação.
 */
readonly class InstallationResult
{
    public function __construct(
        public bool $authorized,
        public ?string $installationId,
        public bool $alreadyRegistered,
    ) {}
}
