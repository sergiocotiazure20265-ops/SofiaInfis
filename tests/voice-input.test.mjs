import test from 'node:test';
import assert from 'node:assert/strict';
import { bindVoiceInput } from '../SofiaInfis.Web/wwwroot/voice-input.mjs';

class Element {
    listeners = new Map(); dataset = {}; attributes = {}; disabled = false; value = ''; maxLength = 2000;
    addEventListener(name, fn) { this.listeners.set(name, fn); }
    removeEventListener(name, fn) { if (this.listeners.get(name) === fn) this.listeners.delete(name); }
    setAttribute(name, value) { this.attributes[name] = value; }
    fire(name, event = {}) { this.listeners.get(name)?.(event); }
}
const flush = async () => { await new Promise(resolve => setImmediate(resolve)); };
function setup({ secure = true, supported = true, prefix = false, text = '', reject = null } = {}) {
    const button = new Element(), question = new Element(), root = new Element(), document = new Element();
    question.value = text;
    root.querySelector = selector => selector === 'textarea' ? question : button;
    const instances = [], calls = [], timers = new Map(); let timerId = 0;
    class Recognition {
        starts = 0; stops = 0; aborts = 0;
        constructor() { instances.push(this); }
        start() { this.starts++; }
        stop() { this.stops++; }
        abort() { this.aborts++; this.onend?.(); }
        result(parts) {
            this.onresult?.({ results: parts.map(([transcript, isFinal]) => Object.assign([{ transcript }], { isFinal })) });
        }
    }
    const environment = {
        isSecureContext: secure, document,
        setTimeout(fn) { timers.set(++timerId, fn); return timerId; },
        clearTimeout(id) { timers.delete(id); }
    };
    if (supported) environment[prefix ? 'webkitSpeechRecognition' : 'SpeechRecognition'] = Recognition;
    const binding = bindVoiceInput(root, { async invokeMethodAsync(method, ...args) {
        if (reject === method) throw new Error('Circuit unavailable');
        calls.push([method, ...args]);
    } }, environment);
    return { binding, button, question, root, document, instances, calls, timers };
}

test('starts only on an explicit click and configures Brazilian Portuguese', async () => {
    const s = setup({ prefix: true }); await flush();
    assert.equal(s.instances.length, 0);
    assert.deepEqual(s.calls[0], ['VoiceAvailabilityChanged', true, true]);
    s.button.fire('click'); const r = s.instances[0];
    assert.equal(r.starts, 1); assert.equal(r.lang, 'pt-BR'); assert.equal(r.continuous, false);
    assert.equal(r.interimResults, true); assert.equal(s.binding.active, true);
    r.onstart(); await flush();
    assert.ok(s.calls.some(c => c[0] === 'VoiceStateChanged' && c[1] === 'listening'));
    s.binding.dispose();
});

for (const options of [{ supported: false }, { secure: false }]) {
    test(`unsupported/insecure contexts keep typing available: ${JSON.stringify(options)}`, async () => {
        const s = setup({ ...options, text: 'Pergunta já digitada' }); await flush();
        assert.equal(s.calls[0][1], false);
        s.button.fire('click'); assert.equal(s.instances.length, 0);
        assert.equal(s.question.value, 'Pergunta já digitada');
        s.binding.dispose();
    });
}

test('preserves typed text and replaces cumulative results without duplicating words or sending', async () => {
    const s = setup({ text: 'Analise' }); s.button.fire('click'); const r = s.instances[0];
    r.result([['este documento', false]]); await flush();
    assert.equal(s.question.value, 'Analise');
    assert.ok(s.calls.some(c => c[0] === 'VoiceTranscriptChanged' && c[2] === 'este documento'));
    r.result([['este documento', true]]);
    r.result([['este documento', true], ['fiscal', false]]);
    r.result([['este documento', true], ['fiscal', true]]);
    await flush();
    assert.equal(s.question.value, 'Analise este documento fiscal');
    assert.deepEqual(s.calls.filter(c => c[0] === 'VoiceTranscriptChanged').at(-1), ['VoiceTranscriptChanged', 'Analise este documento fiscal', '', false]);
    assert.ok(s.calls.every(c => c[0].startsWith('Voice')));
    s.binding.dispose();
});

test('click and Escape stop recognition and final results remain editable', async () => {
    const s = setup(); s.button.fire('click'); const r = s.instances[0];
    s.root.fire('keydown', { key: 'Escape', preventDefault() {} });
    assert.equal(r.stops, 1);
    r.result([['Pergunta final reconhecida', true]]); r.onend(); await flush();
    assert.equal(s.binding.active, false); assert.equal(s.question.value, 'Pergunta final reconhecida');
    assert.ok(s.calls.some(c => c[0] === 'VoiceStateChanged' && c[1] === 'idle'));
    s.binding.dispose();
});

test('permission/network errors preserve the existing question and allow another attempt', async () => {
    const s = setup({ text: 'Texto preservado' }); s.button.fire('click');
    s.instances[0].onerror({ error: 'not-allowed' }); await flush();
    assert.equal(s.question.value, 'Texto preservado'); assert.equal(s.binding.active, false);
    assert.deepEqual(s.calls.filter(c => c[0] === 'VoiceError'), [['VoiceError', 'not-allowed']]);
    s.button.fire('click'); assert.equal(s.instances.length, 2);
    s.instances[1].onerror({ error: 'network' }); await flush();
    assert.deepEqual(s.calls.filter(c => c[0] === 'VoiceError').at(-1), ['VoiceError', 'network']);
    s.binding.dispose();
});

test('navigation/reset cancels pending text and late recognition events', async () => {
    const s = setup(); s.button.fire('click'); const r = s.instances[0];
    const lateResult = r.onresult;
    r.result([['Não deve entrar na conversa nova', true]]);
    s.binding.cancel(); s.question.value = '';
    lateResult({ results: [Object.assign([{ transcript: 'Evento atrasado' }], { isFinal: true })] });
    await flush();
    assert.equal(s.calls.filter(c => c[0] === 'VoiceTranscriptChanged').length, 0);
    assert.equal(s.question.value, ''); assert.equal(s.binding.active, false); assert.equal(r.aborts, 1);
    s.binding.dispose();
});

test('enforces maxlength without splitting Unicode and reports that text was limited', async () => {
    const s = setup({ text: 'a'.repeat(1998) }); s.button.fire('click'); const r = s.instances[0];
    r.result([['😀 mais conteúdo', true]]); await flush();
    assert.ok(s.question.value.length <= 2000);
    assert.equal(/[\uD800-\uDBFF]$/.test(s.question.value), false);
    assert.equal(s.calls.filter(c => c[0] === 'VoiceTranscriptChanged').at(-1)[3], true);
    assert.equal(r.aborts, 1); assert.equal(s.binding.active, false);
    s.binding.dispose();
});

test('limits capture to one minute and clears its timer on completion', async () => {
    const s = setup(); s.button.fire('click'); const r = s.instances[0];
    [...s.timers.values()][0](); await flush();
    assert.deepEqual(s.calls.filter(c => c[0] === 'VoiceError'), [['VoiceError', 'timeout']]);
    assert.equal(r.aborts, 1); assert.equal(s.timers.size, 0);
    s.binding.dispose();
});

test('leaving the tab ends capture and retains finalized text', async () => {
    const s = setup(); s.button.fire('click'); const r = s.instances[0];
    r.result([['Texto final', true]]); s.document.hidden = true; s.document.fire('visibilitychange'); await flush();
    assert.equal(s.question.value, 'Texto final'); assert.equal(r.aborts, 1);
    assert.deepEqual(s.calls.filter(c => c[0] === 'VoiceError'), [['VoiceError', 'hidden']]);
    s.binding.dispose();
});

test('disposing or losing the Blazor circuit releases capture and removes handlers', async () => {
    const s = setup({ reject: 'VoiceTranscriptChanged' }); await flush();
    s.button.fire('click'); const r = s.instances[0]; r.result([['Texto', true]]); await flush();
    assert.equal(r.aborts, 1); assert.equal(s.binding.active, false);
    assert.equal(s.button.listeners.size, 0); assert.equal(s.root.listeners.size, 0); assert.equal(s.document.listeners.size, 0);
    s.binding.dispose(); assert.equal(r.aborts, 1);
});

test('disabled microphone cannot start and repeated clicks do not create duplicate recognizers', async () => {
    const s = setup(); s.button.disabled = true; s.button.fire('click'); assert.equal(s.instances.length, 0);
    s.button.disabled = false; s.button.fire('click'); s.button.fire('click');
    assert.equal(s.instances.length, 1); assert.equal(s.instances[0].starts, 1); assert.equal(s.instances[0].stops, 1);
    s.binding.dispose();
});
