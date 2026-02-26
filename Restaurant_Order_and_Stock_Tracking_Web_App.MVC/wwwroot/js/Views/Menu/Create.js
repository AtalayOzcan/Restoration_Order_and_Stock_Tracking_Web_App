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
    token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    try {
        const res = await fetch('/Menu/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json', // C# tarafýndaki [FromBody] bunu bekler
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
            if (box) {
                box.textContent = data.message;
                box.style.display = 'block';
            } else {
                alert(data.message); // alertBox bulunamazsa fallback
            }
        }
    } catch (error) {
        btn.disabled = false;
        const box = document.getElementById('alertBox');
        if (box) {
            box.textContent = "Baðlantý hatasý oluþtu.";
            box.style.display = 'block';
        }
    }
});
