Gem::Specification.new do |spec|
  spec.name          = "licensemanager-sdk"
  spec.version       = "1.0.4"
  spec.authors       = ["LicenciamentoSoftware"]
  spec.summary       = "SDK cliente para a API de validação do LicenseManager"
  spec.description   = "Encapsula autenticação HMAC-SHA256 e os endpoints de validação de licença."
  spec.homepage      = "https://github.com/carloscampos2014/LicenciamentoSoftware"
  spec.license       = "MIT"
  spec.required_ruby_version = ">= 3.0.0"

  spec.files         = Dir["lib/**/*.rb", "README.md", "licensemanager_sdk.gemspec"]
  spec.require_paths = ["lib"]
end
