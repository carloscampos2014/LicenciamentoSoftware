---
inclusion: always
---

# Knowledge RAG — Memória e Busca Semântica

Este projeto tem dois MCPs de memória disponíveis. Use-os ativamente — não espere o usuário pedir.

---

## Dois MCPs, dois propósitos

### `knowledge-rag` — busca semântica em documentos

Indexa e busca em `.kiro/knowledge/` (sessões, patterns, specs, docs).
Use quando precisar de contexto que não está no steering atual.

**Quando usar:**
- Antes de iniciar qualquer task de um spec — buscar decisões anteriores relacionadas
- Quando o usuário mencionar algo que pode ter histórico ("aquele bug do check-in", "a decisão sobre Dapper")
- Quando encontrar um padrão de código desconhecido — buscar se já foi documentado
- Antes de propor uma arquitetura — verificar se já existe ADR sobre o tema

**Como usar:**
```
// Buscar contexto antes de implementar
knowledge_rag_search("autenticação JWT ProvaVida")
knowledge_rag_search("decisão Dapper vs EF Core")
knowledge_rag_search("bug check-in mobile OnAppearing")
```

**Quando NÃO usar:**
- Para buscar código-fonte (use as ferramentas de leitura de arquivo normais)
- Para sessões triviais sem histórico relevante

---

### `memory` — grafo de conhecimento persistente

Armazena entidades, relações e observações que devem sobreviver entre sessões.
Diferente do knowledge-rag (que busca em arquivos), o memory *escreve* fatos estruturados.

**Quando usar — ESCREVER:**
- Ao descobrir uma decisão de design importante → criar entidade
- Ao resolver um bug não-óbvio → registrar como observação
- Ao identificar uma dependência crítica entre módulos → criar relação
- Ao final de sessão com descobertas importantes

**Quando usar — LER:**
- No início de sessão sobre um tema específico — carregar contexto do grafo
- Antes de refatorar um módulo — verificar relações conhecidas

**Como usar:**
```
// Registrar decisão
memory_create_entities([{
  name: "DecisaoDapper",
  entityType: "ADR",
  observations: ["Usamos Dapper em vez de EF Core por performance em queries complexas de relatório"]
}])

// Registrar relação
memory_create_relations([{
  from: "CheckinService",
  to: "HangfireJob",
  relationType: "dispara"
}])

// Consultar no início da sessão
memory_search_nodes("autenticação")
```

---

## Fluxo recomendado por situação

### Iniciando trabalho em uma feature nova
1. `knowledge_rag_search("[nome da feature]")` — há histórico?
2. `memory_search_nodes("[módulo envolvido]")` — há relações/decisões registradas?
3. Prosseguir com contexto completo

### Ao encontrar um bug
1. `knowledge_rag_search("[descrição do bug]")` — já foi visto antes?
2. Resolver
3. `memory_create_entities` com o mecanismo do bug e da correção

### Ao encerrar sessão com descobertas importantes
1. O hook `session-summary` já salva em `.kiro/knowledge/sessions/`
2. Complementar com `memory_create_entities` para fatos estruturados que merecem ser recuperáveis por nome

---

## O que NÃO fazer

- Não ignorar esses MCPs — eles existem para evitar retrabalho e decisões conflitantes
- Não duplicar no memory o que já está nos arquivos de sessão — o knowledge-rag já indexa esses arquivos
- Não registrar no memory fatos óbvios ou temporários — apenas decisões e descobertas que uma sessão futura precisaria saber
