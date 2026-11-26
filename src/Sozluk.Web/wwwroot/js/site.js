(function () {
    const token = () => document.querySelector('#af input[name="__RequestVerificationToken"]')?.value;

    async function post(url, data) {
        const body = new URLSearchParams(data || {});
        const res = await fetch(url, {
            method: 'POST',
            headers: { 'RequestVerificationToken': token(), 'Content-Type': 'application/x-www-form-urlencoded' },
            body
        });
        if (res.status === 401 || res.redirected) { location.href = '/giris?returnUrl=' + encodeURIComponent(location.pathname); return null; }
        const json = await res.json().catch(() => null);
        if (!res.ok) { alert(json?.error || 'işlem başarısız'); return null; }
        return json;
    }

    document.addEventListener('click', async (ev) => {
        const btn = ev.target.closest('button');
        if (!btn) return;
        const entry = btn.closest('.entry');

        if (entry && btn.classList.contains('vote')) {
            ev.preventDefault();
            const r = await post(`/entry/${entry.dataset.id}/oy`, { value: btn.dataset.value });
            if (!r) return;
            const up = entry.querySelector('.vote.up'), down = entry.querySelector('.vote.down');
            up.querySelector('span').textContent = r.up;
            down.querySelector('span').textContent = r.down;
            up.classList.toggle('on', r.mine > 0);
            down.classList.toggle('on', r.mine < 0);
        } else if (entry && btn.classList.contains('fav')) {
            ev.preventDefault();
            const r = await post(`/entry/${entry.dataset.id}/favori`);
            if (!r) return;
            btn.querySelector('span').textContent = r.count;
            btn.classList.toggle('on', r.mine);
        } else if (entry && btn.classList.contains('report')) {
            ev.preventDefault();
            const reason = prompt('şikayet sebebi?');
            if (reason === null) return;
            const r = await post(`/entry/${entry.dataset.id}/sikayet`, { reason });
            if (r) { btn.textContent = 'şikayet edildi'; btn.disabled = true; }
        } else if (btn.dataset.wrap) {
            ev.preventDefault();
            const ta = btn.closest('form').querySelector('textarea');
            const s = ta.selectionStart, e = ta.selectionEnd, sel = ta.value.slice(s, e);
            const map = { bkz: ['(bkz: ', ')'], hede: ['`', '`'], ara: ['(ara: ', ')'] };
            const [a, b] = map[btn.dataset.wrap];
            ta.setRangeText(a + sel + b, s, e, sel ? 'end' : 'select');
            if (!sel) ta.setSelectionRange(s + a.length, s + a.length);
            ta.focus();
        }
    });
})();
