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
        
        if (addItemId && isUserAuthenticated()) {
            // Add product to cart
            addProductToCart(addItemId);
            
            // Show notification
            showNotification('Produit ajouté au panier automatiquement!', 'success');
            
            // Update cart badge
            updateCartBadge();
            
            // Clean up URL without refreshing the page
            const newUrl = window.location.pathname + window.location.search.replace(/[&?]addItem=[^&]*/, '').replace(/^&/, '?');
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
        notification.className = `alert alert-${type === 'success' ? 'success' : 'info'} position-fixed`;
        notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);';
        notification.textContent = message;
        
        document.body.appendChild(notification);
        
        // Remove after 3 seconds
        setTimeout(() => {
            notification.style.transition = 'opacity 0.3s ease';
            notification.style.opacity = '0';
            setTimeout(() => {
                document.body.removeChild(notification);
            }, 300);
        }, 3000);
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

