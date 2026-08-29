/**
 * EJLAcademy - Gerenciador de Tema Claro e Escuro
 */
(function () {
    const STORAGE_KEY = 'ejlacademy-theme';

    // Recupera tema preferido ou salvo
    function getPreferredTheme() {
        const savedTheme = localStorage.getItem(STORAGE_KEY);
        if (savedTheme === 'light' || savedTheme === 'dark') {
            return savedTheme;
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    // Aplica o tema no documento
    function setTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(STORAGE_KEY, theme);
        updateToggleButtonUI(theme);
    }

    // Atualiza o texto/ícone do botão na interface
    function updateToggleButtonUI(theme) {
        const labels = document.querySelectorAll('.theme-label-text');
        labels.forEach(el => {
            el.textContent = theme === 'dark' ? 'Modo Claro' : 'Modo Escuro';
        });

        const radios = document.querySelectorAll('input[name="radioEscolhaTema"]');
        radios.forEach(radio => {
            if (radio.value === theme) {
                radio.checked = true;
            }
        });
    }

    // Alterna entre claro e escuro
    window.toggleTheme = function () {
        const currentTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        setTheme(newTheme);
    };

    // Define tema específico (usado em configurações/perfil)
    window.setSpecificTheme = function (theme) {
        if (theme === 'auto') {
            localStorage.removeItem(STORAGE_KEY);
            const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
            setTheme(systemTheme);
        } else {
            setTheme(theme);
        }
    };

    // Inicialização imediata (evita piscar a tela)
    const initialTheme = getPreferredTheme();
    document.documentElement.setAttribute('data-bs-theme', initialTheme);
    document.documentElement.setAttribute('data-theme', initialTheme);

    // Escuta mudanças do sistema operacional se o usuário não tiver salvo preferência fixa
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
        if (!localStorage.getItem(STORAGE_KEY)) {
            setTheme(e.matches ? 'dark' : 'light');
        }
    });

    // Ao carregar o DOM, atualiza textos dos botões
    document.addEventListener('DOMContentLoaded', () => {
        const activeTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
        updateToggleButtonUI(activeTheme);
    });
})();
