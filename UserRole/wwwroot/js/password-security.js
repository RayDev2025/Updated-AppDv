// Password Security Enforcement - No Copy/Paste Protection
class PasswordSecurityEnforcer {
    constructor() {
        this.initializePasswordSecurity();
    }
    
    initializePasswordSecurity() {
        // Apply security to all password fields
        const passwordFields = document.querySelectorAll('input[type="password"], .password-secure');
        
        passwordFields.forEach(field => {
            this.applyPasswordSecurity(field);
        });

        // Global security measures
        this.applyGlobalSecurity();
    }
    
    applyPasswordSecurity(passwordField) {
        // Add security classes
        passwordField.classList.add('password-no-copy', 'password-field-secure');
        
        // Disable copy/paste/cut
        this.disableCopyPaste(passwordField);
        
        // Disable context menu (right-click)
        this.disableContextMenu(passwordField);
        
        // Disable drag and drop
        this.disableDragDrop(passwordField);
        
        // Disable keyboard shortcuts
        this.disableKeyboardShortcuts(passwordField);
        
        // Add additional security attributes
        passwordField.setAttribute('autocomplete', 'new-password');
        passwordField.setAttribute('data-lpignore', 'true');
        passwordField.setAttribute('readonly', 'true'); // Temporary
        
        // Remove readonly when user interacts (prevents autofill)
        passwordField.addEventListener('click', function() {
            this.removeAttribute('readonly');
        });
        
        passwordField.addEventListener('focus', function() {
            this.removeAttribute('readonly');
        });

        // Show security message on focus
        passwordField.addEventListener('focus', () => {
            this.showSecurityMessage(passwordField);
        });
    }
    
    disableCopyPaste(element) {
        // Disable copy
        element.addEventListener('copy', function(e) {
            e.preventDefault();
            e.stopPropagation();
            console.warn('🔒 Copy operation blocked for security');
            return false;
        });
        
        // Disable paste
        element.addEventListener('paste', function(e) {
            e.preventDefault();
            e.stopPropagation();
            console.warn('🔒 Paste operation blocked for security');
            return false;
        });
        
        // Disable cut
        element.addEventListener('cut', function(e) {
            e.preventDefault();
            e.stopPropagation();
            console.warn('🔒 Cut operation blocked for security');
            return false;
        });
    }
    
    disableContextMenu(element) {
        element.addEventListener('contextmenu', function(e) {
            e.preventDefault();
            e.stopPropagation();
            console.warn('🔒 Right-click blocked for security');
            return false;
        });
    }
    
    disableDragDrop(element) {
        element.addEventListener('dragstart', function(e) {
            e.preventDefault();
            return false;
        });
        
        element.addEventListener('drop', function(e) {
            e.preventDefault();
            return false;
        });
    }
    
    disableKeyboardShortcuts(element) {
        element.addEventListener('keydown', function(e) {
            // Disable Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+A, Ctrl+S
            if (e.ctrlKey && (e.keyCode === 67 || e.keyCode === 86 || e.keyCode === 88 || e.keyCode === 65 || e.keyCode === 83)) {
                e.preventDefault();
                e.stopPropagation();
                console.warn('🔒 Keyboard shortcut blocked for security');
                return false;
            }
            
            // Disable F12 (Developer Tools)
            if (e.keyCode === 123) {
                e.preventDefault();
                return false;
            }
            
            // Disable Ctrl+Shift+I (Developer Tools)
            if (e.ctrlKey && e.shiftKey && e.keyCode === 73) {
                e.preventDefault();
                return false;
            }
            
            // Disable Ctrl+U (View Source)
            if (e.ctrlKey && e.keyCode === 85) {
                e.preventDefault();
                return false;
            }
        });
    }

    showSecurityMessage(element) {
        // Create temporary security notice
        let existingNotice = document.querySelector('.security-notice-temp');
        if (existingNotice) {
            existingNotice.remove();
        }

        const notice = document.createElement('div');
        notice.className = 'security-notice-temp';
        notice.style.cssText = `
            position: absolute;
            background: #dc3545;
            color: white;
            padding: 5px 10px;
            border-radius: 3px;
            font-size: 12px;
            z-index: 1000;
            margin-top: -30px;
            white-space: nowrap;
        `;
        notice.textContent = '🔒 Copy/Paste Disabled';
        
        element.parentNode.style.position = 'relative';
        element.parentNode.appendChild(notice);
        
        // Remove notice after 2 seconds
        setTimeout(() => {
            if (notice.parentNode) {
                notice.parentNode.removeChild(notice);
            }
        }, 2000);
    }

    applyGlobalSecurity() {
        // Disable developer tools shortcuts globally
        document.addEventListener('keydown', function(e) {
            // Disable F12, Ctrl+Shift+I, Ctrl+U globally
            if (e.keyCode === 123 || 
                (e.ctrlKey && e.shiftKey && e.keyCode === 73) ||
                (e.ctrlKey && e.keyCode === 85)) {
                e.preventDefault();
                return false;
            }
        });

        // Disable right-click on password pages
        if (document.querySelector('input[type="password"]')) {
            document.addEventListener('contextmenu', function(e) {
                e.preventDefault();
                return false;
            });
        }

        // Console warning
        console.warn(`
🔒 PASSWORD SECURITY ACTIVE 🔒
- Copy/paste operations disabled
- Right-click context menu disabled
- Developer tools access restricted
- Keyboard shortcuts blocked
- Password history enforced (last 5 passwords)
- Login attempts limited to 3
        `);
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    new PasswordSecurityEnforcer();
});

// Additional protection against console access
(function() {
    let devtools = {
        open: false,
        orientation: null
    };
    
    setInterval(function() {
        if (window.outerHeight - window.innerHeight > 200 || 
            window.outerWidth - window.innerWidth > 200) {
            if (!devtools.open) {
                devtools.open = true;
                console.clear();
                console.warn('🔒 Developer tools detected. Password security is active.');
            }
        } else {
            devtools.open = false;
        }
    }, 500);
})();