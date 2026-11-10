# Especificações da SOFIA

Atualizado em 2026-11-05. As duas entregas estão implementadas e possuem validação
local registrada. Isso não equivale a validação com agentes reais ou aceite de produção.

| Feature | Escopo | Documentos |
|---|---|---|
|001 — Integração A2A | Backend, contratos, descoberta, delegação, histórico e segurança | [Spec](001-integracao-a2a/spec.md), [plano](001-integracao-a2a/plan.md), [tarefas](001-integracao-a2a/tasks.md), [validação](001-integracao-a2a/validation-results.md) |
|002 — Interface Blazor | Apresentação, nome, chat, documentos, agentes, síntese e rastreamento | [Spec](002-interface-blazor/spec.md), [plano](002-interface-blazor/plan.md), [quickstart](002-interface-blazor/quickstart.md), [validação](../SofiaInfis.Web/VALIDACAO.md) |

O backend 001 não foi reespecificado para incorporar a UI. A 002 documenta seu
consumidor e a síntese sob demanda pelo endpoint legado, mantendo os contratos.
O diagnóstico inicial é [histórico](../RELATORIO_ARQUITETURA_ATUAL_SOFIA.md);
o estado consolidado está na [arquitetura atual](../docs/ARQUITETURA_SOFIA.md).

O registro 002 foi consolidado após a implementação, com evidências já existentes.
Ele não representa execução retroativa de comandos Spec Kit ou uma nova rodada
de testes. Nenhuma branch, checklist de requisitos ou constituição foi alterada.

## Contexto dos comandos Spec Kit

Há mais de uma feature na branch `main`. Para apontar comandos futuros à 002 sem
persistir mudança no seletor do repositório, o helper existente aceita:

```powershell
$env:SPECIFY_FEATURE_DIRECTORY = (Resolve-Path 'specs/002-interface-blazor').Path
$env:SPECIFY_FEATURE_NO_PERSIST = '1'
```

Essas variáveis não foram aplicadas por esta atualização documental. Se o próximo
comando exigir artefatos adicionais (por exemplo, tasks.md), eles devem ser
elaborados para o trabalho solicitado; não inferir tarefas pendentes apenas da
ausência de um arquivo gerado numa documentação retrospectiva.
