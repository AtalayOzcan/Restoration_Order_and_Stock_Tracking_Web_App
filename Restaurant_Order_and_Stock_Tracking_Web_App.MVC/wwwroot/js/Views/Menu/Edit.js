

// ── Edit ─────────────────────────────────────────────────────
document.getElementById('editForm')?.addEventListener('submit', async e => {
    e.preventDefault();
    const btn = e.submitter;
    btn.disabled = true;

    const payload = {
        id: parseInt(document.getElementById('menuItemId').value) || 0,
        menuItemName: document.getElementById('menuItemName').value,
        categoryId: parseInt(document.getElementById('categoryId').value) || 0,
        menuItemPriceStr: document.getElementById('menuItemPrice').value,
        description: document.getElementById('description').value,
        stockQuantity: parseInt(document.getElementById('stockQuantity').value) || 0,
        trackStock: document.getElementById('trackStock').checked,
        isAvailable: document.getElementById('isAvailable').checked
    };
    token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    try {
        const res = await fetch('/Menu/Edit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(payload)
        });

        const data = await res.json();
        btn.disabled = false;


        if (data.success) {
            window.location.href = '/Menu';
        } else {
            const box = document.getElementById('alertBox');
            box.textContent = data.message;
            box.style.display = 'block';
        }
    } catch (error) {
        btn.disabled = false;
        const box = document.getElementById('alertBox');
        if (box) {
            box.textContent = "Bağlantı hatası oluştu.";
         
            box.style.display = 'block';
        }
    }
    
});
