// Admin JavaScript
// Admin-specific scripts including notification system

/**
 * Show a styled notification in the admin space
 * @param {string} message - The notification message
 * @param {string} type - Notification type: 'success', 'error', 'info', 'warning'
 * @param {number} duration - Duration in milliseconds (default: 4000)
 */
function showAdminNotification(message, type = 'info', duration = 4000) {
    // Ensure notification container exists
    let container = document.getElementById('admin-notification-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'admin-notification-container';
        container.className = 'admin-notification-container';
        document.body.appendChild(container);
    }

    // Create notification element
    const notification = document.createElement('div');
    notification.className = `admin-notification ${type}`;

    // Determine icon based on type
    let iconSvg = '';
    switch (type) {
        case 'success':
            iconSvg = '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" viewBox="0 0 16 16"><path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 5.384 7.323a.75.75 0 0 0-1.06 1.06L6.97 11.03a.75.75 0 0 0 1.079-.02l3.992-4.99a.75.75 0 0 0-.01-1.05z"/></svg>';
            break;
        case 'error':
            iconSvg = '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" viewBox="0 0 16 16"><path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zM5.354 4.646a.5.5 0 1 0-.708.708L7.293 8l-2.647 2.646a.5.5 0 0 0 .708.708L8 8.707l2.646 2.647a.5.5 0 0 0 .708-.708L8.707 8l2.647-2.646a.5.5 0 0 0-.708-.708L8 7.293 5.354 4.646z"/></svg>';
            break;
        case 'warning':
            iconSvg = '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" viewBox="0 0 16 16"><path d="M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5zm.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2z"/></svg>';
            break;
        default: // info
            iconSvg = '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" viewBox="0 0 16 16"><path d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16zm.93-9.412-1 4.705c-.07.34.029.533.304.533.194 0 .487-.07.686-.246l-.088.416c-.287.346-.92.598-1.465.598-.703 0-1.002-.422-.808-1.319l.738-3.468c.064-.293.006-.399-.287-.47l-.451-.081.082-.381 2.29-.287zM8 5.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2z"/></svg>';
    }

    // Determine title based on type
    let title = '';
    switch (type) {
        case 'success':
            title = 'Succès';
            break;
        case 'error':
            title = 'Erreur';
            break;
        case 'warning':
            title = 'Attention';
            break;
        default:
            title = 'Information';
    }

    // Build notification HTML
    notification.innerHTML = `
        <div class="admin-notification-icon">${iconSvg}</div>
        <div class="admin-notification-content">
            <div class="admin-notification-title">${title}</div>
            <div class="admin-notification-message">${message}</div>
        </div>
        <button class="admin-notification-close" onclick="this.closest('.admin-notification').remove()">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                <path d="M2.146 2.854a.5.5 0 1 1 .708-.708L8 7.293l5.146-5.147a.5.5 0 0 1 .708.708L8.707 8l5.147 5.146a.5.5 0 0 1-.708.708L8 8.707l-5.146 5.147a.5.5 0 0 1-.708-.708L7.293 8 2.146 2.854Z"/>
            </svg>
        </button>
        <div class="admin-notification-progress">
            <div class="admin-notification-progress-bar"></div>
        </div>
    `;

    // Add to container
    container.appendChild(notification);

    // Trigger animation
    setTimeout(() => {
        notification.classList.add('show');
    }, 10);

    // Auto-remove after duration
    const removeNotification = () => {
        notification.classList.remove('show');
        notification.classList.add('hide');
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 300);
    };

    // Set timeout for auto-remove
    const timeoutId = setTimeout(removeNotification, duration);

    // Clear timeout if user closes manually
    const closeBtn = notification.querySelector('.admin-notification-close');
    closeBtn.addEventListener('click', () => {
        clearTimeout(timeoutId);
        removeNotification();
    });
}

// Make function globally available
window.showAdminNotification = showAdminNotification;

/**
 * Show a styled confirmation dialog in the admin space
 * @param {string} message - The confirmation message
 * @param {string} title - Optional title (default: 'Confirmation')
 * @param {string} confirmText - Text for confirm button (default: 'Confirmer')
 * @param {string} cancelText - Text for cancel button (default: 'Annuler')
 * @returns {Promise<boolean>} - Promise that resolves to true if confirmed, false if cancelled
 */
function showAdminConfirm(message, title = 'Confirmation', confirmText = 'Confirmer', cancelText = 'Annuler') {
    return new Promise((resolve) => {
        // Create modal overlay
        const overlay = document.createElement('div');
        overlay.className = 'admin-confirm-overlay';
        overlay.style.cssText = 'position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0, 0, 0, 0.5); z-index: 10001; display: flex; align-items: center; justify-content: center; opacity: 0; transition: opacity 0.3s;';
        
        // Create modal
        const modal = document.createElement('div');
        modal.className = 'admin-confirm-modal';
        modal.style.cssText = 'background: white; border-radius: 16px; box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3); max-width: 480px; width: 90%; max-height: 90vh; overflow: hidden; transform: scale(0.9); transition: transform 0.3s;';
        
        // Build modal content
        modal.innerHTML = `
            <div style="padding: 24px 28px 20px;">
                <div style="display: flex; align-items: flex-start; gap: 16px; margin-bottom: 20px;">
                    <div class="admin-confirm-icon" style="flex-shrink: 0; width: 48px; height: 48px; display: flex; align-items: center; justify-content: center; background: linear-gradient(135deg, rgba(192, 108, 80, 0.15) 0%, rgba(192, 108, 80, 0.25) 100%); border-radius: 12px;">
                        <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" fill="#C06C50" viewBox="0 0 16 16">
                            <path d="M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5zm.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2z"/>
                        </svg>
                    </div>
                    <div style="flex: 1;">
                        <h3 class="admin-confirm-title" style="margin: 0 0 8px 0; font-size: 20px; font-weight: 600; color: #305C7D; font-family: var(--font-display, 'Playfair Display', serif);">${title}</h3>
                        <div class="admin-confirm-message" style="font-size: 15px; line-height: 1.6; color: #6D6C6A; font-family: var(--font-sans, 'Source Sans 3', sans-serif); white-space: pre-line;">${message}</div>
                    </div>
                </div>
            </div>
            <div style="padding: 16px 28px 24px; background: #F9F9F9; border-top: 1px solid #EAEAEA; display: flex; gap: 12px; justify-content: flex-end;">
                <button class="admin-confirm-btn admin-confirm-btn-cancel" style="padding: 10px 24px; border: 1px solid #D0D0D0; background: white; border-radius: 8px; font-size: 14px; font-weight: 500; color: #6D6C6A; cursor: pointer; transition: all 0.2s; font-family: var(--font-sans, 'Source Sans 3', sans-serif);">${cancelText}</button>
                <button class="admin-confirm-btn admin-confirm-btn-confirm" style="padding: 10px 24px; border: none; background: linear-gradient(135deg, #C06C50 0%, #B85A3F 100%); color: white; border-radius: 8px; font-size: 14px; font-weight: 600; cursor: pointer; transition: all 0.2s; font-family: var(--font-sans, 'Source Sans 3', sans-serif); box-shadow: 0 2px 8px rgba(192, 108, 80, 0.3);">${confirmText}</button>
            </div>
        `;
        
        overlay.appendChild(modal);
        document.body.appendChild(overlay);
        
        // Animate in
        requestAnimationFrame(() => {
            overlay.style.opacity = '1';
            modal.style.transform = 'scale(1)';
        });
        
        // Handle confirm
        const confirmBtn = modal.querySelector('.admin-confirm-btn-confirm');
        confirmBtn.addEventListener('click', () => {
            overlay.style.opacity = '0';
            modal.style.transform = 'scale(0.9)';
            setTimeout(() => {
                document.body.removeChild(overlay);
                resolve(true);
            }, 300);
        });
        
        // Handle cancel
        const cancelBtn = modal.querySelector('.admin-confirm-btn-cancel');
        const handleCancel = () => {
            overlay.style.opacity = '0';
            modal.style.transform = 'scale(0.9)';
            setTimeout(() => {
                document.body.removeChild(overlay);
                resolve(false);
            }, 300);
        };
        
        cancelBtn.addEventListener('click', handleCancel);
        
        // Close on overlay click
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                handleCancel();
            }
        });
        
        // Close on Escape key
        const handleEscape = (e) => {
            if (e.key === 'Escape') {
                handleCancel();
                document.removeEventListener('keydown', handleEscape);
            }
        };
        document.addEventListener('keydown', handleEscape);
        
        // Hover effects
        confirmBtn.addEventListener('mouseenter', () => {
            confirmBtn.style.transform = 'translateY(-1px)';
            confirmBtn.style.boxShadow = '0 4px 12px rgba(192, 108, 80, 0.4)';
        });
        confirmBtn.addEventListener('mouseleave', () => {
            confirmBtn.style.transform = 'translateY(0)';
            confirmBtn.style.boxShadow = '0 2px 8px rgba(192, 108, 80, 0.3)';
        });
        
        cancelBtn.addEventListener('mouseenter', () => {
            cancelBtn.style.background = '#F5F5F5';
            cancelBtn.style.borderColor = '#C0C0C0';
        });
        cancelBtn.addEventListener('mouseleave', () => {
            cancelBtn.style.background = 'white';
            cancelBtn.style.borderColor = '#D0D0D0';
        });
    });
}

// Make function globally available
window.showAdminConfirm = showAdminConfirm;

document.addEventListener('DOMContentLoaded', function() {
    // Any admin-specific initialization code can go here
});
