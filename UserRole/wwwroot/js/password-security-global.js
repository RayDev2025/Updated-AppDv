document.addEventListener('DOMContentLoaded', function () {
    applyPasswordSecurity();
});

function applyPasswordSecurity() {
    // Get ALL password fields on the page
    const allPasswordFields = document.querySelectorAll('input[type="password"], .password-secure');

    allPasswordFields.forEach(field => {
        // Disable copy
        field.addEventListener('copy', function (e) {
            e.preventDefault();
            console.warn('Copy disabled for security');
            return false;
        });

        // Disable paste
        field.addEventListener('paste', function (e) {
            e.preventDefault();
            console.warn('Paste disabled for security');
            return false;
        });

        // Disable cut
        field.addEventListener('cut', function (e) {
            e.preventDefault();
            console.warn('Cut disabled for security');
            return false;
        });

        // Disable right-click
        field.addEventListener('contextmenu', function (e) {
            e.preventDefault();
            return false;
        });

        // Disable keyboard shortcuts (Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+A)
        field.addEventListener('keydown', function (e) {
            if (e.ctrlKey && (e.keyCode === 67 || e.keyCode === 86 || e.keyCode === 88 || e.keyCode === 65)) {
                e.preventDefault();
                return false;
            }
        });

        // Disable drag and drop
        field.addEventListener('dragstart', function (e) {
            e.preventDefault();
            return false;
        });

        field.addEventListener('drop', function (e) {
            e.preventDefault();
            return false;
        });

        // Add visual indicator
        field.style.userSelect = 'none';
        field.style.webkitUserSelect = 'none';
        field.style.mozUserSelect = 'none';
        field.style.msUserSelect = 'none';
    });

    console.warn('Password security active on all password fields');
}