# SDK Ruby

[![Gem Version](https://img.shields.io/gem/v/licensemanager-sdk)](https://rubygems.org/gems/licensemanager-sdk)

## Instalação

```bash
gem install licensemanager-sdk
```

Ou no `Gemfile`:

```ruby
gem 'licensemanager-sdk'
```

## Uso (Ruby puro)

```ruby
require 'licensemanager_sdk'

client = LicenseManagerSdk::Client.new(
  base_url:   'https://licensemanager-api.enzojb.com.br',
  token:      'seu-token',
  license_id: 'guid-da-licenca'
)

login = client.login('usuario@empresa.com')
if login.authorized
  client.heartbeat(login.session_id)
  client.logout(login.session_id)
end
```

## Uso (Rails)

```ruby
# config/initializers/licensemanager.rb
LICENSE_CLIENT = LicenseManagerSdk::Client.new(
  base_url:   ENV.fetch('LICENSE_API_URL'),
  token:      ENV.fetch('LICENSE_TOKEN'),
  license_id: ENV.fetch('LICENSE_ID')
)
```
