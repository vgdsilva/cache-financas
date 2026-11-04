(() => {
  "use strict";

  const COLLAPSE_KEY = "nav-collapsed";

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

    // Linha selecionada: os botões da barra do topo ([data-grid-actions] > [data-row-action="nomeDoDataset"]) agem sobre ela.
    // O endereço vem do data-* da linha; sem endereço (ex.: o próprio usuário não se remove) o botão fica desabilitado.
    const actions = [...document.querySelectorAll("[data-grid-actions] [data-row-action]")];
    if (actions.length === 0) return;

    const selected = () => rows.find((row) => row.getAttribute("aria-selected") === "true");

    const syncActions = () => {
      const row = selected();
      actions.forEach((button) => {
        const url = row?.dataset[button.dataset.rowAction];
        if (url) button.setAttribute("href", url); else button.removeAttribute("href");
        button.setAttribute("aria-disabled", String(!url));
      });
    };

    const select = (row) => {
      rows.forEach((r) => r.removeAttribute("aria-selected"));
      row?.setAttribute("aria-selected", "true");
      syncActions();
    };

    actions.forEach((button) => {
      button.hidden = false;
      button.addEventListener("click", (event) => {
        if (button.getAttribute("aria-disabled") === "true") event.preventDefault();
      });
    });

    rows.forEach((row) => {
      row.addEventListener("click", (event) => {
        if (!event.target.closest("a")) select(row);
      });
      row.addEventListener("dblclick", () => { if (row.dataset.editUrl) window.location.href = row.dataset.editUrl; });
      row.addEventListener("keydown", (event) => {
        if (event.target !== row) return;
        if (event.key === " ") { event.preventDefault(); select(row); }
        if (event.key === "Enter" && row.dataset.editUrl) window.location.href = row.dataset.editUrl;
      });
    });

    // Filtrar ou trocar de página pode esconder a linha selecionada: a seleção some junto.
    const hideSelected = () => { if (selected()?.hidden) select(null); };
    grid.addEventListener("input", hideSelected);
    pager?.addEventListener("click", hideSelected);

    syncActions();
  });

  // ---- Abas: sem JS as seções aparecem todas; com JS a barra de abas mostra uma por vez (abre na primeira que tem erro) ----
  document.querySelectorAll("[data-tabs]").forEach((bar) => {
    const tabs = [...bar.querySelectorAll("[role=tab]")];
    const panelOf = (tab) => document.getElementById(tab.getAttribute("aria-controls"));

    const show = (target) => {
      tabs.forEach((tab) => {
        const active = tab === target;
        tab.setAttribute("aria-selected", String(active));
        tab.tabIndex = active ? 0 : -1;
        const panel = panelOf(tab);
        if (panel) panel.hidden = !active;
      });
    };

    tabs.forEach((tab, i) => {
      tab.addEventListener("click", () => show(tab));
      tab.addEventListener("keydown", (event) => {
        const next = { ArrowRight: i + 1, ArrowLeft: i - 1, Home: 0, End: tabs.length - 1 }[event.key];
        if (next === undefined) return;
        event.preventDefault();
        const target = tabs[(next + tabs.length) % tabs.length];
        show(target);
        target.focus();
      });
    });

    bar.hidden = false;
    show(tabs.find((tab) => panelOf(tab)?.querySelector(".input-validation-error")) ?? tabs[0]);
  });

  // ---- Login: olho de mostrar/ocultar a senha e tela de carregando ----
  document.querySelectorAll("[data-password-toggle]").forEach((button) => {
    const input = button.closest(".auth-input")?.querySelector("input");
    const icon = button.querySelector(".ico");
    if (!input) return;

    button.hidden = false; // sem JS o botão fica oculto e a senha segue mascarada
    button.addEventListener("click", () => {
      const show = input.type === "password";
      input.type = show ? "text" : "password";
      icon?.classList.toggle("fa-eye", show);
      icon?.classList.toggle("fa-eye-slash", !show);
      button.setAttribute("aria-label", show ? "Ocultar senha" : "Mostrar senha");
    });
  });

  const loginForm = document.querySelector("[data-login-form]");

  if (loginForm) {
    const loading = document.querySelector("[data-loading]");

    // A tela de carregando cobre a página inteira, então também impede um segundo envio.
    loginForm.addEventListener("submit", () => { if (loading) loading.hidden = false; });

    // Voltar pelo histórico (bfcache) não deve manter a tela de carregando.
    window.addEventListener("pageshow", (event) => {
      if (event.persisted && loading) loading.hidden = true;
    });
  }
})();
