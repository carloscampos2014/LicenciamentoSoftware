module LicenseManagerSdk
  # Cliente para a API de validação do LicenseManager.
  # Encapsula geração de HMAC-SHA256 e os 4 endpoints de validação.
  #
  # @example Uso básico
  #   client = LicenseManagerSdk::Client.new(
  #     base_url:   "https://licensemanager-api.enzojb.com.br",
  #     token:      "seu-token",
  #     license_id: "guid-da-licenca"
  #   )
  #   login = client.login("usuario@empresa.com")
  #   client.heartbeat(login.session_id) if login.authorized
  class Client
    MAX_RETRIES = 3

    def initialize(base_url:, token:, license_id:, timeout: 30)
      raise ArgumentError, "base_url é obrigatório" if base_url.nil? || base_url.strip.empty?
      raise ArgumentError, "token é obrigatório"    if token.nil?    || token.strip.empty?
      raise ArgumentError, "license_id é obrigatório" if license_id.nil? || license_id.strip.empty?

      @base_url   = base_url.chomp("/")
      @token      = token
      # Normaliza GUID para lowercase com hífens — igual ao servidor (idLicenca:D)
      @license_id = license_id.match?(/\A[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\z/i) \
        ? license_id.downcase \
        : license_id
      @timeout    = timeout
    end

    # Valida login de um usuário numa licença.
    # @param user_id [String] identificador único do usuário
    # @return [LoginResult]
    def login(user_id)
      body = { idLicenca: @license_id, identificadorUsuario: user_id }
      data = post("api/validacao/login", body)
      LoginResult.new(
        authorized: data["autorizado"] || false,
        session_id: data["idSessao"]
      )
    end

    # Envia heartbeat para manter a sessão ativa.
    # @param session_id [String]
    def heartbeat(session_id)
      body = { idLicenca: @license_id, idSessao: session_id }
      post("api/validacao/heartbeat", body)
      nil
    end

    # Encerra a sessão (idempotente).
    # @param session_id [String]
    def logout(session_id)
      body = { idLicenca: @license_id, idSessao: session_id }
      post("api/validacao/logout", body)
      nil
    end

    # Valida ou registra uma instalação da aplicação cliente.
    # @param machine_id [String] identificador único da máquina
    # @return [InstallationResult]
    def validate_installation(machine_id)
      body = { idLicenca: @license_id, identificadorMaquina: machine_id }
      data = post("api/validacao/instalacao", body)
      InstallationResult.new(
        authorized:        data["autorizado"]    || false,
        installation_id:   data["idInstalacao"],
        already_registered: data["jaRegistrada"] || false
      )
    end

    # @api private
    def compute_signature(license_id, timestamp, body_json)
      # Normaliza para lowercase com hífens — igual ao servidor (idLicenca:D)
      normalized_id = license_id.match?(/\A[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\z/i) \
        ? license_id.downcase \
        : license_id
      payload = "#{normalized_id}:#{timestamp}:#{body_json}"
      OpenSSL::HMAC.hexdigest("SHA256", @token, payload)
    end

    private

    def post(path, body)
      body_json  = JSON.generate(body)
      uri = URI.parse("#{@base_url}/#{path}")

      last_error = nil
      MAX_RETRIES.times do |attempt|
        begin
          timestamp  = Time.now.utc.strftime("%Y-%m-%dT%H:%M:%SZ")
          nonce      = SecureRandom.hex(16)
          signature  = compute_signature(@license_id, timestamp, body_json)

          headers = {
            "Content-Type" => "application/json",
            "X-Token"      => @token,
            "X-Timestamp"  => timestamp,
            "X-Nonce"      => nonce,
            "X-Signature"  => signature
          }

          http = Net::HTTP.new(uri.host, uri.port)
          http.use_ssl     = uri.scheme == "https"
          http.read_timeout = @timeout
          http.open_timeout = @timeout

          response = http.post(uri.request_uri, body_json, headers)
          code = response.code.to_i

          if (code == 429 || code >= 500) && attempt < MAX_RETRIES - 1
            sleep(2**attempt)
            next
          end

          unless response.is_a?(Net::HTTPSuccess)
            raise LicenseManagerError.new(code, response.body.to_s)
          end

          body = response.body
          return (body.nil? || body.empty?) ? {} : JSON.parse(body)
        rescue LicenseManagerError
          raise
        rescue StandardError => e
          last_error = e
          sleep(2**attempt) if attempt < MAX_RETRIES - 1
        end
      end

      raise LicenseManagerError.new(0, "Erro de rede: #{last_error&.message}")
    end
  end
end
