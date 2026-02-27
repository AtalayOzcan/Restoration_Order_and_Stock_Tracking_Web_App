document.addEventListener("DOMContentLoaded", () => {

    // ── 1. C# Verilerini HTML'den (JSON Adacığından) Oku ──
    const configEl = document.getElementById('orderConfigData');
    let config = { orderTotal: 0, alreadyPaid: 0, orderId: 0 };

    if (configEl) {
        config = JSON.parse(configEl.textContent);
    }

    const orderTotal = parseFloat(config.orderTotal);
    const alreadyPaid = parseFloat(config.alreadyPaid);
    const orderId = parseInt(config.orderId);
    let currentMethod = 'cash';

    // ── helpers ──────────────────────────────────────────────────
    function parseLD(str) {
        if (!str) return 0;
        const v = parseFloat(str.trim().replace(/\./g, '').replace(',', '.'));
        return isNaN(v) ? 0 : v;
    }
    function fmt(n) { return '₺' + n.toFixed(2).replace('.', ','); }

    // ── modal (HTML'den ulaşılabilmesi için window'a ekliyoruz) ──
    window.openModal = function (id) { document.getElementById(id).classList.add('open'); };
    window.closeModal = function (id) { document.getElementById(id).classList.remove('open'); };

    document.querySelectorAll('.modal-overlay').forEach(o => {
        o.addEventListener('click', e => { if (e.target === o) o.classList.remove('open'); });
    });

    // ═══════════════════════════════════════════════════════════
    // AIM — Çoklu Ürün Ekleme (Mini Sepet Mantığı)
    // ═══════════════════════════════════════════════════════════
    let aimPrice = 0;
    let aimQty = 1;
    let aimCurId = null;   // seçili ürün id
    let aimCurName = '';
    let aimCatActive = 'all';
    let aimBasket = [];

    // ── Ürün seç ──
    window.aimPick = function (id) {
        const row = document.getElementById('arow-' + id);
        if (!row) return;

        document.querySelectorAll('.aim-row.picked').forEach(r => r.classList.remove('picked'));
        row.classList.add('picked');

        aimCurId = parseInt(id);
        aimCurName = row.dataset.name;
        aimPrice = parseFloat(row.dataset.price);
        aimQty = 1;

        document.getElementById('aimSelName').textContent = aimCurName;
        document.getElementById('aimSelUnit').textContent = fmt(aimPrice) + ' / adet';
        document.getElementById('aimNoteInp').value = '';
        aimRefresh();

        document.getElementById('aimPh').style.display = 'none';
        document.getElementById('aimForm').style.display = 'flex';
        document.getElementById('aimForm').style.flexDirection = 'column';
    };

    window.aimDelta = function (d) {
        aimQty = Math.max(1, Math.min(99, aimQty + d));
        aimRefresh();
    };

    function aimRefresh() {
        document.getElementById('aimQtyNum').textContent = aimQty;
        document.getElementById('aimTotal').textContent = fmt(aimPrice * aimQty);
        document.getElementById('aimAddBtn').textContent =
            '+ Sepete Ekle (' + aimQty + ' adet — ' + fmt(aimPrice * aimQty) + ')';
    }

    // ── Sepete ekle ──
    window.aimAddToBasket = function () {
        if (!aimCurId) return;
        const note = (document.getElementById('aimNoteInp').value || '').trim();

        const existing = aimBasket.find(i => i.id === aimCurId && i.note === note);
        if (existing) {
            existing.qty = Math.min(99, existing.qty + aimQty);
        } else {
            aimBasket.push({ id: aimCurId, name: aimCurName, price: aimPrice, qty: aimQty, note });
        }

        aimQty = 1;
        document.getElementById('aimNoteInp').value = '';
        aimRefresh();

        document.getElementById('aimPh').style.display = '';
        document.getElementById('aimForm').style.display = 'none';
        document.querySelectorAll('.aim-row.picked').forEach(r => r.classList.remove('picked'));
        aimCurId = null;

        aimRenderBasket();
        document.getElementById('aimBasketWrap').scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    };

    // ── Sepet render ──
    function aimRenderBasket() {
        const list = document.getElementById('aimBasketList');
        const empty = document.getElementById('aimBasketEmpty');
        const countEl = document.getElementById('aimBasketCount');
        const totalEl = document.getElementById('aimBasketTotalVal');
        const sendBtn = document.getElementById('aimSendBtn');

        Array.from(list.children).forEach(child => {
            if (child !== empty) child.remove();
        });

        if (!aimBasket.length) {
            empty.style.display = '';
            countEl.textContent = '0';
            totalEl.textContent = '₺0,00';
            sendBtn.disabled = true;
            sendBtn.textContent = '✓ Tümünü Adisyona Gönder';
            return;
        }

        empty.style.display = 'none';
        let total = 0;

        aimBasket.forEach((item, idx) => {
            total += item.price * item.qty;
            const div = document.createElement('div');
            div.className = 'aim-basket-item';
            div.innerHTML =
                `<span class="aim-bi-name" title="${item.name}">${item.name}</span>` +
                `<span class="aim-bi-qty">×${item.qty}</span>` +
                `<span class="aim-bi-price">${fmt(item.price * item.qty)}</span>` +
                `<button type="button" class="aim-bi-del" onclick="aimRemoveFromBasket(${idx})" title="Kaldır">×</button>`;
            if (item.note) {
                const noteEl = document.createElement('span');
                noteEl.className = 'aim-bi-note';
                noteEl.textContent = '📝 ' + item.note;
                div.appendChild(noteEl);
            }
            list.appendChild(div);
        });

        const totalItems = aimBasket.reduce((s, i) => s + i.qty, 0);
        countEl.textContent = totalItems;
        totalEl.textContent = fmt(total);
        sendBtn.disabled = false;
        sendBtn.textContent = '✓ ' + totalItems + ' Ürünü Adisyona Gönder';
    }

    window.aimRemoveFromBasket = function (idx) {
        aimBasket.splice(idx, 1);
        aimRenderBasket();
    };

    window.aimClearBasket = function () {
        aimBasket = [];
        aimRenderBasket();
    };

    // ── Tümünü gönder (Bulk) ──
    window.aimSendAll = async function () {
        if (!aimBasket.length) return;

        const btn = document.getElementById('aimSendBtn');
        btn.disabled = true;
        btn.textContent = '⏳ Gönderiliyor...';

        const payload = {
            orderId: orderId, // JSON'dan gelen değer
            items: aimBasket.map(i => ({
                menuItemId: i.id,
                quantity: i.qty,
                note: i.note || null
            }))
        };

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

        try {
            const res = await fetch('/Orders/AddItemBulk', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(payload)
            });

            // Oturum düştüyse sunucu HTML login sayfasına yönlendirir (302→200)
            // Content-Type kontrolüyle bunu yakala ve sayfayı yenile
            const ct = res.headers.get('content-type') || '';
            if (res.status === 401 || (!ct.includes('application/json') && !res.ok)) {
                alert('Oturumunuz sona erdi. Giriş sayfasına yönlendiriliyorsunuz.');
                window.location.href = '/Auth/Login';
                return;
            }

            if (res.ok) {
                window.closeModal('addItemModal');
                location.reload();
            } else {
                const data = await res.json().catch(() => ({}));
                btn.disabled = false;
                btn.textContent = '✓ Tümünü Adisyona Gönder';
                alert('Hata: ' + (data.error || 'Bilinmeyen hata'));
            }
        } catch (e) {
            btn.disabled = false;
            btn.textContent = '✓ Tümünü Adisyona Gönder';
            alert('İstek gönderilemedi. Lütfen sayfayı yenileyip tekrar deneyin.');
        }
    };

    // ── Kategori tab ──
    window.aimCat = function (catKey, btn) {
        aimCatActive = catKey;
        document.querySelectorAll('.aim-ctab').forEach(b => b.classList.remove('on'));
        if (btn) btn.classList.add('on');
        document.getElementById('aimQ').value = '';
        document.getElementById('aimLbl').classList.remove('on');
        document.querySelectorAll('.aim-row').forEach(r => r.style.display = '');
        document.querySelectorAll('.aim-cat-blk').forEach(b => {
            b.style.display = (catKey === 'all' || b.id === catKey) ? '' : 'none';
        });
        document.getElementById('aimNoRes').classList.remove('on');
    };

    // ── Arama ──
    window.aimSearch = function (q) {
        q = (q || '').toLowerCase().trim();
        const lbl = document.getElementById('aimLbl');
        if (!q) {
            window.aimCat(aimCatActive, document.querySelector('.aim-ctab[data-c="' + aimCatActive + '"]'));
            return;
        }
        document.querySelectorAll('.aim-ctab').forEach(b => b.classList.remove('on'));
        document.querySelectorAll('.aim-cat-blk').forEach(b => b.style.display = '');
        let n = 0;
        document.querySelectorAll('.aim-row').forEach(r => {
            const ok = r.dataset.kw.includes(q);
            r.style.display = ok ? '' : 'none';
            if (ok) n++;
        });
        document.querySelectorAll('.aim-cat-blk').forEach(b => {
            const any = [...b.querySelectorAll('.aim-row')].some(r => r.style.display !== 'none');
            b.style.display = any ? '' : 'none';
        });
        lbl.textContent = '"' + q + '" — ' + n + ' ürün';
        lbl.classList.toggle('on', true);
        document.getElementById('aimNoRes').classList.toggle('on', n === 0);
    };

    // ── Modal açıldığında reset ──
    const _origOpenModal = window.openModal;
    window.openModal = function (id) {
        _origOpenModal(id);
        if (id === 'addItemModal') {
            aimBasket = [];
            aimCurId = null;
            aimQty = 1;
            document.querySelectorAll('.aim-row.picked').forEach(r => r.classList.remove('picked'));
            document.getElementById('aimPh').style.display = '';
            document.getElementById('aimForm').style.display = 'none';
            document.getElementById('aimQ').value = '';
            window.aimSearch('');
            window.aimCat('all', document.querySelector('.aim-ctab[data-c="all"]'));
            aimRenderBasket();
        }
    };

    // ═══════════════════════════════════════════════════════════
    // ÖDEME MODAL: kalem seçimi
    // ═══════════════════════════════════════════════════════════
    const piselState = {};
    document.querySelectorAll('.pisel-row').forEach(row => {
        const id = parseInt(row.dataset.itemId);
        const max = parseInt(row.dataset.maxQty);
        const up = parseFloat(row.dataset.unitPrice);
        piselState[id] = { selected: 0, max, up };
    });

    window.piselChange = function (id, delta) {
        const s = piselState[id];
        if (!s || s.max === 0) return;
        s.selected = Math.max(0, Math.min(s.max, s.selected + delta));

        const qEl = document.getElementById('pisel-qty-' + id);
        qEl.textContent = s.selected;
        qEl.classList.toggle('has-sel', s.selected > 0);

        const sub = s.selected * s.up;
        document.getElementById('pisel-sub-' + id).textContent = s.selected > 0 ? fmt(sub) : '—';

        document.getElementById('pisel-minus-' + id).style.opacity = s.selected === 0 ? '0.3' : '1';
        document.getElementById('pisel-plus-' + id).style.opacity = s.selected === s.max ? '0.3' : '1';

        updatePiselTotal();
    };

    function updatePiselTotal() {
        let t = 0;
        Object.values(piselState).forEach(s => { t += s.selected * s.up; });
        document.getElementById('piselTotalVal').textContent = fmt(t);
        document.getElementById('piselApplyBtn').disabled = t <= 0;
    }

    window.applyPisel = function () {
        let t = 0;
        Object.values(piselState).forEach(s => { t += s.selected * s.up; });
        if (t <= 0) return;
        document.getElementById('payAmountDisplay').value = t.toFixed(2).replace('.', ',');
        window.updateChange();
    };

    // ── ödeme formu ──
    window.selectMethod = function (btn, method) {
        document.querySelectorAll('.method-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        document.getElementById('selectedMethod').value = method;
        currentMethod = method;
        document.getElementById('changeRow').style.display = method === 'cash' ? 'block' : 'none';
        window.updateChange();
    };

    window.updateRemaining = function () {
        const disc = parseLD(document.getElementById('discountDisplay').value);
        const net = Math.max(0, orderTotal - disc - alreadyPaid);
        const netFull = Math.max(0, orderTotal - disc);
        document.getElementById('pm-remaining').textContent = fmt(net);
        document.getElementById('fillAmountLabel').textContent = fmt(net);
        const dr = document.getElementById('pm-disc-row');
        const dl = document.getElementById('disc-lbl');
        if (disc > 0) {
            dr.style.display = 'flex';
            dl.style.display = 'inline';
            document.getElementById('pm-disc-val').textContent = '−' + fmt(disc);
            document.getElementById('net-amount').textContent = netFull.toFixed(2).replace('.', ',');
        } else {
            dr.style.display = 'none';
            dl.style.display = 'none';
        }
        window.updateChange();
    };

    window.fillRemaining = function () {
        const disc = parseLD(document.getElementById('discountDisplay').value);
        const rem = Math.max(0, orderTotal - disc - alreadyPaid);
        document.getElementById('payAmountDisplay').value = rem.toFixed(2).replace('.', ',');
        window.updateChange();
    };

    window.updateChange = function () {
        if (currentMethod !== 'cash') return;
        const disc = parseLD(document.getElementById('discountDisplay').value);
        const rem = Math.max(0, orderTotal - disc - alreadyPaid);
        const entered = parseLD(document.getElementById('payAmountDisplay').value);
        const change = Math.max(0, entered - rem);
        document.getElementById('changeDisplay').textContent = fmt(change);
    };

    window.syncPayForm = function () {
        const payVal = parseLD(document.getElementById('payAmountDisplay').value);
        const discVal = parseLD(document.getElementById('discountDisplay').value);
        const err = document.getElementById('err-amount');
        if (payVal <= 0) { err.style.display = 'block'; return false; }
        err.style.display = 'none';
        document.getElementById('paymentAmountStr').value = payVal.toFixed(2);
        document.getElementById('discountAmountStr').value = discVal.toFixed(2);

        const c = document.getElementById('piselHiddenInputs');
        c.innerHTML = '';
        Object.entries(piselState).forEach(([id, s]) => {
            if (s.selected > 0) {
                c.innerHTML += `<input type="hidden" name="paidItemIds"  value="${id}">` +
                    `<input type="hidden" name="paidItemQtys" value="${s.selected}">`;
            }
        });
        return true;
    };

    // ═══════════════════════════════════════════════════════════
    // İPTAL MODAL — JS
    // ═══════════════════════════════════════════════════════════
    let cimMaxQty = 1;
    let cimUnitPrice = 0;
    let cimQty = 1;
    let cimTracksStock = false;

    window.openCancelModal = function (itemId, name, unitPrice, maxQty, tracksStock) {
        cimMaxQty = maxQty;
        cimUnitPrice = unitPrice;
        cimTracksStock = tracksStock;
        cimQty = 1;

        document.getElementById('cimItemId').value = itemId;
        document.getElementById('cimProductName').textContent = name;
        document.getElementById('cimQtyHidden').value = 1;
        document.getElementById('cimReason').value = '';
        document.getElementById('cimQtyMax').textContent = '(maks. ' + maxQty + ')';

        document.getElementById('cimWasteField').style.display = tracksStock ? '' : 'none';
        document.getElementById('cimIsWastedHidden').value = 'false';

        document.querySelectorAll('#cancelItemModal input[type="radio"]')
            .forEach(r => r.checked = r.value === 'false');

        cimRefresh();
        window.openModal('cancelItemModal');
    };

    window.cimDelta = function (d) {
        cimQty = Math.max(1, Math.min(cimMaxQty, cimQty + d));
        cimRefresh();
    };

    function cimRefresh() {
        document.getElementById('cimQtyNum').textContent = cimQty;
        document.getElementById('cimQtyHidden').value = cimQty;
        document.getElementById('cimRefundAmt').textContent = fmt(cimUnitPrice * cimQty);
        document.getElementById('cimConfirmBtn').textContent =
            '✕ ' + cimQty + ' Adet İptal Et (−' + fmt(cimUnitPrice * cimQty) + ')';
    }

    window.cimWasteChange = function (isWasted) {
        document.getElementById('cimIsWastedHidden').value = isWasted ? 'true' : 'false';
    };

    // Uyarıları gizleme
    setTimeout(() => {
        document.querySelectorAll('.alert').forEach(a => {
            a.style.transition = 'opacity .5s';
            a.style.opacity = '0';
            setTimeout(() => a.remove(), 500);
        });
    }, 3000);

});