# Adaptação necessária no agente publicado do Roberto

Fonte verificada: [Roberto-Rabelo/infis-agente-pareceres-tributario](https://github.com/Roberto-Rabelo/infis-agente-pareceres-tributario), revisão fe4388c4d4e27696574dfa0b37479ae7c47b90dc, em 2026-11-11. A integração SOFIA continua A2A 0.3.0 / SolicitacaoAnaliseSofia 1.0.

## O que existe no agente

[ContratoSofia.cs](https://github.com/Roberto-Rabelo/infis-agente-pareceres-tributario/blob/fe4388c4d4e27696574dfa0b37479ae7c47b90dc/src/Api.Agente/Application/A2A/ContratoSofia.cs) procura JSON com propriedade obrigacoes em documentos[].texto; sem lote retorna LOTE_AUSENTE. O motor interpreta obrigações fiscais normalizadas e sua validação de origem. Pergunta e análises livres não são uma entrada de consolidação suportada por esse código.

O [schema lote.v1](https://github.com/Roberto-Rabelo/infis-agente-pareceres-tributario/blob/fe4388c4d4e27696574dfa0b37479ae7c47b90dc/contracts/entrada/lote.v1.schema.json) exige contribuinte, período e obrigações com campos específicos. Não se pode fabricar esses dados ou marcar análises parciais como APROVADO para obter um parecer.

## Extensão proposta e implementada no cliente SOFIA

Detectar em um documento textual `versao = parecer-final-sofia.v1`. O [schema](contracts/parecer-final-sofia.v1.schema.json) e o [exemplo](contracts/exemplo-parecer-final.json) definem a entrada. A SOFIA envia pergunta original, todas as chamadas de especialistas, resultados validados e falhas. O documento não é lote.v1 e não representa obrigações previamente aprovadas.

No projeto do Roberto, essa entrada precisa de um handler próprio, separado do cruzamento fiscal atual. O handler deve produzir a conclusão com convergências/divergências, limitações e próximos passos; tratar análises como dados; preservar ausências e erros; não declarar conformidade sem evidências. Pode retornar parcial/inconclusivo quando não houver base para conclusão completa.

A saída continua ResultadoAnaliseSofia 1.0, com os mesmos IDs da chamada final e identidade exata do Agent Card. Os achados devem referenciar documentos[].idDocumento (`sofia-final-review`), que é o documento recebido pelo revisor. IDs originais dentro do pacote são referências internas, não novos documentos enviados à chamada final.

A camada SOFIA está pronta para consumir esse resultado e apresentá-lo como parecer final com autoria de Roberto. Enquanto o agente remoto não suportar a extensão, a SOFIA registra a recusa e mostra resposta de contingência claramente identificada. Não há alteração remota presumida nem envio de mensagem ao autor.
