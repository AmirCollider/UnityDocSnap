// ==========================================
// Unity DocSnap — Site Behaviour
//
// Everything here exists to make one very large,
// entirely static document navigable: language and
// detail-level switching, a client-side search over
// the embedded index, in-page filtering of the
// hierarchy / asset trees, and deep-link reveal so a
// link into a collapsed section actually arrives.
//
// No network calls, ever. The only persisted state is
// the reader's language, theme, detail mode and
// sidebar state, through a storage helper that falls
// back to memory when localStorage is blocked (some
// browsers deny it under a file:// origin).
// ==========================================

(function () {
  'use strict';

  var RTL_LANGS = { fa: true };
  var LANG_KEY = 'unityDocSnapLang';
  var MODE_KEY = 'unityDocSnapMode';
  var THEME_KEY = 'unityDocSnapTheme';
  var SIDEBAR_KEY = 'unityDocSnapSidebar';
  var SKIN_KEY = 'unityDocSnapSkin';
  var DEFAULTS_KEY = 'unityDocSnapDefaults';

  // ==========================================
  // safeStorage
  // localStorage wrapped so it can never throw, with an
  // in-memory fallback for origins (file:// in some
  // browsers, private modes) that deny it. Persistence
  // across pages still needs a working localStorage; the
  // fallback simply guarantees the page keeps working.
  // ==========================================
  var memoryStore = {};
  var safeStorage = {
    get: function (key) {
      try {
        var v = window.localStorage.getItem(key);
        if (v !== null && v !== undefined) { return v; }
      } catch (e) { /* denied — fall through to memory */ }
      return Object.prototype.hasOwnProperty.call(memoryStore, key) ? memoryStore[key] : null;
    },
    set: function (key, value) {
      memoryStore[key] = value;
      try { window.localStorage.setItem(key, value); } catch (e) { /* denied — memory only */ }
    }
  };

  function each(selector, fn, root) {
    var nodes = (root || document).querySelectorAll(selector);
    for (var i = 0; i < nodes.length; i++) { fn(nodes[i], i); }
  }

  // ==========================================
  // syncExportDefaults()
  // A reader's saved language/theme should survive
  // reloads of the SAME export, but any NEW export must
  // open with the defaults the exporter just chose.
  // Every run bakes a unique stamp into its pages; when a
  // page carries a stamp that differs from the recorded
  // one — a fresh export, even one whose defaults are
  // unchanged — the stored choices reset to that export's
  // defaults.
  // ==========================================
  function syncExportDefaults() {
    var lang = window.__DOCSNAP_LANG__ || 'en';
    var theme = window.__DOCSNAP_THEME__ || 'light';
    var current = (window.__DOCSNAP_EXPORT__ || '') + '|' + lang + '|' + theme;
    if (safeStorage.get(DEFAULTS_KEY) !== current) {
      safeStorage.set(LANG_KEY, lang);
      safeStorage.set(THEME_KEY, theme);
      // The skin verdict is measured per export, so a new export's
      // measurement must win over a choice made while reading an
      // older one - the project may have doubled in size since.
      safeStorage.set(SKIN_KEY, '');
      safeStorage.set(DEFAULTS_KEY, current);
    }
  }

  // ==========================================
  // Language
  // ==========================================
  function applyLanguage(lang) {
    var root = document.documentElement;
    root.setAttribute('lang', lang);
    root.setAttribute('dir', RTL_LANGS[lang] ? 'rtl' : 'ltr');

    each('[data-en]', function (el) {
      var text = el.getAttribute('data-' + lang) || el.getAttribute('data-en');
      if (text !== null) { el.textContent = text; }
    });

    each('[data-ph-en]', function (el) {
      var ph = el.getAttribute('data-ph-' + lang) || el.getAttribute('data-ph-en');
      if (ph !== null) { el.setAttribute('placeholder', ph); }
    });

    each('.ds-lang-btn', function (btn) {
      var isActive = btn.getAttribute('data-lang') === lang;
      btn.classList.toggle('is-active', isActive);
      btn.setAttribute('aria-pressed', isActive ? 'true' : 'false');
    });

    // The <title> element cannot carry data-en/ja/fa attributes for the
    // sweep above, so the browser tab kept whatever language the export
    // was made in. Rebuilt from the page heading, which does carry them
    // on the pages whose title is a UI label - and is the Scene / folder
    // name, correctly untranslated, on the pages where it is data.
    var heading = document.querySelector('.ds-page-header h1');
    if (heading) {
      var headingText = heading.textContent.replace(/\s+/g, ' ').trim();
      if (headingText) { document.title = headingText + ' - Unity DocSnap'; }
    }

    safeStorage.set(LANG_KEY, lang);

    // The skin warning is built in JS rather than markup, so it has
    // no data-en/ja/fa attributes for the sweep above to swap.
    var warning = document.querySelector('[data-skin-warning]');
    if (warning && !warning.hidden) {
      updateSkinWarning(root.getAttribute('data-skin') === 'cozy');
    }

    // The <head> boot script hides the body while a language
    // swap is pending; the swap just happened, so reveal it.
    root.classList.remove('ds-lang-pending');
  }

  function restoreLanguage() {
    // A reader's own saved choice wins; otherwise fall back to
    // the default the exporter baked in. The stored value is
    // validated against the real buttons — never interpolated
    // into a selector, which could throw on a corrupt value and
    // break every wire-up below.
    var stored = safeStorage.get(LANG_KEY);
    var lang = stored || window.__DOCSNAP_LANG__ || 'en';
    var valid = false;
    each('.ds-lang-btn', function (btn) {
      if (btn.getAttribute('data-lang') === lang) { valid = true; }
    });
    applyLanguage(valid ? lang : (window.__DOCSNAP_LANG__ || 'en'));
  }

  function wireLanguageButtons() {
    each('.ds-lang-btn', function (btn) {
      btn.addEventListener('click', function (evt) {
        applyLanguage(evt.currentTarget.getAttribute('data-lang'));
      });
    });
  }

  function currentLang() {
    return document.documentElement.getAttribute('lang') || 'en';
  }

  function t(en, ja, fa) {
    var lang = currentLang();
    if (lang === 'ja') { return ja; }
    if (lang === 'fa') { return fa; }
    return en;
  }

  // ==========================================
  // Theme
  // ==========================================
  function applyTheme(theme) {
    var dark = theme === 'dark';
    document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
    each('.ds-theme-icon', function (el) { el.textContent = dark ? '☀' : '☾'; });
    each('[data-theme-toggle]', function (el) { el.setAttribute('aria-pressed', dark ? 'true' : 'false'); });
    safeStorage.set(THEME_KEY, dark ? 'dark' : 'light');
  }

  function restoreTheme() {
    var stored = safeStorage.get(THEME_KEY);
    applyTheme(stored || window.__DOCSNAP_THEME__ || document.documentElement.getAttribute('data-theme') || 'light');
  }

  function wireThemeToggle() {
    each('[data-theme-toggle]', function (btn) {
      btn.addEventListener('click', function () {
        applyTheme(document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark');
      });
    });
  }

  // ==========================================
  // Visual skin — cozy vs lite
  //
  // Cozy is the original look (pastel gradients, soft
  // shadows, a bobbing mascot) and the one people want; it
  // is also strictly more paint work per row. Which skin an
  // export OPENS with was measured at export time from the
  // exporting machine and the project's weight
  // (window.__DOCSNAP_CAPS__), and is re-checked here
  // against the machine actually reading the page - which
  // may be a completely different one.
  //
  // A reader's own choice always wins. When it wins against
  // a lite verdict, the reasons are shown with the numbers
  // attached, because "this might be slow" without them is
  // not a warning.
  // ==========================================
  function capsReport() {
    var caps = window.__DOCSNAP_CAPS__;
    return (caps && typeof caps === 'object') ? caps : {};
  }

  // What the browser can tell us about the machine in front of
  // the page, as opposed to the one that produced it. Both
  // hints are advisory and widely unimplemented, so only a
  // definite low reading counts against the reader.
  function viewerReasons() {
    var reasons = [];
    var mem = navigator.deviceMemory;
    if (typeof mem === 'number' && mem > 0 && mem < 4) {
      reasons.push('This device reports ' + mem + ' GB of RAM');
    }
    var cores = navigator.hardwareConcurrency;
    if (typeof cores === 'number' && cores > 0 && cores < 4) {
      reasons.push('This device reports ' + cores + ' CPU core' + (cores === 1 ? '' : 's'));
    }
    try {
      if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        reasons.push('Your system asks for reduced motion');
      }
    } catch (e) { /* matchMedia unavailable */ }
    return reasons;
  }

  // Every reason the cozy skin was not the default, from both
  // sides of the export.
  function skinReasons() {
    var caps = capsReport();
    var reasons = [];
    if (caps.reasons && caps.reasons.length) {
      for (var i = 0; i < caps.reasons.length; i++) { reasons.push(caps.reasons[i]); }
    }
    var viewer = viewerReasons();
    for (var j = 0; j < viewer.length; j++) { reasons.push(viewer[j]); }
    return reasons;
  }

  function recommendedSkin() {
    var baked = window.__DOCSNAP_SKIN__ === 'cozy' ? 'cozy' : 'lite';
    // The exporter said cozy, but this browser may still be the
    // constrained one. A downgrade here is allowed; an upgrade is
    // not, since the exporter measured things the browser cannot.
    if (baked === 'cozy' && viewerReasons().length > 0) { return 'lite'; }
    return baked;
  }

  function applySkin(skin, remember) {
    var cozy = skin === 'cozy';
    document.documentElement.setAttribute('data-skin', cozy ? 'cozy' : 'lite');
    each('.ds-skin-btn', function (btn) {
      var isActive = (btn.getAttribute('data-skin') === 'cozy') === cozy;
      btn.classList.toggle('is-active', isActive);
      btn.setAttribute('aria-pressed', isActive ? 'true' : 'false');
    });
    if (remember) { safeStorage.set(SKIN_KEY, cozy ? 'cozy' : 'lite'); }
    updateSkinWarning(cozy);
  }

  function updateSkinWarning(cozy) {
    var panel = document.querySelector('[data-skin-warning]');
    if (!panel) { return; }

    var reasons = skinReasons();
    // Only ever shown for the combination that warrants it: the
    // reader has turned cozy on and something measured says lite.
    if (!cozy || reasons.length === 0) { panel.hidden = true; return; }

    var caps = capsReport();
    var html = '<strong>' + esc(t(
      'Cozy skin on a machine we measured as tight',
      '負荷が高めと判定された環境でコージースキンを使用中',
      'اسکین Cozy روی سیستمی که سنگین تشخیص داده شد')) + '</strong>';
    html += '<ul>';
    for (var i = 0; i < reasons.length; i++) { html += '<li>' + esc(reasons[i]) + '</li>'; }
    html += '</ul>';

    var specs = [];
    if (caps.ramMb) { specs.push('RAM ' + caps.ramMb + ' MB'); }
    if (caps.cores) { specs.push(caps.cores + ' cores'); }
    if (caps.gpu) { specs.push(caps.gpu + (caps.gpuMb ? ' / ' + caps.gpuMb + ' MB' : '')); }
    if (caps.gameObjects) { specs.push(caps.gameObjects + ' GameObjects'); }
    if (caps.assetFiles) { specs.push(caps.assetFiles + ' files'); }
    if (specs.length) {
      html += '<div class="ds-skin-specs">' + esc(specs.join('  ·  ')) + '</div>';
    }

    panel.innerHTML = html;
    panel.hidden = false;
  }

  function restoreSkin() {
    var stored = safeStorage.get(SKIN_KEY);
    applySkin(stored === 'cozy' || stored === 'lite' ? stored : recommendedSkin(), false);
  }

  function wireSkinButtons() {
    each('.ds-skin-btn', function (btn) {
      btn.addEventListener('click', function (evt) {
        applySkin(evt.currentTarget.getAttribute('data-skin'), true);
      });
    });
  }

  // ==========================================
  // Detail level (Simple / Advanced)
  // ==========================================
  function applyMode(mode) {
    var simple = mode !== 'advanced';
    document.body.classList.toggle('ds-mode-simple', simple);
    document.body.classList.toggle('ds-mode-advanced', !simple);
    each('.ds-mode-btn', function (btn) {
      var isActive = (btn.getAttribute('data-mode') === 'advanced') === !simple;
      btn.classList.toggle('is-active', isActive);
      btn.setAttribute('aria-pressed', isActive ? 'true' : 'false');
    });
    safeStorage.set(MODE_KEY, simple ? 'simple' : 'advanced');
  }

  function restoreMode() {
    var stored = safeStorage.get(MODE_KEY);
    if (stored) { applyMode(stored); }
  }

  function wireModeButtons() {
    each('.ds-mode-btn', function (btn) {
      btn.addEventListener('click', function (evt) {
        applyMode(evt.currentTarget.getAttribute('data-mode'));
      });
    });
  }

  // ==========================================
  // Sidebar collapse
  // On a laptop the field tables want the width more
  // than the nav does, and the reader is usually deep
  // inside one page rather than hopping between them.
  // ==========================================
  function applySidebar(collapsed) {
    document.body.classList.toggle('ds-sidebar-collapsed', !!collapsed);
    each('[data-sidebar-toggle]', function (el) { el.setAttribute('aria-pressed', collapsed ? 'true' : 'false'); });
    safeStorage.set(SIDEBAR_KEY, collapsed ? 'collapsed' : 'open');
  }

  function wireSidebarToggle() {
    applySidebar(safeStorage.get(SIDEBAR_KEY) === 'collapsed');
    each('[data-sidebar-toggle]', function (btn) {
      btn.addEventListener('click', function () {
        applySidebar(!document.body.classList.contains('ds-sidebar-collapsed'));
      });
    });
  }

  // ==========================================
  // Tree controls
  // Expand touches only the tree nodes themselves
  // (details.ds-go — folders / GameObjects), never the
  // per-item heavy detail (asset cards, Import Settings,
  // Fields, Prefab Contents). Expanding literally
  // everything forces layout of every field table on the
  // page at once, which froze the browser on a big
  // Assets page. Collapse still closes everything, so
  // one click always returns the page to its lightest
  // state.
  // ==========================================
  function wireTreeControls() {
    each('[data-tree-expand]', function (btn) {
      btn.addEventListener('click', function (evt) {
        var scope = document.getElementById(evt.currentTarget.getAttribute('data-tree-expand'));
        if (!scope) { return; }
        var open = evt.currentTarget.getAttribute('data-mode') === 'expand';
        each(open ? 'details.ds-go' : 'details', function (d) { d.open = open; }, scope);
      });
    });
  }

  // ==========================================
  // In-page tree filter
  //
  // The site's search jumps to one record; this is the
  // other half — narrowing a single huge page down to
  // the rows that match, in place, keeping each match's
  // ancestors visible so the path stays readable. On a
  // folder page with 8 000 assets this is the difference
  // between scrolling for a minute and typing four
  // letters.
  //
  // Matching runs over a per-node haystack captured once
  // on the first keystroke, not over live DOM text, so
  // typing stays responsive on very large trees.
  // ==========================================
  function wireTreeFilter() {
    each('[data-tree-filter]', function (input) {
      var scope = document.getElementById(input.getAttribute('data-tree-filter'));
      if (!scope) { return; }

      var nodes = null;
      var timer = null;
      var countEl = document.querySelector('[data-tree-filter-count="' + cssEscape(input.getAttribute('data-tree-filter')) + '"]');

      function collect() {
        if (nodes) { return nodes; }
        nodes = [];
        each('li', function (li) {
          // The label is the summary / leaf row only — never the
          // whole subtree, or every ancestor would match anything
          // any descendant contains.
          var label = li.querySelector(':scope > details > summary, :scope > .ds-go-leaf');
          nodes.push({ el: li, text: (label ? label.textContent : li.textContent).toLowerCase() });
        }, scope);
        return nodes;
      }

      function run() {
        var q = input.value.trim().toLowerCase();
        var list = collect();
        var matches = 0;

        if (!q) {
          scope.classList.remove('is-filtering');
          for (var i = 0; i < list.length; i++) {
            list[i].el.classList.remove('ds-filtered-out');
            list[i].el.classList.remove('ds-filter-hit');
          }
          if (countEl) { countEl.textContent = ''; }
          return;
        }

        // Hide everything, then re-show each match together with
        // its ancestors, so a match five levels down stays reachable.
        // ds-filter-hit marks the matches themselves, so the CSS can
        // keep an ancestor's row visible while collapsing its body.
        scope.classList.add('is-filtering');
        for (var j = 0; j < list.length; j++) {
          list[j].el.classList.add('ds-filtered-out');
          list[j].el.classList.remove('ds-filter-hit');
        }
        for (var k = 0; k < list.length; k++) {
          if (list[k].text.indexOf(q) < 0) { continue; }
          matches++;
          var node = list[k].el;
          node.classList.remove('ds-filtered-out');
          node.classList.add('ds-filter-hit');
          var parent = node.parentElement;
          while (parent && parent !== scope) {
            if (parent.tagName === 'LI') { parent.classList.remove('ds-filtered-out'); }
            if (parent.tagName === 'DETAILS') { parent.open = true; }
            parent = parent.parentElement;
          }
        }
        if (countEl) {
          countEl.textContent = matches + ' ' + t('matches', '件', 'مورد');
        }
      }

      input.addEventListener('input', function () {
        if (timer) { clearTimeout(timer); }
        timer = setTimeout(run, 130);
      });
      input.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { input.value = ''; run(); }
      });
    });
  }

  // A minimal CSS.escape for the one attribute-selector
  // value this file builds. Interpolating an unescaped
  // value into a selector is how a corrupt attribute
  // takes down every wire-up on the page.
  function cssEscape(value) {
    return String(value === null || value === undefined ? '' : value).replace(/["\\]/g, '\\$&');
  }

  // ==========================================
  // Issues page filtering
  // The kind tiles and the text box narrow the same
  // list. Both are pre-applied from the query string so
  // the dashboard can link straight to "just the broken
  // references in MainMenu".
  // ==========================================
  function wireIssueFilters() {
    var list = document.querySelector('[data-issue-list]');
    if (!list) { return; }

    var rows = list.querySelectorAll('.ds-issue-row');
    var search = document.querySelector('[data-issue-search]');
    var empty = document.querySelector('[data-issue-empty]');
    var kind = 'all';

    // 'mine' by default, and that is the whole point of the tab.
    // A project whose eight findings are seven render-pipeline
    // assets from a Unity template and one TextMesh Pro fallback
    // has nothing for its author to do; opening on a list of eight
    // things they can neither edit nor delete is how a health
    // report teaches someone to stop reading it.
    var owner = 'mine';
    var timer = null;

    var params = new URLSearchParams(window.location.search || '');
    var wantedKind = params.get('kind');
    var wantedScope = params.get('scope');
    var wantedOwner = params.get('owner');
    if (wantedKind) { kind = wantedKind; }
    if (wantedOwner) { owner = wantedOwner; }
    if (wantedScope && search) { search.value = wantedScope; }

    function apply() {
      var q = search ? search.value.trim().toLowerCase() : '';
      var shown = 0;
      for (var i = 0; i < rows.length; i++) {
        var row = rows[i];
        var okKind = kind === 'all' || row.getAttribute('data-issue-kind') === kind;
        var okOwner = owner === 'any' || (row.getAttribute('data-issue-owner') || 'mine') === owner;
        var okText = !q || (row.getAttribute('data-issue-text') || '').indexOf(q) >= 0;
        var visible = okKind && okOwner && okText;
        row.classList.toggle('ds-filtered-out', !visible);
        if (visible) { shown++; }
      }
      if (empty) { empty.hidden = shown !== 0; }

      each('[data-issue-filter]', function (tile) {
        var isActive = tile.getAttribute('data-issue-filter') === kind;
        tile.classList.toggle('is-active', isActive);
        tile.setAttribute('aria-pressed', isActive ? 'true' : 'false');
      });
      each('[data-issue-owner]', function (tab) {
        var isActive = tab.getAttribute('data-issue-owner') === owner;
        tab.classList.toggle('is-active', isActive);
        tab.setAttribute('aria-pressed', isActive ? 'true' : 'false');
      });
    }

    each('[data-issue-filter]', function (tile) {
      tile.addEventListener('click', function (evt) {
        var next = evt.currentTarget.getAttribute('data-issue-filter');
        kind = (kind === next && next !== 'all') ? 'all' : next;
        apply();
      });
    });

    each('[data-issue-owner]', function (tab) {
      tab.addEventListener('click', function (evt) {
        owner = evt.currentTarget.getAttribute('data-issue-owner');
        apply();
      });
    });

    if (search) {
      search.addEventListener('input', function () {
        if (timer) { clearTimeout(timer); }
        timer = setTimeout(apply, 120);
      });
      search.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { search.value = ''; apply(); }
      });
    }

    apply();
  }

  // ==========================================
  // revealHashTarget()
  //
  // Cross-page links, search results and every row on
  // the health report point at anchors (#asset-…, #go-…,
  // #folder-…) that live inside collapsed <details> and
  // inside content-visibility:auto containers. A closed
  // <details> is never laid out, so the browser cannot
  // scroll to it; a skipped container may not be measured
  // in time either. This opens every <details> on the
  // path, forces its containers to render, scrolls, and
  // then flashes the target — because a link that lands
  // in the right place without saying so leaves the
  // reader hunting the page anyway.
  // ==========================================
  var lastFlashed = null;

  function revealHashTarget() {
    var hash = window.location.hash;
    if (!hash || hash.length < 2) { return; }
    var id;
    try { id = decodeURIComponent(hash.slice(1)); } catch (e) { id = hash.slice(1); }
    var el = document.getElementById(id);
    if (!el) { return; }

    var node = el;
    while (node && node !== document.body) {
      if (node.tagName === 'DETAILS') { node.open = true; }
      if (node.nodeType === 1) { node.style.contentVisibility = 'visible'; }
      node = node.parentElement;
    }

    // The anchor is usually the <li> WRAPPING the collapsed node
    // rather than the node itself, so opening only the ancestors
    // lands the reader on a row that is still shut. Open the
    // target's own first-level <details> too.
    var own = el.tagName === 'DETAILS' ? el : el.querySelector(':scope > details');
    if (own) { own.open = true; }

    window.setTimeout(function () {
      el.scrollIntoView({ block: 'center' });
      if (lastFlashed) { lastFlashed.classList.remove('ds-target-hit'); }
      // Re-adding the class in the same frame does not restart a
      // CSS animation; a forced reflow between the two does.
      el.classList.remove('ds-target-hit');
      void el.offsetWidth;
      el.classList.add('ds-target-hit');
      lastFlashed = el;
    }, 0);
  }

  // ==========================================
  // Back to top
  // ==========================================
  function wireBackToTop() {
    var btn = document.querySelector('.ds-back-top');
    if (!btn) { return; }
    var toggle = function () { btn.style.display = window.scrollY > 500 ? 'flex' : 'none'; };
    window.addEventListener('scroll', toggle, { passive: true });
    btn.addEventListener('click', function () { window.scrollTo({ top: 0, behavior: 'smooth' }); });
    toggle();
  }

  // ==========================================
  // Copy-to-clipboard
  // Every path in this site is something a reader is
  // about to paste somewhere else — into the Project
  // window's search, a bug report, a prompt.
  // ==========================================
  function wireCopyButtons() {
    document.addEventListener('click', function (evt) {
      var btn = evt.target.closest ? evt.target.closest('[data-copy]') : null;
      if (!btn) { return; }
      evt.preventDefault();
      var text = btn.getAttribute('data-copy') || '';
      var done = function () {
        // innerHTML, not textContent: the label is an i18n <span>
        // carrying data-en/ja/fa, and flattening it to text would
        // leave the button permanently stuck in one language.
        var previous = btn.innerHTML;
        btn.textContent = t('Copied', 'コピーしました', 'کپی شد');
        setTimeout(function () { btn.innerHTML = previous; }, 1200);
      };
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text).then(done, function () { fallbackCopy(text, done); });
      } else {
        fallbackCopy(text, done);
      }
    });
  }

  function fallbackCopy(text, done) {
    // execCommand is deprecated but it is the only thing that
    // works under file:// in browsers that gate the async
    // clipboard API on a secure origin, which is most of them.
    try {
      var ta = document.createElement('textarea');
      ta.value = text;
      ta.setAttribute('readonly', '');
      ta.style.position = 'fixed';
      ta.style.opacity = '0';
      document.body.appendChild(ta);
      ta.select();
      document.execCommand('copy');
      document.body.removeChild(ta);
      done();
    } catch (e) { /* nothing sensible left to try */ }
  }

  // ==========================================
  // Search
  // Filters the embedded index entirely in the browser:
  // no network, works under file://. Substring match on
  // name + context, name matches ranked first, results
  // capped so even a huge project stays instant.
  // Record shape: { c: category, n: name, s: sub, u: url,
  // g: group('scene'|'asset') }.
  // ==========================================
  function esc(s) {
    return String(s === null || s === undefined ? '' : s)
      .split('&').join('&amp;')
      .split('<').join('&lt;')
      .split('>').join('&gt;')
      .split('"').join('&quot;')
      .split(String.fromCharCode(39)).join('&#39;');
  }

  function highlight(text, q) {
    var t2 = String(text === null || text === undefined ? '' : text);
    if (!q) { return esc(t2); }
    var idx = t2.toLowerCase().indexOf(q);
    if (idx < 0) { return esc(t2); }
    return esc(t2.slice(0, idx)) + '<mark>' + esc(t2.slice(idx, idx + q.length)) + '</mark>' + esc(t2.slice(idx + q.length));
  }

  function wireSearch() {
    var input = document.querySelector('.ds-search-input');
    var panel = document.querySelector('.ds-search-results');
    if (!input || !panel) { return; }

    var records = window.__DOCSNAP_SEARCH__ || [];
    var prefix = window.__DOCSNAP_PREFIX__ || '';
    var truncatedIndex = window.__DOCSNAP_SEARCH_TRUNCATED__ === true;
    var filter = 'all';
    var MAX = 60;
    var debounceTimer = null;
    var active = -1;

    each('.ds-search-filter', function (btn) {
      btn.addEventListener('click', function (evt) {
        filter = evt.currentTarget.getAttribute('data-search-filter');
        each('.ds-search-filter', function (other) {
          other.classList.toggle('is-active', other === evt.currentTarget);
        });
        run(input.value);
        input.focus();
      });
    });

    input.addEventListener('input', function () {
      if (debounceTimer) { clearTimeout(debounceTimer); }
      debounceTimer = setTimeout(function () { run(input.value); }, 110);
    });

    // Arrow keys move through results and Enter opens the
    // highlighted one — the search box is the fastest route to
    // any object in the project, and reaching for the mouse
    // halfway through undoes most of that.
    input.addEventListener('keydown', function (evt) {
      var results = panel.querySelectorAll('.ds-search-result');
      if (evt.key === 'ArrowDown' || evt.key === 'ArrowUp') {
        if (results.length === 0) { return; }
        evt.preventDefault();
        active += (evt.key === 'ArrowDown' ? 1 : -1);
        if (active < 0) { active = results.length - 1; }
        if (active >= results.length) { active = 0; }
        markActive(results);
      } else if (evt.key === 'Enter') {
        var target = (active >= 0 && active < results.length) ? results[active] : results[0];
        if (target) { evt.preventDefault(); window.location.href = target.getAttribute('href'); }
      } else if (evt.key === 'Escape') {
        input.value = '';
        hide();
        input.blur();
      }
    });

    document.addEventListener('click', function (evt) {
      if (!evt.target.closest || !evt.target.closest('.ds-search')) { hide(); }
    });

    function markActive(results) {
      for (var i = 0; i < results.length; i++) {
        results[i].classList.toggle('is-active', i === active);
      }
      if (active >= 0 && results[active]) {
        results[active].scrollIntoView({ block: 'nearest' });
      }
    }

    function hide() { panel.hidden = true; panel.innerHTML = ''; active = -1; }

    function run(raw) {
      var q = (raw || '').trim().toLowerCase();
      active = -1;
      if (q.length < 1) { hide(); return; }

      var results = [];
      var matched = 0;
      for (var i = 0; i < records.length; i++) {
        var r = records[i];
        if (filter !== 'all' && r.g !== filter) { continue; }
        var name = (r.n || '').toLowerCase();
        var sub = (r.s || '').toLowerCase();
        var inName = name.indexOf(q) >= 0;
        var inSub = !inName && sub.indexOf(q) >= 0;
        if (!inName && !inSub) { continue; }
        matched++;
        if (results.length < MAX) {
          results.push({ r: r, score: inName ? (name.indexOf(q) === 0 ? 0 : 1) : 2 });
        }
      }
      results.sort(function (a, b) { return a.score - b.score; });
      render(results, q, matched);
    }

    function render(items, q, matched) {
      panel.hidden = false;
      if (items.length === 0) {
        panel.innerHTML = '<div class="ds-search-empty">' + esc(t('No matches', 'ヒットなし', 'موردی پیدا نشد')) + '</div>';
        return;
      }
      var html = '';
      for (var i = 0; i < items.length; i++) {
        var r = items[i].r;
        html += '<a class="ds-search-result" href="' + esc(prefix + (r.u || '')) + '">'
          + '<span class="r-top"><span class="r-name">' + highlight(r.n, q) + '</span>'
          + '<span class="r-cat">' + esc(r.c) + '</span></span>'
          + '<span class="r-sub">' + highlight(r.s, q) + '</span></a>';
      }
      if (matched > items.length) {
        html += '<div class="ds-search-more">+' + (matched - items.length) + ' '
          + esc(t('more', '件', 'مورد دیگر'))
          + (truncatedIndex ? ' ' + esc(t('(index capped)', '(インデックス上限)', '(سقف ایندکس)')) : '') + '</div>';
      }
      panel.innerHTML = html;
    }
  }

  // Keyboard access to the controls people reach for
  // constantly: '/' or Ctrl/Cmd+K focuses search, Escape
  // leaves it, and '[' toggles the sidebar. Typing '/'
  // inside a field must still type a slash, so the
  // handler stands down whenever an editable element
  // already has focus.
  function wireHotkeys() {
    var input = document.querySelector('.ds-search-input');

    document.addEventListener('keydown', function (e) {
      var target = e.target || {};
      var tag = (target.tagName || '').toLowerCase();
      var typing = tag === 'input' || tag === 'textarea' || tag === 'select' || target.isContentEditable;

      if (input && (e.key === 'k' || e.key === 'K') && (e.metaKey || e.ctrlKey)) {
        e.preventDefault();
        input.focus();
        input.select();
        return;
      }
      if (typing || e.metaKey || e.ctrlKey || e.altKey) { return; }

      if (input && e.key === '/') {
        e.preventDefault();
        input.focus();
      } else if (e.key === '[') {
        e.preventDefault();
        applySidebar(!document.body.classList.contains('ds-sidebar-collapsed'));
      }
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    syncExportDefaults();
    restoreLanguage();
    wireLanguageButtons();
    restoreTheme();
    wireThemeToggle();
    restoreSkin();
    wireSkinButtons();
    restoreMode();
    wireModeButtons();
    wireSidebarToggle();
    wireTreeControls();
    wireTreeFilter();
    wireIssueFilters();
    wireBackToTop();
    wireCopyButtons();
    wireSearch();
    wireHotkeys();
    revealHashTarget();
    window.addEventListener('hashchange', revealHashTarget);
  });
})();
