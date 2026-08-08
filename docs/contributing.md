# Contribuindo

## Pré-requisitos

- .NET 10 SDK
- Docker (para PostgreSQL local)
- Git

## Configurar o ambiente

```bash
git clone https://github.com/carloscampos2014/LicenciamentoSoftware.git
cd LicenciamentoSoftware

# Instalar hooks de git
.\scripts\install-git-hooks.ps1

# Subir o banco de dados
docker-compose up -d postgres
```

## Rodar os testes

```bash
# Testes unitários
dotnet test tests/LicenciamentoSoftware.Domain.Tests
dotnet test tests/LicenciamentoSoftware.Application.Tests

# Testes de integração (requer PostgreSQL)
dotnet test tests/LicenciamentoSoftware.IntegrationTests
```

## Workflow de desenvolvimento

1. Criar branch a partir do `master`: `git checkout -b feature/minha-feature`
2. Implementar e commitar seguindo o padrão `feat(area): descrição`
3. Abrir Pull Request para `master`
4. CI deve passar (build + testes) antes do merge

## Padrões de commit

| Prefixo | Uso |
|---|---|
| `feat:` | Nova funcionalidade |
| `fix:` | Correção de bug |
| `docs:` | Documentação |
| `chore:` | Configuração/infra |
| `tests:` | Testes |
