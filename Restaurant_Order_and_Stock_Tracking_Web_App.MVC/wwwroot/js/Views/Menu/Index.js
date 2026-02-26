
// ── Helpers ─────────────────────────────────────────────────
function openModal(id) { document.getElementById(id).classList.add('open'); }
function closeModal(id) { document.getElementById(id).classList.remove('open'); }

document.querySelectorAll('.modal-overlay').forEach(o =>
    o.addEventListener('click', e => { if (e.target === o) o.classList.remove('open'); })
);

function showToast(msg, type = 'success') {
    const c = document.getElementById('toastContainer');
    const t = document.createElement('div');
    t.className = `toast toast-${type}`;
    t.innerHTML = `<span>${type === 'success' ? '✅' : '❌'}</span><span>${msg}</span>`;
    c.appendChild(t);
    setTimeout(() => t.remove(), 3500);
}

function getToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]').value;
}

// ── Filter ───────────────────────────────────────────────────
function filterTable() {
    const search = document.getElementById('searchInput').value.toLowerCase();
    const cat = document.getElementById('catFilter').value;
    const status = document.getElementById('statusFilter').value;

    document.querySelectorAll('#menuTable tbody tr[data-name]').forEach(row => {
        const matchName = row.dataset.name.includes(search);
        const matchCat = !cat || row.dataset.cat === cat;
        const matchStatus = !status || row.dataset.status === status;
        row.style.display = (matchName && matchCat && matchStatus) ? '' : 'none';
    });
}

// ── Create ───────────────────────────────────────────────────
function openCreateModal() {
    document.getElementById('createForm').reset();
    document.getElementById('c_isAvailable').checked = true;
    openModal('createModal');
}
// ── Create ───────────────────────────────────────────────────
document.getElementById('createForm')?.addEventListener('submit', async e => {
    e.preventDefault();
    const btn = e.submitter;
    btn.disabled = true;

    // DTO'ya birebir uygun JSON payload
    const payload = {
        menuItemName: document.getElementById('c_name').value,
        categoryId: parseInt(document.getElementById('c_categoryId').value) || 0,
        menuItemPriceStr: document.getElementById('c_price').value,
        description: document.getElementById('c_description').value,
        stockQuantity: parseInt(document.getElementById('c_stock').value) || 0,
        trackStock: document.getElementById('c_trackStock').checked,
        isAvailable: document.getElementById('c_isAvailable').checked
    };

    try {
        const res = await fetch('/Menu/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json', // C# tarafındaki [FromBody] bunu bekler
                'RequestVerificationToken': getToken()
            },
            body: JSON.stringify(payload)
        });

        const data = await res.json();
        btn.disabled = false;

        if (data.success) {
            closeModal('createModal');
            showToast(data.message, 'success');
            setTimeout(() => location.reload(), 800);
        } else {
            showToast(data.message, 'error');
        }
    } catch (error) {
        btn.disabled = false;
        showToast("Bağlantı hatası oluştu.", 'error');
    }
});

// ── Edit ─────────────────────────────────────────────────────
async function openEditModal(id) {
    const res = await fetch(`/Menu/GetById/${id}`);
    const data = await res.json();
    if (!data.success) { showToast('Veri alınamadı.', 'error'); return; }

    document.getElementById('e_id').value = data.menuItemId;
    document.getElementById('e_name').value = data.menuItemName;
    document.getElementById('e_categoryId').value = data.categoryId;
    document.getElementById('e_price').value = data.menuItemPrice;
    document.getElementById('e_description').value = data.description ?? '';
    document.getElementById('e_stock').value = data.stockQuantity;
    document.getElementById('e_trackStock').checked = data.trackStock;
    document.getElementById('e_isAvailable').checked = data.isAvailable;
    openModal('editModal');
}

document.getElementById('editForm')?.addEventListener('submit', async e => {
    e.preventDefault();
    const btn = e.submitter;
    btn.disabled = true;

    const payload = {
        id: parseInt(document.getElementById('e_id').value), // Sadece Edit'te Id var
        menuItemName: document.getElementById('e_name').value,
        categoryId: parseInt(document.getElementById('e_categoryId').value) || 0,
        menuItemPriceStr: document.getElementById('e_price').value,
        description: document.getElementById('e_description').value,
        stockQuantity: parseInt(document.getElementById('e_stock').value) || 0,
        trackStock: document.getElementById('e_trackStock').checked,
        isAvailable: document.getElementById('e_isAvailable').checked
    };

    try {
        const res = await fetch('/Menu/Edit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getToken()
            },
            body: JSON.stringify(payload)
        });

        const data = await res.json();
        btn.disabled = false;

        if (data.success) {
            closeModal('editModal');
            showToast(data.message, 'success');
            setTimeout(() => location.reload(), 800);
        } else {
            showToast(data.message, 'error');
        }
    } catch (error) {
        btn.disabled = false;
        showToast("Bağlantı hatası oluştu.", 'error');
    }
});
// ── Delete ───────────────────────────────────────────────────
function openDeleteModal(id, name) {
    document.getElementById('d_id').value = id;
    document.getElementById('d_name').textContent = name;
    openModal('deleteModal');
}

document.getElementById('deleteForm').addEventListener('submit', async e => {
    e.preventDefault();
    const btn = e.submitter; btn.disabled = true;

    const body = new URLSearchParams({
        id: document.getElementById('d_id').value,
        __RequestVerificationToken: getToken()
    });

    const res = await fetch('/Menu/Delete', { method: 'POST', body });
    const data = await res.json();
    btn.disabled = false;

    if (data.success) {
        closeModal('deleteModal');
        showToast(data.message, 'success');
        setTimeout(() => location.reload(), 800);
    } else {
        closeModal('deleteModal');
        showToast(data.message, 'error');
    }
});
