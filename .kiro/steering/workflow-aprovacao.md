---
inclusion: always
---

# Workflow de Aprovação antes de Implementar

Antes de implementar qualquer fase, feature ou conjunto de mudanças não triviais, o agente DEVE:

1. **Enviar um briefing** ao usuário com:
   - O que será criado/modificado (lista de arquivos)
   - O que cada parte faz (descrição concisa)
   - Dependências ou decisões de design relevantes
   - O que NÃO será feito (escopo negativo, se houver)

2. **Aguardar aprovação explícita** do usuário ("pode implementar", "aprovado", "sim", etc.) antes de escrever qualquer arquivo de código.

3. Só após aprovação: implementar, buildar e testar conforme os critérios da fase.

**Este fluxo se aplica a:**
- Implementação de fases do plano de desenvolvimento
- Novas features ou refactors de múltiplos arquivos
- Mudanças em arquitetura ou configuração de build/CI

**Não se aplica a:**
- Correções de build/lint/teste já em andamento
- Ajustes de arquivos de configuração simples (ex: .gitignore, appsettings)
- Perguntas, explicações ou análises sem escrita de código
