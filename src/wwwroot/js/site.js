(() => {
  "use strict";

  const COLLAPSE_KEY = "nav-collapsed";

  // ---- Tema: "light", "dark" ou "system" (sem atributo: segue o aparelho). O <head> já aplicou o salvo. ----
  const THEME_KEY = "theme";

  const currentTheme = () => {
    const attr = document.documentElement.getAttribute("data-theme");
    return attr === "light" || attr === "dark" ? attr : "system";
  };

  const applyTheme = (theme) => {
    if (theme === "light" || theme === "dark") {
      document.documentElement.setAttribute("data-theme", theme);
    } else {
      document.documentElement.removeAttribute("data-theme");
    }

    try {
      if (theme === "system") {
        localStorage.removeItem(THEME_KEY);
      } else {
        localStorage.setItem(THEME_KEY, theme);
      }
    } catch {
      // Sem armazenamento (modo privado): a escolha só vale nesta página.
    }

    syncThemeButtons();
  };

  function syncThemeButtons() {
    const theme = currentTheme();
    document.querySelectorAll("[data-theme-option]").forEach((button) => {
      button.setAttribute("aria-pressed", String(button.dataset.themeOption === theme));
    });
  }

  document.querySelectorAll("[data-theme-option]").forEach((button) => {
    button.addEventListener("click", () => applyTheme(button.dataset.themeOption));
  });

  syncThemeButtons();

  // ---- Shell: gaveta (mobile), sidebar recolhível (desktop) e menu do usuário ----
  const shell = document.querySelector("[data-app-shell]");

  if (shell) {
    const toggler = shell.querySelector("[data-nav-toggle]");
    const closeBtn = shell.querySelector("[data-nav-close]");
    const collapseBtn = shell.querySelector("[data-nav-collapse]");
    const backdrop = shell.querySelector("[data-nav-backdrop]");
    const userMenu = shell.querySelector("[data-user-menu]");
    const desktop = window.matchMedia("(min-width: 768px)");

    const setOpen = (open) => {
      shell.classList.toggle("is-open", open);
      toggler?.setAttribute("aria-expanded", String(open));
      toggler?.setAttribute("aria-label", open ? "Fechar menu" : "Abrir menu");
    };

    toggler?.addEventListener("click", () => setOpen(!shell.classList.contains("is-open")));
    closeBtn?.addEventListener("click", () => {
      setOpen(false);
      toggler?.focus();
    });
    backdrop?.addEventListener("click", () => setOpen(false));

    // Ao passar para o layout desktop a gaveta deixa de existir.
    desktop.addEventListener("change", () => setOpen(false));

    const syncCollapseButton = () => {
      const collapsed = document.documentElement.classList.contains("nav-collapsed");
      collapseBtn?.setAttribute("aria-expanded", String(!collapsed));
      collapseBtn?.setAttribute("aria-label", collapsed ? "Expandir menu" : "Recolher menu");
      collapseBtn?.setAttribute("title", collapsed ? "Expandir menu" : "Recolher menu");
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

    // Menu do usuário (dropdown no header).
    if (userMenu) {
      const userToggle = userMenu.querySelector("[data-user-toggle]");
      const dropdown = userMenu.querySelector(".user-dropdown");

      const setUserMenu = (open) => {
        if (!dropdown) return;
        dropdown.hidden = !open;
        userToggle?.setAttribute("aria-expanded", String(open));
      };

      userToggle?.addEventListener("click", () => setUserMenu(dropdown?.hidden ?? false));

      document.addEventListener("click", (event) => {
        if (!userMenu.contains(event.target)) setUserMenu(false);
      });

      document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && dropdown && !dropdown.hidden) {
          setUserMenu(false);
          userToggle?.focus();
        }
      });
    }

    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape" && shell.classList.contains("is-open")) {
        setOpen(false);
        toggler?.focus();
      }
    });
  }

  // ---- Cadastro de usuário: administrador acessa todos os menus, então a lista de menus fica desabilitada ----
  document.querySelectorAll("[data-admin-toggle]").forEach((checkbox) => {
    const menus = document.querySelector("[data-menus]");
    if (!menus) return;

    const sync = () => { menus.disabled = checkbox.checked; };
    checkbox.addEventListener("change", sync);
    sync();
  });

  // ---- Menus de Acesso: marcar/limpar tudo, marcar/desmarcar por grupo e contagem (aprimoramento; sem JS os checkboxes seguem funcionando) ----
  document.querySelectorAll("[data-menus]").forEach((block) => {
    const items = () => [...block.querySelectorAll("[data-menu-item]")];
    const groups = [...block.querySelectorAll("[data-menus-group]")];
    const count = block.querySelector("[data-menus-count]");
    const allBtn = block.querySelector("[data-menus-all]");
    const noneBtn = block.querySelector("[data-menus-none]");

    const refresh = () => {
      const all = items();
      const marked = all.filter((i) => i.checked).length;
      if (count) count.textContent = `(${marked}/${all.length})`;

      groups.forEach((group) => {
        const boxes = [...group.querySelectorAll("[data-menu-item]")];
        const toggle = group.querySelector("[data-group-toggle]");
        if (toggle) toggle.textContent = boxes.length > 0 && boxes.every((b) => b.checked) ? "Desmarcar" : "Marcar";
      });
    };

    const setAll = (boxes, value) => {
      boxes.forEach((b) => { b.checked = value; });
      refresh();
    };

    [allBtn, noneBtn].forEach((b) => { if (b) b.hidden = false; });
    allBtn?.addEventListener("click", () => setAll(items(), true));
    noneBtn?.addEventListener("click", () => setAll(items(), false));

    groups.forEach((group) => {
      const toggle = group.querySelector("[data-group-toggle]");
      if (!toggle) return;

      toggle.hidden = false;
      toggle.addEventListener("click", () => {
        const boxes = [...group.querySelectorAll("[data-menu-item]")];
        setAll(boxes, !boxes.every((b) => b.checked));
      });
    });

    block.addEventListener("change", (event) => {
      if (event.target instanceof HTMLInputElement && event.target.matches("[data-menu-item]")) refresh();
    });

    refresh();
  });

  // ---- Tabela (Data Grid): filtros por coluna e paginação no navegador (aprimoramento; sem JS a lista aparece inteira) ----
  // Marcação: [data-grid] > tbody [data-grid-row], inputs [data-grid-filter="índice da coluna"], [data-grid-empty],
  // [data-grid-summary] e [data-grid-pager]. O tamanho da página vem de data-page-size.
  document.querySelectorAll("[data-grid]").forEach((grid) => {
    const rows = [...grid.querySelectorAll("tbody [data-grid-row]")];
    const filters = [...grid.querySelectorAll("[data-grid-filter]")];
    const empty = grid.querySelector("[data-grid-empty]");
    const summary = grid.querySelector("[data-grid-summary]");
    const pager = grid.querySelector("[data-grid-pager]");
    const pageSize = Number(grid.dataset.pageSize) || 10;
    let page = 1;

    // Sem acento e sem diferença de caixa: "joao" acha "João".
    const fold = (text) => text.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase().trim();

    const pageButton = (label, target, { current = false, disabled = false, name = label } = {}) => {
      const button = document.createElement("button");
      button.type = "button";
      button.textContent = label;
      button.disabled = disabled;
      button.setAttribute("aria-label", name);
      if (current) button.setAttribute("aria-current", "page");
      button.addEventListener("click", () => { page = target; render(); });
      return button;
    };

    const renderPager = (pages) => {
      if (!pager) return;

      // Primeira, última e a vizinhança da atual; o que sobra entre elas vira reticências.
      const numbers = [...new Set([1, page - 1, page, page + 1, pages])].filter((n) => n >= 1 && n <= pages).sort((a, b) => a - b);
      const items = [pageButton("‹", page - 1, { disabled: page === 1, name: "Página anterior" })];

      numbers.forEach((n, i) => {
        if (i > 0 && n - numbers[i - 1] > 1) {
          const gap = document.createElement("span");
          gap.textContent = "…";
          gap.setAttribute("aria-hidden", "true");
          items.push(gap);
        }
        items.push(pageButton(String(n), n, { current: n === page, name: `Página ${n}` }));
      });

      items.push(pageButton("›", page + 1, { disabled: page === pages, name: "Próxima página" }));
      pager.replaceChildren(...items);
      pager.hidden = false;
    };

    const render = () => {
      const terms = filters
        .map((input) => ({ column: Number(input.dataset.gridFilter), value: fold(input.value) }))
        .filter((term) => term.value);
      const matches = rows.filter((row) => terms.every((term) => fold(row.cells[term.column]?.textContent ?? "").includes(term.value)));
      const pages = Math.max(1, Math.ceil(matches.length / pageSize));
      page = Math.min(page, pages);

      const start = (page - 1) * pageSize;
      const visible = new Set(matches.slice(start, start + pageSize));
      rows.forEach((row) => { row.hidden = !visible.has(row); });

      if (empty) empty.hidden = matches.length > 0;
      if (summary) {
        summary.textContent = matches.length === 0
          ? "Nenhum registro encontrado"
          : `Exibindo ${start + 1}–${start + visible.size} de ${matches.length} registros`;
      }

      renderPager(pages);
    };

    filters.forEach((input) => input.addEventListener("input", () => { page = 1; render(); }));
    render();
  });

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
