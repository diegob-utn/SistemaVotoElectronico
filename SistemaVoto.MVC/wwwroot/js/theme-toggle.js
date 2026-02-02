/**
 * Theme Toggle - Sistema Voto Electrónico
 * Maneja el cambio entre tema Ayu Dark y Light
 */

(function () {
  'use strict';

  const THEME_KEY = 'sistemavoto-theme';
  const DARK = 'dark';
  const LIGHT = 'light';

  /**
   * Obtiene el tema preferido del usuario
   */
  function getPreferredTheme() {
    // Primero revisar localStorage
    const stored = localStorage.getItem(THEME_KEY);
    if (stored) {
      return stored;
    }

    // Si no hay tema guardado, usar preferencia del sistema
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
      return DARK;
    }

    return LIGHT;
  }

  /**
   * Aplica el tema al documento
   */
  function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem(THEME_KEY, theme);

    // Actualizar iconos del toggle
    updateToggleIcons(theme);

    // Disparar evento personalizado para que otros scripts puedan reaccionar
    window.dispatchEvent(new CustomEvent('themechange', { detail: { theme } }));
  }

  /**
   * Actualiza los iconos del botón toggle
   */
  function updateToggleIcons(theme) {
    const toggleBtns = document.querySelectorAll('.theme-toggle');
    toggleBtns.forEach(btn => {
      const sunIcon = btn.querySelector('.icon-sun');
      const moonIcon = btn.querySelector('.icon-moon');

      if (sunIcon && moonIcon) {
        if (theme === DARK) {
          sunIcon.style.display = 'block';
          moonIcon.style.display = 'none';
        } else {
          sunIcon.style.display = 'none';
          moonIcon.style.display = 'block';
        }
      }
    });
  }

  /**
   * Alterna entre temas
   */
  function toggleTheme() {
    const current = document.documentElement.getAttribute('data-theme') || LIGHT;
    const next = current === DARK ? LIGHT : DARK;
    applyTheme(next);
  }

  /**
   * Inicializa el sistema de temas
   */
  function init() {
    // Aplicar tema inicial inmediatamente
    const initialTheme = getPreferredTheme();
    applyTheme(initialTheme);

    // Configurar listeners para botones de toggle
    document.addEventListener('click', function (e) {
      const toggle = e.target.closest('.theme-toggle');
      if (toggle) {
        toggleTheme();
      }
    });

    // Escuchar cambios en preferencia del sistema
    if (window.matchMedia) {
      window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
        // Solo cambiar automáticamente si no hay preferencia guardada
        if (!localStorage.getItem(THEME_KEY)) {
          applyTheme(e.matches ? DARK : LIGHT);
        }
      });
    }
  }

  // Inicializar cuando el DOM esté listo
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  // Exponer API global
  window.themeToggle = {
    toggle: toggleTheme,
    set: applyTheme,
    get: function () {
      return document.documentElement.getAttribute('data-theme') || LIGHT;
    }
  };
})();
