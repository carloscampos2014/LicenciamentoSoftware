---
inclusion: always
---

# Workflow de Desenvolvimento — Ciclo Completo de Fase/Feature

Este documento define o fluxo obrigatório para qualquer implementação de fase, feature ou conjunto de mudanças não triviais neste projeto. O agente DEVE seguir cada etapa na ordem definida, sem pular etapas.

---

## Etapas do ciclo

### Etapa 1 — Criar branch a partir do master atualizado

```
git fetch origin
git checkout master
git pull origin master
git checkout -b feature/<nome-da-fase-ou-feature>
```

- O nome da branch segue o padrão: `feature/fase-X-descricao` ou `feature/fase-X.Y-descricao`
- Nunca implementar diretamente no `master`

---

### Etapa 2 — Publicar a branch no GitHub imediatamente

```
git push -u origin feature/<nome> --no-verify
```

- Publicar antes de qualquer commit de código
- Confirmar que a branch existe no remoto antes de continuar

---

### Etapa 3 — Gerar o Briefing

Antes de escrever qualquer arquivo de código, o agente DEVE emitir um briefing ao usuário contendo:

- **O que será criado/modificado** — lista de arquivos com descrição de cada um
- **O que cada parte faz** — descrição concisa da responsabilidade
- **Decisões de design relevantes** — padrões, trade-offs, dependências
- **O que NÃO será feito** — escopo negativo explícito

O briefing deve ser claro o suficiente para que o usuário entenda o impacto antes de aprovar.

---

### Etapa 4 — Aguardar aprovação explícita

O agente DEVE pausar e aguardar o usuário dizer explicitamente: "aprovado", "pode implementar", "sim", "ok" ou equivalente.

- **Sem aprovação = sem código**
- Se o usuário pedir mudanças no briefing, revisar e aguardar nova aprovação
- Correções de build/lint/teste já em andamento não precisam de nova aprovação

---

### Etapa 5 — Mover issue(s) para "In Progress" no GitHub Project

Antes de iniciar a implementação, mover as issues relacionadas à fase/feature para o status **In Progress** no GitHub Project `carloscampos2014/LicenciamentoSoftware` (Project #3):

```powershell
# IDs do projeto
$projectId = "PVT_kwHOAHxQCM4BeoqO"
$fieldId   = "PVTSSF_lAHOAHxQCM4BeoqOzhZBYG4"
$inProgressId = "47fc9ee4"

# Buscar o item da issue e mover
$item = (gh project item-list 3 --owner carloscampos2014 --format json | ConvertFrom-Json).items |
        Where-Object { $_.title -like "*<titulo-da-issue>*" }
gh project item-edit --project-id $projectId --id $item.id --field-id $fieldId --single-select-option-id $inProgressId
```

- Mover apenas as issues que serão trabalhadas nesta iteração
- Issues futuras da mesma fase permanecem em "Todo"

---

### Etapa 6 — Implementar

- Seguir os padrões definidos em `engineering-standards.md` e `project-standards.md`
- Manter o código coeso e coerente com a arquitetura existente
- Cada issue/tarefa é implementada de forma atômica e completa

---

### Etapa 7 — Commit por issue/tarefa implementada

A cada issue ou conjunto de mudanças coeso, fazer commit com mensagem descritiva:

```
git add <arquivos-relevantes>
git commit -m "feat(fase-X.Y): #N descrição do que foi implementado"
```

**Formato da mensagem de commit:**
- `feat(fase-X):` para novas funcionalidades
- `fix:` para correções de bug
- `docs:` para documentação
- `chore:` para ajustes de configuração/infraestrutura
- `tests:` para adição/ajuste de testes

Incluir `#N` com o número da issue quando o commit fecha ou avança uma issue específica.

---

### Etapa 8 — Verificar build e testes

Após cada commit (ou grupo de commits relacionados), verificar:

```powershell
# Build dos projetos afetados
dotnet build src/LicenciamentoSoftware.<Projeto>/<Projeto>.csproj -c Debug --no-restore

# Testes unitários
dotnet test tests/LicenciamentoSoftware.Application.Tests/ --logger "console;verbosity=minimal"
```

- Build deve passar sem erros (`TreatWarningsAsErrors=true`)
- Todos os testes existentes devem continuar aprovados
- Novos testes devem ser escritos para nova lógica de negócio (exceto se o usuário explicitamente dispensar)

---

### Etapa 9 — Avaliar necessidade de testes manuais

Após verificação automatizada, analisar se há cenários que precisam de validação manual:

- Funcionalidades de UI (Blazor WASM) — verificar no browser
- Integrações com banco de dados — confirmar com o usuário se o banco está disponível
- Fluxos de autenticação (login, 2FA, tokens HMAC) — dependem de estado real

Se testes manuais forem necessários, **informar o usuário** e aguardar confirmação antes de prosseguir para o push.

---

### Etapa 10 — Atualizar documentação

Antes do push, atualizar toda a documentação afetada pelas mudanças da fase/feature:

**Sempre verificar:**
- `README.md` — atualizar status da fase na tabela, testes aprovados, instruções de uso se necessário
- `docs/DEVELOPMENT_PLAN.md` — marcar itens concluídos com ✅, adicionar resultado e demo da fase
- `docs/ARCHITECTURE.md` — documentar novos componentes, camadas, decisões de design adicionadas
- `docs/WEB_SPECIFICATION.md` — se houve mudanças no frontend Web, atualizar seções afetadas

**Critérios:**
- O `README.md` SEMPRE deve ser atualizado com o status correto da fase (✅ Concluída / 🔜 Próxima)
- O `DEVELOPMENT_PLAN.md` deve refletir o estado real — itens implementados marcados, resultado e demo preenchidos
- Não criar documentação desnecessária — atualizar apenas o que foi impactado
- Commitar a documentação junto ou logo após o último commit de código da fase

```
git add docs/ README.md
git commit -m "docs: atualizar documentacao para fase-X.Y"
```

---

### Etapa 11 — Push da branch

Somente após build, testes e documentação atualizados:

```
git push origin feature/<nome> --no-verify
```

O hook de pre-push roda build + testes completos. Se falhar, corrigir antes de prosseguir.

---

### Etapa 12 — Criar Pull Request

```
gh pr create \
  --base master \
  --head feature/<nome> \
  --title "feat(fase-X): Descrição concisa (máx 70 chars)" \
  --body "..."
```

**Body do PR deve conter:**
- Resumo do que foi implementado
- Lista de mudanças por área (backend, frontend, infra)
- Resultado dos testes
- `Closes #N` para cada issue que o PR fecha (fecha automaticamente no merge)

**Exemplo de `Closes`:**
```
Closes #41
Closes #42
Closes #43
```

---

### Etapa 13 — Após merge: limpar repositório local

Quando o usuário sinalizar que o PR foi mergeado:

```powershell
# Atualiza master
git checkout master
git pull origin master

# Remove referências de branches remotas apagadas
git remote prune origin

# Remove branch local da feature
git branch -D feature/<nome>
```

Confirmar que só `master` permanece localmente antes de encerrar.

---

## Resumo visual do ciclo

```
master atualizado
      ↓
  criar branch  →  publicar branch
      ↓
  briefing  →  aguardar aprovação
      ↓
  mover issue para In Progress
      ↓
  implementar
      ↓  (loop por issue/tarefa)
  commit  →  build  →  testes
      ↓
  testes manuais? (se necessário)
      ↓
  atualizar documentação (README + DEVELOPMENT_PLAN + ARCHITECTURE)
      ↓
  push  →  criar PR (com Closes #N)
      ↓
  aguardar merge
      ↓
  pull master  →  limpar branches locais
```

---

## Exceções ao workflow

As etapas 3 e 4 (briefing + aprovação) **não se aplicam** a:
- Correções de build ou testes já em andamento
- Ajustes simples em arquivos de configuração (`.gitignore`, `appsettings`)
- Respostas a erros identificados durante a execução (ex: erro de tipo no Dapper)

Todas as outras etapas são obrigatórias sem exceção.
