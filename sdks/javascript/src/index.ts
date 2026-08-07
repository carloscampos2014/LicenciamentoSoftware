import { createHmac, randomUUID } from "crypto";

// -------------------------------------------------------------------------
// Tipos públicos
// -------------------------------------------------------------------------

export interface LicenseManagerConfig {
  baseUrl: string;
  token: string;
  licenseId: string;
  /** Timeout em ms (padrão: 30000) */
  timeoutMs?: number;
}

export interface LoginResult {
  authorized: boolean;
  sessionId: string | null;
}

export interface InstallationResult {
  authorized: boolean;
  installationId: string | null;
  alreadyRegistered: boolean;
}

// -------------------------------------------------------------------------
// Exceção
// -------------------------------------------------------------------------

export class LicenseManagerError extends Error {
  constructor(
    public readonly statusCode: number,
    public readonly responseBody: string
  ) {
    super(`LicenseManager API error ${statusCode}: ${responseBody}`);
    this.name = "LicenseManagerError";
  }
}

// -------------------------------------------------------------------------
// Cliente principal
// -------------------------------------------------------------------------

export class LicenseManagerClient {
  private readonly baseUrl: string;
  private readonly token: string;
  private readonly licenseId: string;
  private readonly timeoutMs: number;

  constructor(config: LicenseManagerConfig) {
    if (!config.baseUrl?.trim())   throw new Error("baseUrl é obrigatório");
    if (!config.token?.trim())     throw new Error("token é obrigatório");
    if (!config.licenseId?.trim()) throw new Error("licenseId é obrigatório");

    this.baseUrl   = config.baseUrl.replace(/\/$/, "");
    this.token     = config.token;
    this.licenseId = config.licenseId;
    this.timeoutMs = config.timeoutMs ?? 30_000;
  }

  // -------------------------------------------------------------------------
  // Endpoints públicos
  // -------------------------------------------------------------------------

  async login(userId: string): Promise<LoginResult> {
    const body = { idLicenca: this.licenseId, identificadorUsuario: userId };
    const data = await this.post("api/validacao/login", body);
    return {
      authorized: (data.autorizado as boolean) ?? false,
      sessionId:  (data.idSessao as string | null) ?? null,
    };
  }

  async heartbeat(sessionId: string): Promise<void> {
    const body = { idLicenca: this.licenseId, idSessao: sessionId };
    await this.post("api/validacao/heartbeat", body);
  }

  async logout(sessionId: string): Promise<void> {
    const body = { idLicenca: this.licenseId, idSessao: sessionId };
    await this.post("api/validacao/logout", body);
  }

  async validateInstallation(machineId: string): Promise<InstallationResult> {
    const body = { idLicenca: this.licenseId, identificadorMaquina: machineId };
    const data = await this.post("api/validacao/instalacao", body);
    return {
      authorized:        (data.autorizado     as boolean) ?? false,
      installationId:    (data.idInstalacao   as string | null) ?? null,
      alreadyRegistered: (data.jaRegistrada   as boolean) ?? false,
    };
  }

  // -------------------------------------------------------------------------
  // Infraestrutura HMAC
  // -------------------------------------------------------------------------

  private async post(path: string, body: object): Promise<Record<string, unknown>> {
    const bodyJson  = JSON.stringify(body);
    const timestamp = new Date().toISOString().replace(/\.\d{3}Z$/, "Z");
    const nonce     = randomUUID().replace(/-/g, "");
    const signature = this.computeSignature(this.licenseId, timestamp, bodyJson);

    const url = `${this.baseUrl}/${path}`;
    const headers: Record<string, string> = {
      "Content-Type": "application/json",
      "X-Token":      this.token,
      "X-Timestamp":  timestamp,
      "X-Nonce":      nonce,
      "X-Signature":  signature,
    };

    let lastError: unknown;
    for (let attempt = 1; attempt <= 3; attempt++) {
      try {
        const controller = new AbortController();
        const timer      = setTimeout(() => controller.abort(), this.timeoutMs);

        let response: Response;
        try {
          response = await fetch(url, {
            method:  "POST",
            headers,
            body:    bodyJson,
            signal:  controller.signal,
          });
        } finally {
          clearTimeout(timer);
        }

        if (response.status === 429 || response.status >= 500) {
          if (attempt < 3) {
            await sleep(Math.pow(2, attempt) * 1000);
            continue;
          }
        }

        if (!response.ok) {
          const text = await response.text();
          throw new LicenseManagerError(response.status, text);
        }

        if (response.status === 204 || response.headers.get("content-length") === "0") {
          return {};
        }
        return (await response.json()) as Record<string, unknown>;
      } catch (err) {
        if (err instanceof LicenseManagerError) throw err;
        lastError = err;
        if (attempt < 3) await sleep(Math.pow(2, attempt) * 1000);
      }
    }
    throw new LicenseManagerError(0, `Erro de rede: ${lastError}`);
  }

  computeSignature(licenseId: string, timestamp: string, bodyJson: string): string {
    const payload = `${licenseId}:${timestamp}:${bodyJson}`;
    return createHmac("sha256", this.token).update(payload, "utf8").digest("hex");
  }
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
