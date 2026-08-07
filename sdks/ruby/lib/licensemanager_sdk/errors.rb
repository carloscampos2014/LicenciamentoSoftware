module LicenseManagerSdk
  class LicenseManagerError < StandardError
    attr_reader :status_code, :response_body

    def initialize(status_code, response_body)
      super("LicenseManager API error #{status_code}: #{response_body}")
      @status_code   = status_code
      @response_body = response_body
    end
  end
end
