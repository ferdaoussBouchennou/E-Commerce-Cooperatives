/**
 * checkout.js - Gestionnaire de la page checkout avec 3 étapes
 */

(function () {
    'use strict';

    // Wait for DOM to be fully loaded
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    function init() {
        console.log('Checkout script initialized');

        let currentStep = 1;
        let cartItems = [];
        let deliveryData = {};
        let paymentData = {};

        const cart = JSON.parse(localStorage.getItem('cart')) || [];
        console.log('Cart from localStorage:', cart);

        if (cart.length === 0) {
            console.warn('Cart is empty, redirecting to cart page');
            // Don't redirect immediately, give user a chance to see the page
            setTimeout(() => {
                if (confirm('Votre panier est vide. Voulez-vous retourner au panier ?')) {
                    window.location.href = '/Panier';
                }
            }, 1000);
        }

        // Initialize all components
        loadCartItems();
        setupStepNavigation();
        setupDeliveryOptions();
        setupPaymentOptions();
        setupCardInputs();

        function loadCartItems() {
            console.log('Loading cart items...');

            const requestData = cart.map(item => ({
                ProduitId: parseInt(item.produitId) || 0,
                VarianteId: item.varianteId ? parseInt(item.varianteId) : null,
                Quantite: parseInt(item.quantite) || 1
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
                    console.log('Cart details response:', data);
                    if (data.success) {
                        cartItems = data.items;
                        renderOrderItems(data.items);
                        updateTotals(data.items);

                        const cartInput = document.getElementById('cart-items-input');
                        if (cartInput) {
                            cartInput.value = JSON.stringify(
                                data.items.map(item => ({
                                    ProduitId: item.produitId,
                                    VarianteId: item.varianteId,
                                    Quantite: item.quantite,
                                    PrixUnitaire: item.prixUnitaire,
                                    Nom: item.nom
                                }))
                            );
                        }
                    } else {
                        console.warn('API returned error, using localStorage fallback');
                        useFallbackCart();
                    }
                })
                .catch(error => {
                    console.error('Fetch error:', error);
                    useFallbackCart();
                });
        }

        function useFallbackCart() {
            cartItems = cart.map(item => ({
                produitId: parseInt(item.produitId) || 0,
                varianteId: item.varianteId ? parseInt(item.varianteId) : null,
                quantite: parseInt(item.quantite) || 1,
                prixUnitaire: parseFloat(item.prix) || 0,
                nom: item.nom || 'Produit',
                imageUrl: item.image || '/Content/images/default-product.jpg'
            }));
            renderOrderItems(cartItems);
            updateTotals(cartItems);

            const cartInput = document.getElementById('cart-items-input');
            if (cartInput) {
                cartInput.value = JSON.stringify(
                    cartItems.map(item => ({
                        ProduitId: item.produitId,
                        VarianteId: item.varianteId,
                        Quantite: item.quantite,
                        PrixUnitaire: item.prixUnitaire,
                        Nom: item.nom
                    }))
                );
            }
        }

        function renderOrderItems(items) {
            const container = document.getElementById('order-items');
            if (!container) {
                console.warn('Order items container not found');
                return;
            }

            container.innerHTML = items.map(item => {
                const price = parseFloat(item.prixUnitaire) || 0;
                const qty = parseInt(item.quantite) || 0;
                const total = price * qty;

                return `
                <div class="d-flex gap-3 mb-3">
                    <img src="${item.imageUrl || '/Content/images/default-product.jpg'}" alt="${item.nom || 'Produit'}" 
                         class="rounded-3 object-cover" 
                         style="width: 64px; height: 64px; object-fit: cover;">
                    <div class="flex-grow-1 min-w-0">
                        <h3 class="fw-medium mb-0 small" style="color: #305C7D; line-height: 1.4;">
                            ${item.nom || 'Produit'}
                        </h3>
                        <p class="small text-muted mb-0">Qté: ${qty}</p>
                        <p class="small fw-semibold mb-0" style="color: #305C7D;">
                            ${total.toFixed(2)} MAD
                        </p>
                    </div>
                </div>
            `;
            }).join('');
        }

        function updateTotals(items) {
            const subtotal = items.reduce((acc, item) => {
                const price = parseFloat(item.prixUnitaire) || 0;
                const qty = parseInt(item.quantite) || 0;
                return acc + (price * qty);
            }, 0);

            // No TVA calculation - prices already include VAT
            const selectedOption = document.querySelector('.delivery-option.selected') || document.querySelector('.delivery-option');
            const deliveryFee = parseFloat(selectedOption?.dataset.price || '0');
            const total = subtotal + deliveryFee;

            const elements = {
                'subtotal': subtotal.toFixed(2) + ' MAD',
                'delivery-fee': deliveryFee.toFixed(2) + ' MAD',
                'total': total.toFixed(2) + ' MAD'
            };

            Object.keys(elements).forEach(id => {
                const el = document.getElementById(id);
                if (el) el.textContent = elements[id];
            });
        }

        function setupDeliveryOptions() {
            const options = document.querySelectorAll('.delivery-option');
            console.log('Found delivery options:', options.length);

            options.forEach(option => {
                option.addEventListener('click', function () {
                    options.forEach(opt => {
                        opt.classList.remove('selected');
                        opt.style.borderColor = '#dee2e6';
                        opt.style.backgroundColor = 'transparent';
                    });

                    this.classList.add('selected');
                    this.style.borderColor = '#C06C50';
                    this.style.backgroundColor = 'rgba(192, 108, 80, 0.05)';

                    const radio = this.querySelector('input[type="radio"]');
                    if (radio) radio.checked = true;

                    updateTotals(cartItems);
                });
            });

            if (options.length > 0) {
                options[0].click();
            }
        }

        function setupPaymentOptions() {
            const options = document.querySelectorAll('.payment-option');
            console.log('Found payment options:', options.length);

            options.forEach(option => {
                option.addEventListener('click', function () {
                    options.forEach(opt => {
                        opt.style.borderColor = '#dee2e6';
                        opt.style.backgroundColor = 'transparent';
                    });

                    this.style.borderColor = '#C06C50';
                    this.style.backgroundColor = 'rgba(192, 108, 80, 0.05)';

                    const radio = this.querySelector('input[type="radio"]');
                    if (radio) {
                        radio.checked = true;
                        const cardDetails = document.getElementById('card-details');
                        if (cardDetails) {
                            cardDetails.style.display = radio.value === 'card' ? 'block' : 'none';
                        }
                    }
                });
            });

            // Show/hide card details based on initial selection
            const cardRadio = document.getElementById('payment-card');
            if (cardRadio && cardRadio.checked) {
                const cardDetails = document.getElementById('card-details');
                if (cardDetails) cardDetails.style.display = 'block';
            }
        }

        function setupCardInputs() {
            const cardNumber = document.getElementById('CardNumber');
            const expiryDate = document.getElementById('ExpiryDate');
            const cvv = document.getElementById('CVV');

            if (cardNumber) {
                cardNumber.addEventListener('input', function (e) {
                    let value = e.target.value.replace(/\s/g, '');
                    let formattedValue = value.match(/.{1,4}/g)?.join(' ') || value;
                    e.target.value = formattedValue;
                });
            }

            if (expiryDate) {
                expiryDate.addEventListener('input', function (e) {
                    let value = e.target.value.replace(/\D/g, '');
                    if (value.length >= 2) {
                        value = value.substring(0, 2) + '/' + value.substring(2, 4);
                    }
                    e.target.value = value;
                });
            }

            if (cvv) {
                cvv.addEventListener('input', function (e) {
                    e.target.value = e.target.value.replace(/\D/g, '');
                });
            }
        }

        function setupStepNavigation() {
            console.log('Setting up step navigation...');

            // Continue to payment button
            const btnContinuePayment = document.getElementById('btn-continue-payment');
            console.log('Continue button found:', !!btnContinuePayment);

            if (btnContinuePayment) {
                // Remove any existing listeners
                const newBtn = btnContinuePayment.cloneNode(true);
                btnContinuePayment.parentNode.replaceChild(newBtn, btnContinuePayment);

                newBtn.addEventListener('click', function (e) {
                    e.preventDefault();
                    console.log('Continue button clicked!');

                    if (validateStep1()) {
                        console.log('Validation passed, moving to step 2');
                        saveStep1Data();
                        goToStep(2);
                    } else {
                        console.log('Validation failed');
                    }
                });
                console.log('Event listener attached to continue button');
            } else {
                console.error('Continue button not found!');
            }

            // Back to delivery button
            const btnBackDelivery = document.getElementById('btn-back-delivery');
            if (btnBackDelivery) {
                btnBackDelivery.addEventListener('click', function () {
                    goToStep(1);
                });
            }

            // Verify order button
            const btnVerifyOrder = document.getElementById('btn-verify-order');
            if (btnVerifyOrder) {
                btnVerifyOrder.addEventListener('click', function () {
                    if (validateStep2()) {
                        // S'assurer que les données de livraison sont sauvegardées
                        saveStep1Data();
                        saveStep2Data();
                        updateSummary();
                        goToStep(3);
                    }
                });
            }

            // Back to payment button
            const btnBackPayment = document.getElementById('btn-back-payment');
            if (btnBackPayment) {
                btnBackPayment.addEventListener('click', function () {
                    goToStep(2);
                });
            }

            // Confirm order button
            const btnConfirmOrder = document.getElementById('btn-confirm-order');
            if (btnConfirmOrder) {
                btnConfirmOrder.addEventListener('click', function () {
                    submitOrder();
                });
            }
        }

        function validateStep1() {
            console.log('Validating step 1...');
            const form = document.getElementById('checkout-form-step1');
            if (!form) {
                console.error('Form not found!');
                return false;
            }

            const requiredFields = ['Prenom', 'Nom', 'Email', 'Telephone', 'AdresseComplete', 'Ville', 'CodePostal'];
            let isValid = true;

            requiredFields.forEach(fieldId => {
                const field = document.getElementById(fieldId);
                if (field) {
                    if (!field.value.trim()) {
                        field.classList.add('is-invalid');
                        isValid = false;
                        console.log('Field invalid:', fieldId);
                    } else {
                        field.classList.remove('is-invalid');
                    }
                }
            });

            const selectedMode = document.querySelector('input[name="ModeLivraisonId"]:checked');
            if (!selectedMode) {
                alert('Veuillez sélectionner un mode de livraison');
                isValid = false;
            }

            if (!isValid) {
                form.classList.add('was-validated');
            }

            console.log('Validation result:', isValid);
            return isValid;
        }

        function validateStep2() {
            const paymentMethod = document.querySelector('input[name="PaymentMethod"]:checked');
            if (!paymentMethod) {
                alert('Veuillez sélectionner un mode de paiement');
                return false;
            }

            // No additional validation needed for cash on delivery
            return true;
        }

        function saveStep1Data() {
            deliveryData = {
                prenom: document.getElementById('Prenom')?.value || '',
                nom: document.getElementById('Nom')?.value || '',
                email: document.getElementById('Email')?.value || '',
                telephone: document.getElementById('Telephone')?.value || '',
                adresseComplete: document.getElementById('AdresseComplete')?.value || '',
                ville: document.getElementById('Ville')?.value || '',
                codePostal: document.getElementById('CodePostal')?.value || '',
                notes: document.getElementById('Notes')?.value || '',
                modeLivraisonId: document.querySelector('input[name="ModeLivraisonId"]:checked')?.value || ''
            };
            console.log('Step 1 data saved:', deliveryData);
        }

        function saveStep2Data() {
            const paymentMethod = document.querySelector('input[name="PaymentMethod"]:checked');
            paymentData = {
                method: paymentMethod?.value || 'cod'
            };
        }

        function updateSummary() {
            // S'assurer que les données de livraison sont à jour
            if (!deliveryData || !deliveryData.adresseComplete) {
                saveStep1Data();
            }

            const selectedDelivery = document.querySelector('.delivery-option.selected');
            const deliveryName = selectedDelivery?.querySelector('label')?.textContent || '';
            const deliveryDelai = selectedDelivery?.dataset.delai || '';

            const summaryAddress = document.getElementById('summary-address');
            if (summaryAddress && deliveryData) {
                // Afficher uniquement l'adresse comme dans la facture (sans nom, prénom, téléphone)
                // Format: AdresseComplete sur une ligne, puis Ville, CodePostal sur la ligne suivante
                let adresse = (deliveryData.adresseComplete || '').trim();
                const ville = (deliveryData.ville || '').trim();
                const codePostal = (deliveryData.codePostal || '').trim();
                const prenom = (deliveryData.prenom || '').trim();
                const nom = (deliveryData.nom || '').trim();
                const telephone = (deliveryData.telephone || '').trim();
                
                // Retirer le nom et prénom du début de l'adresse si présents
                const fullName = `${prenom} ${nom}`.trim();
                if (fullName && adresse.startsWith(fullName)) {
                    adresse = adresse.substring(fullName.length).trim();
                }
                
                // Retirer le téléphone de la fin de l'adresse si présent
                if (telephone && adresse.endsWith(telephone)) {
                    adresse = adresse.substring(0, adresse.length - telephone.length).trim();
                }
                // Retirer aussi avec le format +212...
                if (telephone && adresse.includes(telephone)) {
                    adresse = adresse.replace(telephone, '').trim();
                }
                
                // Construire l'adresse finale (uniquement adresse, ville, code postal)
                let addressText = adresse;
                if (ville || codePostal) {
                    const villeCodePostal = (ville ? ville : '') + (ville && codePostal ? ', ' : '') + (codePostal ? codePostal : '');
                    if (villeCodePostal) {
                        addressText += '<br>' + villeCodePostal;
                    }
                }
                
                summaryAddress.innerHTML = addressText;
                console.log('Adresse affichée:', addressText);
            }

            const summaryDelivery = document.getElementById('summary-delivery');
            if (summaryDelivery) {
                summaryDelivery.textContent = `${deliveryName} - ${deliveryDelai}`;
            }

            const summaryPayment = document.getElementById('summary-payment');
            if (summaryPayment) {
                summaryPayment.textContent = 'Paiement à la livraison';
            }
        }

        function goToStep(step) {
            console.log('Going to step:', step);

            // Hide all steps
            document.querySelectorAll('.checkout-step-content').forEach(el => {
                el.style.display = 'none';
            });

            // Show current step
            const stepNames = ['delivery', 'payment', 'confirmation'];
            const stepElement = document.getElementById(`step-${stepNames[step - 1]}`);
            if (stepElement) {
                stepElement.style.display = 'block';
                console.log('Showing step:', stepNames[step - 1]);
            } else {
                console.error('Step element not found:', stepNames[step - 1]);
            }

            // Update progress indicators
            updateProgressIndicators(step);

            currentStep = step;

            // Scroll to top
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }

        function updateProgressIndicators(activeStep) {
            for (let i = 1; i <= 3; i++) {
                const stepElement = document.getElementById(`step-${i}`);
                const connector = document.getElementById(`connector-${i}-${i + 1}`);
                const labelElement = stepElement?.nextElementSibling;

                if (!stepElement) continue;

                if (i < activeStep) {
                    // Completed step
                    stepElement.style.backgroundColor = '#C06C50';
                    stepElement.style.color = 'white';
                    stepElement.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" viewBox="0 0 16 16"><path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 4.992 6.93a.75.75 0 0 0-1.08 1.04l2.75 2.75a.75.75 0 0 0 1.08-.022l4.75-5.5a.75.75 0 0 0-.022-1.08z"/></svg>';
                    if (connector) connector.style.backgroundColor = '#C06C50';
                    if (labelElement && labelElement.tagName === 'SPAN') {
                        labelElement.style.color = '#305C7D';
                        labelElement.style.fontWeight = '500';
                    }
                } else if (i === activeStep) {
                    // Active step
                    stepElement.style.backgroundColor = '#C06C50';
                    stepElement.style.color = 'white';
                    stepElement.innerHTML = i;
                    if (connector && i < 3) connector.style.backgroundColor = '#C06C50';
                    if (labelElement && labelElement.tagName === 'SPAN') {
                        labelElement.style.color = '#305C7D';
                        labelElement.style.fontWeight = '500';
                    }
                } else {
                    // Inactive step
                    stepElement.style.backgroundColor = '#e0e0e0';
                    stepElement.style.color = '#9e9e9e';
                    stepElement.innerHTML = i;
                    if (connector) connector.style.backgroundColor = '#e0e0e0';
                    if (labelElement && labelElement.tagName === 'SPAN') {
                        labelElement.style.color = '#9e9e9e';
                        labelElement.style.fontWeight = '400';
                    }
                }
            }
        }

        function submitOrder() {
            console.log('Submitting order...');
            const btn = document.getElementById('btn-confirm-order');
            if (btn) {
                btn.disabled = true;
                btn.textContent = 'Traitement...';
            }

            // Populate hidden form fields
            const fields = {
                'form-Prenom': deliveryData.prenom,
                'form-Nom': deliveryData.nom,
                'form-Email': deliveryData.email,
                'form-Telephone': deliveryData.telephone,
                'form-AdresseComplete': deliveryData.adresseComplete,
                'form-Ville': deliveryData.ville,
                'form-CodePostal': deliveryData.codePostal,
                'form-Notes': deliveryData.notes,
                'form-ModeLivraisonId': deliveryData.modeLivraisonId
            };

            Object.keys(fields).forEach(id => {
                const element = document.getElementById(id);
                if (element) {
                    element.value = fields[id] || '';
                }
            });

            const form = document.getElementById('checkout-form');
            if (!form) {
                console.error('Form not found!');
                alert('Erreur: Formulaire introuvable');
                if (btn) {
                    btn.disabled = false;
                    btn.textContent = 'Confirmer la commande';
                }
                return;
            }

            const formData = new FormData(form);

            fetch('/Checkout/ProcessOrder', {
                method: 'POST',
                body: formData
            })
                .then(response => {
                    console.log('Response status:', response.status);
                    if (!response.ok) {
                        throw new Error(`HTTP error! status: ${response.status}`);
                    }
                    return response.json();
                })
                .then(data => {
                    console.log('Response data:', data);
                    if (data.success) {
                        localStorage.removeItem('cart');
                        if (window.updateCartBadge) window.updateCartBadge();
                        window.location.href = '/commande-confirmation/' + data.numeroCommande;
                    } else {
                        alert('Erreur: ' + (data.message || 'Une erreur est survenue'));
                        if (btn) {
                            btn.disabled = false;
                            btn.textContent = 'Confirmer la commande';
                        }
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    alert('Une erreur est survenue lors de la soumission de la commande: ' + error.message);
                    if (btn) {
                        btn.disabled = false;
                        btn.textContent = 'Confirmer la commande';
                    }
                });
        }
    }
})();
