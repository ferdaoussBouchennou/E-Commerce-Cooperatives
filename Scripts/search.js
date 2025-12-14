/* Global Search Logic */
(function() {
    'use strict';

    document.addEventListener('DOMContentLoaded', function() {
        const searchContainers = document.querySelectorAll('.smart-search-container');

        searchContainers.forEach(container => {
            const searchInput = container.querySelector('.smart-search-input');
            const suggestionsContainer = container.querySelector('.search-suggestions');
            let debounceTimeout;

            if (!searchInput || !suggestionsContainer) return;

            // Event listener for input
            searchInput.addEventListener('input', function() {
                const term = this.value.trim();
                
                clearTimeout(debounceTimeout);
                
                if (term.length < 2) {
                    suggestionsContainer.style.display = 'none';
                    return;
                }

                debounceTimeout = setTimeout(() => {
                    fetchSuggestions(term, suggestionsContainer, searchInput);
                }, 300);
            });

            // Close suggestions when clicking outside
            document.addEventListener('click', function(e) {
                if (!container.contains(e.target)) {
                    suggestionsContainer.style.display = 'none';
                }
            });
        });

        function fetchSuggestions(term, container, input) {
            fetch('/Catalogue/Suggestions?term=' + encodeURIComponent(term))
                .then(response => response.json())
                .then(data => {
                    renderSuggestions(data, container, input);
                })
                .catch(error => {
                    console.error('Error fetching suggestions:', error);
                });
        }

        function renderSuggestions(products, container, input) {
            if (!products || products.length === 0) {
                container.style.display = 'none';
                return;
            }

            let html = '<div class="list-group list-group-flush">';
            
            products.forEach(product => {
                const imageUrl = product.ImageUrl || '/Content/images/default-product.jpg';
                const price = new Intl.NumberFormat('fr-MA', { style: 'currency', currency: 'MAD' }).format(product.Prix);
                
                html += `
                    <a href="/Catalogue?search=${encodeURIComponent(product.Nom)}" class="list-group-item list-group-item-action d-flex align-items-center gap-3 py-2">
                        <img src="${imageUrl}" alt="${product.Nom}" class="rounded" style="width: 40px; height: 40px; object-fit: cover;">
                        <div class="flex-grow-1 overflow-hidden">
                            <h6 class="mb-0 text-truncate" style="font-size: 0.9rem;">${highlightTerm(product.Nom, input.value)}</h6>
                            <small class="text-muted fw-bold" style="color: var(--terracota-calido) !important;">${price}</small>
                        </div>
                    </a>
                `;
            });
            
            // Add "See all results" link via form submission if possible, else link
            if (input.form) {
             html += `
                <button type="submit" form="${input.form.id}" class="list-group-item list-group-item-action text-center text-primary small fw-bold py-2 bg-light border-top">
                    Voir tous les résultats
                </button>
            `;
            }

            html += '</div>';
            
            container.innerHTML = html;
            container.style.display = 'block';
        }

        function highlightTerm(text, term) {
            if (!term) return text;
            const regex = new RegExp(`(${term})`, 'gi');
            return text.replace(regex, '<span class="bg-warning bg-opacity-25 fw-bold">$1</span>');
        }
    });
})();
