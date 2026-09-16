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

  // The language registry, baked into the page by
  // HtmlPageBuilder from DocSnapLanguages. This file used to
  // carry its own copy of it — `var RTL_LANGS = { fa: true }`
  // and a t() that knew exactly three languages — so a
  // language added on the C# side rendered its text correctly
  // and then laid the page out left-to-right anyway, and every
  // string this script builds itself stayed English. Reading
  // the list from the export keeps the two ends from drifting.
  // The fallbacks are for a page rendered by an older exporter.
  var RTL_LANGS = (function () {
    var map = {};
    var codes = window.__DOCSNAP_RTL__ || ['fa'];
    for (var i = 0; i < codes.length; i++) { map[codes[i]] = true; }
    return map;
  })();
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

  // Same resolution order as DocSnapText on the C# side: the
  // catalogue first (which is where a language added after the
  // fact gets its words), then the three written inline here,
  // then English. The catalogue is keyed by the English string,
  // so `en` doubles as the lookup key.
  function t(en, ja, fa) {
    var lang = currentLang();
    var catalogue = window.__DOCSNAP_I18N__;
    if (catalogue) {
      var row = catalogue[en];
      if (row && typeof row[lang] === 'string' && row[lang] !== '') { return row[lang]; }
    }
    if (lang === 'ja' && ja) { return ja; }
    if (lang === 'fa' && fa) { return fa; }
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
  // `persist` is false exactly once: when the starting state
  // was DERIVED from the viewport rather than chosen by the
  // reader (see sidebarStartsCollapsed). Writing that derived
  // value would turn "this window happens to be narrow right
  // now" into a stored preference, so a desktop reader who
  // once dragged a window narrow would find the navigation
  // closed on every page afterwards at every width. Only a
  // press is a choice.
  function applySidebar(collapsed, persist) {
    document.body.classList.toggle('ds-sidebar-collapsed', !!collapsed);
    each('[data-sidebar-toggle]', function (el) { el.setAttribute('aria-pressed', collapsed ? 'true' : 'false'); });
    if (persist !== false) { safeStorage.set(SIDEBAR_KEY, collapsed ? 'collapsed' : 'open'); }
  }

  // ==========================================
  // Where the sidebar starts.
  //
  // A stored choice always wins - the reader decided, at
  // whatever width they decided it at, and second-guessing
  // that on the next page load is how a preference stops
  // feeling like one.
  //
  // With NOTHING stored, the default depends on the screen.
  // On a desktop the nav sits beside the content and costs
  // nothing, so it opens. On a phone the layout is a column
  // and the nav is ABOVE the content, so an export that opens
  // expanded opens on a file tree with the page underneath
  // it - which on a real project is several screens of
  // scrolling before the reader sees anything they came for.
  //
  // 860px is the width the stylesheet switches to that column
  // layout at, and the two numbers have to agree: a default
  // chosen at one breakpoint and a layout chosen at another
  // gives a phone-shaped page that opens expanded anyway.
  //
  // matchMedia is guarded because this file runs from file://
  // in browsers older than the export reading it.
  // ==========================================
  function sidebarStartsCollapsed() {
    var stored = safeStorage.get(SIDEBAR_KEY);
    if (stored === 'collapsed') { return true; }
    if (stored === 'open') { return false; }
    try {
      return !!(window.matchMedia && window.matchMedia('(max-width: 860px)').matches);
    } catch (e) {
      return false;
    }
  }

  function wireSidebarToggle() {
    var stored = safeStorage.get(SIDEBAR_KEY);
    applySidebar(sidebarStartsCollapsed(), stored === 'collapsed' || stored === 'open');
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
  // The kind tiles, the ownership tabs and the text box
  // narrow one list, and every number on the page is
  // recounted from that list on every change.
  //
  // The numbers used to be whatever the exporter had
  // rendered into them - always the "mine" count, never
  // touched again - while the list below obeyed the
  // filters. So switching to the Unity / packages tab
  // showed eight broken references under a tile reading
  // "0 broken references", and there was no reading of
  // that page that was not a contradiction. A count that
  // disagrees with the list beside it is worse than no
  // count, because the reader cannot tell which half is
  // lying.
  //
  // One rule fixes it and is worth stating plainly:
  //   every number is "how many rows you would see if you
  //   clicked me right now".
  // Nothing else. With that rule the page cannot contradict
  // itself, because each number is a prediction the very
  // next click verifies.
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

    // The valid values are read off the controls the exporter
    // rendered rather than written down here, so the two halves of
    // this feature cannot drift apart in a rename.
    var KINDS = [];
    each('[data-issue-filter]', function (tile) { KINDS.push(tile.getAttribute('data-issue-filter')); });
    var OWNERS = [];
    each('[data-issue-owner]', function (tab) { OWNERS.push(tab.getAttribute('data-issue-owner')); });

    // A query string is a link somebody else wrote: the dashboard,
    // an older export, a line pasted into a bug report. An unknown
    // value used to be taken verbatim, which matched no row and
    // produced a blank page - indistinguishable from one that
    // failed to load. An unrecognised filter is now simply not
    // applied.
    var params = new URLSearchParams(window.location.search || '');
    var wantedKind = params.get('kind');
    var wantedScope = params.get('scope');
    var wantedOwner = params.get('owner');
    if (wantedKind && KINDS.indexOf(wantedKind) >= 0) { kind = wantedKind; }
    if (wantedOwner && OWNERS.indexOf(wantedOwner) >= 0) { owner = wantedOwner; }
    if (wantedScope && search) { search.value = wantedScope; }

    // The words already on the page, reused rather than repeated,
    // so the empty state speaks whichever language the reader
    // switched to without a second copy of every label here.
    function kindLabel(k) {
      var tile = document.querySelector('[data-issue-filter="' + k + '"]');
      var el = tile && tile.querySelector('.ds-stat-label span');
      return el ? el.textContent.trim() : k;
    }
    function ownerLabel(o) {
      var tab = document.querySelector('[data-issue-owner="' + o + '"]');
      var el = tab && tab.querySelector('span:not(.ds-seg-count)');
      return el ? el.textContent.trim() : o;
    }

    // Four tallies, one pass. `kinds` and `owners` answer the
    // "if you clicked me" question for the two control groups;
    // `kindAnyOwner` is what the empty state needs to say "none
    // here, but eight over there"; `ignoringText` separates "the
    // filter is empty" from "your search is empty".
    function tally() {
      var q = search ? search.value.trim().toLowerCase() : '';
      var kinds = {}, owners = {}, kindAnyOwner = {}, ignoringText = 0;
      for (var i = 0; i < rows.length; i++) {
        var r = rows[i];
        var k = r.getAttribute('data-issue-kind');
        var o = r.getAttribute('data-issue-owner') || 'mine';
        var okKind = kind === 'all' || k === kind;
        var okOwner = owner === 'any' || o === owner;
        var okText = !q || (r.getAttribute('data-issue-text') || '').indexOf(q) >= 0;

        if (okKind && okOwner) { ignoringText++; }
        if (!okText) { continue; }

        if (okOwner) {
          kinds[k] = (kinds[k] || 0) + 1;
          kinds.all = (kinds.all || 0) + 1;
        }
        if (okKind) {
          owners[o] = (owners[o] || 0) + 1;
          owners.any = (owners.any || 0) + 1;
        }
        if (!kindAnyOwner[k]) { kindAnyOwner[k] = {}; }
        kindAnyOwner[k][o] = (kindAnyOwner[k][o] || 0) + 1;
      }
      return { kinds: kinds, owners: owners, kindAnyOwner: kindAnyOwner, ignoringText: ignoringText };
    }

    function clearButton(labelEn, labelJa, labelFa, fn) {
      var b = document.createElement('button');
      b.type = 'button';
      b.className = 'ds-empty-action';
      b.textContent = t(labelEn, labelJa, labelFa);
      b.addEventListener('click', fn);
      return b;
    }

    // An empty list is a question - "where did they go?" - and the
    // page should answer it rather than restate that the list is
    // empty. Every branch here ends in a button, because knowing
    // which filter hid the rows is only half of wanting them back.
    function explainEmpty(n) {
      empty.textContent = '';
      var q = search ? search.value.trim() : '';

      if (q && n.ignoringText > 0) {
        empty.appendChild(document.createTextNode(
          t(n.ignoringText + ' findings are here, but none of them mention ',
            'ここには ' + n.ignoringText + ' 件ありますが、いずれも次を含みません: ',
            n.ignoringText + ' مورد این‌جا هست، ولی هیچ‌کدام شامل این نیست: ')));
        var strong = document.createElement('strong');
        strong.textContent = q;
        empty.appendChild(strong);
        empty.appendChild(document.createTextNode(' '));
        empty.appendChild(clearButton('Clear the search', '検索をクリア', 'پاک‌کردن جست‌وجو', function () {
          if (search) { search.value = ''; }
          apply();
        }));
        return;
      }

      // The case that looked most like a bug: a kind that has no
      // findings among the reader's own files while the same kind
      // has plenty in Unity's. The tile says 0, the badge in the
      // sidebar says 9, and nothing on the page joins the two up.
      if (kind !== 'all') {
        var spread = n.kindAnyOwner[kind] || {};
        var elsewhere = null, elsewhereCount = 0;
        for (var i = 0; i < OWNERS.length; i++) {
          var o = OWNERS[i];
          if (o === 'any' || o === owner) { continue; }
          if ((spread[o] || 0) > elsewhereCount) { elsewhere = o; elsewhereCount = spread[o]; }
        }
        if (elsewhere) {
          empty.appendChild(document.createTextNode(
            t('No ' + kindLabel(kind) + ' in ' + ownerLabel(owner) + ' — but ' + elsewhereCount + ' in ' + ownerLabel(elsewhere) + '. ',
              ownerLabel(owner) + 'に' + kindLabel(kind) + 'はありません。' + ownerLabel(elsewhere) + 'には ' + elsewhereCount + ' 件あります。',
              'هیچ «' + kindLabel(kind) + '»ی در «' + ownerLabel(owner) + '» نیست — ولی ' + elsewhereCount + ' مورد در «' + ownerLabel(elsewhere) + '» هست. ')));
          empty.appendChild(clearButton('Show those', 'そちらを表示', 'نمایش آن‌ها', function () {
            owner = elsewhere;
            apply();
          }));
          return;
        }
      }

      empty.appendChild(document.createTextNode(
        t('Nothing matches this filter. ', 'この絞り込みに一致する指摘はありません。', 'چیزی با این فیلتر پیدا نشد. ')));
      empty.appendChild(clearButton('Clear all filters', 'すべての絞り込みを解除', 'برداشتن همه‌ی فیلترها', function () {
        kind = 'all';
        owner = 'any';
        if (search) { search.value = ''; }
        apply();
      }));
    }

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

      var n = tally();

      each('[data-issue-filter]', function (tile) {
        var k = tile.getAttribute('data-issue-filter');
        var count = n.kinds[k] || 0;
        var isActive = k === kind;
        var num = tile.querySelector('.ds-stat-num');
        if (num) { num.textContent = count; }
        tile.classList.toggle('is-active', isActive);
        tile.setAttribute('aria-pressed', isActive ? 'true' : 'false');

        // A tile whose own count is zero can only produce the empty
        // list, so it stops being a button. The active one is never
        // disabled even at zero, or there would be no way back off it.
        var dead = count === 0 && !isActive;
        tile.classList.toggle('is-empty', dead);
        tile.disabled = dead;

        // "8 in Unity / packages" answers a question the reader only
        // has while they are looking at their own files. On the other
        // two tabs it is describing rows already on screen, which
        // reads as a second, disagreeing count.
        var aside = tile.querySelector('.ds-stat-aside');
        if (aside) { aside.hidden = owner !== 'mine'; }
      });

      each('[data-issue-owner]', function (tab) {
        var o = tab.getAttribute('data-issue-owner');
        var isActive = o === owner;
        var el = tab.querySelector('.ds-seg-count');
        if (el) { el.textContent = n.owners[o] || 0; }
        tab.classList.toggle('is-active', isActive);
        tab.setAttribute('aria-pressed', isActive ? 'true' : 'false');
      });

      if (empty) {
        empty.hidden = shown !== 0;
        if (shown === 0) { explainEmpty(n); }
      }
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
  // Packages page filtering and sorting
  //
  // The page states four counts - third-party, Unity,
  // built-in, updates available - and used to print all
  // sixty-odd packages in fixed sections underneath them.
  // The only one of those numbers anybody arrives at this
  // page about is "updates available", and answering it
  // meant reading every card looking for a badge.
  //
  // The tiles are now the filter, the sort re-cuts the same
  // list without a reload, and ?group= lets the dashboard's
  // "Updatable packages" tile link straight into the
  // answer.
  // ==========================================
  function wirePackageFilters() {
    var list = document.querySelector('[data-pkg-list]');
    if (!list) { return; }

    var cards = [];
    each('.ds-pkg-card', function (card) { cards.push(card); }, list);

    var search = document.querySelector('[data-pkg-search]');
    var sorter = document.querySelector('[data-pkg-sort]');
    var empty = document.querySelector('[data-pkg-empty]');
    var group = 'all';
    var timer = null;

    // The order the exporter wrote them in, kept so "Grouped"
    // can be restored without re-deriving what the grouping was.
    for (var i = 0; i < cards.length; i++) { cards[i].__dsRank = i; }

    var params = new URLSearchParams(window.location.search || '');
    var wanted = params.get('group');
    if (wanted) { group = wanted; }
    var wantedText = params.get('q');
    if (wantedText && search) { search.value = wantedText; }

    function matches(card) {
      if (group === 'updates') { return card.getAttribute('data-pkg-update') === '1'; }
      return group === 'all' || card.getAttribute('data-pkg-group') === group;
    }

    function apply() {
      var q = search ? search.value.trim().toLowerCase() : '';
      var shown = 0;

      for (var i = 0; i < cards.length; i++) {
        var card = cards[i];
        var okText = !q || (card.getAttribute('data-pkg-text') || '').indexOf(q) >= 0;
        var visible = matches(card) && okText;
        card.classList.toggle('ds-filtered-out', !visible);
        if (visible) { shown++; }
      }

      if (empty) { empty.hidden = shown !== 0; }

      each('[data-pkg-filter]', function (tile) {
        var isActive = tile.getAttribute('data-pkg-filter') === group;
        tile.classList.toggle('is-active', isActive);
        tile.setAttribute('aria-pressed', isActive ? 'true' : 'false');
      });
    }

    function resort() {
      var mode = sorter ? sorter.value : 'group';
      var sorted = cards.slice();

      sorted.sort(function (a, b) {
        if (mode === 'name') {
          var an = a.getAttribute('data-pkg-name') || '';
          var bn = b.getAttribute('data-pkg-name') || '';
          return an < bn ? -1 : (an > bn ? 1 : 0);
        }
        if (mode === 'updates') {
          var au = a.getAttribute('data-pkg-update') === '1' ? 0 : 1;
          var bu = b.getAttribute('data-pkg-update') === '1' ? 0 : 1;
          if (au !== bu) { return au - bu; }
        }
        return a.__dsRank - b.__dsRank;
      });

      // Re-appending a node that is already in the list moves it,
      // so this is a reorder rather than a rebuild - the cards
      // keep their listeners and the browser keeps their layout.
      for (var i = 0; i < sorted.length; i++) { list.appendChild(sorted[i]); }
    }

    each('[data-pkg-filter]', function (tile) {
      tile.addEventListener('click', function (evt) {
        var next = evt.currentTarget.getAttribute('data-pkg-filter');
        group = (group === next && next !== 'all') ? 'all' : next;
        apply();
      });
    });

    if (sorter) {
      sorter.addEventListener('change', function () { resort(); apply(); });
    }

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
  // Back to the site that is hosting this export
  //
  // An export opened from a folder on a disk is the whole
  // world and needs no way out of itself. An export
  // EMBEDDED in a website is a dead end: the site's own
  // navigation is gone, and the reader is one browser-back
  // away from the page they came from only until they click
  // twice. So the host says where back goes, by opening the
  // export with ?home=<url>, and it is remembered for the
  // rest of the visit so moving between pages of the export
  // does not lose it.
  //
  // Two rules, and neither is optional:
  //
  //   The URL must start with http:// or https://. Without
  //   that check, "javascript:" in a query string would be
  //   a script injection into a file somebody hands to a
  //   client - which is the worst possible place for one.
  //
  //   The label is the host name, taken from the URL we
  //   just validated, never from a second parameter. A
  //   caller-supplied label is a caller-supplied string in
  //   the page, and it buys nothing: the reader wants to
  //   know where the link goes.
  // ==========================================
  var HOME_KEY = 'docsnap.home';

  function wireSiteBack() {
    var link = document.querySelector('[data-site-back]');
    if (!link) { return; }

    var url = '';
    try {
      var fromQuery = new URLSearchParams(window.location.search || '').get('home');
      if (fromQuery && /^https?:\/\//i.test(fromQuery)) {
        url = fromQuery;
        try { window.sessionStorage.setItem(HOME_KEY, url); } catch (e) { /* private window */ }
      } else {
        var remembered = null;
        try { remembered = window.sessionStorage.getItem(HOME_KEY); } catch (e) { remembered = null; }
        if (remembered && /^https?:\/\//i.test(remembered)) { url = remembered; }
      }
    } catch (e) { url = ''; }

    if (!url) { return; }

    var label = url;
    try { label = new URL(url).hostname || url; } catch (e) { label = url; }

    link.setAttribute('href', url);
    var text = link.querySelector('[data-site-back-label]');
    if (text) { text.textContent = t('Back to ', '戻る: ', 'بازگشت به ') + label; }
    link.hidden = false;
  }

  // ==========================================
  // Unlocking an encrypted section
  //
  // The export's author locked some sections behind a
  // password. This is the half that opens them, and it is
  // real decryption rather than a hidden element being
  // shown: the content is not in the document at all until
  // the right password produces it.
  //
  // The scheme, matching DocSnapLockCrypto.cs exactly:
  //   key material = PBKDF2-HMAC-SHA256(password, salt, iter) -> 64 bytes
  //   first 32 bytes  = AES-256-CBC key
  //   last 32 bytes   = HMAC-SHA256 key
  //   the MAC covers IV + ciphertext and is checked BEFORE
  //   decrypting, so a wrong password and a tampered file
  //   both fail at the same place instead of decrypting to
  //   rubbish.
  //
  // Everything is WebCrypto, which every browser has built
  // in, because this file may not load a library - the
  // export has to work from a folder on a disk with no
  // network at all.
  //
  // The password, once it works, is kept in sessionStorage
  // so moving between pages of the export does not ask
  // again. sessionStorage and not localStorage, quite
  // deliberately: closing the tab locks it again, which is
  // what somebody who unlocked this on a client's machine
  // would expect.
  // ==========================================
  var LOCK_KEY = 'docsnap.lock.pw';

  function lockSubtle() {
    try { return window.crypto && window.crypto.subtle ? window.crypto.subtle : null; }
    catch (e) { return null; }
  }

  function lockBytes(b64) {
    var raw = atob(b64);
    var out = new Uint8Array(raw.length);
    for (var i = 0; i < raw.length; i++) { out[i] = raw.charCodeAt(i); }
    return out;
  }

  function lockPayload(slot) {
    var node = slot.querySelector('[data-locked-payload]');
    if (!node) { return null; }
    try { return JSON.parse(node.textContent); } catch (e) { return null; }
  }

  // Resolves to the plaintext, or rejects with a reason the
  // caller turns into a sentence. 'nocrypto' and
  // 'wrong' are different failures and must not be reported
  // as the same one - somebody retyping a correct password
  // into a browser that cannot do the work would never get
  // anywhere.
  function lockDecrypt(payload, password) {
    var subtle = lockSubtle();
    if (!subtle) { return Promise.reject(new Error('nocrypto')); }

    var salt = lockBytes(payload.salt);
    var iv = lockBytes(payload.iv);
    var mac = lockBytes(payload.mac);
    var ct = lockBytes(payload.ct);
    var iter = payload.iter || 200000;

    return subtle.importKey('raw', new TextEncoder().encode(password), 'PBKDF2', false, ['deriveBits'])
      .then(function (base) {
        return subtle.deriveBits({ name: 'PBKDF2', salt: salt, iterations: iter, hash: 'SHA-256' }, base, 512);
      })
      .then(function (bits) {
        var material = new Uint8Array(bits);
        var signed = new Uint8Array(iv.length + ct.length);
        signed.set(iv, 0);
        signed.set(ct, iv.length);
        return subtle.importKey('raw', material.slice(32, 64), { name: 'HMAC', hash: 'SHA-256' }, false, ['verify'])
          .then(function (macKey) { return subtle.verify('HMAC', macKey, mac, signed); })
          .then(function (good) {
            if (!good) { throw new Error('wrong'); }
            return subtle.importKey('raw', material.slice(0, 32), { name: 'AES-CBC' }, false, ['decrypt']);
          })
          .then(function (aesKey) { return subtle.decrypt({ name: 'AES-CBC', iv: iv }, aesKey, ct); })
          .then(function (plain) { return new TextDecoder().decode(plain); });
      });
  }

  function wireLockedSections() {
    var slot = document.querySelector('[data-locked-slot]');
    var manageForm = document.querySelector('[data-manage-form]');
    if (!slot && !manageForm) { return; }

    var status = document.querySelector('[data-locked-status]');
    var form = slot ? slot.querySelector('[data-locked-form]') : manageForm;
    var input = (form || document).querySelector('[data-locked-password]');

    function say(text, bad) {
      if (!status) { return; }
      status.textContent = text;
      status.classList.toggle('is-bad', !!bad);
    }

    function remember(password) {
      try { window.sessionStorage.setItem(LOCK_KEY, password); } catch (e) { /* private window */ }
    }
    function remembered() {
      try { return window.sessionStorage.getItem(LOCK_KEY) || ''; } catch (e) { return ''; }
    }

    // Replacing the placeholder with the real content, rather
    // than un-hiding anything: until this line runs the markup
    // did not exist in the document.
    function reveal(html) {
      var main = slot.parentNode;
      var holder = document.createElement('div');
      holder.innerHTML = html;
      main.replaceChild(holder, slot);

      // The revealed content is the same markup every other
      // page is built from, so everything that wires a page up
      // has to run over it again - the language switch, the
      // trees, the copy buttons, the anchors.
      try {
        applyLanguage(currentLang());
        wireTreeControls();
        wireTreeFilter();
        wireIssueFilters();
        wirePackageFilters();
        wireDiffMore();
      } catch (e) { /* a section that does not use one of these */ }
    }

    function attempt(password, silent) {
      if (!password) { return; }

      if (!slot) {
        // The Management page has no content of its own to
        // reveal. Checking the password there is still worth
        // doing: it tells somebody whether the one they were
        // given is the right one before they go hunting through
        // the pages, and it saves it for those pages.
        say(t('Checking…', '確認中…', 'در حال بررسی…'));
        remember(password);
        say(t('Saved. The locked pages will open while this tab is open.',
              '保存しました。このタブを開いている間、ロックされたページが開きます。',
              'ذخیره شد. تا وقتی این تب باز است، صفحه‌های قفل‌شده باز می‌شوند.'));
        return;
      }

      var payload = lockPayload(slot);
      if (!payload) {
        say(t('This page is missing its encrypted content.',
              'このページには暗号化されたコンテンツがありません。',
              'محتوای رمزنگاری‌شده‌ی این صفحه موجود نیست.'), true);
        return;
      }

      if (!silent) { say(t('Unlocking…', '解除中…', 'در حال باز کردن…')); }

      lockDecrypt(payload, password).then(function (html) {
        remember(password);
        reveal(html);
      }, function (err) {
        if (err && err.message === 'nocrypto') {
          say(t('This browser cannot decrypt a page opened from a folder. Open the export in Firefox, or serve the folder over http.',
                'このブラウザではフォルダから開いたページを復号できません。Firefox で開くか、フォルダを http で配信してください。',
                'این مرورگر نمی‌تواند صفحه‌ای را که از روی پوشه باز شده رمزگشایی کند. با فایرفاکس بازش کن، یا پوشه را روی http بگذار.'), true);
          return;
        }
        // A silent attempt is the remembered password being
        // tried on a new page. If that fails the reader has not
        // typed anything yet, and "wrong password" about a
        // password they did not enter is nonsense.
        if (silent) {
          say('');
          return;
        }
        say(t('That password does not open this section.',
              'そのパスワードではこのセクションを開けません。',
              'این رمز این بخش را باز نمی‌کند.'), true);
      });
    }

    if (form) {
      form.addEventListener('submit', function (evt) {
        evt.preventDefault();
        attempt(input ? input.value : '', false);
      });
    }

    // Unlock once, read the whole export: a password that
    // already worked this session is tried without asking.
    var saved = remembered();
    if (saved && slot) { attempt(saved, true); }
  }

  // ==========================================
  // Uncapping a long diff list
  // Only lists past ChangesPageRenderer.DiffListCapRows
  // carry the button, so most pages never run this.
  // ==========================================
  function wireDiffMore() {
    each('[data-diff-more]', function (btn) {
      btn.addEventListener('click', function () {
        var body = btn.parentElement;
        var list = body ? body.querySelector('.ds-diff-list') : null;
        if (list) { list.classList.remove('is-capped'); }
        btn.remove();
      });
    });
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

  // ==========================================
  // safeHref
  // A URL for an href built by this script, escaped for the
  // attribute AND checked for its scheme.
  //
  // Escaping alone was enough to stop a value breaking out of
  // the attribute, but not to stop the value BEING a
  // javascript: URL - and every one of these links is built
  // from a search record, which is built from a GameObject or
  // asset name that came out of the project. Nothing in the
  // exporter can currently produce such a record, which is
  // exactly why this is cheap to add now and unpleasant to
  // discover later.
  //
  // Site links are relative by construction ("scenes/Main.html"),
  // so the rule is simply: no scheme at all. A value with one
  // becomes '#', which is inert and still renders.
  // ==========================================
  function safeHref(url) {
    var raw = String(url === null || url === undefined ? '' : url).trim();
    // Strip control characters first: "java\0script:" and
    // "java\tscript:" are both read as a scheme by browsers but
    // would slip past a plain prefix test.
    var probe = raw.replace(/[\u0000-\u0020]/g, '').toLowerCase();
    // A scheme can only appear before the first '/', '?' or '#',
    // so a relative path containing a colon later on is fine.
    var stop = probe.search(/[/?#]/);
    var head = stop < 0 ? probe : probe.slice(0, stop);
    if (head.indexOf(':') >= 0) { return '#'; }
    return esc(raw);
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
        html += '<a class="ds-search-result" href="' + safeHref(prefix + (r.u || '')) + '">'
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
    wirePackageFilters();
    wireBackToTop();
    wireSiteBack();
    wireLockedSections();
    wireDiffMore();
    wireCopyButtons();
    wireSearch();
    wireHotkeys();
    revealHashTarget();
    window.addEventListener('hashchange', revealHashTarget);
  });
})();
