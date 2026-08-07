module LicenseManagerSdk
  LoginResult = Struct.new(:authorized, :session_id, keyword_init: true)
  InstallationResult = Struct.new(:authorized, :installation_id, :already_registered, keyword_init: true)
end
