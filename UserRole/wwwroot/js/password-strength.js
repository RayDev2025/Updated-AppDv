class PasswordStrengthIndicator {
    constructor(passwordInputId, options = {}) {
        this.passwordInput = document.getElementById(passwordInputId);
        this.options = {
            showRequirements: options.showRequirements !== false,
            showToggle: options.showToggle !== false,
            containerClass: options.containerClass || 'password-field-container',
            ...options
        };

        if (!this.passwordInput) {
            console.error(`Password input with ID '${passwordInputId}' not found`);
            return;
        }

        this.init();
    }

    init() {
        this.wrapPasswordField();
        this.createStrengthIndicator();
        if (this.options.showRequirements) {
            this.createRequirements();
        }
        if (this.options.showToggle) {
            this.createToggleButton();
        }
        this.bindEvents();
    }

    wrapPasswordField() {
        const wrapper = document.createElement('div');
        wrapper.className = this.options.containerClass;
        this.passwordInput.parentNode.insertBefore(wrapper, this.passwordInput);

        const inputWrapper = document.createElement('div');
        inputWrapper.className = 'password-input-wrapper';
        wrapper.appendChild(inputWrapper);
        inputWrapper.appendChild(this.passwordInput);

        this.container = wrapper;
        this.inputWrapper = inputWrapper;
    }

    createStrengthIndicator() {
        const strengthContainer = document.createElement('div');
        strengthContainer.className = 'password-strength-container';

        const strengthBar = document.createElement('div');
        strengthBar.className = 'password-strength-bar';

        const strengthFill = document.createElement('div');
        strengthFill.className = 'password-strength-fill';

        const strengthText = document.createElement('div');
        strengthText.className = 'password-strength-text';
        strengthText.textContent = 'Enter password';

        strengthBar.appendChild(strengthFill);
        strengthContainer.appendChild(strengthBar);
        strengthContainer.appendChild(strengthText);

        this.container.appendChild(strengthContainer);

        this.strengthContainer = strengthContainer;
        this.strengthText = strengthText;
    }

    createRequirements() {
        const requirementsContainer = document.createElement('div');
        requirementsContainer.className = 'password-requirements';

        const title = document.createElement('h6');
        title.textContent = 'Password Requirements:';
        requirementsContainer.appendChild(title);

        const requirements = [
            { id: 'length', text: 'At least 8 characters' },
            { id: 'uppercase', text: 'One uppercase letter (A-Z)' },
            { id: 'lowercase', text: 'One lowercase letter (a-z)' },
            { id: 'number', text: 'One number (0-9)' },
            { id: 'special', text: 'One special character (!@#$%^&*)' }
        ];

        this.requirements = {};

        requirements.forEach(req => {
            const reqElement = document.createElement('div');
            reqElement.className = 'requirement';
            reqElement.innerHTML = `
                <span class="requirement-icon">✗</span>
                <span>${req.text}</span>
            `;

            requirementsContainer.appendChild(reqElement);
            this.requirements[req.id] = reqElement;
        });

        this.container.appendChild(requirementsContainer);
        this.requirementsContainer = requirementsContainer;
    }

    createToggleButton() {
        const toggleButton = document.createElement('button');
        toggleButton.type = 'button';
        toggleButton.className = 'password-toggle';
        toggleButton.innerHTML = '👁️';
        toggleButton.title = 'Toggle password visibility';

        this.inputWrapper.appendChild(toggleButton);
        this.toggleButton = toggleButton;
    }

    bindEvents() {
        this.passwordInput.addEventListener('input', (e) => {
            this.updateStrength(e.target.value);
        });

        this.passwordInput.addEventListener('focus', () => {
            if (this.requirementsContainer) {
                this.requirementsContainer.style.display = 'block';
            }
        });

        this.passwordInput.addEventListener('blur', () => {
            if (this.requirementsContainer && !this.passwordInput.value) {
                this.requirementsContainer.style.display = 'none';
            }
        });

        if (this.toggleButton) {
            this.toggleButton.addEventListener('click', () => {
                this.togglePasswordVisibility();
            });
        }
    }

    updateStrength(password) {
        const strength = this.calculateStrength(password);
        const strengthClass = this.getStrengthClass(strength.score);
        const strengthText = this.getStrengthText(strength.score);

        
        this.strengthContainer.className = `password-strength-container ${strengthClass}`;
        this.strengthText.textContent = password ? strengthText : 'Enter password';

        if (this.requirements) {
            this.updateRequirements(strength.checks);
        }
    }

    calculateStrength(password) {
        const checks = {
            length: password.length >= 8,
            uppercase: /[A-Z]/.test(password),
            lowercase: /[a-z]/.test(password),
            number: /[0-9]/.test(password),
            special: /[!@#$%^&*(),.?":{}|<>]/.test(password)
        };

        const score = Object.values(checks).filter(Boolean).length;

        
        const commonPasswords = ['password', '123456', '123456789', '12345678', '12345', '1234567', '1234567890', 'qwerty', 'abc123', 'password123'];
        const isCommon = commonPasswords.some(common => password.toLowerCase().includes(common.toLowerCase()));

        return {
            score: isCommon ? Math.max(0, score - 2) : score,
            checks
        };
    }

    getStrengthClass(score) {
        if (score <= 1) return 'strength-weak';
        if (score <= 2) return 'strength-fair';
        if (score <= 4) return 'strength-good';
        return 'strength-strong';
    }

    getStrengthText(score) {
        if (score <= 1) return 'Weak Password';
        if (score <= 2) return 'Fair Password';
        if (score <= 4) return 'Good Password';
        return 'Strong Password';
    }

    updateRequirements(checks) {
        Object.keys(checks).forEach(key => {
            if (this.requirements[key]) {
                const requirement = this.requirements[key];
                const icon = requirement.querySelector('.requirement-icon');

                if (checks[key]) {
                    requirement.classList.add('met');
                    icon.innerHTML = '✓';
                } else {
                    requirement.classList.remove('met');
                    icon.innerHTML = '✗';
                }
            }
        });
    }

    togglePasswordVisibility() {
        const type = this.passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
        this.passwordInput.setAttribute('type', type);
        this.toggleButton.innerHTML = type === 'password' ? '👁️' : '🙈';
        this.toggleButton.title = type === 'password' ? 'Show password' : 'Hide password';
    }
}


document.addEventListener('DOMContentLoaded', function () {
   
    if (document.getElementById('Password')) {
        new PasswordStrengthIndicator('Password', {
            showRequirements: true,
            showToggle: true
        });
    }

    
    if (document.getElementById('LoginPassword')) {
        const loginPasswordInput = document.getElementById('LoginPassword');
        const wrapper = document.createElement('div');
        wrapper.className = 'password-input-wrapper';
        loginPasswordInput.parentNode.insertBefore(wrapper, loginPasswordInput);
        wrapper.appendChild(loginPasswordInput);

        const toggleButton = document.createElement('button');
        toggleButton.type = 'button';
        toggleButton.className = 'password-toggle';
        toggleButton.innerHTML = '👁️';
        toggleButton.title = 'Toggle password visibility';
        wrapper.appendChild(toggleButton);

        toggleButton.addEventListener('click', function () {
            const type = loginPasswordInput.getAttribute('type') === 'password' ? 'text' : 'password';
            loginPasswordInput.setAttribute('type', type);
            toggleButton.innerHTML = type === 'password' ? '👁️' : '🙈';
            toggleButton.title = type === 'password' ? 'Show password' : 'Hide password';
        });
    }
    document.addEventListener('DOMContentLoaded', function () {
        // Initialize for registration page
        if (document.getElementById('Password')) {
            new PasswordStrengthIndicator('Password', {
                showRequirements: true,
                showToggle: true
            });
        }

        // Initialize for reset password page
        if (document.getElementById('NewPassword')) {
            new PasswordStrengthIndicator('NewPassword', {
                showRequirements: true,
                showToggle: true
            });
        }

        // Initialize for login page (password toggle only, no strength indicator)
        if (document.getElementById('LoginPassword')) {
            const loginPasswordInput = document.getElementById('LoginPassword');
            const wrapper = document.createElement('div');
            wrapper.className = 'password-input-wrapper';
            loginPasswordInput.parentNode.insertBefore(wrapper, loginPasswordInput);
            wrapper.appendChild(loginPasswordInput);

            const toggleButton = document.createElement('button');
            toggleButton.type = 'button';
            toggleButton.className = 'password-toggle';
            toggleButton.innerHTML = '👁️';
            toggleButton.title = 'Toggle password visibility';
            wrapper.appendChild(toggleButton);

            toggleButton.addEventListener('click', function () {
                const type = loginPasswordInput.getAttribute('type') === 'password' ? 'text' : 'password';
                loginPasswordInput.setAttribute('type', type);
                toggleButton.innerHTML = type === 'password' ? '👁️' : '🙈';
                toggleButton.title = type === 'password' ? 'Show password' : 'Hide password';
            });
        }
    });
});