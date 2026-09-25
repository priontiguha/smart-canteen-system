document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.toggle-password').forEach((button) => {
        button.addEventListener('click', function () {
            const targetName = this.dataset.target;
            const input = document.querySelector(`input[name="${targetName}"]`);
            if (!input) return;

            const isPassword = input.type === 'password';
            input.type = isPassword ? 'text' : 'password';
            this.textContent = isPassword ? '🙈' : '👁';
            this.setAttribute('aria-label', isPassword ? 'Hide password' : 'Show password');
        });
    });

    const orderForm = document.getElementById('order-form');
    if (!orderForm) {
        return;
    }

    const selectedContainer = document.getElementById('selectedItemsContainer');
    const cartItemsContainer = document.getElementById('cartItems');
    const subtotalElement = document.getElementById('cartSubtotal');

    const updateSelection = () => {
        const cards = document.querySelectorAll('.menu-card');
        const selected = [];

        cards.forEach((card) => {
            const quantity = Number(card.querySelector('.qty-value')?.dataset.qty || 0);
            if (quantity > 0) {
                selected.push({
                    id: card.dataset.menuId,
                    name: card.dataset.name,
                    price: Number(card.dataset.price),
                    quantity: quantity
                });
            }
        });

        if (selectedContainer) {
            selectedContainer.innerHTML = '';
            selected.forEach((item, index) => {
                const menuIdInput = document.createElement('input');
                menuIdInput.type = 'hidden';
                menuIdInput.name = `Items[${index}].MenuItemId`;
                menuIdInput.value = item.id;

                const quantityInput = document.createElement('input');
                quantityInput.type = 'hidden';
                quantityInput.name = `Items[${index}].Quantity`;
                quantityInput.value = item.quantity;

                selectedContainer.appendChild(menuIdInput);
                selectedContainer.appendChild(quantityInput);
            });
        }

        if (cartItemsContainer) {
            cartItemsContainer.innerHTML = '';
            if (selected.length === 0) {
                cartItemsContainer.innerHTML = '<p class="text-muted mb-0">No items selected yet.</p>';
            } else {
                let subtotal = 0;
                selected.forEach((item) => {
                    const subtotalItem = item.price * item.quantity;
                    subtotal += subtotalItem;

                    const row = document.createElement('div');
                    row.className = 'd-flex justify-content-between align-items-center border-bottom pb-2 mb-2';
                    row.innerHTML = `
                        <div>
                            <div class="fw-semibold">${item.name}</div>
                            <small class="text-muted">${item.quantity} x $${item.price.toFixed(2)}</small>
                        </div>
                        <strong>$${subtotalItem.toFixed(2)}</strong>
                    `;
                    cartItemsContainer.appendChild(row);
                });

                if (subtotalElement) {
                    subtotalElement.textContent = `$${subtotal.toFixed(2)}`;
                }
            }
        }

        if (subtotalElement && selected.length === 0) {
            subtotalElement.textContent = '$0.00';
        }
    };

    document.querySelectorAll('.qty-btn').forEach((button) => {
        button.addEventListener('click', function () {
            const card = this.closest('.menu-card');
            const qtyValue = card.querySelector('.qty-value');
            const maxStock = Number(card.dataset.maxStock || 0);
            let currentQty = Number(qtyValue.dataset.qty || 0);

            if (this.dataset.action === 'increase') {
                if (currentQty < maxStock) {
                    currentQty += 1;
                }
            } else if (this.dataset.action === 'decrease') {
                if (currentQty > 0) {
                    currentQty -= 1;
                }
            }

            qtyValue.dataset.qty = currentQty;
            qtyValue.textContent = currentQty;
            updateSelection();
        });
    });

    orderForm.addEventListener('submit', function (event) {
        const selected = document.querySelectorAll('.qty-value[data-qty="0"]').length === document.querySelectorAll('.qty-value').length;

        if (selected) {
            event.preventDefault();
            const error = document.querySelector('.alert-danger');
            if (error) {
                error.remove();
            }

            const alert = document.createElement('div');
            alert.className = 'alert alert-danger mt-3';
            alert.textContent = 'Please select at least one item before placing the order.';
            orderForm.insertBefore(alert, orderForm.firstChild);
            return;
        }

        updateSelection();
    });

    updateSelection();
});
