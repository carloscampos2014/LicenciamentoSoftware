---
inclusion: always
---

# Padrão de Briefing Detalhado

Todo briefing deve ser visualmente limpo e fácil de escanear. Use separadores, emojis de status e blocos de código curtos.

---

## Estrutura do briefing

### Cabeçalho

```
## 🔧 Briefing — #NNN Título da issue

> Uma frase resumindo o problema central e o impacto.
```

### Tabela de escopo (sempre presente)

```
| Arquivo | Método | O que muda |
|---------|--------|------------|
| Foo.cs  | Bar()  | descrição  |
```

### Detalhe por arquivo

Usar o formato abaixo para cada arquivo. Separar com `---`.

```
### 📄 NomeDoArquivo.cs — NomeDoMétodo()

**🐛 Problema**
O que acontece hoje, com trecho de código se ajudar:
\`\`\`csharp
// código problemático (máx 5 linhas)
\`\`\`

**💥 Consequência**
O que o usuário experimenta de forma concreta (ex: "widget mostra check-in não feito mesmo após registrar").

**✅ Correção**
O que muda — antes vs. depois:
\`\`\`csharp
// antes
foo();

// depois
var ok = await foo();
if (!ok) retentar();
\`\`\`

**💡 Decisão**
Por que essa abordagem em 1 frase.
```

### Rodapé

```
### ⛔ Fora do escopo
- Item que não será feito e por quê
```

---

## Regras de apresentação

- Máximo 5 linhas de código por bloco — resumir se maior
- Consequência sempre do ponto de vista do usuário, não do sistema
- Nunca usar termos vagos como "melhora robustez" sem explicar como
- Toda decisão de design precisa de razão em 1 frase
- Se forem mais de 4 arquivos, agrupar os que têm a mesma correção
