require "spec_helper"
require "webmock/rspec"

RSpec.describe LicenseManagerSdk::Client do
  let(:base_url)   { "https://api.example.com" }
  let(:token)      { "test-secret" }
  let(:license_id) { "lic-123" }
  let(:client)     { described_class.new(base_url: base_url, token: token, license_id: license_id) }

  # -------------------------------------------------------------------------
  # HMAC
  # -------------------------------------------------------------------------

  describe "#compute_signature" do
    it "retorna o mesmo hash para o mesmo input" do
      s1 = client.compute_signature("lic", "2026-01-01T00:00:00Z", "{}")
      s2 = client.compute_signature("lic", "2026-01-01T00:00:00Z", "{}")
      expect(s1).to eq(s2)
    end

    it "retorna hashes diferentes para inputs diferentes" do
      s1 = client.compute_signature("lic", "2026-01-01T00:00:00Z", "{}")
      s2 = client.compute_signature("lic", "2026-01-01T00:00:01Z", "{}")
      expect(s1).not_to eq(s2)
    end

    it "resultado é hex lowercase de 64 chars" do
      sig = client.compute_signature("lic", "2026-01-01T00:00:00Z", "{}")
      expect(sig).to match(/\A[0-9a-f]{64}\z/)
    end

    it "bate com cálculo manual do HMAC-SHA256" do
      license_id = "abc-123"
      timestamp  = "2026-08-06T12:00:00Z"
      body       = '{"idLicenca":"abc-123"}'
      payload    = "#{license_id}:#{timestamp}:#{body}"
      expected   = OpenSSL::HMAC.hexdigest("SHA256", token, payload)

      c = described_class.new(base_url: base_url, token: token, license_id: license_id)
      expect(c.compute_signature(license_id, timestamp, body)).to eq(expected)
    end
  end

  # -------------------------------------------------------------------------
  # Endpoints
  # -------------------------------------------------------------------------

  describe "#login" do
    it "resposta autorizada retorna session_id" do
      stub_request(:post, "#{base_url}/api/validacao/login")
        .to_return(status: 200, body: '{"autorizado":true,"idSessao":"sess-1"}',
                   headers: { "Content-Type" => "application/json" })

      result = client.login("user@test.com")
      expect(result.authorized).to be true
      expect(result.session_id).to eq("sess-1")
    end

    it "erro 401 lança LicenseManagerError" do
      stub_request(:post, "#{base_url}/api/validacao/login")
        .to_return(status: 401, body: '{"erro":"Token inválido"}')

      expect { client.login("user@test.com") }
        .to raise_error(LicenseManagerSdk::LicenseManagerError) do |e|
          expect(e.status_code).to eq(401)
        end
    end
  end

  describe "#heartbeat" do
    it "resposta 204 não lança erro" do
      stub_request(:post, "#{base_url}/api/validacao/heartbeat").to_return(status: 204, body: "")
      expect { client.heartbeat("sess-1") }.not_to raise_error
    end
  end

  describe "#logout" do
    it "resposta 204 não lança erro" do
      stub_request(:post, "#{base_url}/api/validacao/logout").to_return(status: 204, body: "")
      expect { client.logout("sess-1") }.not_to raise_error
    end
  end

  describe "#validate_installation" do
    it "resposta autorizada retorna installation_id" do
      stub_request(:post, "#{base_url}/api/validacao/instalacao")
        .to_return(status: 200,
                   body: '{"autorizado":true,"idInstalacao":"inst-42","jaRegistrada":false}',
                   headers: { "Content-Type" => "application/json" })

      result = client.validate_installation("MACHINE-001")
      expect(result.authorized).to be true
      expect(result.installation_id).to eq("inst-42")
      expect(result.already_registered).to be false
    end
  end

  # -------------------------------------------------------------------------
  # Construtor
  # -------------------------------------------------------------------------

  describe ".new" do
    it "base_url vazia lança ArgumentError" do
      expect { described_class.new(base_url: "", token: token, license_id: license_id) }
        .to raise_error(ArgumentError)
    end

    it "token vazio lança ArgumentError" do
      expect { described_class.new(base_url: base_url, token: "", license_id: license_id) }
        .to raise_error(ArgumentError)
    end

    it "license_id vazio lança ArgumentError" do
      expect { described_class.new(base_url: base_url, token: token, license_id: "") }
        .to raise_error(ArgumentError)
    end
  end
end
