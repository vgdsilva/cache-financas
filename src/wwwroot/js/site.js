(() => {
  "use strict";

  const COLLAPSE_KEY = "nav-collapsed";

  // ---- Navegação: menu que desce (mobile) e sidebar recolhível (desktop) ----
  const shell = document.querySelector("[data-app-shell]");

  if (shell) {
    const toggler = shell.querySelector("[data-nav-toggle]");
    const collapseBtn = shell.querySelector("[data-nav-collapse]");
    const backdrop = shell.querySelector("[data-nav-backdrop]");
    const desktop = window.matchMedia("(min-width: 992px)");

    const setOpen = (open) => {
      shell.classList.toggle("is-open", open);
      toggler?.setAttribute("aria-expanded", String(open));
      toggler?.setAttribute("aria-label", open ? "Fechar menu" : "Abrir menu");
    };

    toggler?.addEventListener("click", () => setOpen(!shell.classList.contains("is-open")));
    backdrop?.addEventListener("click", () => setOpen(false));

    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape" && shell.classList.contains("is-open")) {
        setOpen(false);
        toggler?.focus();
      }
    });

    // Ao passar para o layout desktop o menu deixa de ser "aberto".
    desktop.addEventListener("change", () => setOpen(false));

    const syncCollapseButton = () => {
      const collapsed = document.documentElement.classList.contains("nav-collapsed");
      collapseBtn?.setAttribute("aria-expanded", String(!collapsed));
      collapseBtn?.setAttribute("aria-label", collapsed ? "Expandir menu" : "Recolher menu");
    };

    collapseBtn?.addEventListener("click", () => {
      const collapsed = document.documentElement.classList.toggle("nav-collapsed");
      try {
        localStorage.setItem(COLLAPSE_KEY, collapsed ? "1" : "0");
      } catch {
        // Sem armazenamento (modo privado): o estado só vale nesta página.
      }
      syncCollapseButton();
    });

    syncCollapseButton();
  }

  // ---- Login: mostrar/ocultar senha e estado de carregando ----
  document.querySelectorAll("[data-password-toggle]").forEach((button) => {
    const input = button.closest(".input-group")?.querySelector("input");
    if (!input) return;

    button.hidden = false; // sem JS o botão fica oculto e a senha segue mascarada
    button.addEventListener("click", () => {
      const show = input.type === "password";
      input.type = show ? "text" : "password";
      button.textContent = show ? "Ocultar" : "Mostrar";
      button.setAttribute("aria-label", show ? "Ocultar senha" : "Mostrar senha");
    });
  });

  const loginForm = document.querySelector("[data-login-form]");

  if (loginForm) {
    const submit = loginForm.querySelector("[data-submit]");

    loginForm.addEventListener("submit", () => {
      submit?.classList.add("is-loading");
      submit?.setAttribute("aria-busy", "true");
      // Desabilita depois do envio iniciar, para não cancelar o submit.
      setTimeout(() => { if (submit) submit.disabled = true; }, 0);
    });

    // Voltar pelo histórico (bfcache) não deve manter o botão travado.
    window.addEventListener("pageshow", (event) => {
      if (event.persisted && submit) {
        submit.disabled = false;
        submit.classList.remove("is-loading");
        submit.removeAttribute("aria-busy");
      }
    });
  }
})();
