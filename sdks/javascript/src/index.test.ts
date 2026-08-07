import { createHmac } from "crypto";
import { LicenseManagerClient, LicenseManagerError } from "./index";

const BASE_URL    = "https://api.example.com";
const TOKEN       = "test-secret";
const LICENSE_ID  = "lic-123";

function makeClient(fetchFn?: typeof global.fetch): LicenseManagerClient {
  if (fetchFn) (global as unknown as Record<string, unknown>).fetch = fetchFn;
  return new LicenseManagerClient({ baseUrl: BASE_URL, token: TOKEN, licenseId: LICENSE_ID });
}

function mockFetch(status: number, body: unknown): typeof global.fetch {
  return async () =>
    ({
      ok:      status >= 200 && status < 300,
      status,
      headers: { get: () => null },
      text:    async () => JSON.stringify(body),
      json:    async () => body,
    } as unknown as Response);
}

// -------------------------------------------------------------------------
// HMAC
// -------------------------------------------------------------------------

describe("computeSignature", () => {
  const client = new LicenseManagerClient({ baseUrl: BASE_URL, token: TOKEN, licenseId: LICENSE_ID });

  it("retorna o mesmo hash para o mesmo input", () => {
    const s1 = client.computeSignature("lic", "2026-01-01T00:00:00Z", "{}");
    const s2 = client.computeSignature("lic", "2026-01-01T00:00:00Z", "{}");
    expect(s1).toBe(s2);
  });

  it("retorna hashes diferentes para inputs diferentes", () => {
    const s1 = client.computeSignature("lic", "2026-01-01T00:00:00Z", "{}");
    const s2 = client.computeSignature("lic", "2026-01-01T00:00:01Z", "{}");
    expect(s1).not.toBe(s2);
  });

  it("resultado é hex lowercase de 64 chars", () => {
    const sig = client.computeSignature("lic", "2026-01-01T00:00:00Z", "{}");
    expect(sig).toMatch(/^[0-9a-f]{64}$/);
  });

  it("bate com calculo manual do HMAC-SHA256", () => {
    const licenseId = "abc-123";
    const timestamp = "2026-08-06T12:00:00Z";
    const body      = '{"idLicenca":"abc-123"}';
    const payload   = `${licenseId}:${timestamp}:${body}`;
    const expected  = createHmac("sha256", TOKEN).update(payload, "utf8").digest("hex");

    const c = new LicenseManagerClient({ baseUrl: BASE_URL, token: TOKEN, licenseId });
    expect(c.computeSignature(licenseId, timestamp, body)).toBe(expected);
  });
});

// -------------------------------------------------------------------------
// Endpoints
// -------------------------------------------------------------------------

describe("login", () => {
  it("resposta autorizada retorna sessionId", async () => {
    const client = makeClient(mockFetch(200, { autorizado: true, idSessao: "sess-1" }));
    const result = await client.login("user@test.com");
    expect(result.authorized).toBe(true);
    expect(result.sessionId).toBe("sess-1");
  });

  it("erro 401 lança LicenseManagerError", async () => {
    const client = makeClient(mockFetch(401, { erro: "Token inválido" }));
    await expect(client.login("user@test.com")).rejects.toThrow(LicenseManagerError);
  });
});

describe("heartbeat", () => {
  it("resposta 204 não lança erro", async () => {
    const client = makeClient(async () => ({
      ok: true, status: 204,
      headers: { get: () => "0" },
      text: async () => "", json: async () => ({})
    } as unknown as Response));
    await expect(client.heartbeat("sess-1")).resolves.toBeUndefined();
  });
});

describe("logout", () => {
  it("resposta 204 não lança erro", async () => {
    const client = makeClient(async () => ({
      ok: true, status: 204,
      headers: { get: () => "0" },
      text: async () => "", json: async () => ({})
    } as unknown as Response));
    await expect(client.logout("sess-1")).resolves.toBeUndefined();
  });
});

describe("validateInstallation", () => {
  it("resposta autorizada retorna installationId", async () => {
    const client = makeClient(mockFetch(200, { autorizado: true, idInstalacao: "inst-42", jaRegistrada: false }));
    const result = await client.validateInstallation("MACHINE-001");
    expect(result.authorized).toBe(true);
    expect(result.installationId).toBe("inst-42");
    expect(result.alreadyRegistered).toBe(false);
  });
});

describe("constructor", () => {
  it("baseUrl vazia lança erro", () => {
    expect(() => new LicenseManagerClient({ baseUrl: "", token: TOKEN, licenseId: LICENSE_ID })).toThrow();
  });
  it("token vazio lança erro", () => {
    expect(() => new LicenseManagerClient({ baseUrl: BASE_URL, token: "", licenseId: LICENSE_ID })).toThrow();
  });
  it("licenseId vazio lança erro", () => {
    expect(() => new LicenseManagerClient({ baseUrl: BASE_URL, token: TOKEN, licenseId: "" })).toThrow();
  });
});
