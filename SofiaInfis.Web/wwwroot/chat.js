export function bindComposer(root, dotnet) {
    let depth = 0;
    const over = e => { if (e.dataTransfer?.types.includes('Files')) { e.preventDefault(); e.dataTransfer.dropEffect = 'copy'; } };
    const enter = e => { if (e.dataTransfer?.types.includes('Files')) { e.preventDefault(); depth++; root.classList.add('dragging'); } };
    const leave = e => { e.preventDefault(); if (--depth <= 0) { depth = 0; root.classList.remove('dragging'); } };
    const drop = e => {
        e.preventDefault(); depth = 0; root.classList.remove('dragging');
        const input = root.querySelector('input[type=file]');
        if (!input || input.disabled || !e.dataTransfer?.files.length) return;
        input.files = e.dataTransfer.files;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    };
    const key = e => { if (e.target.tagName === 'TEXTAREA' && e.key === 'Enter' && !e.shiftKey && !e.isComposing) { e.preventDefault(); dotnet.invokeMethodAsync('SendFromKeyboard'); } };
    root.addEventListener('dragenter', enter); root.addEventListener('dragover', over); root.addEventListener('dragleave', leave); root.addEventListener('drop', drop); root.addEventListener('keydown', key);
    return { dispose() { root.removeEventListener('dragenter', enter); root.removeEventListener('dragover', over); root.removeEventListener('dragleave', leave); root.removeEventListener('drop', drop); root.removeEventListener('keydown', key); } };
}
export function follow(area) { requestAnimationFrame(() => { if (area.scrollHeight - area.scrollTop - area.clientHeight < 320) area.scrollTo({ top: area.scrollHeight, behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth' }); }); }
export function scrollToTurn(id) { document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' }); }
export function focusDialog(root) {
    root.addEventListener('keydown', e => {
        if (e.key !== 'Tab') return;
        const controls = [...root.querySelectorAll('input,button')].filter(el => !el.disabled);
        const first = controls[0], last = controls.at(-1);
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
    });
}
