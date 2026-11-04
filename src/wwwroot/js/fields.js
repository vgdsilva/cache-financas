// Campos do padrão de formulário (modelo antigo do CRM), em JS puro: dinheiro com máscara R$, data dd/mm/aaaa com calendário e
// select com busca. Tudo é aprimoramento: sem JS o campo nativo segue funcionando e enviando o mesmo valor.
// Marcação de cada tipo: ver "Campos de formulário" no CLAUDE.md.
(() => {
  "use strict";

  const fold = (text) => text.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase().trim();

  // ---- Dinheiro: <input type="text" data-money name="X" value="1234.56"> ----
  // O campo mostra "R$ 1.234,56" (digita-se da direita para a esquerda, como no autoNumeric do CRM); o valor enviado é o
  // decimal com ponto ("1234.56", cultura invariável), num <input type="hidden"> que herda o name.
  const money = new Intl.NumberFormat("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

  document.querySelectorAll("input[data-money]").forEach((text) => {
    const hidden = document.createElement("input");
    hidden.type = "hidden";
    hidden.name = text.name;
    text.removeAttribute("name");
    text.inputMode = "numeric";
    text.autocomplete = "off";
    text.after(hidden);

    const show = (cents) => {
      text.value = cents === null ? "" : `R$ ${money.format(cents / 100)}`;
      hidden.value = cents === null ? "" : (cents / 100).toFixed(2);
    };

    const initial = Number.parseFloat(text.value.replace(",", "."));
    show(Number.isFinite(initial) ? Math.round(initial * 100) : null);

    text.addEventListener("input", () => {
      const digits = text.value.replace(/\D/g, "").replace(/^0+/, "").slice(0, 14);
      show(digits ? Number(digits) : null);
      text.setSelectionRange(text.value.length, text.value.length);
    });
  });

  // ---- Data: <input type="date" data-date name="X" value="2026-11-04"> ----
  // Vira um campo de texto com máscara dd/mm/aaaa e ícone de calendário; o <input type="date"> original fica escondido,
  // continua no formulário (valor aaaa-mm-dd) e é ele que abre o calendário do aparelho.
  const toText = (iso) => {
    const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso);
    return m ? `${m[3]}/${m[2]}/${m[1]}` : "";
  };

  const toIso = (text) => {
    const m = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(text);
    if (!m) return "";
    const [, d, mo, y] = m;
    const date = new Date(Number(y), Number(mo) - 1, Number(d));
    const real = date.getFullYear() === Number(y) && date.getMonth() === Number(mo) - 1 && date.getDate() === Number(d);
    return real ? `${y}-${mo}-${d}` : "";
  };

  const mask = (digits) => [digits.slice(0, 2), digits.slice(2, 4), digits.slice(4, 8)].filter(Boolean).join("/");

  document.querySelectorAll("input[type=date][data-date]").forEach((native) => {
    const wrap = document.createElement("div");
    wrap.className = "input-type";

    const text = document.createElement("input");
    text.type = "text";
    text.inputMode = "numeric";
    text.autocomplete = "off";
    text.maxLength = 10;
    text.placeholder = "dd/mm/aaaa";
    text.className = native.className;
    text.disabled = native.disabled;
    text.readOnly = native.readOnly;
    text.value = toText(native.value);
    if (native.id) {
      text.id = `${native.id}-text`;
      document.querySelectorAll(`label[for="${native.id}"]`).forEach((label) => label.setAttribute("for", text.id));
    }

    const button = document.createElement("button");
    button.type = "button";
    button.className = "input-type-btn";
    button.tabIndex = -1;
    button.disabled = native.disabled || native.readOnly;
    button.setAttribute("aria-label", "Abrir calendário");
    button.innerHTML = '<i class="ico fa-calendar-alt" aria-hidden="true"></i>';

    native.before(wrap);
    wrap.append(button, text, native);
    native.classList.add("date-native");
    native.tabIndex = -1;
    native.setAttribute("aria-hidden", "true");

    text.addEventListener("input", () => {
      text.value = mask(text.value.replace(/\D/g, ""));
      native.value = toIso(text.value);
      text.removeAttribute("aria-invalid");
    });

    // Data incompleta ou inexistente: marca o campo e não envia valor (a regra de verdade continua no servidor).
    text.addEventListener("blur", () => {
      if (text.value && !native.value) text.setAttribute("aria-invalid", "true");
    });

    native.addEventListener("change", () => {
      text.value = toText(native.value);
      text.removeAttribute("aria-invalid");
    });

    button.addEventListener("click", () => {
      if (typeof native.showPicker === "function") {
        try { native.showPicker(); } catch { text.focus(); }
      } else {
        text.focus();
      }
    });
  });

  // ---- Select com busca: <select data-search> ----
  // O <select> continua no formulário (escondido); no lugar aparece um botão que abre uma lista filtrável por texto
  // (sem acento e sem diferença de caixa), com setas, Enter e Esc.
  document.querySelectorAll("select[data-search]").forEach((select) => {
    const box = document.createElement("div");
    box.className = "select-box";

    const trigger = document.createElement("button");
    trigger.type = "button";
    trigger.className = "select-trigger";
    trigger.setAttribute("aria-haspopup", "listbox");
    trigger.setAttribute("aria-expanded", "false");
    trigger.disabled = select.disabled;
    trigger.innerHTML = '<span></span><i class="ico fa-chevron-down" aria-hidden="true"></i>';
    const current = trigger.firstElementChild;

    const panel = document.createElement("div");
    panel.className = "select-panel";
    panel.hidden = true;

    const search = document.createElement("input");
    search.type = "search";
    search.className = "select-search";
    search.placeholder = "Pesquisar";
    search.autocomplete = "off";
    search.setAttribute("aria-label", "Pesquisar");

    const list = document.createElement("ul");
    list.className = "select-options";
    list.setAttribute("role", "listbox");
    list.id = `${select.id || `sel-${Math.random().toString(36).slice(2, 8)}`}-list`;
    trigger.setAttribute("aria-controls", list.id);

    panel.append(search, list);
    select.before(box);
    box.append(trigger, panel, select);
    select.tabIndex = -1;
    select.setAttribute("aria-hidden", "true");

    // Label do campo leva o foco para o botão (o <select> original não é mais focável).
    if (select.id) {
      document.querySelectorAll(`label[for="${select.id}"]`).forEach((label) => {
        label.addEventListener("click", (event) => { event.preventDefault(); trigger.focus(); });
      });
    }

    let items = [];
    let active = -1;

    const syncTrigger = () => {
      const option = select.selectedOptions[0];
      const placeholder = !option || option.value === "";
      current.textContent = option ? option.textContent : "";
      current.classList.toggle("is-placeholder", placeholder);
    };

    const setActive = (index) => {
      active = index;
      items.forEach((li, i) => li.classList.toggle("is-active", i === index));
      items[index]?.scrollIntoView({ block: "nearest" });
    };

    const render = () => {
      const term = fold(search.value);
      list.replaceChildren();
      items = [];

      [...select.options].forEach((option) => {
        if (term && !fold(option.textContent).includes(term)) return;
        const li = document.createElement("li");
        li.setAttribute("role", "option");
        li.textContent = option.textContent;
        li.dataset.value = option.value;
        li.setAttribute("aria-selected", String(option.selected));
        li.addEventListener("mousedown", (event) => { event.preventDefault(); choose(li); });
        items.push(li);
        list.append(li);
      });

      if (items.length === 0) {
        const empty = document.createElement("li");
        empty.className = "is-empty";
        empty.textContent = "Nenhum resultado";
        list.append(empty);
      }

      setActive(items.length ? Math.max(0, items.findIndex((li) => li.getAttribute("aria-selected") === "true")) : -1);
    };

    const open = () => {
      if (trigger.disabled) return;
      search.value = "";
      render();
      panel.hidden = false;
      box.classList.add("is-open");
      trigger.setAttribute("aria-expanded", "true");
      search.focus();
    };

    const close = (focusTrigger = true) => {
      panel.hidden = true;
      box.classList.remove("is-open");
      trigger.setAttribute("aria-expanded", "false");
      if (focusTrigger) trigger.focus();
    };

    function choose(li) {
      select.value = li.dataset.value;
      select.dispatchEvent(new Event("change", { bubbles: true }));
      syncTrigger();
      close();
    }

    trigger.addEventListener("click", () => (panel.hidden ? open() : close()));
    trigger.addEventListener("keydown", (event) => {
      if (event.key === "ArrowDown" || event.key === "ArrowUp") { event.preventDefault(); open(); }
    });

    search.addEventListener("input", render);
    search.addEventListener("keydown", (event) => {
      if (event.key === "ArrowDown") { event.preventDefault(); setActive(Math.min(active + 1, items.length - 1)); }
      else if (event.key === "ArrowUp") { event.preventDefault(); setActive(Math.max(active - 1, 0)); }
      else if (event.key === "Enter") { event.preventDefault(); if (items[active]) choose(items[active]); }
      else if (event.key === "Escape") { event.preventDefault(); close(); }
      else if (event.key === "Tab") close(false);
    });

    document.addEventListener("mousedown", (event) => { if (!box.contains(event.target) && !panel.hidden) close(false); });
    select.addEventListener("change", syncTrigger);
    syncTrigger();
  });
})();
