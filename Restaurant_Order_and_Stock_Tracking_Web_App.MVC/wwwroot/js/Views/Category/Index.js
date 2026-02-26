document.addEventListener("DOMContentLoaded", () => {

    // ── Helpers ─────────────────────────────────────────────────
    window.openModal = function (id) {
        document.getElementById(id).classList.add('open');
    };

    window.closeModal = function (id) {
        document.getElementById(id).classList.remove('open');
    };

    // Modal dışına (overlay) tıklayınca kapatma
    document.querySelectorAll('.modal-overlay').forEach(overlay => {
        overlay.addEventListener('click', e => {
            if (e.target === overlay) overlay.classList.remove('open');
        });
    });

    function showToast(message, type = 'success') {
        const container = document.getElementById('toastContainer');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.innerHTML = `<span>${type === 'success' ? '✅' : '❌'}</span><span>${message}</span>`;
        container.appendChild(toast);

        setTimeout(() => toast.remove(), 3500);
    }

    function getToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    // ── Create ───────────────────────────────────────────────────
    window.openCreateModal = function () {
        document.getElementById('createForm').reset();
        document.getElementById('create_isActive').checked = true;
        openModal('createModal');
    };

    document.getElementById('createForm')?.addEventListener('submit', async e => {
        e.preventDefault();
        const btn = e.submitter;
        btn.disabled = true;

        // CategoryCreateDto ile BİREBİR eşleşen payload (Id YOK)
        const payload = {
            categoryName: document.getElementById('create_categoryName').value,
            categorySortOrder: parseInt(document.getElementById('create_sortOrder').value) || 0,
            isActive: document.getElementById('create_isActive').checked
        };

        try {
            const res = await fetch('/Category/Create', {
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
                closeModal('createModal');
                showToast(data.message, 'success');
                setTimeout(() => location.reload(), 800);
            } else {
                showToast(data.message, 'error');
            }
        } catch (error) {
            btn.disabled = false;
            showToast('İşlem sırasında bir hata oluştu.', 'error');
        }
    });

    // ── Edit ─────────────────────────────────────────────────────
    window.openEditModal = async function (id) {
        try {
            const res = await fetch(`/Category/GetById/${id}`);
            const data = await res.json();

            if (!data.success) {
                showToast('Veri alınamadı.', 'error');
                return;
            }

            document.getElementById('edit_id').value = data.categoryId;
            document.getElementById('edit_categoryName').value = data.categoryName;
            document.getElementById('edit_sortOrder').value = data.categorySortOrder;
            document.getElementById('edit_isActive').checked = data.isActive;
            openModal('editModal');
        } catch (error) {
            showToast('Veri çekilirken hata oluştu.', 'error');
        }
    };

    document.getElementById('editForm')?.addEventListener('submit', async e => {
        e.preventDefault();
        const btn = e.submitter;
        btn.disabled = true;

        // CategoryEditDto ile BİREBİR eşleşen payload (Id VAR)
        const payload = {
            id: parseInt(document.getElementById('edit_id').value),
            categoryName: document.getElementById('edit_categoryName').value,
            categorySortOrder: parseInt(document.getElementById('edit_sortOrder').value) || 0,
            isActive: document.getElementById('edit_isActive').checked
        };

        try {
            const res = await fetch('/Category/Edit', {
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
            showToast('İşlem sırasında bir hata oluştu.', 'error');
        }
    });

    // ── Delete ───────────────────────────────────────────────────
    window.openDeleteModal = function (id, name) {
        document.getElementById('delete_id').value = id;
        document.getElementById('delete_name').textContent = name;
        openModal('deleteModal');
    };

    document.getElementById('deleteForm')?.addEventListener('submit', async e => {
        e.preventDefault();
        const btn = e.submitter;
        btn.disabled = true;

        // Silme işlemi için ID gönderiyoruz (C# tarafında DeleteDto kullandığımızı varsayarak)
        const payload = {
            id: parseInt(document.getElementById('delete_id').value)
        };

        try {
            const res = await fetch('/Category/Delete', {
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
                closeModal('deleteModal');
                showToast(data.message, 'success');
                setTimeout(() => location.reload(), 800);
            } else {
                closeModal('deleteModal');
                showToast(data.message, 'error');
            }
        } catch (error) {
            btn.disabled = false;
            closeModal('deleteModal');
            showToast('İşlem sırasında bir hata oluştu.', 'error');
        }
    });

});