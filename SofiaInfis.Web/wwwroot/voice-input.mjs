// The browser owns microphone permission and speech recognition. No audio is
// recorded or uploaded by SOFIA; only the transcription reaches Blazor.
export function bindVoiceInput(root, dotnet, environment = globalThis) {
    const button = root.querySelector('[data-voice-button]');
    const question = root.querySelector('textarea');
    const Recognition = environment.SpeechRecognition || environment.webkitSpeechRecognition;
    const supported = typeof Recognition === 'function' && environment.isSecureContext === true;
    let disposed = false, current = null, latest = null, notifications = Promise.resolve();

    const notify = (method, args = [], session = null) => {
        notifications = notifications.then(() => {
            if (!disposed && !session?.cancelled) return dotnet.invokeMethodAsync(method, ...args);
        }).catch(() => dispose()); // A lost circuit must not leave the microphone running.
    };
    const state = (value, session = null) => {
        if (button) {
            button.dataset.voiceState = value;
            button.setAttribute('aria-pressed', value === 'idle' ? 'false' : 'true');
        }
        notify('VoiceStateChanged', [value], session);
    };
    const finish = (session, error = null) => {
        if (session.closed) return;
        session.closed = true;
        environment.clearTimeout(session.timer);
        if (current === session) current = null;
        if (error) {
            if (button) { button.dataset.voiceState = 'idle'; button.setAttribute('aria-pressed', 'false'); }
            notify('VoiceError', [error], session);
        } else state('idle', session);
    };
    const abort = session => { try { session.recognition.abort(); } catch { /* Already stopped. */ } };
    const cancel = () => {
        if (latest) latest.cancelled = true; // Discard callbacks queued before navigation/reset.
        if (current) { const session = current; finish(session); abort(session); }
    };
    const stop = () => {
        if (!current) return;
        const session = current;
        if (session.stopping) { finish(session); abort(session); return; }
        session.stopping = true;
        state('processing', session);
        try { session.recognition.stop(); }
        catch { finish(session); abort(session); }
    };
    const limitText = (text, maximum) => {
        let value = text.slice(0, maximum);
        // maxlength is in UTF-16 units. Never cut a surrogate pair in half.
        if (value.length < text.length && /[\uD800-\uDBFF]$/.test(value)) value = value.slice(0, -1);
        return value;
    };
    const click = () => {
        if (disposed || !supported || !button || button.disabled || !question) return;
        if (current) { stop(); return; }
        if (latest) latest.cancelled = true;
        let recognition;
        try { recognition = new Recognition(); }
        catch { notify('VoiceError', ['not-supported']); return; }
        const session = { recognition, base: question.value, closed: false, cancelled: false, stopping: false, timer: null };
        current = latest = session;
        recognition.lang = 'pt-BR';
        recognition.continuous = false;
        recognition.interimResults = true;
        recognition.maxAlternatives = 1;
        state('requesting', session);
        recognition.onstart = () => {
            if (disposed || session.closed || session !== current) { abort(session); return; }
            state('listening', session);
        };
        recognition.onresult = event => {
            if (disposed || session.closed || session !== current) return;
            const final = [], interim = [];
            // Results are cumulative; rebuilding prevents duplicate finalized words.
            for (let i = 0; i < event.results.length; i++) {
                const result = event.results[i];
                (result.isFinal ? final : interim).push(result[0].transcript.trim());
            }
            const spoken = final.join(' ').trim();
            const combined = session.base + (session.base && spoken && !/\s$/.test(session.base) ? ' ' : '') + spoken;
            const maximum = question.maxLength > 0 ? question.maxLength : 2000;
            const limited = combined.length > maximum;
            const text = limitText(combined, maximum);
            question.value = text;
            notify('VoiceTranscriptChanged', [text, limitText(interim.join(' '), maximum), limited], session);
            if (limited) { finish(session); abort(session); }
        };
        recognition.onerror = event => {
            if (disposed || session.closed || session !== current) return;
            finish(session, event.error || 'unknown');
            abort(session);
        };
        recognition.onend = () => {
            finish(session);
            recognition.onstart = recognition.onresult = recognition.onerror = recognition.onend = null;
        };
        session.timer = environment.setTimeout(() => {
            if (current === session && !session.closed) { finish(session, 'timeout'); abort(session); }
        }, 60000);
        try { recognition.start(); } // Synchronous native click preserves user activation.
        catch (error) { finish(session, error?.name === 'NotAllowedError' ? 'not-allowed' : 'start-failed'); abort(session); }
    };
    const key = event => { if (event.key === 'Escape' && current) { event.preventDefault(); stop(); } };
    const visibility = () => {
        if (environment.document?.hidden && current) { const session = current; finish(session, 'hidden'); abort(session); }
    };
    function dispose() {
        if (disposed) return;
        disposed = true;
        cancel();
        button?.removeEventListener('click', click);
        root.removeEventListener('keydown', key);
        environment.document?.removeEventListener('visibilitychange', visibility);
    }
    button?.addEventListener('click', click);
    root.addEventListener('keydown', key);
    environment.document?.addEventListener('visibilitychange', visibility);
    notify('VoiceAvailabilityChanged', [supported, environment.isSecureContext === true]);
    return { dispose, cancel, stop, get active() { return current !== null; } };
}
