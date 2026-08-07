# licensemanager-sdk — Ruby

SDK cliente para a API de validação do LicenseManager. Compatível com Ruby 3.0+ e Ruby on Rails.

## Instalação

```ruby
# Gemfile
gem "licensemanager-sdk"
```

```bash
bundle install
```

Ou diretamente:

```bash
gem install licensemanager-sdk
```

## Uso (Ruby puro)

```ruby
require "licensemanager_sdk"

client = LicenseManagerSdk::Client.new(
  base_url:   "https://licensemanager-api.enzojb.com.br",
  token:      "seu-token",
  license_id: "guid-da-licenca"
)

login = client.login("usuario@empresa.com")
if login.authorized
  client.heartbeat(login.session_id)
  client.logout(login.session_id)
end

inst = client.validate_installation("MACHINE-001")
puts "Instalação: #{inst.installation_id}" if inst.authorized
```

## Uso (Rails — initializer)

```ruby
# config/initializers/licensemanager.rb
require "licensemanager_sdk"

LICENSE_CLIENT = LicenseManagerSdk::Client.new(
  base_url:   ENV.fetch("LICENSE_API_URL"),
  token:      ENV.fetch("LICENSE_TOKEN"),
  license_id: ENV.fetch("LICENSE_ID")
)
```

## Testes

```bash
bundle exec rspec
```
