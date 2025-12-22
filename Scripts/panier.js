/**
 * panier.js - Gestionnaire de la page panier
 */

document.addEventListener('DOMContentLoaded', function() {
    'use strict';

    const cartContent = document.getElementById('cart-content');
    const emptyTemplate = document.getElementById('empty-cart-template');
    const layoutTemplate = document.getElementById('cart-layout-template');

    // Initial load
    renderCart();

    function renderCart() {
        const cart = JSON.parse(localStorage.getItem('cart')) || [];

        if (cart.length === 0) {
            showEmptyState();
            return;
        }

        // Show layout
        cartContent.innerHTML = layoutTemplate.innerHTML;
        
        // Fetch details from server
        fetchCartDetails(cart);
    }

    function showEmptyState() {
        cartContent.innerHTML = emptyTemplate.innerHTML;
    }

    function fetchCartDetails(cartData) {
        // Map to match C# property names (ProduitId, VarianteId, Quantite)
        const requestData = cartData.map(item => ({
            ProduitId: parseInt(item.produitId),
            VarianteId: item.varianteId ? parseInt(item.varianteId) : null,
            Quantite: parseInt(item.quantite)
        }));

        fetch('/Panier/GetCartDetails', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestData)
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                renderItems(data.items);
                updateSummary(data.items);
                attachEventListeners();
            } else {
                console.error('Error fetching cart details:', data.message);
                showErrorMessage();
            }
        })
        .catch(error => {
            console.error('Fetch error:', error);
            showErrorMessage();
        });
    }

    function renderItems(items) {
        const listContainer = document.getElementById('cart-items-list');
        if (!listContainer) return;

        listContainer.innerHTML = items.map(item => `
            <div class="bg-white rounded-4 p-4 shadow-soft d-flex gap-4 cart-item-card" data-produit-id="${item.produitId}" data-variante-id="${item.varianteId || ''}">
                <!-- Image -->
                <a href="/Produit/Details/${item.produitId}" class="flex-shrink-0">
                    <img src="${item.imageUrl}" alt="${item.nom}" class="rounded-3 object-cover" style="width: 100px; height: 100px; md-width: 120px; md-height: 120px;">
                </a>

                <!-- Details -->
                <div class="flex-grow-1 min-w-0">
                    <div class="d-flex justify-content-between align-items-start mb-1">
                        <a href="/Produit/Details/${item.produitId}" class="text-decoration-none">
                            <h3 class="h6 fw-bold mb-0 text-dark line-clamp-2 hover-accent transition-colors">${item.nom}</h3>
                        </a>
                        <button class="btn btn-link text-muted p-0 remove-item-btn" title="Supprimer">
                            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="currentColor" class="bi bi-trash" viewBox="0 0 16 16">
                                <path d="M5.5 5.5A.5.5 0 0 1 6 6v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm2.5 0a.5.5 0 0 1 .5.5v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5zm3 .5a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0V6z"/>
                                <path fill-rule="evenodd" d="M14.5 3a1 1 0 0 1-1 1H13v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4h-.5a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1H6a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1h3.5a1 1 0 0 1 1 1v1zM4.118 4 4 4.059V13a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V4.059L11.882 4H4.118zM2.5 3V2h11v1h-11z"/>
                            </svg>
                        </button>
                    </div>
                    
                    <p class="small text-muted mb-1">${item.cooperativeNom}</p>
                    ${item.varianteNom ? `<p class="small text-accent mb-2 fw-medium">${item.varianteNom}</p>` : ''}

                    <div class="d-flex align-items-center justify-content-between mt-3">
                        <!-- Quantity Controls -->
                        <div class="d-flex align-items-center border rounded-3 bg-light overflow-hidden">
                            <button class="btn btn-sm px-2 py-1 update-qty-btn" data-delta="-1">
                                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" class="bi bi-minus" viewBox="0 0 16 16">
                                    <path d="M4 8a.5.5 0 0 1 .5-.5h7a.5.5 0 0 1 0 1h-7A.5.5 0 0 1 4 8z"/>
                                </svg>
                            </button>
                            <span class="px-2 fw-semibold small" style="min-width: 30px; text-align: center;">${item.quantite}</span>
                            <button class="btn btn-sm px-2 py-1 update-qty-btn" data-delta="1">
                                <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" class="bi bi-plus" viewBox="0 0 16 16">
                                    <path d="M8 4a.5.5 0 0 1 .5.5v3h3a.5.5 0 0 1 0 1h-3v3a.5.5 0 0 1-1 0v-3h-3a.5.5 0 0 1 0-1h3v-3A.5.5 0 0 1 8 4z"/>
                                </svg>
                            </button>
                        </div>

                        <!-- Price -->
                        <div class="text-end">
                            <span class="fw-bold text-dark">${(item.prixUnitaire * item.quantite).toFixed(2)} MAD</span>
                        </div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    function updateSummary(items) {
        const total = items.reduce((acc, item) => acc + (item.prixUnitaire * item.quantite), 0);
        const itemCount = items.reduce((acc, item) => acc + item.quantite, 0);
        
        // Update UI
        const itemsCountElem = document.getElementById('items-count');
        const totalAmountElem = document.getElementById('total-amount');

        if (itemsCountElem) itemsCountElem.textContent = itemCount + (itemCount > 1 ? ' articles' : ' article');
        if (totalAmountElem) totalAmountElem.textContent = total.toFixed(2) + ' MAD';
    }

    function attachEventListeners() {
        // Quantity Buttons
        document.querySelectorAll('.update-qty-btn').forEach(btn => {
            btn.addEventListener('click', function() {
                const card = this.closest('.cart-item-card');
                const pId = card.dataset.produitId;
                const vId = card.dataset.varianteId || null;
                const delta = parseInt(this.dataset.delta);
                updateCartQuantity(pId, vId, delta);
            });
        });

        // Remove Buttons
        document.querySelectorAll('.remove-item-btn').forEach(btn => {
            btn.addEventListener('click', function() {
                const card = this.closest('.cart-item-card');
                const pId = card.dataset.produitId;
                const vId = card.dataset.varianteId || null;
                removeFromCart(pId, vId);
            });
        });

        // Clear Cart
        const clearBtn = document.getElementById('clear-cart-btn');
        if (clearBtn) {
            clearBtn.addEventListener('click', function() {
                if (confirm('Voulez-vous vraiment vider votre panier ?')) {
                    localStorage.removeItem('cart');
                    renderCart();
                    if (window.updateCartBadge) window.updateCartBadge();
                }
            });
        }
    }

    function updateCartQuantity(produitId, varianteId, delta) {
        let cart = JSON.parse(localStorage.getItem('cart')) || [];
        const index = cart.findIndex(item => item.produitId == produitId && (varianteId ? item.varianteId == varianteId : !item.varianteId));
        
        if (index !== -1) {
            cart[index].quantite += delta;
            
            if (cart[index].quantite <= 0) {
                cart.splice(index, 1);
            }
            
            localStorage.setItem('cart', JSON.stringify(cart));
            renderCart();
            if (window.updateCartBadge) window.updateCartBadge();
        }
    }

    function removeFromCart(produitId, varianteId) {
        let cart = JSON.parse(localStorage.getItem('cart')) || [];
        cart = cart.filter(item => !(item.produitId == produitId && (varianteId ? item.varianteId == varianteId : !item.varianteId)));
        
        localStorage.setItem('cart', JSON.stringify(cart));
        renderCart();
        if (window.updateCartBadge) window.updateCartBadge();
    }

    function showErrorMessage() {
        cartContent.innerHTML = `
            <div class="alert alert-danger shadow-soft rounded-4 p-4 text-center">
                Une erreur est survenue lors du chargement de votre panier. 
                <button class="btn btn-outline-danger btn-sm ms-3" onclick="location.reload()">Réessayer</button>
            </div>
        `;
    }
});
