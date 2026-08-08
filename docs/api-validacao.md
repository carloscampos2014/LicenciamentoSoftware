# API de Validação

A API de validação é o ponto de entrada para as aplicações clientes verificarem se uma licença está ativa.

**URL base:** `https://licensemanager-api.enzojb.com.br`

## Autenticação HMAC-SHA256

Toda requisição deve incluir os seguintes headers:

| Header | Descrição |
|---|---|
| `X-Token` | Token da licença (obtido na emissão) |
| `X-Timestamp` | Data/hora UTC no formato ISO-8601 (ex: `2026-08-08T12:00:00Z`) |
| `X-Nonce` | String única por requisição (ex: UUID v4) |
| `X-Signature` | HMAC-SHA256 em hex de `{idLicenca}:{timestamp}:{body}` usando o token como chave |

!!! warning "Janela de tempo"
    O `X-Timestamp` deve estar dentro de **±5 minutos** do horário do servidor.
    Requisições fora dessa janela são rejeitadas como proteção anti-replay.

## Endpoints

### POST /api/validacao/login

Valida o login de um usuário na licença.

**Request:**
```json
{
  "idLicenca": "guid-da-licenca",
  "identificadorUsuario": "usuario@empresa.com"
}
```

**Response 200:**
```json
{
  "autorizado": true,
  "idSessao": "guid-da-sessao"
}
```

---

### POST /api/validacao/heartbeat

Mantém a sessão ativa. Deve ser chamado periodicamente.

**Request:**
```json
{
  "idLicenca": "guid-da-licenca",
  "idSessao": "guid-da-sessao"
}
```

**Response:** `204 No Content`

---

### POST /api/validacao/logout

Encerra a sessão (idempotente).

**Request:**
```json
{
  "idLicenca": "guid-da-licenca",
  "idSessao": "guid-da-sessao"
}
```

**Response:** `204 No Content`

---

### POST /api/validacao/instalacao

Valida ou registra uma instalação na máquina.

**Request:**
```json
{
  "idLicenca": "guid-da-licenca",
  "identificadorMaquina": "NOME-DA-MAQUINA"
}
```

**Response 200:**
```json
{
  "autorizado": true,
  "idInstalacao": "guid-da-instalacao",
  "jaRegistrada": false
}
```

## Códigos de erro

| Código | Significado |
|---|---|
| `401` | Token inválido ou expirado |
| `404` | Licença não encontrada |
| `422` | Dados inválidos (campos obrigatórios ausentes) |
| `429` | Rate limit excedido |
