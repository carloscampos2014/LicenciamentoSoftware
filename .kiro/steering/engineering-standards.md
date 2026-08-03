---
inclusion: auto
---

# Padrões de Engenharia de Software

Todo código produzido deve seguir obrigatoriamente os princípios abaixo. Eles não são opcionais e se aplicam a qualquer tarefa de implementação, independente do tamanho.

## SOLID

- **S** — Single Responsibility: cada classe, método e módulo tem uma única razão para mudar.
- **O** — Open/Closed: aberto para extensão, fechado para modificação.
- **L** — Liskov Substitution: subtipos são substituíveis por seus tipos base sem alterar o comportamento.
- **I** — Interface Segregation: interfaces pequenas e específicas; nunca forçar implementação de métodos não usados.
- **D** — Dependency Inversion: depender de abstrações, nunca de implementações concretas.

## Clean Code

- Nomes revelam intenção: variáveis, métodos e classes têm nomes que dispensam comentário.
- Métodos fazem uma coisa só e são curtos.
- Sem números mágicos — usar constantes nomeadas.
- Sem comentários que explicam "o quê" — o código deve ser autoexplicativo. Comentários explicam "por quê".
- Sem código morto, sem TODOs esquecidos, sem código comentado.
- Tratamento de erros explícito — nunca engolir exceções silenciosamente.

## Clean Architecture

- Regra de dependência: camadas internas nunca dependem de camadas externas.
  - `Domain` → sem dependências externas
  - `Application` → depende apenas de `Domain`
  - `Infrastructure` → depende de `Application` e `Domain`
  - `Api` → depende de `Application` e `Infrastructure`
- `Domain` e `Application` não conhecem PostgreSQL, Dapper, controllers, HTTP ou qualquer detalhe de infraestrutura.
- Controllers são finos: recebem request, delegam para caso de uso, retornam resposta.
- Casos de uso retornam resultados explícitos (`Result`, `NotFound`, `Conflict`, `ValidationError`) — sem tipos HTTP na camada de aplicação.
- Sem repositório genérico (`GenericRepository`), sem serviço de gestão genérico (`ManagementService`).

## TDD — Test-Driven Development

- O ciclo é sempre: **Red → Green → Refactor**.
- Nenhuma regra de negócio nova é implementada sem teste automatizado correspondente escrito antes.
- Testes unitários cobrem domínio e handlers de aplicação.
- Testes de integração usam banco real via Testcontainers.
- Testes de API usam `WebApplicationFactory`.
- Testes têm nomes descritivos no formato: `Metodo_Cenario_ResultadoEsperado`.
- Sem testes que apenas verificam que "não lança exceção" — testar comportamento real.

## Regras gerais

- Sem credenciais, segredos ou connection strings no código ou repositório.
- Validação de entrada em toda operação de escrita.
- Warnings tratados como erros — o build não aceita warnings.
