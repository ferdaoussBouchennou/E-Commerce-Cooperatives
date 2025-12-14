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
        // Get all products from the DOM
        const productItems = document.querySelectorAll('.product-item');
        allProducts = Array.from(productItems);

        // Initialize filters from URL parameters
        initFiltersFromURL();

        // Search input
        const searchInput = document.getElementById('search-input');
        if (searchInput) {
            let searchTimeout;
            searchInput.addEventListener('input', function(e) {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    currentFilters.search = e.target.value.toLowerCase().trim();
                    applyFilters();
                }, 300);
            });
        }

        // Category filters (desktop)
        const categoryFilters = document.querySelectorAll('.filter-category');
        categoryFilters.forEach(filter => {
            filter.addEventListener('change', function() {
                updateCategoryFilters();
                applyFilters();
            });
        });

        // Category filters (mobile)
        const categoryFiltersMobile = document.querySelectorAll('.filter-category-mobile');
        categoryFiltersMobile.forEach(filter => {
            filter.addEventListener('change', function() {
                // Sync with desktop
                const desktopFilter = document.getElementById('cat-' + this.value);
                if (desktopFilter) {
                    desktopFilter.checked = this.checked;
                }
                updateCategoryFilters();
                applyFilters();
            });
        });

        // Cooperative filters (desktop)
        const cooperativeFilters = document.querySelectorAll('.filter-cooperative');
        cooperativeFilters.forEach(filter => {
            filter.addEventListener('change', function() {
                updateCooperativeFilters();
                applyFilters();
            });
        });

        // Cooperative filters (mobile)
        const cooperativeFiltersMobile = document.querySelectorAll('.filter-cooperative-mobile');
        cooperativeFiltersMobile.forEach(filter => {
            filter.addEventListener('change', function() {
                // Sync with desktop
                const desktopFilter = document.getElementById('coop-' + this.value);
                if (desktopFilter) {
                    desktopFilter.checked = this.checked;
                }
                updateCooperativeFilters();
                applyFilters();
            });
        });

        // Only available filter (desktop)
        const onlyAvailable = document.getElementById('only-available');
        if (onlyAvailable) {
            onlyAvailable.addEventListener('change', function() {
                currentFilters.onlyAvailable = this.checked;
                // Sync with mobile
                const mobileFilter = document.getElementById('only-available-mobile');
                if (mobileFilter) {
                    mobileFilter.checked = this.checked;
                }
                applyFilters();
            });
        }

        // Only available filter (mobile)
        const onlyAvailableMobile = document.getElementById('only-available-mobile');
        if (onlyAvailableMobile) {
            onlyAvailableMobile.addEventListener('change', function() {
                currentFilters.onlyAvailable = this.checked;
                // Sync with desktop
                const desktopFilter = document.getElementById('only-available');
                if (desktopFilter) {
                    desktopFilter.checked = this.checked;
                }
                applyFilters();
            });
        }

        // Sort select
        const sortSelect = document.getElementById('sort-select');
        if (sortSelect) {
            sortSelect.addEventListener('change', function() {
                currentFilters.sortBy = this.value;
                applyFilters();
            });
        }

        // Clear filters buttons
        const clearFiltersBtn = document.getElementById('clear-filters');
        if (clearFiltersBtn) {
            clearFiltersBtn.addEventListener('click', clearAllFilters);
        }

        const clearFiltersMobileBtn = document.getElementById('clear-filters-mobile');
        if (clearFiltersMobileBtn) {
            clearFiltersMobileBtn.addEventListener('click', clearAllFilters);
        }

        const resetFiltersBtn = document.getElementById('reset-filters-btn');
        if (resetFiltersBtn) {
            resetFiltersBtn.addEventListener('click', clearAllFilters);
        }

        // Initial filter application
        applyFilters();
    }

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
        products.sort((a, b) => {
            switch (currentFilters.sortBy) {
                case 'newest':
                    const aNew = a.dataset.estNouveau === 'true' ? 1 : 0;
                    const bNew = b.dataset.estNouveau === 'true' ? 1 : 0;
                    return bNew - aNew;

                case 'price-asc':
                    return parseFloat(a.dataset.prix || 0) - parseFloat(b.dataset.prix || 0);

                case 'price-desc':
                    return parseFloat(b.dataset.prix || 0) - parseFloat(a.dataset.prix || 0);

                case 'rating':
                    return parseFloat(b.dataset.note || 0) - parseFloat(a.dataset.note || 0);

                case 'popular':
                default:
                    return parseInt(b.dataset.nombreAvis || 0) - parseInt(a.dataset.nombreAvis || 0);
            }
        });
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
        const searchInput = document.getElementById('search-input');
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

