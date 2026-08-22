---
description: "Padrões de engenharia obrigatórios — SOLID, Clean Code, segurança, testes. Aplicar em toda implementação."
---

# Padrões de Engenharia de Software

Todo código produzido deve seguir obrigatoriamente os princípios abaixo.
Eles não são opcionais e se aplicam a qualquer tarefa de implementação, independente do tamanho.

## SOLID

- **S** — Single Responsibility: cada classe, método e módulo tem uma única razão para mudar.
- **O** — Open/Closed: aberto para extensão, fechado para modificação.
- **L** — Liskov Substitution: subtipos são substituíveis por seus tipos base sem alterar o comportamento.
- **I** — Interface Segregation: interfaces pequenas e específicas; nunca forçar implementação de métodos não usados.
- **D** — Dependency Inversion: depender de abstrações, nunca de implementações concretas.

## Clean Code

- Nomes revelam intenção: variáveis, métodos e classes têm nomes que dispensam comentário.
- Métodos/funções fazem uma coisa só e são curtos.
- Sem números mágicos — usar constantes nomeadas.
- Comentários explicam "por quê", nunca "o quê" — o código deve ser autoexplicativo.
- Sem código morto, sem TODOs esquecidos, sem código comentado.
- Tratamento de erros explícito — nunca engolir exceções silenciosamente.

## Separação de Responsabilidades

- Controllers/handlers são finos: receber request → delegar → retornar resposta.
- Lógica de negócio nunca fica em controllers ou camadas de apresentação.
- Serviços de infraestrutura acessados via interfaces/abstrações.

## Segurança

- Sem credenciais, segredos ou connection strings no código ou repositório.
- Validação de entrada em toda operação de escrita.
- Senhas sempre com hash seguro (bcrypt/argon2).
- Comunicação cliente-servidor via HTTPS em produção.

## Testes

- Testes unitários cobrem lógica de negócio e serviços de aplicação.
- Nomes no formato: `Metodo_Cenario_ResultadoEsperado`.
- Sem testes que apenas verificam que "não lança exceção" — testar comportamento real.

## Anti-Patterns Proibidos

```
❌ God Classes — classes com muitas responsabilidades
❌ Métodos longos — quebrar em métodos menores e focados
❌ Magic Numbers — usar constantes nomeadas
❌ Deep Nesting — extrair métodos, usar early returns
❌ Code Duplication — aplicar DRY
❌ Poor Naming — nomes vagos como "data", "temp", "manager"
❌ Tight Coupling — depender de abstrações, não implementações
❌ Over-engineering — não adicionar complexidade desnecessária
```
