// Local synthetic contract double. No real student service or fiscal conclusion.
const http = require('http');
http.createServer(async (req, res) => {
  const path = new URL(req.url, 'http://localhost').pathname;
  const send = (status, body) => { res.writeHead(status, {'Content-Type': 'application/json'}); res.end(JSON.stringify(body)); };
  if (req.method === 'GET' && path === '/.well-known/agent-card.json') {
    return send(200, { protocolVersion: '0.3.0', preferredTransport: 'JSONRPC', name: 'postman-fiscal',
      description: 'Synthetic specialist', version: '1.0.0', url: 'http://127.0.0.1:5190/fiscal/rpc',
      capabilities: {streaming: false, pushNotifications: false}, defaultInputModes: ['text/plain'], defaultOutputModes: ['text/plain'],
      skills: [{id: 'analisar_documentos', name: 'Analysis', description: 'Synthetic analysis', tags: ['test']}],
      securitySchemes: {sofiaService: {type: 'http', scheme: 'bearer'}}, security: [{sofiaService: []}] });
  }
  if (req.method === 'POST' && path === '/rpc') {
    if (req.headers.authorization !== 'Bearer postman-local-test-only') return send(401, {error: 'Unauthorized'});
    let raw = ''; if (!req.body) for await (const chunk of req) raw += chunk;
    try {
      const rpc = typeof req.body === 'object' ? req.body : JSON.parse(req.body || raw);
      if (rpc.method !== 'message/send') return send(501, {error: 'Not modeled'});
      const payload = JSON.parse(rpc.params.message.parts[0].text);
      const analysis = {versaoContrato: '1.0', idExecucao: payload.idExecucao, idSolicitacao: payload.idSolicitacao,
        agente: {idAgente: 'postman-fiscal', nome: 'postman-fiscal'}, status: 'concluido',
        analise: 'Synthetic contract result; no real fiscal assertion.', achados: []};
      return send(200, {jsonrpc: '2.0', id: rpc.id, result: {kind: 'task', id: 'task-' + payload.idSolicitacao,
        contextId: 'ctx-' + payload.idExecucao, status: {state: 'completed'},
        artifacts: [{artifactId: 'analysis', name: 'analise-sofia-v1', parts: [{kind: 'text', text: JSON.stringify(analysis)}]}]}});
    } catch { return send(400, {error: 'Invalid synthetic request'}); }
  }
  return send(501, {error: 'Not modeled'});
}).listen(process.env.PORT || 5190, '127.0.0.1');
