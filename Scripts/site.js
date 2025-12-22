// CoopShop Custom JavaScript

(function() {
    'use strict';

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function() {
        initBootstrapDropdowns();
        initAddToCart();
        initSmoothScroll();
        initAnimations();
        handleHashRedirects();
        handleAutoAddToCart();
    });

    // Handle hash redirects (like #connexion, #inscription)
    function handleHashRedirects() {
        // Check on page load
        checkHashAndRedirect();
        
        // Listen for hash changes
        window.addEventListener('hashchange', function() {
            checkHashAndRedirect();
        });
    }

    function checkHashAndRedirect() {
        const hash = window.location.hash;
        
        if (hash === '#connexion' || hash === '#login') {
            window.location.replace('/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search));
            return;
        }
        
        if (hash === '#inscription' || hash === '#register') {
            window.location.replace('/Account/Register');
            return;
        }
    }

    // Initialize Bootstrap dropdowns
    function initBootstrapDropdowns() {
        // Initialize all dropdowns
        const dropdowns = document.querySelectorAll('.dropdown-toggle');
        dropdowns.forEach(dropdown => {
            // Ensure dropdown works with both Bootstrap 4 and 5
            dropdown.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                
                const dropdownMenu = this.nextElementSibling;
                if (dropdownMenu && dropdownMenu.classList.contains('dropdown-menu')) {
                    // Toggle dropdown manually if Bootstrap isn't working
                    const isOpen = dropdownMenu.style.display === 'block';
                    if (isOpen) {
                        dropdownMenu.style.display = 'none';
                        this.setAttribute('aria-expanded', 'false');
                    } else {
                        // Close other dropdowns first
                        document.querySelectorAll('.dropdown-menu').forEach(menu => {
                            menu.style.display = 'none';
                        });
                        document.querySelectorAll('.dropdown-toggle').forEach(toggle => {
                            toggle.setAttribute('aria-expanded', 'false');
                        });
                        
                        dropdownMenu.style.display = 'block';
                        this.setAttribute('aria-expanded', 'true');
                    }
                }
            });
        });

        // Close dropdowns when clicking outside
        document.addEventListener('click', function(e) {
            if (!e.target.closest('.dropdown')) {
                document.querySelectorAll('.dropdown-menu').forEach(menu => {
                    menu.style.display = 'none';
                });
                document.querySelectorAll('.dropdown-toggle').forEach(toggle => {
                    toggle.setAttribute('aria-expanded', 'false');
                });
            }
        });

        // Make dropdown items clickable
        document.querySelectorAll('.dropdown-item').forEach(item => {
            item.addEventListener('click', function(e) {
                const href = this.getAttribute('href');
                if (href && href !== '#' && !href.startsWith('javascript:')) {
                    // Allow navigation
                    return true;
                }
            });
        });
    }

    // Check if user is authenticated
    function isUserAuthenticated() {
        // Check if there's a user session variable set by the server
        // This will be set to true when user logs in
        const authElement = document.getElementById('user-authenticated');
        if (authElement) {
            return authElement.getAttribute('data-authenticated') === 'true';
        }
        
        // Fallback: check localStorage (for client-side only scenarios)
        return localStorage.getItem('userAuthenticated') === 'true';
    }

    // Add to Cart functionality
    function initAddToCart() {
        document.addEventListener('click', function(e) {
            const button = e.target.closest('.add-to-cart-btn');
            if (button) {
                e.preventDefault();
                e.stopPropagation();
                
                const produitId = button.getAttribute('data-produit-id');
                const hasVariants = button.getAttribute('data-has-variants') === '1';
                
                console.log('Add to cart clicked:', { produitId, hasVariants });
                
                if (hasVariants) {
                    setTimeout(() => {
                        window.location.href = `/Produit/Details/${produitId}`;
                    }, 500);
                    return;
                }
                
                // Check if user is authenticated
                if (!isUserAuthenticated()) {
                    const currentUrl = window.location.pathname + window.location.search;
                    const separator = currentUrl.includes('?') ? '&' : '?';
                    const returnUrl = encodeURIComponent(currentUrl + separator + 'addItem=' + produitId);
                    window.location.href = '/Account/Login?returnUrl=' + returnUrl;
                    return;
                }
                
                addProductToCart(produitId);
                showNotification('Produit ajouté au panier!', 'success');
                updateCartBadge();
            }
        });
    }

    // ... (rest of the functions)

    // Expose functions globally
    window.isUserAuthenticated = isUserAuthenticated;
    window.addProductToCart = addProductToCart;
    window.showNotification = showNotification;
    window.updateCartBadge = updateCartBadge;

    // Handle automatically adding product to cart after login
    function handleAutoAddToCart() {
        const urlParams = new URLSearchParams(window.location.search);
        const addItemId = urlParams.get('addItem');
        const addVariantId = urlParams.get('addVariant');
        const addQty = urlParams.get('addQty') || 1;
        
        if (addItemId && isUserAuthenticated()) {
            // Add product to cart with variant and quantity
            // Ensure ids are numbers where expected to match direct additions
            const pId = String(addItemId);
            const vId = addVariantId ? parseInt(addVariantId) : null;
            const qty = parseInt(addQty) || 1;
            
            addProductToCart(pId, qty, vId);
            
            // Show notification
            showNotification('Produit ajouté au panier automatiquement!', 'success');
            
            // Update cart badge
            updateCartBadge();
            
            // Clean up URL without refreshing the page
            let newUrl = window.location.pathname + window.location.search
                .replace(/[&?]addItem=[^&]*/, '')
                .replace(/[&?]addVariant=[^&]*/, '')
                .replace(/[&?]addQty=[^&]*/, '')
                .replace(/^&/, '?')
                .replace(/\?$/, '');
            window.history.replaceState({}, document.title, newUrl);
        }
    }

    // Add product to cart
    function addProductToCart(produitId, quantite = 1, varianteId = null) {
        // Get existing cart from localStorage
        let cart = JSON.parse(localStorage.getItem('cart')) || [];
        
        // Check if product with identical variant already exists
        const existingItem = cart.find(item => 
            item.produitId === produitId && 
            (varianteId ? item.varianteId === varianteId : !item.varianteId)
        );
        
        if (existingItem) {
            existingItem.quantite += parseInt(quantite);
        } else {
            cart.push({
                produitId: produitId,
                varianteId: varianteId,
                quantite: parseInt(quantite),
                dateAjout: new Date().toISOString()
            });
        }
        
        // Save to localStorage
        localStorage.setItem('cart', JSON.stringify(cart));
    }

    // Update cart badge
    function updateCartBadge() {
        const cart = JSON.parse(localStorage.getItem('cart')) || [];
        const totalItems = cart.reduce((sum, item) => sum + item.quantite, 0);
        
        const cartBadge = document.getElementById('cart-badge');
        if (cartBadge) {
            if (totalItems > 0) {
                cartBadge.textContent = totalItems;
                cartBadge.style.display = 'flex';
            } else {
                cartBadge.style.display = 'none';
            }
        }
    }
    window.updateCartBadge = updateCartBadge;

    // Initialize cart badge on page load
    window.addEventListener('load', function() {
        updateCartBadge();
    });

    // Smooth scroll for anchor links
    function initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                const href = this.getAttribute('href');
                
                // Special handling for connexion and inscription anchors - redirect to actual pages
                if (href === '#connexion' || href === '#login') {
                    e.preventDefault();
                    e.stopPropagation();
                    window.location.replace('/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search));
                    return false;
                }
                
                if (href === '#inscription' || href === '#register') {
                    e.preventDefault();
                    e.stopPropagation();
                    window.location.replace('/Account/Register');
                    return false;
                }
                
                if (href !== '#' && href.length > 1 && !href.includes('?')) {
                    const target = document.querySelector(href);
                    if (target) {
                        e.preventDefault();
                        const offset = 80; // Header height
                        const targetPosition = target.offsetTop - offset;
                        
                        window.scrollTo({
                            top: targetPosition,
                            behavior: 'smooth'
                        });
                    } else {
                        // If target doesn't exist, prevent default to avoid adding hash to URL
                        e.preventDefault();
                    }
                }
            });
        });
    }

    // Initialize animations
    function initAnimations() {
        // Fade in elements on scroll
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver(function(entries) {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('fade-in');
                    observer.unobserve(entry.target);
                }
            });
        }, observerOptions);

        // Observe product and category cards
        document.querySelectorAll('.product-card, .category-card').forEach(el => {
            observer.observe(el);
        });
    }

    // Show notification
    function showNotification(message, type) {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `alert alert-${type === 'success' ? 'success' : 'info'} position-fixed d-flex align-items-center`;
        // Use #305C7D for info/dark blue
        const themeBlue = '#305C7D';
        notification.style.cssText = 'top: 20px; right: 20px; z-index: 10000; min-width: 320px; max-width: 450px; box-shadow: 0 10px 25px rgba(0,0,0,0.1); border: none; border-radius: 12px; padding: 16px; backdrop-filter: blur(8px); background-color: rgba(255,255,255,0.98); color: #333; border-left: 5px solid ' + (type === 'success' ? '#28a745' : themeBlue) + ';';
        
        const icon = type === 'success' ? 
            '<svg class="me-3 flex-shrink-0" width="24" height="24" fill="#28a745" viewBox="0 0 16 16"><path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 5.384 7.323a.75.75 0 0 0-1.06 1.06L6.97 11.03a.75.75 0 0 0 1.079-.02l3.992-4.99a.75.75 0 0 0-.01-1.05z"/></svg>' :
            `<svg class="me-3 flex-shrink-0" width="24" height="24" fill="${themeBlue}" viewBox="0 0 16 16"><path d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16zm.93-9.412-1 4.705c-.07.34.029.533.304.533.194 0 .487-.07.686-.246l-.088.416c-.287.346-.92.598-1.465.598-.703 0-1.002-.422-.808-1.319l.738-3.468c.064-.293.006-.399-.287-.47l-.451-.081.082-.381 2.29-.287zM8 5.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2z"/></svg>`;

        notification.innerHTML = `${icon}<div style="font-size: 0.95rem;">${message}</div>`;
        
        document.body.appendChild(notification);
        
        // Animation
        notification.style.transition = 'all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275)';
        notification.style.transform = 'translateX(100%)';
        notification.style.opacity = '0';
        
        requestAnimationFrame(() => {
            notification.style.transform = 'translateX(0)';
            notification.style.opacity = '1';
        });
        
        // Remove after 7 seconds (increased from 4s)
        setTimeout(() => {
            notification.style.transform = 'translateX(100%)';
            notification.style.opacity = '0';
            setTimeout(() => {
                if (document.body.contains(notification)) {
                    document.body.removeChild(notification);
                }
            }, 400);
        }, 7000);
    }

    // Mobile menu toggle enhancement
    const navbarToggler = document.querySelector('.navbar-toggler');
    if (navbarToggler) {
        navbarToggler.addEventListener('click', function() {
            const navbar = document.querySelector('#navbarNav');
            if (navbar) {
                navbar.classList.toggle('show');
            }
        });
    }

})();

