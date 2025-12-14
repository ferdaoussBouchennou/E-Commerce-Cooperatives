// Catalogue filtering and sorting functionality
(function() {
    'use strict';

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function() {
        initCatalogueFilters();
        initPriceRange();
        initResetButton();
    });

    function initCatalogueFilters() {
        // Desktop Cooperatives Multi-select Logic
        const filterForm = document.getElementById('filter-form');
        const coopsHidden = document.getElementById('coops-hidden');
        const coopCheckboxes = document.querySelectorAll('.filter-cooperative-checkbox');

        if (filterForm && coopsHidden) {
            coopCheckboxes.forEach(cb => {
                cb.addEventListener('change', function() {
                    const checkedCoops = Array.from(coopCheckboxes)
                        .filter(c => c.checked)
                        .map(c => c.value);
                    console.log('Coops changed:', checkedCoops);
                    coopsHidden.value = checkedCoops.join(',');
                    filterForm.submit();
                });
            });
        }

        // Mobile Filter Apply Button
        const applyMobileBtn = document.getElementById('apply-mobile-filters');
        if (applyMobileBtn) {
            applyMobileBtn.addEventListener('click', applyMobileFilters);
        }

        // Mobile Clear Filters
        const clearMobileBtn = document.getElementById('clear-filters-mobile');
        if (clearMobileBtn) {
            clearMobileBtn.addEventListener('click', function() {
                window.location.href = window.location.pathname;
            });
        }

        // Initialize mobile price displays
        updateMobilePriceDisplay();
    }

    function initPriceRange() {
        // Desktop price range
        const priceMinRange = document.getElementById('price-range-min');
        const priceMaxRange = document.getElementById('price-range-max');
        const priceMinDisplay = document.getElementById('price-min');
        const priceMaxDisplay = document.getElementById('price-max');

        if (priceMinRange && priceMaxRange) {
            priceMinRange.addEventListener('input', function() {
                const minVal = parseInt(this.value);
                const maxVal = parseInt(priceMaxRange.value);
                
                // Prevent crossing
                if (minVal > maxVal) {
                    this.value = maxVal;
                }
                
                if (priceMinDisplay) {
                    priceMinDisplay.textContent = this.value + ' MAD';
                }
            });

            priceMaxRange.addEventListener('input', function() {
                const maxVal = parseInt(this.value);
                const minVal = parseInt(priceMinRange.value);
                
                // Prevent crossing
                if (maxVal < minVal) {
                    this.value = minVal;
                }
                
                if (priceMaxDisplay) {
                    priceMaxDisplay.textContent = this.value + ' MAD';
                }
            });
        }

        // Mobile price range
        const priceMinRangeMobile = document.getElementById('price-range-min-mobile');
        const priceMaxRangeMobile = document.getElementById('price-range-max-mobile');
        const priceMinDisplayMobile = document.getElementById('price-min-mobile');
        const priceMaxDisplayMobile = document.getElementById('price-max-mobile');

        if (priceMinRangeMobile && priceMaxRangeMobile) {
            priceMinRangeMobile.addEventListener('input', function() {
                const minVal = parseInt(this.value);
                const maxVal = parseInt(priceMaxRangeMobile.value);
                if (minVal > maxVal) {
                    this.value = maxVal;
                }
                if (priceMinDisplayMobile) {
                    priceMinDisplayMobile.textContent = this.value + ' MAD';
                }
            });

            priceMaxRangeMobile.addEventListener('input', function() {
                const maxVal = parseInt(this.value);
                const minVal = parseInt(priceMinRangeMobile.value);
                if (maxVal < minVal) {
                    this.value = minVal;
                }
                if (priceMaxDisplayMobile) {
                    priceMaxDisplayMobile.textContent = this.value + ' MAD';
                }
            });
        }
    }

    function updateMobilePriceDisplay() {
        const mobilePriceMin = document.getElementById('price-range-min-mobile');
        const mobilePriceMax = document.getElementById('price-range-max-mobile');
        const mobilePriceMinDisplay = document.getElementById('price-min-mobile');
        const mobilePriceMaxDisplay = document.getElementById('price-max-mobile');

        if (mobilePriceMin && mobilePriceMinDisplay) {
            mobilePriceMinDisplay.textContent = mobilePriceMin.value + ' MAD';
        }
        if (mobilePriceMax && mobilePriceMaxDisplay) {
            mobilePriceMaxDisplay.textContent = mobilePriceMax.value + ' MAD';
        }
    }

    window.applyMobileFilters = function() {
        const params = new URLSearchParams(window.location.search);
        
        // Price
        const mobilePriceMin = document.getElementById('price-range-min-mobile');
        const mobilePriceMax = document.getElementById('price-range-max-mobile');
        if (mobilePriceMin) params.set('minPrice', mobilePriceMin.value);
        if (mobilePriceMax) params.set('maxPrice', mobilePriceMax.value);

        // Categories - Single select (radio)
        const checkedCat = document.querySelector('.filter-category-mobile:checked');
        if (checkedCat && checkedCat.value) {
            params.set('categorie', checkedCat.value);
        } else {
            params.delete('categorie');
        }

        // Cooperatives - Multiple select
        const checkedCoops = Array.from(document.querySelectorAll('.filter-cooperative-mobile:checked'))
            .map(cb => cb.value);
        if (checkedCoops.length > 0) {
            params.set('coops', checkedCoops.join(','));
        } else {
            params.delete('coops');
        }

        // Availability
        const availCb = document.getElementById('only-available-mobile');
        if (availCb && availCb.checked) {
            params.set('onlyAvailable', 'true');
        } else {
            params.delete('onlyAvailable');
        }

        // Rating
        const checkedRating = document.querySelector('.filter-rating-mobile:checked');
        if (checkedRating && checkedRating.value) {
            params.set('minRating', checkedRating.value);
        } else {
            params.delete('minRating');
        }

        // Reset page to 1
        params.set('page', '1');

        // Close offcanvas and navigate
        const offcanvas = bootstrap.Offcanvas.getInstance(document.getElementById('filterOffcanvas'));
        if (offcanvas) {
            offcanvas.hide();
        }

        window.location.href = window.location.pathname + '?' + params.toString();
    };

    function initResetButton() {
        const resetBtn = document.getElementById('reset-filters-btn');
        if (resetBtn) {
            resetBtn.addEventListener('click', function() {
                window.location.href = window.location.pathname;
            });
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
        .form-range {
            cursor: pointer;
        }
        .form-check-input {
            cursor: pointer;
        }
    `;
    document.head.appendChild(style);

})();