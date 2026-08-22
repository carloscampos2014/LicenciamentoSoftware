# /generate-docs

Gera ou atualiza documentação técnica do projeto em formato Markdown estruturado.

## Uso

```
/generate-docs [escopo]

Exemplos:
/generate-docs api          → documenta todos os endpoints da API
/generate-docs modulo X     → documenta o módulo X (domínio, use cases, banco)
/generate-docs arquitetura  → gera/atualiza ARCHITECTURE.md
/generate-docs adr          → lista ADRs existentes e cria índice
/generate-docs readme       → atualiza README com estado atual do projeto
```

## Processo

### Etapa 1 — Análise do que documentar

Antes de escrever qualquer doc:
1. Ler o código relevante ao escopo solicitado
2. Verificar documentação existente (`docs/`, `README.md`, `.kiro/knowledge/`)
3. Identificar o que está desatualizado vs o que falta
4. Não recriar o que já existe e está correto — atualizar somente o necessário

### Etapa 2 — Padrão de nomenclatura

```
docs/
├── NN - doc_nome_descritivo.md      ← artigos técnicos
├── ARCHITECTURE.md                  ← visão arquitetural
├── DEVELOPMENT_PLAN.md              ← fases e status
├── API.md                           ← endpoints (ou openapi.yaml)
└── adr/
    ├── ADR-001-escolha-banco.md
    └── ADR-002-autenticacao.md
```

Onde `NN` = número de dois dígitos (00, 01, ...) para ordenação.

### Etapa 3 — Estrutura interna de um documento

```markdown
# TÍTULO DO DOCUMENTO
## Seção NNN — Subtítulo

---

## 1. PRIMEIRA SEÇÃO

Conteúdo...

### 1.1 Subseção

---

**Versão:** X.X
**Data:** DD/MM/AAAA
**Status:** Rascunho | Revisão | Aprovado
```

### Etapa 4 — Geração por tipo de escopo

**Para `/generate-docs api`:**
- Listar todos os controllers/routes
- Por endpoint: método HTTP, rota, autenticação, request body, responses
- Gerar em `docs/API.md` ou `openapi.yaml`

**Para `/generate-docs arquitetura`:**
- Diagrama C4 Level 1 (contexto) e Level 2 (containers)
- Decisão de dependência entre camadas
- Stack tecnológica com versões
- Gerar/atualizar `docs/ARCHITECTURE.md`

**Para `/generate-docs modulo X`:**
- Entidades e value objects do domínio
- Use cases disponíveis (input/output)
- Endpoints expostos
- Modelo de banco (tabelas, índices, relacionamentos)
- Regras de negócio relevantes

## Validação da documentação gerada

Antes de entregar, verificar:
- [ ] Sem informações inventadas — tudo baseado no código real
- [ ] Exemplos de código testados (não copiados sem verificar)
- [ ] Links internos funcionando
- [ ] Versão e data atualizadas
- [ ] Diagrama Mermaid renderiza corretamente (sem syntax errors)

## Formato de entrega

```
📄 Documentação gerada — [escopo]

Arquivos criados/atualizados:
- [caminho] — [criado/atualizado] — [N linhas]

Resumo do conteúdo:
- [seção 1]: [o que documenta]
- [seção 2]: [o que documenta]

Próxima atualização recomendada quando:
- [trigger: mudança de API, nova entidade, etc.]
```
