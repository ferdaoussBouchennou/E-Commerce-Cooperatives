// Catalogue filtering and sorting functionality
(function() {
    'use strict';

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function() {
        initCatalogueFilters();
        initPriceRange();
    });

    let allProducts = [];
    let filteredProducts = [];
    let currentFilters = {
        search: '',
        categories: [],
        cooperatives: [],
        priceMin: 0,
        priceMax: 1500,
        onlyAvailable: false,
        sortBy: 'popular'
    };

    function initCatalogueFilters() {
        // Price Range Display
        const priceMinRange = document.getElementById('price-range-min');
        const priceMaxRange = document.getElementById('price-range-max');
        const priceMinDisplay = document.getElementById('price-min');
        const priceMaxDisplay = document.getElementById('price-max');

        if (priceMinRange && priceMaxRange) {
             priceMinRange.addEventListener('input', function() {
                if (priceMinDisplay) priceMinDisplay.textContent = this.value + ' MAD';
             });
             priceMaxRange.addEventListener('input', function() {
                if (priceMaxDisplay) priceMaxDisplay.textContent = this.value + ' MAD';
             });
             // We rely on 'change' (mouse up) to submit form, which is added inline in HTML
        }

        // Cooperatives Multi-select Logic
        const filterForm = document.getElementById('filter-form');
        const coopsHidden = document.getElementById('coops-hidden');
        const coopCheckboxes = document.querySelectorAll('.filter-cooperative-checkbox');

        coopCheckboxes.forEach(cb => {
            cb.addEventListener('change', function() {
                if (filterForm && coopsHidden) {
                    const checkedCoops = Array.from(coopCheckboxes)
                        .filter(c => c.checked)
                        .map(c => c.value);
                    coopsHidden.value = checkedCoops.join(',');
                    filterForm.submit();
                }
            });
        });

        // Initialize mobile filters if needed (but currently stripped down for this fix)
    }

    // Legacy function stubs to prevent errors if invoked
    function applyFilters() {}
    function initFiltersFromURL() {}
    function initPriceRange() {}

    function initFiltersFromURL() {
        const urlParams = new URLSearchParams(window.location.search);
        const categorieId = urlParams.get('categorie');
        
        if (categorieId) {
            const categoryFilter = document.getElementById('cat-' + categorieId);
            if (categoryFilter) {
                categoryFilter.checked = true;
                const mobileFilter = document.getElementById('cat-mobile-' + categorieId);
                if (mobileFilter) {
                    mobileFilter.checked = true;
                }
            }
            updateCategoryFilters();
        }
    }

    function updateCategoryFilters() {
        const checked = document.querySelectorAll('.filter-category:checked, .filter-category-mobile:checked');
        currentFilters.categories = Array.from(checked).map(cb => parseInt(cb.value));
    }

    function updateCooperativeFilters() {
        const checked = document.querySelectorAll('.filter-cooperative:checked, .filter-cooperative-mobile:checked');
        currentFilters.cooperatives = Array.from(checked).map(cb => parseInt(cb.value));
    }

    function initPriceRange() {
        // Calculate max price from all products
        const maxPrice = allProducts.length > 0 
            ? Math.max(...allProducts.map(p => parseFloat(p.dataset.prix || 0)))
            : 1500;
        const roundedMax = Math.max(Math.ceil(maxPrice / 100) * 100, 1500);

        // Desktop price range
        const priceMinRange = document.getElementById('price-range-min');
        const priceMaxRange = document.getElementById('price-range-max');
        const priceMinDisplay = document.getElementById('price-min');
        const priceMaxDisplay = document.getElementById('price-max');

        if (priceMinRange && priceMaxRange) {
            // Set max value
            priceMaxRange.max = roundedMax;
            priceMaxRange.value = roundedMax;
            currentFilters.priceMax = roundedMax;

            if (priceMaxDisplay) {
                priceMaxDisplay.textContent = roundedMax + ' MAD';
            }

            priceMinRange.addEventListener('input', function() {
                const minVal = parseInt(this.value);
                const maxVal = parseInt(priceMaxRange.value);
                if (minVal > maxVal) {
                    this.value = maxVal;
                    currentFilters.priceMin = maxVal;
                } else {
                    currentFilters.priceMin = minVal;
                }
                if (priceMinDisplay) {
                    priceMinDisplay.textContent = this.value + ' MAD';
                }
                applyFilters();
            });

            priceMaxRange.addEventListener('input', function() {
                const maxVal = parseInt(this.value);
                const minVal = parseInt(priceMinRange.value);
                if (maxVal < minVal) {
                    this.value = minVal;
                    currentFilters.priceMax = minVal;
                } else {
                    currentFilters.priceMax = maxVal;
                }
                if (priceMaxDisplay) {
                    priceMaxDisplay.textContent = this.value + ' MAD';
                }
                applyFilters();
            });
        }

        // Mobile price range
        const priceMinRangeMobile = document.getElementById('price-range-min-mobile');
        const priceMaxRangeMobile = document.getElementById('price-range-max-mobile');
        const priceMinDisplayMobile = document.getElementById('price-min-mobile');
        const priceMaxDisplayMobile = document.getElementById('price-max-mobile');

        if (priceMinRangeMobile && priceMaxRangeMobile) {
            priceMaxRangeMobile.max = roundedMax;
            priceMaxRangeMobile.value = roundedMax;
            currentFilters.priceMax = roundedMax;

            if (priceMaxDisplayMobile) {
                priceMaxDisplayMobile.textContent = roundedMax + ' MAD';
            }

            priceMinRangeMobile.addEventListener('input', function() {
                const minVal = parseInt(this.value);
                const maxVal = parseInt(priceMaxRangeMobile.value);
                if (minVal > maxVal) {
                    this.value = maxVal;
                    currentFilters.priceMin = maxVal;
                } else {
                    currentFilters.priceMin = minVal;
                }
                if (priceMinDisplayMobile) {
                    priceMinDisplayMobile.textContent = this.value + ' MAD';
                }
                // Sync with desktop
                if (priceMinRange && priceMinDisplay) {
                    priceMinRange.value = this.value;
                    priceMinDisplay.textContent = this.value + ' MAD';
                }
                applyFilters();
            });

            priceMaxRangeMobile.addEventListener('input', function() {
                const maxVal = parseInt(this.value);
                const minVal = parseInt(priceMinRangeMobile.value);
                if (maxVal < minVal) {
                    this.value = minVal;
                    currentFilters.priceMax = minVal;
                } else {
                    currentFilters.priceMax = maxVal;
                }
                if (priceMaxDisplayMobile) {
                    priceMaxDisplayMobile.textContent = this.value + ' MAD';
                }
                // Sync with desktop
                if (priceMaxRange && priceMaxDisplay) {
                    priceMaxRange.value = this.value;
                    priceMaxDisplay.textContent = this.value + ' MAD';
                }
                applyFilters();
            });
        }
    }

    function applyFilters() {
        filteredProducts = allProducts.filter(product => {
            // Search filter
            if (currentFilters.search) {
                const nom = product.dataset.nom || '';
                const description = product.dataset.description || '';
                if (!nom.includes(currentFilters.search) && !description.includes(currentFilters.search)) {
                    return false;
                }
            }

            // Category filter
            if (currentFilters.categories.length > 0) {
                const categorieId = parseInt(product.dataset.categorieId || '0');
                if (!currentFilters.categories.includes(categorieId)) {
                    return false;
                }
            }

            // Cooperative filter
            if (currentFilters.cooperatives.length > 0) {
                const cooperativeId = parseInt(product.dataset.cooperativeId || '0');
                if (!currentFilters.cooperatives.includes(cooperativeId)) {
                    return false;
                }
            }

            // Price filter
            const prix = parseFloat(product.dataset.prix || 0);
            if (prix < currentFilters.priceMin || prix > currentFilters.priceMax) {
                return false;
            }

            // Availability filter
            if (currentFilters.onlyAvailable) {
                const estDisponible = product.dataset.estDisponible === 'true';
                const stock = parseInt(product.dataset.stock || '0');
                if (!estDisponible || stock <= 0) {
                    return false;
                }
            }

            return true;
        });

        // Sort products
        sortProducts(filteredProducts);

        // Update display
        displayProducts(filteredProducts);
    }

    function sortProducts(products) {
       // Sorting is now handled server-side.
       // This function is kept empty or removed to prevent client-side reordering that overrides server order.
    }

    function displayProducts(products) {
        const container = document.getElementById('products-grid');
        const productCount = document.getElementById('product-count');

        if (!container) return;

        // Update count
        if (productCount) {
            productCount.textContent = products.length;
        }

        // Hide all products first
        allProducts.forEach(product => {
            product.style.display = 'none';
        });

        // Show filtered products
        if (products.length > 0) {
            products.forEach((product, index) => {
                product.style.display = '';
                // Add fade-in animation
                product.style.opacity = '0';
                setTimeout(() => {
                    product.style.transition = 'opacity 0.3s ease';
                    product.style.opacity = '1';
                }, index * 50);
            });
        } else {
            // Show empty state
            const emptyState = document.querySelector('.text-center.py-5');
            if (!emptyState) {
                container.innerHTML = `
                    <div class="col-12">
                        <div class="text-center py-5">
                            <p class="text-muted mb-4">Aucun produit ne correspond à vos critères</p>
                            <button type="button" class="btn btn-outline-secondary" id="reset-filters-btn">Réinitialiser les filtres</button>
                        </div>
                    </div>
                `;
                const resetBtn = document.getElementById('reset-filters-btn');
                if (resetBtn) {
                    resetBtn.addEventListener('click', clearAllFilters);
                }
            }
        }
    }

    function clearAllFilters() {
        // Clear search
        const searchInput = document.getElementById('catalogue-search-input');
        if (searchInput) {
            searchInput.value = '';
        }

        // Clear checkboxes
        document.querySelectorAll('.filter-category, .filter-category-mobile, .filter-cooperative, .filter-cooperative-mobile').forEach(cb => {
            cb.checked = false;
        });

        // Clear availability
        const onlyAvailable = document.getElementById('only-available');
        const onlyAvailableMobile = document.getElementById('only-available-mobile');
        if (onlyAvailable) onlyAvailable.checked = false;
        if (onlyAvailableMobile) onlyAvailableMobile.checked = false;

        // Reset price range
        const priceMinRange = document.getElementById('price-range-min');
        const priceMaxRange = document.getElementById('price-range-max');
        const priceMinRangeMobile = document.getElementById('price-range-min-mobile');
        const priceMaxRangeMobile = document.getElementById('price-range-max-mobile');
        const maxPrice = allProducts.length > 0 
            ? Math.max(...allProducts.map(p => parseFloat(p.dataset.prix || 0)))
            : 1500;
        const roundedMax = Math.max(Math.ceil(maxPrice / 100) * 100, 1500);

        if (priceMinRange) {
            priceMinRange.value = 0;
            const priceMinDisplay = document.getElementById('price-min');
            if (priceMinDisplay) priceMinDisplay.textContent = '0 MAD';
        }
        if (priceMaxRange) {
            priceMaxRange.value = roundedMax;
            const priceMaxDisplay = document.getElementById('price-max');
            if (priceMaxDisplay) priceMaxDisplay.textContent = roundedMax + ' MAD';
        }
        if (priceMinRangeMobile) {
            priceMinRangeMobile.value = 0;
            const priceMinDisplayMobile = document.getElementById('price-min-mobile');
            if (priceMinDisplayMobile) priceMinDisplayMobile.textContent = '0 MAD';
        }
        if (priceMaxRangeMobile) {
            priceMaxRangeMobile.value = roundedMax;
            const priceMaxDisplayMobile = document.getElementById('price-max-mobile');
            if (priceMaxDisplayMobile) priceMaxDisplayMobile.textContent = roundedMax + ' MAD';
        }

        // Reset sort
        const sortSelect = document.getElementById('sort-select');
        if (sortSelect) {
            sortSelect.value = 'popular';
            // If we are clearing filters, we might want to reload purely, 
            // but the current implementation of applyFilters() is primarily client-side hiding.
            // Since we moved sorting to server-side, client-side applyFilters 
            // will just hide/show items based on price/categories/etc. but won't resort server list.
            // FOR NOW, let's keep it as is, but note that sort won't change until reload.
            // Actually, if clearAllFilters assumes client-side reset, it functionality is now split.
            // To properly clear filters with server-side implementation, we should probably 
            // reload the page with no params.
            window.location.href = window.location.pathname;
            return;
        }

        // Reset filter state
        currentFilters = {
            search: '',
            categories: [],
            cooperatives: [],
            priceMin: 0,
            priceMax: parseInt(priceMaxRange ? priceMaxRange.value : 1500),
            onlyAvailable: false,
            sortBy: 'popular'
        };

        // Apply filters
        applyFilters();

        // Close mobile offcanvas if open
        const offcanvas = bootstrap.Offcanvas.getInstance(document.getElementById('filterOffcanvas'));
        if (offcanvas) {
            offcanvas.hide();
        }
    }

    // Add cursor pointer style for labels
    const style = document.createElement('style');
    style.textContent = `
        .cursor-pointer {
            cursor: pointer;
        }
        .product-item {
            transition: opacity 0.3s ease;
        }
    `;
    document.head.appendChild(style);

})();

