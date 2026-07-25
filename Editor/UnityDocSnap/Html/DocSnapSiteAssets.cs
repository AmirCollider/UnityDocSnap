// ==========================================
// DocSnapSiteAssets
// Embedded static site assets (CSS + JS) so the
// generated output has zero external file
// dependencies at export time and zero network
// requests when opened.
//
// Both blocks are C# verbatim strings, so a literal
// double quote is written twice. The readable
// sources they were authored from are plain
// style.css / app.js; keep the two in step.
// ==========================================
namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapSiteAssets
    {
        // ==========================================
        // StyleCss - theme/style.css contents
        //
        // Fonts are prepended from DocSnapFontAssets
        // (base64 woff2) rather than pulled from a CDN,
        // so an exported site is genuinely self-contained
        // and makes no network requests when opened
        // offline. The stylesheet itself asks for a system
        // UI face first and falls back to the embedded
        // families, so on a normal desktop the text is the
        // one the OS renders best and the embedded bytes
        // are only paid for when they are actually needed.
        // ==========================================
        public static readonly string StyleCss = DocSnapFontAssets.FontFaceCss + @"/* ==========================================
   Unity DocSnap — Site Stylesheet

   Rebuilt for the one thing the generated site
   is actually for: finding a specific thing in a
   very large project, quickly.

   The previous stylesheet dressed every surface —
   gradient card headers, 22px radii, lift-on-hover
   shadows on every row, a pastel wash behind the
   sidebar. On a small demo that reads as charm; on
   a real project it is thousands of decorated rows
   between a reader and the object they came for,
   and every one of those decorations is layout and
   paint work the browser has to do before the page
   responds. This version keeps the brand in the
   places a person looks once (the mark, the accent,
   the empty states) and gets out of the way
   everywhere a person looks a thousand times.

   Structurally that means: flat surfaces separated
   by 1px borders instead of shadows, one accent
   colour instead of four pastels, a tighter type
   scale, and content-visibility on every repeated
   block so an off-screen GameObject costs the
   browser nothing.
   ========================================== */

/* ==========================================
   Design tokens
   One neutral ramp, one accent, three semantic
   colours. Everything else is composed from
   these, which is what makes the dark theme a
   token swap rather than a second stylesheet.
   ========================================== */
:root {
  color-scheme: light;

  --bg: #fbfbfd;
  --surface: #ffffff;
  --surface-2: #f5f5f8;
  --surface-3: #ecebf1;
  --border: #e4e3ec;
  --border-strong: #d3d1de;

  --text: #1d1b26;
  --text-dim: #64616f;
  --text-faint: #97949f;

  --accent: #7c5cd6;
  --accent-hover: #6a4bc4;
  --accent-soft: #f1ecfd;
  --accent-border: #d9ccf7;
  --accent-contrast: #ffffff;

  --danger: #d64545;
  --danger-soft: #fdeded;
  --danger-border: #f4c9c9;

  --ok: #2f8f5b;
  --ok-soft: #e9f7ef;
  --ok-border: #bfe6cf;

  --info: #2f6fbf;
  --info-soft: #eaf2fd;
  --info-border: #c5daf6;

  --radius-lg: 12px;
  --radius-md: 9px;
  --radius-sm: 6px;

  /* Deliberately almost nothing. Depth is carried by
     borders and surface steps; a shadow on every row
     is a paint cost per row. */
  --shadow: 0 1px 2px rgba(20, 16, 40, .05);
  --shadow-pop: 0 8px 28px rgba(20, 16, 40, .14);

  /* System UI first: it renders at native hinting and
     needs zero bytes. The embedded families stay in the
     stack as an offline-identical fallback, so a machine
     without a decent UI font still gets the branded
     look rather than Times New Roman. */
  --font-body: system-ui, -apple-system, 'Segoe UI', Roboto, 'Quicksand', 'Helvetica Neue', Arial, sans-serif;
  --font-display: system-ui, -apple-system, 'Segoe UI', Roboto, 'Quicksand', 'Helvetica Neue', Arial, sans-serif;
  --font-brand: 'Baloo 2', system-ui, sans-serif;
  --font-mono: ui-monospace, SFMono-Regular, 'SF Mono', 'Space Mono', Menlo, Consolas, 'Liberation Mono', monospace;

  --sidebar-w: 272px;
  --content-max: 1320px;
}

:root[data-theme=dark] {
  color-scheme: dark;

  --bg: #131218;
  --surface: #1a1922;
  --surface-2: #211f2b;
  --surface-3: #2a2836;
  --border: #2e2c3a;
  --border-strong: #3d3a4c;

  --text: #eceaf3;
  --text-dim: #a5a1b3;
  --text-faint: #797588;

  --accent: #a78bfa;
  --accent-hover: #b9a3fc;
  --accent-soft: #262038;
  --accent-border: #3d3358;
  --accent-contrast: #17141f;

  --danger: #f38080;
  --danger-soft: #2f1e21;
  --danger-border: #4a2b2e;

  --ok: #6fd39b;
  --ok-soft: #17291f;
  --ok-border: #2a4534;

  --info: #7ab0f5;
  --info-soft: #16233a;
  --info-border: #2a3d5c;

  --shadow: 0 1px 2px rgba(0, 0, 0, .4);
  --shadow-pop: 0 8px 28px rgba(0, 0, 0, .55);
}

/* Japanese has no embedded web font (a CJK face is ~2 MB);
   it uses the gothic faces every desktop OS ships with. */
:lang(ja) {
  --font-body: system-ui, 'Hiragino Kaku Gothic ProN', 'Yu Gothic', 'YuGothic', 'Meiryo', sans-serif;
  --font-display: system-ui, 'Hiragino Kaku Gothic ProN', 'Yu Gothic', 'YuGothic', 'Meiryo', sans-serif;
}

/* Persian keeps Vazirmatn first: system Arabic-script
   faces vary enough that the layout genuinely breaks
   without it, which is not true of the Latin stack. */
:lang(fa) {
  --font-body: 'Vazirmatn', system-ui, sans-serif;
  --font-display: 'Vazirmatn', system-ui, sans-serif;
}

/* ==========================================
   Base
   ========================================== */
*, *::before, *::after { box-sizing: border-box; }

/* Set by the boot script in <head> when the reader's
   stored language differs from the baked one: the body
   stays invisible until app.js swaps the text, so there
   is never a flash of the wrong language or direction.
   A timeout in the boot script clears it after 1.5s even
   if app.js never runs. */
html.ds-lang-pending body { visibility: hidden; }

html { scroll-behavior: smooth; }

@media (prefers-reduced-motion: reduce) {
  html { scroll-behavior: auto; }
  *, *::before, *::after { animation-duration: .001ms !important; transition-duration: .001ms !important; }
}

body {
  margin: 0;
  background: var(--bg);
  color: var(--text);
  font-family: var(--font-body);
  font-size: 14px;
  line-height: 1.55;
  -webkit-font-smoothing: antialiased;
  text-rendering: optimizeLegibility;
}

html[dir=rtl] body { direction: rtl; }

a { color: var(--accent); text-decoration: none; }
a:hover { color: var(--accent-hover); text-decoration: underline; }

:focus-visible {
  outline: 2px solid var(--accent);
  outline-offset: 2px;
  border-radius: 3px;
}

h1, h2, h3, h4 {
  font-family: var(--font-display);
  font-weight: 650;
  color: var(--text);
  margin: 0 0 .4em;
  letter-spacing: -.01em;
}

code, .mono { font-family: var(--font-mono); font-size: .92em; }

button { font-family: inherit; }

::selection { background: var(--accent-soft); color: var(--text); }

/* ==========================================
   Scrollbars — the site is mostly long scrolling
   lists, so the default chunky bars are a real
   share of the visual noise.
   ========================================== */
* { scrollbar-width: thin; scrollbar-color: var(--border-strong) transparent; }
*::-webkit-scrollbar { width: 10px; height: 10px; }
*::-webkit-scrollbar-track { background: transparent; }
*::-webkit-scrollbar-thumb { background: var(--border-strong); border-radius: 6px; border: 2px solid transparent; background-clip: content-box; }
*::-webkit-scrollbar-thumb:hover { background: var(--text-faint); background-clip: content-box; }

/* ==========================================
   Shell
   ========================================== */
.ds-shell { display: flex; min-height: 100vh; align-items: stretch; }

.ds-sidebar {
  width: var(--sidebar-w);
  flex: 0 0 var(--sidebar-w);
  background: var(--surface);
  border-inline-end: 1px solid var(--border);
  padding: 16px 12px 16px;
  position: sticky;
  top: 0;
  height: 100vh;
  overflow-y: auto;
  overscroll-behavior: contain;
}

/* Collapsed by the sidebar toggle and remembered per
   reader. On a 13"" laptop the field tables are the
   thing that needs the width, not the nav. */
body.ds-sidebar-collapsed .ds-sidebar { display: none; }
body.ds-sidebar-collapsed .ds-sidebar-reopen { display: inline-flex; }

.ds-main {
  flex: 1 1 auto;
  min-width: 0;
  padding: 22px 30px 72px;
}
.ds-main > * { max-width: var(--content-max); }

/* ==========================================
   Sidebar — brand + controls
   ========================================== */
.ds-brand { display: flex; align-items: center; gap: 9px; padding: 2px 6px 0; }
.ds-brand svg, .ds-brand img { width: 30px; height: 30px; flex: none; }
.ds-brand-text h1 { font-family: var(--font-brand); font-size: 16px; margin: 0; line-height: 1.15; letter-spacing: 0; }
.ds-brand-text span { font-size: 11px; color: var(--text-faint); }

.ds-tagline {
  font-size: 11.5px;
  color: var(--text-dim);
  margin: 8px 6px 12px;
  font-family: var(--font-mono);
  direction: ltr;
  unicode-bidi: isolate;
  overflow-wrap: anywhere;
}

.ds-topbar { display: flex; align-items: stretch; gap: 6px; margin: 0 4px 8px; }

.ds-langbar, .ds-modebar {
  display: flex;
  gap: 2px;
  padding: 2px;
  background: var(--surface-2);
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
}
.ds-langbar { flex: 1; margin: 0; }
.ds-modebar { margin: 0 4px 12px; }

.ds-lang-btn, .ds-mode-btn {
  flex: 1;
  font-size: 11.5px;
  font-weight: 600;
  padding: 5px 4px;
  border: none;
  background: transparent;
  color: var(--text-dim);
  border-radius: var(--radius-sm);
  cursor: pointer;
  white-space: nowrap;
  transition: background .12s ease, color .12s ease;
}
.ds-lang-btn:hover, .ds-mode-btn:hover { color: var(--text); background: var(--surface-3); }
.ds-lang-btn.is-active, .ds-mode-btn.is-active {
  background: var(--surface);
  color: var(--text);
  box-shadow: var(--shadow);
}
:root[data-theme=dark] .ds-lang-btn.is-active,
:root[data-theme=dark] .ds-mode-btn.is-active { background: var(--surface-3); }

body.ds-mode-simple .ds-adv { display: none !important; }

.ds-icon-btn, .ds-theme-toggle {
  flex: none;
  width: 34px;
  height: auto;
  min-height: 32px;
  border: 1px solid var(--border);
  background: var(--surface-2);
  color: var(--text-dim);
  border-radius: var(--radius-md);
  cursor: pointer;
  font-size: 14px;
  line-height: 1;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: background .12s ease, color .12s ease;
}
.ds-icon-btn:hover, .ds-theme-toggle:hover { background: var(--surface-3); color: var(--text); }

/* ==========================================
   Sidebar — search
   ========================================== */
.ds-search { position: relative; margin: 0 4px 14px; }

.ds-search-input {
  width: 100%;
  font-family: inherit;
  font-size: 13px;
  padding: 8px 10px 8px 30px;
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  background: var(--surface-2);
  color: var(--text);
  outline: none;
  transition: border-color .12s ease, background .12s ease;
}
html[dir=rtl] .ds-search-input { padding: 8px 30px 8px 10px; }
.ds-search-input:focus { border-color: var(--accent); background: var(--surface); }
.ds-search-input::placeholder { color: var(--text-faint); }
.ds-search::before {
  content: '⌕';
  position: absolute;
  inset-inline-start: 10px;
  top: 6px;
  font-size: 15px;
  color: var(--text-faint);
  pointer-events: none;
}

.ds-search-hint {
  position: absolute;
  inset-inline-end: 8px;
  top: 8px;
  font-family: var(--font-mono);
  font-size: 10px;
  color: var(--text-faint);
  border: 1px solid var(--border);
  border-radius: 4px;
  padding: 0 4px;
  pointer-events: none;
}
.ds-search-input:focus ~ .ds-search-hint { display: none; }

.ds-search-filters { display: flex; gap: 4px; margin-top: 6px; }
.ds-search-filter {
  flex: 1;
  font-size: 11px;
  font-weight: 600;
  padding: 4px 4px;
  border: 1px solid var(--border);
  background: transparent;
  color: var(--text-dim);
  border-radius: 999px;
  cursor: pointer;
}
.ds-search-filter:hover { background: var(--surface-2); color: var(--text); }
.ds-search-filter.is-active { background: var(--accent); color: var(--accent-contrast); border-color: var(--accent); }

.ds-search-results {
  position: absolute;
  z-index: 40;
  inset-inline: 0;
  margin-top: 8px;
  max-height: 62vh;
  overflow-y: auto;
  overscroll-behavior: contain;
  background: var(--surface);
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-pop);
  padding: 4px;
}
.ds-search-result {
  display: block;
  padding: 7px 9px;
  border-radius: var(--radius-sm);
  color: var(--text);
}
.ds-search-result:hover, .ds-search-result.is-active { background: var(--accent-soft); text-decoration: none; }
.ds-search-result .r-top { display: flex; align-items: center; gap: 6px; }
.ds-search-result .r-name { font-weight: 600; font-size: 12.5px; overflow-wrap: anywhere; }
.ds-search-result .r-cat {
  margin-inline-start: auto;
  flex: none;
  font-family: var(--font-mono);
  font-size: 9.5px;
  font-weight: 600;
  color: var(--accent);
  background: var(--accent-soft);
  border-radius: 999px;
  padding: 1px 6px;
}
.ds-search-result .r-sub {
  display: block;
  font-family: var(--font-mono);
  font-size: 10.5px;
  color: var(--text-dim);
  margin-top: 1px;
  overflow-wrap: anywhere;
  direction: ltr;
  unicode-bidi: isolate;
}
.ds-search-empty { padding: 12px 10px; font-size: 12px; color: var(--text-faint); text-align: center; }
.ds-search-more { padding: 7px 10px 3px; font-size: 11px; color: var(--text-faint); text-align: center; }
mark { background: var(--accent-soft); color: var(--accent); border-radius: 2px; padding: 0 1px; font-weight: 700; }

/* ==========================================
   Sidebar — navigation
   ========================================== */
.ds-nav-section { margin-bottom: 12px; }
.ds-nav-title {
  font-size: 10.5px;
  font-weight: 700;
  letter-spacing: .06em;
  text-transform: uppercase;
  color: var(--text-faint);
  margin: 0 0 4px 10px;
}
.ds-nav-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 1px; }
.ds-nav-link {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 10px;
  border-radius: var(--radius-sm);
  color: var(--text-dim);
  font-size: 13px;
  font-weight: 500;
  min-width: 0;
}
.ds-nav-link > span:first-child { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.ds-nav-link:hover { background: var(--surface-2); color: var(--text); text-decoration: none; }
.ds-nav-link.is-current { background: var(--accent-soft); color: var(--accent); font-weight: 650; }
.ds-nav-empty { font-size: 12px; color: var(--text-faint); padding: 4px 10px; }
.ds-nav-count {
  margin-inline-start: auto;
  flex: none;
  font-family: var(--font-mono);
  font-size: 10px;
  color: var(--text-faint);
  background: var(--surface-2);
  border-radius: 999px;
  padding: 1px 6px;
}
.ds-nav-count.is-warn { background: var(--danger-soft); color: var(--danger); font-weight: 700; }
.ds-nav-count.is-ok { background: var(--ok-soft); color: var(--ok); }

/* Long projects: the Scenes / Assets lists get their own
   scroll rather than pushing the footer a screen down. */
.ds-nav-scroll { max-height: 34vh; overflow-y: auto; overscroll-behavior: contain; }

.ds-sidebar-footer {
  margin-top: 16px;
  padding: 12px 10px 0;
  border-top: 1px solid var(--border);
  font-size: 11px;
  color: var(--text-faint);
}
.ds-sidebar-footer a { color: var(--text-dim); }

.ds-sidebar-reopen {
  display: none;
  position: fixed;
  inset-block-start: 14px;
  inset-inline-start: 14px;
  z-index: 45;
  width: 32px;
  height: 32px;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--border);
  background: var(--surface);
  color: var(--text-dim);
  border-radius: var(--radius-md);
  cursor: pointer;
  box-shadow: var(--shadow-pop);
}

/* ==========================================
   Page header
   ========================================== */
.ds-breadcrumb {
  font-size: 12px;
  color: var(--text-faint);
  margin-bottom: 8px;
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}
.ds-breadcrumb a { color: var(--text-dim); }
.ds-breadcrumb .sep { color: var(--border-strong); }

.ds-page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;
  padding-bottom: 16px;
  margin-bottom: 18px;
  border-bottom: 1px solid var(--border);
}
.ds-page-header h1 {
  font-size: 22px;
  display: flex;
  align-items: center;
  gap: 9px;
  margin: 0;
  overflow-wrap: anywhere;
}
.ds-page-sub { color: var(--text-dim); font-size: 12.5px; margin: 5px 0 0; font-family: var(--font-mono); direction: ltr; unicode-bidi: isolate; }

/* ==========================================
   Badges
   ========================================== */
.ds-badge-row { display: flex; gap: 6px; flex-wrap: wrap; align-items: center; }
.ds-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 11.5px;
  font-weight: 600;
  padding: 3px 9px;
  border-radius: 999px;
  background: var(--surface-2);
  color: var(--text-dim);
  border: 1px solid var(--border);
  white-space: nowrap;
}
a.ds-badge:hover { text-decoration: none; border-color: var(--border-strong); color: var(--text); }
.ds-badge.pink, .ds-badge.lav { background: var(--accent-soft); border-color: var(--accent-border); color: var(--accent); }
.ds-badge.mint { background: var(--ok-soft); border-color: var(--ok-border); color: var(--ok); }
.ds-badge.warn { background: var(--danger-soft); border-color: var(--danger-border); color: var(--danger); }
a.ds-badge.warn:hover { color: var(--danger); border-color: var(--danger); }
.ds-badge.ghost { background: transparent; }

/* ==========================================
   Cards
   ========================================== */
.ds-card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: 16px 18px;
  margin-bottom: 14px;
}
.ds-card > h3 { font-size: 14px; margin: 0 0 10px; }

.ds-card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
  margin-bottom: 10px;
}
.ds-card-head h3 { margin: 0; font-size: 14px; }
.ds-card-action { font-size: 12px; font-weight: 600; }

.ds-toolbar { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; justify-content: flex-end; flex: 1 1 auto; min-width: 0; }

/* One shared control for every in-page filter box
   (tree filter, issues filter). Narrow enough not to
   dominate a card header, wide enough to type a path. */
.ds-inline-filter {
  font-family: inherit;
  font-size: 12.5px;
  padding: 5px 10px;
  min-width: 130px;
  flex: 0 1 230px;
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  background: var(--surface-2);
  color: var(--text);
  outline: none;
}
.ds-inline-filter:focus { border-color: var(--accent); background: var(--surface); }
.ds-inline-filter::placeholder { color: var(--text-faint); }

.ds-chip-btn {
  font-size: 11.5px;
  font-weight: 600;
  padding: 4px 10px;
  border: 1px solid var(--border);
  background: var(--surface-2);
  color: var(--text-dim);
  border-radius: 999px;
  cursor: pointer;
  white-space: nowrap;
}
.ds-chip-btn:hover { background: var(--surface-3); color: var(--text); }

/* ==========================================
   Stat tiles
   ========================================== */
.ds-stat-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 10px; margin-bottom: 16px; }
.ds-stat-tile {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: 13px 15px;
  text-align: start;
}
.ds-stat-num { font-size: 24px; font-weight: 650; line-height: 1.1; letter-spacing: -.02em; font-variant-numeric: tabular-nums; }
.ds-stat-label { font-size: 11.5px; color: var(--text-dim); margin-top: 3px; font-weight: 500; }

.ds-stat-tile.ds-tile-mint .ds-stat-num { color: var(--ok); }
.ds-stat-tile.ds-tile-warn .ds-stat-num { color: var(--danger); }
.ds-stat-tile.ds-tile-lav .ds-stat-num { color: var(--accent); }
.ds-stat-tile.ds-tile-pink .ds-stat-num { color: var(--accent); }

/* ==========================================
   Rows (dashboard lists)
   ========================================== */
.ds-folder-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; }
.ds-folder-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  border-radius: var(--radius-sm);
  color: var(--text);
  border: 1px solid transparent;
  min-width: 0;
}
.ds-folder-row:hover { background: var(--surface-2); border-color: var(--border); text-decoration: none; }
.ds-folder-path {
  font-family: var(--font-mono);
  font-size: 12px;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.ds-folder-meta { margin-inline-start: auto; flex: none; font-size: 11.5px; color: var(--text-faint); font-variant-numeric: tabular-nums; }

/* ==========================================
   Trees — GameObject hierarchy and folder tree

   content-visibility on every node is the single
   biggest performance lever in the site: a Scene
   with 20 000 GameObjects otherwise makes the
   browser lay out 20 000 rows before it paints
   anything. With it, only what is near the viewport
   costs anything, and contain-intrinsic-size keeps
   the scrollbar from jumping while that happens.
   ========================================== */
.ds-tree, .ds-tree ul { list-style: none; margin: 0; padding-inline-start: 0; }
.ds-tree ul {
  padding-inline-start: 13px;
  margin-inline-start: 7px;
  border-inline-start: 1px solid var(--border);
}
.ds-tree ul ul ul ul { padding-inline-start: 9px; margin-inline-start: 4px; }
.ds-tree li { margin: 0; content-visibility: auto; contain-intrinsic-size: auto 30px; }

/* An open node holds real content, so it must not be
   skipped — otherwise the browser cannot scroll to a
   deep-linked child inside it. */
.ds-tree li:has(> details[open]) { content-visibility: visible; }

.ds-go > summary, .ds-go-leaf {
  cursor: pointer;
  list-style: none;
  display: flex;
  align-items: center;
  gap: 7px;
  padding: 4px 8px;
  border-radius: var(--radius-sm);
  font-weight: 500;
  font-size: 13px;
  min-width: 0;
}
.ds-go > summary::-webkit-details-marker { display: none; }
.ds-go > summary::before {
  content: '';
  flex: none;
  width: 0;
  height: 0;
  border-inline-start: 4.5px solid var(--text-faint);
  border-top: 4px solid transparent;
  border-bottom: 4px solid transparent;
  transition: transform .12s ease;
}
html[dir=rtl] .ds-go > summary::before { transform: scaleX(-1); }
.ds-go[open] > summary::before { transform: rotate(90deg); }
html[dir=rtl] .ds-go[open] > summary::before { transform: rotate(90deg) scaleX(-1); }
.ds-go > summary:hover, .ds-go-leaf:hover { background: var(--surface-2); }
.ds-go-leaf { padding-inline-start: 19.5px; cursor: default; }

.ds-go-inactive > summary, .ds-go-inactive.ds-go-leaf { color: var(--text-faint); }

.ds-go-tag {
  font-family: var(--font-mono);
  font-size: 10px;
  color: var(--text-dim);
  background: var(--surface-2);
  border: 1px solid var(--border);
  border-radius: 4px;
  padding: 0 5px;
  flex: none;
}

/* The deep-link flash. A link that lands on the right
   card but gives no sign of it makes the reader hunt
   the page anyway, which is the exact failure the
   issues page exists to fix. */
@keyframes ds-target-flash {
  0%   { background: var(--accent-soft); box-shadow: 0 0 0 3px var(--accent-soft); }
  100% { background: transparent; box-shadow: 0 0 0 3px transparent; }
}
.ds-target-hit { animation: ds-target-flash 1.8s ease-out 1; border-radius: var(--radius-sm); }
.ds-target-hit > details > summary,
.ds-target-hit > summary { background: var(--accent-soft) !important; }

/* Rows hidden by the in-page filter. display:none rather
   than visibility so the hidden rows cost no layout at
   all on a page with thousands of them. */
.ds-filtered-out { display: none !important; }

/* While a filter is active, an ancestor is only on screen
   to keep the path readable — its own Inspector body is
   not what was searched for, and on a Canvas holding a
   dozen components it buries the actual match under a
   screen of fields. Matches keep theirs. */
.ds-tree.is-filtering li:not(.ds-filter-hit) > details > .ds-go-card-body { display: none; }

/* ==========================================
   Detail bodies (GameObject / asset cards)
   ========================================== */
.ds-go-card, .ds-asset-card {
  scroll-margin-top: 14px;
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  background: var(--surface);
  margin-bottom: 12px;
  overflow: hidden;
}
.ds-go-card-head, .ds-asset-card-head {
  padding: 10px 14px;
  background: var(--surface-2);
  border-bottom: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  flex-wrap: wrap;
}
.ds-go-card-head h3, .ds-asset-card-head h3 { font-size: 14px; margin: 0; display: flex; align-items: center; gap: 7px; min-width: 0; overflow-wrap: anywhere; }
.ds-go-card-body, .ds-asset-card-body { padding: 10px 14px 14px; container-type: inline-size; }

/* A GameObject's detail body only exists once its node
   is open, but it is still the heaviest thing on the
   page, so it is skipped while off-screen too. */
.ds-go-card-body { content-visibility: auto; contain-intrinsic-size: auto 220px; }

/* ==========================================
   Collapsed asset cards — closed, each is one
   compact header row (name + type + size); open,
   it reveals the full info and takes the whole
   row so its tables have room.
   ========================================== */
details.ds-asset-card {
  margin-bottom: 0;
  content-visibility: auto;
  contain-intrinsic-size: auto 40px;
}
details.ds-asset-card[open] { contain-intrinsic-size: auto 480px; }
details.ds-asset-card > summary.ds-asset-card-head {
  cursor: pointer;
  list-style: none;
  padding: 7px 11px;
  background: var(--surface);
  border-bottom: none;
  justify-content: flex-start;
  flex-wrap: nowrap;
  overflow: hidden;
}
details.ds-asset-card[open] > summary.ds-asset-card-head { background: var(--surface-2); border-bottom: 1px solid var(--border); }
details.ds-asset-card > summary.ds-asset-card-head::-webkit-details-marker { display: none; }
details.ds-asset-card > summary.ds-asset-card-head::before {
  content: '';
  flex: none;
  width: 0;
  height: 0;
  border-inline-start: 4.5px solid var(--text-faint);
  border-top: 4px solid transparent;
  border-bottom: 4px solid transparent;
  transition: transform .12s ease;
}
html[dir=rtl] details.ds-asset-card > summary.ds-asset-card-head::before { transform: scaleX(-1); }
details.ds-asset-card[open] > summary.ds-asset-card-head::before { transform: rotate(90deg); }
html[dir=rtl] details.ds-asset-card[open] > summary.ds-asset-card-head::before { transform: rotate(90deg) scaleX(-1); }
details.ds-asset-card > summary.ds-asset-card-head:hover { background: var(--surface-2); }
details.ds-asset-card > summary.ds-asset-card-head h3 {
  font-size: 13px;
  font-weight: 500;
  flex: 1 1 auto;
  min-width: 0;
  display: block;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.ds-asset-head-meta {
  flex: none;
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-family: var(--font-mono);
  font-size: 10px;
  color: var(--text-faint);
  direction: ltr;
  unicode-bidi: isolate;
  white-space: nowrap;
}
.ds-asset-head-meta .t {
  background: var(--surface-2);
  border: 1px solid var(--border);
  border-radius: 999px;
  padding: 0 7px;
}

.ds-asset-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(340px, 100%), 1fr));
  gap: 6px;
  align-items: start;
}
.ds-asset-grid > * { min-width: 0; }
.ds-asset-grid > .ds-asset-card[open] { grid-column: 1 / -1; }

/* ==========================================
   Component cards
   ========================================== */
.ds-component {
  border: 1px solid var(--border);
  border-inline-start: 3px solid var(--border-strong);
  border-radius: var(--radius-md);
  margin: 8px 0;
  overflow: hidden;
  content-visibility: auto;
  contain-intrinsic-size: auto 120px;
}
.ds-component.is-user-script { border-inline-start-color: var(--accent); }
.ds-component.is-missing { border-inline-start-color: var(--danger); background: var(--danger-soft); }
.ds-component-head {
  padding: 7px 12px;
  background: var(--surface-2);
  display: flex;
  align-items: center;
  gap: 7px;
  font-weight: 600;
  font-size: 12.5px;
  flex-wrap: wrap;
}
.ds-component.is-missing .ds-component-head { background: transparent; color: var(--danger); }
.ds-component-toggle { margin-inline-start: auto; font-family: var(--font-mono); font-size: 10px; font-weight: 700; padding: 1px 7px; border-radius: 999px; }
.ds-component-toggle.on { background: var(--ok-soft); color: var(--ok); }
.ds-component-toggle.off { background: var(--surface-3); color: var(--text-faint); }

.ds-transform-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 8px; margin: 10px 0; }
.ds-transform-tile { background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius-sm); padding: 8px 10px; }
.ds-transform-tile .lbl { font-weight: 600; font-size: 10.5px; color: var(--text-faint); display: block; margin-bottom: 3px; text-transform: uppercase; letter-spacing: .05em; }
.ds-vec3 { display: flex; flex-wrap: wrap; gap: 5px; font-family: var(--font-mono); font-size: 12px; direction: ltr; unicode-bidi: isolate; }
.ds-vec3 b { color: var(--text-faint); font-weight: 600; }

/* ==========================================
   Field grid — each .ds-field-grid builds its own
   column tracks from its own available width, so
   nesting one inside another can never compound
   into an ever-narrower fixed percentage the way
   nested <table>s used to. Rows use
   display:contents so their children become direct
   grid items of the surrounding grid.
   ========================================== */
.ds-field-grid {
  display: grid;
  grid-template-columns: minmax(0, 30%) minmax(0, 16%) minmax(0, 1fr);
  column-gap: 12px;
  width: 100%;
  min-width: 0;
  font-size: 12.5px;
}
.ds-field-grid-head, .ds-field-grid-row { display: contents; }
.ds-field-grid-head > span {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: .06em;
  text-transform: uppercase;
  color: var(--text-faint);
  padding: 5px 0;
  border-bottom: 1px solid var(--border);
}
.ds-field-grid-row > div {
  padding: 5px 0;
  border-bottom: 1px solid var(--border);
  min-width: 0;
  align-self: start;
}
.ds-field-grid-row:last-child > div { border-bottom: none; }
.ds-field-name { font-weight: 600; overflow-wrap: anywhere; }
.ds-field-type { color: var(--text-faint); font-family: var(--font-mono); font-size: 11px; overflow-wrap: anywhere; }
.ds-field-value {
  font-family: var(--font-mono);
  font-size: 12px;
  min-width: 0;
  overflow-wrap: anywhere;
  direction: ltr;
  unicode-bidi: isolate;
  text-align: start;
}

/* Narrow-container fallback. Every selector is prefixed
   with .ds-field-grid so it outranks the unprefixed base
   rules above on specificity rather than on source order:
   @container adds no specificity of its own. */
@container (max-width: 340px) {
  .ds-field-grid.ds-field-grid { grid-template-columns: minmax(0, 1fr); }
  .ds-field-grid .ds-field-grid-head { display: none; }
  .ds-field-grid .ds-field-grid-row > div { border-bottom: none; padding: 0; }
  .ds-field-grid .ds-field-grid-row > .ds-field-name { padding-top: 7px; }
  .ds-field-grid .ds-field-grid-row > .ds-field-type { padding-bottom: 2px; }
  .ds-field-grid .ds-field-grid-row > .ds-field-value { padding-bottom: 7px; border-bottom: 1px solid var(--border); }
  .ds-field-grid .ds-field-grid-row:last-child > .ds-field-value { border-bottom: none; }
}

@supports not (container-type: inline-size) {
  @media (max-width: 620px) {
    .ds-field-grid.ds-field-grid { grid-template-columns: minmax(0, 1fr); }
    .ds-field-grid .ds-field-grid-head { display: none; }
    .ds-field-grid .ds-field-grid-row > div { border-bottom: none; padding: 0; }
    .ds-field-grid .ds-field-grid-row > .ds-field-name { padding-top: 7px; }
    .ds-field-grid .ds-field-grid-row > .ds-field-type { padding-bottom: 2px; }
    .ds-field-grid .ds-field-grid-row > .ds-field-value { padding-bottom: 7px; border-bottom: 1px solid var(--border); }
  }
}

.ds-nested-block {
  margin: 3px 0;
  padding: 7px 9px;
  background: var(--surface-2);
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  max-width: 100%;
  overflow-x: auto;
}
.ds-nested-block-title { font-weight: 600; padding: 1px 0 6px; overflow-wrap: anywhere; }

/* ==========================================
   Values
   ========================================== */
.ds-pill { display: inline-flex; align-items: center; gap: 3px; padding: 1px 8px; border-radius: 999px; font-size: 11px; font-weight: 600; font-family: var(--font-body); }
.ds-pill.bool-true { background: var(--ok-soft); color: var(--ok); }
.ds-pill.bool-false { background: var(--surface-3); color: var(--text-faint); }
.ds-pill.enum { background: var(--accent-soft); color: var(--accent); }
.ds-swatch { display: inline-block; width: 12px; height: 12px; border-radius: 3px; border: 1px solid var(--border-strong); vertical-align: -2px; margin-inline-end: 5px; }

.ds-ref-chip {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 9px;
  border-radius: 999px;
  background: var(--accent-soft);
  color: var(--accent);
  font-weight: 600;
  font-size: 11.5px;
  font-family: var(--font-body);
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
a.ds-ref-chip:hover { background: var(--accent); color: var(--accent-contrast); text-decoration: none; }
.ds-ref-chip.is-missing { background: var(--danger-soft); color: var(--danger); }
.ds-ref-chip.is-unresolved { background: var(--surface-2); color: var(--text-faint); }
.ds-ref-chip .type { opacity: .75; font-weight: 500; font-size: 10px; }

.ds-array-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(88px, 1fr));
  gap: 4px;
  max-height: 260px;
  overflow-y: auto;
  padding: 6px;
  background: var(--surface-2);
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
}
.ds-array-cell {
  display: flex;
  flex-direction: column;
  gap: 0;
  min-width: 0;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 5px;
  padding: 3px 7px;
  overflow: hidden;
}
.ds-array-cell .idx { font-size: 9px; font-weight: 700; color: var(--text-faint); line-height: 1.3; font-family: var(--font-mono); }
.ds-array-cell .val {
  font-family: var(--font-mono);
  font-size: 11.5px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
  direction: ltr;
  unicode-bidi: isolate;
  text-align: start;
}

.ds-matrix-scroll {
  overflow: auto;
  max-height: 320px;
  background: var(--surface-2);
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  padding: 6px;
}
.ds-matrix-table { border-collapse: separate; border-spacing: 2px; font-family: var(--font-mono); font-size: 11px; }
.ds-matrix-table thead th {
  position: sticky;
  top: 0;
  background: var(--surface-2);
  color: var(--text-faint);
  font-size: 9.5px;
  font-weight: 700;
  padding: 2px 7px;
  z-index: 1;
}
.ds-matrix-table td {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 4px;
  padding: 2px 8px;
  white-space: nowrap;
  text-align: end;
}
.ds-matrix-row-head {
  position: sticky;
  inset-inline-start: 0;
  background: var(--surface-2) !important;
  text-align: center !important;
  z-index: 1;
}

.ds-array-block-item { margin: 5px 0; padding: 5px 7px; background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius-sm); max-width: 100%; overflow-x: auto; }
.ds-array-more { color: var(--text-faint); font-size: 11px; margin-top: 5px; }
.ds-empty-note { color: var(--text-faint); font-size: 12.5px; padding: 6px 0; margin: 0; }

/* ==========================================
   Thumbnails and media
   ========================================== */
.ds-thumb {
  position: relative;
  width: 100%;
  aspect-ratio: 4 / 3;
  border-radius: var(--radius-sm);
  background:
    repeating-conic-gradient(var(--surface-2) 0% 25%, var(--surface) 0% 50%) 0 0 / 16px 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 30px;
  margin-bottom: 9px;
  border: 1px solid var(--border);
  overflow: hidden;
}
.ds-thumb img { width: 100%; height: 100%; object-fit: contain; display: block; }
.ds-thumb.is-icon { aspect-ratio: auto; min-height: 68px; }
.ds-thumb.is-icon img { width: auto; height: auto; max-width: 44px; max-height: 44px; image-rendering: pixelated; }

.ds-media { width: 100%; display: block; margin: 0 0 9px; border-radius: var(--radius-sm); background: var(--surface-2); }
video.ds-media { max-height: 260px; }
.ds-file-actions { display: flex; flex-wrap: wrap; gap: 5px; margin: 2px 0 8px; }
.ds-file-link {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 11px;
  font-weight: 600;
  padding: 3px 10px;
  border-radius: 999px;
  background: var(--surface-2);
  border: 1px solid var(--border);
  color: var(--text-dim);
  white-space: nowrap;
}
.ds-file-link:hover { background: var(--accent-soft); border-color: var(--accent-border); color: var(--accent); text-decoration: none; }

/* ==========================================
   Collapsible detail sections
   ========================================== */
.ds-detail { margin-top: 8px; border-top: 1px solid var(--border); }
.ds-detail > summary {
  cursor: pointer;
  list-style: none;
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 7px 0 5px;
  font-weight: 600;
  font-size: 12.5px;
  color: var(--text-dim);
}
.ds-detail > summary::-webkit-details-marker { display: none; }
.ds-detail > summary::before {
  content: '';
  flex: none;
  width: 0;
  height: 0;
  border-inline-start: 4.5px solid var(--text-faint);
  border-top: 4px solid transparent;
  border-bottom: 4px solid transparent;
  transition: transform .12s ease;
}
html[dir=rtl] .ds-detail > summary::before { transform: scaleX(-1); }
.ds-detail[open] > summary::before { transform: rotate(90deg); }
html[dir=rtl] .ds-detail[open] > summary::before { transform: rotate(90deg) scaleX(-1); }
.ds-detail > summary:hover { color: var(--accent); }
.ds-detail-body { padding-bottom: 5px; min-width: 0; }

.ds-kv-line {
  display: grid;
  grid-template-columns: minmax(60px, auto) minmax(0, 1fr);
  column-gap: 10px;
  align-items: baseline;
  font-size: 12px;
  padding: 3px 0;
  border-bottom: 1px solid var(--border);
  min-width: 0;
}
.ds-kv-line:last-child { border-bottom: none; }
.ds-kv-line .k { color: var(--text-faint); font-weight: 500; min-width: 0; overflow-wrap: anywhere; }
.ds-kv-line .v {
  font-family: var(--font-mono);
  text-align: end;
  overflow-wrap: anywhere;
  min-width: 0;
  direction: ltr;
  unicode-bidi: isolate;
}

/* ==========================================
   Project health / issues page
   The point of the page is that every row is a
   link that lands on the actual broken thing, so
   rows are sized and spaced for scanning a long
   list rather than for decoration.
   ========================================== */
.ds-issue-tiles { grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); }
.ds-issue-tile {
  font: inherit;
  cursor: pointer;
  border: 1px solid var(--border);
  transition: border-color .12s ease, background .12s ease;
}
.ds-issue-tile:hover { border-color: var(--border-strong); background: var(--surface-2); }
.ds-issue-tile.is-active { border-color: var(--accent); background: var(--accent-soft); }

/* ==========================================
   Segmented control (issues ownership tabs)
   ========================================== */
.ds-segmented {
  display: inline-flex;
  gap: 2px;
  padding: 2px;
  background: var(--surface-2);
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  flex: none;
}
.ds-seg-btn {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 11.5px;
  font-weight: 600;
  padding: 4px 10px;
  border: none;
  background: transparent;
  color: var(--text-dim);
  border-radius: var(--radius-sm);
  cursor: pointer;
  white-space: nowrap;
}
.ds-seg-btn:hover { color: var(--text); background: var(--surface-3); }
.ds-seg-btn.is-active { background: var(--surface); color: var(--text); box-shadow: var(--shadow); }
:root[data-theme=dark] .ds-seg-btn.is-active { background: var(--surface-3); }
.ds-seg-count {
  font-family: var(--font-mono);
  font-size: 10px;
  color: var(--text-faint);
  background: var(--surface-3);
  border-radius: 999px;
  padding: 0 5px;
}
.ds-seg-btn.is-active .ds-seg-count { background: var(--accent-soft); color: var(--accent); }

.ds-stat-aside { display: block; font-size: 10.5px; color: var(--text-faint); margin-top: 3px; }
.ds-callout.ok { background: var(--ok-soft); border-color: var(--ok-border); }

.ds-issue-list { list-style: none; margin: 0; padding: 0; }
.ds-issue-row { border-bottom: 1px solid var(--border); content-visibility: auto; contain-intrinsic-size: auto 46px; }
.ds-issue-row:last-child { border-bottom: none; }

.ds-issue-link {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 8px;
  color: var(--text);
  border-radius: var(--radius-sm);
  min-width: 0;
}
.ds-issue-link:hover { background: var(--surface-2); text-decoration: none; }

.ds-issue-icon {
  flex: none;
  width: 24px;
  height: 24px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-sm);
  font-size: 12px;
  background: var(--surface-3);
}
.ds-issue-missingScript .ds-issue-icon { background: var(--danger-soft); color: var(--danger); }
.ds-issue-missingReference .ds-issue-icon { background: var(--danger-soft); color: var(--danger); }
.ds-issue-unresolvedAsset .ds-issue-icon { background: var(--info-soft); color: var(--info); }

.ds-issue-main { flex: 1 1 auto; min-width: 0; display: flex; flex-direction: column; }
.ds-issue-where {
  font-family: var(--font-mono);
  font-size: 12.5px;
  font-weight: 600;
  overflow-wrap: anywhere;
  direction: ltr;
  unicode-bidi: isolate;
  text-align: start;
}
.ds-issue-detail {
  font-size: 11.5px;
  color: var(--text-dim);
  overflow-wrap: anywhere;
  direction: ltr;
  unicode-bidi: isolate;
  text-align: start;
}
.ds-issue-side { flex: none; display: flex; flex-direction: column; align-items: flex-end; gap: 2px; text-align: end; }
html[dir=rtl] .ds-issue-side { align-items: flex-start; }
.ds-issue-kind { font-size: 11px; font-weight: 600; color: var(--text-dim); white-space: nowrap; }
.ds-issue-scope {
  font-family: var(--font-mono);
  font-size: 10px;
  color: var(--text-faint);
  background: var(--surface-2);
  border: 1px solid var(--border);
  border-radius: 999px;
  padding: 0 7px;
  max-width: 190px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  direction: ltr;
  unicode-bidi: isolate;
}
/* A finding in a folder Unity or a package installed. Dimmed on
   purpose: it is shown for completeness, not for action. */
.ds-issue-scope.is-vendor { border-style: dashed; }
.ds-issue-row[data-issue-owner=vendor] .ds-issue-icon { opacity: .55; }
.ds-issue-row[data-issue-owner=vendor] .ds-issue-where { font-weight: 500; color: var(--text-dim); }

.ds-allclear { text-align: center; padding: 34px 20px; }
.ds-allclear-mark {
  width: 46px;
  height: 46px;
  margin: 0 auto 12px;
  border-radius: 50%;
  background: var(--ok-soft);
  color: var(--ok);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 22px;
  font-weight: 700;
}
.ds-allclear h3 { margin-bottom: 4px; }

/* ==========================================
   Prefab markers
   ========================================== */
.ds-override-dot { color: var(--accent); font-size: 8px; vertical-align: 2px; }
.ds-field-grid-row.is-override > .ds-field-name { color: var(--accent); }
.ds-prefab-tag {
  font-size: 10px;
  font-weight: 600;
  color: var(--accent);
  background: var(--accent-soft);
  border: 1px solid var(--accent-border);
  border-radius: 999px;
  padding: 0 7px;
}
.ds-prefab-mark { font-size: 11px; opacity: .85; cursor: help; flex: none; }

/* ==========================================
   Packages page
   ========================================== */
.ds-pkg-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(320px, 100%), 1fr));
  gap: 10px;
  align-items: start;
  margin-top: 10px;
}
.ds-pkg-card {
  border: 1px solid var(--border);
  border-inline-start: 3px solid var(--border-strong);
  border-radius: var(--radius-md);
  background: var(--surface);
  overflow: hidden;
  min-width: 0;
}
.ds-pkg-head {
  padding: 10px 14px 8px;
  background: var(--surface-2);
  border-bottom: 1px solid var(--border);
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.ds-pkg-head h4 { margin: 0; font-size: 13.5px; overflow-wrap: anywhere; }
.ds-pkg-body { padding: 9px 14px 12px; }
.ds-pkg-id { font-family: var(--font-mono); font-size: 11px; color: var(--text-faint); overflow-wrap: anywhere; direction: ltr; unicode-bidi: isolate; }
.ds-pkg-author { font-size: 11px; color: var(--text-faint); margin-top: 3px; }
.ds-pkg-desc { font-size: 12px; color: var(--text-dim); margin: 7px 0 3px; }
.ds-module-list { list-style: none; margin: 7px 0 0; padding: 0; display: grid; grid-template-columns: repeat(auto-fill, minmax(min(240px, 100%), 1fr)); gap: 2px 14px; }
.ds-module-list li { display: flex; gap: 8px; align-items: baseline; font-size: 11.5px; }
.ds-module-name { color: var(--text-dim); overflow-wrap: anywhere; }
.ds-module-ver { margin-inline-start: auto; font-family: var(--font-mono); font-size: 10px; color: var(--text-faint); }

/* ==========================================
   Export info card
   ========================================== */
.ds-info-lines { display: flex; flex-direction: column; }
.ds-info-line {
  display: grid;
  grid-template-columns: minmax(120px, auto) minmax(0, 1fr);
  column-gap: 12px;
  align-items: baseline;
  font-size: 12.5px;
  padding: 5px 0;
  border-bottom: 1px solid var(--border);
}
.ds-info-line:last-child { border-bottom: none; }
.ds-info-key { color: var(--text-faint); font-weight: 500; }
.ds-info-val { font-family: var(--font-mono); font-size: 12px; overflow-wrap: anywhere; direction: ltr; unicode-bidi: isolate; text-align: start; }
.ds-info-tz { color: var(--text-faint); font-size: 10.5px; }

/* ==========================================
   Changes page
   ========================================== */
.ds-diff-list {
  list-style: none;
  margin: 6px 0 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
  max-height: 440px;
  overflow-y: auto;
  overscroll-behavior: contain;
}
.ds-diff-item {
  font-family: var(--font-mono);
  font-size: 11.5px;
  padding: 4px 9px;
  border-radius: var(--radius-sm);
  border-inline-start: 3px solid var(--border);
  background: var(--surface-2);
  direction: ltr;
  unicode-bidi: isolate;
  text-align: start;
  overflow-wrap: anywhere;
  display: flex;
  align-items: baseline;
  gap: 10px;
  flex-wrap: wrap;
  content-visibility: auto;
  contain-intrinsic-size: auto 26px;
}
.ds-diff-pathwrap { flex: 1 1 auto; min-width: 0; overflow-wrap: anywhere; }
.ds-diff-dir { color: var(--text-faint); }
.ds-diff-file { font-weight: 700; }
.ds-diff-size { flex: none; margin-inline-start: auto; font-size: 10.5px; color: var(--text-faint); white-space: nowrap; }
.ds-diff-size .plus { color: var(--ok); font-weight: 700; }
.ds-diff-size .minus { color: var(--danger); font-weight: 700; }
.ds-diff-links { flex: none; display: inline-flex; gap: 4px; }
.ds-diff-item .ds-file-link { font-size: 10px; padding: 1px 8px; }
.ds-diff-item.ds-diff-added { border-inline-start-color: var(--ok); }
.ds-diff-item.ds-diff-removed { border-inline-start-color: var(--danger); }
.ds-diff-item.ds-diff-changed { border-inline-start-color: var(--accent); }
.ds-diff-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 22px;
  height: 19px;
  padding: 0 6px;
  border-radius: 999px;
  font-family: var(--font-mono);
  font-size: 10.5px;
  font-weight: 700;
  color: var(--accent-contrast);
}
.ds-diff-badge.ds-diff-added { background: var(--ok); }
.ds-diff-badge.ds-diff-removed { background: var(--danger); }
.ds-diff-badge.ds-diff-changed { background: var(--accent); }

/* ==========================================
   Callouts, footer, back-to-top
   ========================================== */
.ds-callout {
  border-radius: var(--radius-md);
  padding: 11px 14px;
  background: var(--info-soft);
  border: 1px solid var(--info-border);
  color: var(--text);
  font-size: 12.5px;
  margin-bottom: 14px;
}
.ds-callout.warn { background: var(--danger-soft); border-color: var(--danger-border); }

.ds-footer {
  margin-top: 32px;
  padding-top: 14px;
  border-top: 1px solid var(--border);
  color: var(--text-faint);
  font-size: 12px;
  display: flex;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px;
}

.ds-back-top {
  position: fixed;
  inset-block-end: 20px;
  inset-inline-end: 22px;
  background: var(--surface);
  color: var(--text-dim);
  border: 1px solid var(--border-strong);
  width: 34px;
  height: 34px;
  border-radius: 50%;
  font-size: 14px;
  line-height: 1;
  padding: 0;
  display: none;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  box-shadow: var(--shadow-pop);
  z-index: 30;
}
.ds-back-top:hover { color: var(--accent); border-color: var(--accent); }

/* ==========================================
   Bidi isolation — Latin data inside an RTL
   document (paths, GUIDs, type names, numbers).
   Without an explicit LTR isolate these get
   reordered into unreadable fragments the moment
   the UI language is switched to Persian.
   ========================================== */
[data-en] { unicode-bidi: isolate; }
.ds-folder-path,
.ds-go-tag,
.ds-field-type,
.ds-matrix-table,
.ds-ref-chip,
code, kbd, samp, pre { direction: ltr; unicode-bidi: isolate; }
.ds-asset-card-head h3, .ds-go-card-head h3 { unicode-bidi: isolate; }

/* ==========================================
   Responsive
   ========================================== */
@media (max-width: 1000px) {
  .ds-main { padding: 20px 20px 60px; }
}

@media (max-width: 860px) {
  .ds-shell { flex-direction: column; }
  .ds-sidebar {
    width: 100%;
    flex-basis: auto;
    position: relative;
    height: auto;
    max-height: none;
    border-inline-end: none;
    border-bottom: 1px solid var(--border);
  }
  .ds-nav-scroll { max-height: none; }
  .ds-main { padding: 18px 14px 50px; }
  .ds-page-header h1 { font-size: 19px; }
  .ds-search-results { position: static; max-height: 40vh; box-shadow: none; }
  body.ds-sidebar-collapsed .ds-sidebar { display: block; }
  .ds-sidebar-reopen { display: none !important; }
}

/* ==========================================
   Print — someone will hand this to a reviewer.
   ========================================== */
@media print {
  .ds-sidebar, .ds-back-top, .ds-search, .ds-toolbar, .ds-icon-btn { display: none !important; }
  .ds-main { padding: 0; }
  .ds-card, .ds-component, .ds-asset-card { break-inside: avoid; border-color: #ccc; }
  details { display: block; }
  details > *:not(summary) { display: block !important; }
}

/* ==========================================
   THE COZY SKIN
   ==========================================
   The original Unity DocSnap look, kept as a
   first-class option rather than deleted: pastel
   gradients, generous radii, soft shadows, the
   rounded display face, and a mascot that bobs.
   It is the signature of the tool and the nicer
   thing to sit in front of.

   It is also strictly more paint work per row,
   which stops being free somewhere between a demo
   Scene and a project with forty thousand
   GameObjects on one page. So which skin an export
   OPENS with is measured at export time
   (DocSnapCapability: RAM, cores, GPU, plus how
   heavy the project is) and re-checked in the
   browser against the machine actually reading the
   page. The reader can always switch, and is shown
   the numbers when they override a ""lite"" verdict.

   Everything below is a token override plus the
   handful of surfaces that carry actual decoration.
   The layout, the markup and the
   content-visibility performance work are shared
   with the lite skin, so cozy is a change of
   clothes rather than a second stylesheet.
   ========================================== */
:root[data-skin=cozy] {
  --bg: #fffaf3;
  --surface: #ffffff;
  --surface-2: #fff3e6;
  --surface-3: #ffe6f1;
  --border: #f3e2ea;
  --border-strong: #f0c8dc;

  --text: #4a3b52;
  --text-dim: #8a7a92;
  --text-faint: #b9adc2;

  --accent: #9678c2;
  --accent-hover: #ff8fa3;
  --accent-soft: #f3ecfd;
  --accent-border: #cdb9ec;
  --accent-contrast: #ffffff;

  --danger: #c9524f;
  --danger-soft: #fff0ee;
  --danger-border: #ffc4bd;

  --ok: #3f7d4b;
  --ok-soft: #eafce9;
  --ok-border: #a8e2a4;

  --info: #6b73c8;
  --info-soft: #eef0fd;
  --info-border: #c9cef3;

  --radius-lg: 20px;
  --radius-md: 14px;
  --radius-sm: 9px;

  --shadow: 0 4px 14px rgba(177, 156, 217, .13);
  --shadow-pop: 0 12px 34px rgba(255, 143, 163, .26);

  /* The embedded faces come FIRST here — this skin is the
     branded one, and the rounded display face is most of
     what makes it read as itself. */
  --font-body: 'Quicksand', 'Vazirmatn', system-ui, -apple-system, 'Segoe UI', sans-serif;
  --font-display: 'Baloo 2', 'Vazirmatn', 'Quicksand', system-ui, sans-serif;
  --font-mono: 'Space Mono', ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
}

:root[data-skin=cozy][data-theme=dark] {
  --bg: #1c1922;
  --surface: #241f2d;
  --surface-2: #2a2533;
  --surface-3: #362e42;
  --border: #38313f;
  --border-strong: #4b4155;

  --text: #ece7f2;
  --text-dim: #b3a8c2;
  --text-faint: #8b8098;

  --accent: #c9b8f0;
  --accent-hover: #ff9db0;
  --accent-soft: #2c2740;
  --accent-border: #453a5e;
  --accent-contrast: #241f2d;

  --danger: #ff9d9d;
  --danger-soft: #3a2626;
  --danger-border: #573535;

  --ok: #8fd98c;
  --ok-soft: #24331f;
  --ok-border: #3a5233;

  --info: #a9b0f0;
  --info-soft: #232840;
  --info-border: #383f63;

  --shadow: 0 4px 14px rgba(0, 0, 0, .4);
  --shadow-pop: 0 12px 34px rgba(0, 0, 0, .55);
}

:root[data-skin=cozy]:lang(ja) {
  --font-body: 'Hiragino Maru Gothic ProN', 'Hiragino Kaku Gothic ProN', 'Yu Gothic', 'Meiryo', system-ui, sans-serif;
  --font-display: 'Hiragino Maru Gothic ProN', 'Hiragino Kaku Gothic ProN', 'Yu Gothic', 'Meiryo', system-ui, sans-serif;
}
:root[data-skin=cozy]:lang(fa) {
  --font-body: 'Vazirmatn', 'Quicksand', system-ui, sans-serif;
  --font-display: 'Vazirmatn', 'Baloo 2', system-ui, sans-serif;
}

/* ---------- Decoration ---------- */

/* The pastel wash behind the nav, which is the single
   most recognisable thing about the original look. */
:root[data-skin=cozy] .ds-sidebar {
  background: linear-gradient(180deg, #ffe6f1 0%, #fff3e6 46%, #fffaf3 100%);
}
:root[data-skin=cozy][data-theme=dark] .ds-sidebar {
  background: linear-gradient(180deg, #2f2438 0%, #262030 48%, #1c1922 100%);
}

:root[data-skin=cozy] .ds-brand-text h1 { font-size: 17px; }

/* Gradient card heads, back where they were. */
:root[data-skin=cozy] .ds-go-card-head,
:root[data-skin=cozy] .ds-asset-card-head,
:root[data-skin=cozy] .ds-pkg-head,
:root[data-skin=cozy] details.ds-asset-card[open] > summary.ds-asset-card-head {
  background: linear-gradient(120deg, var(--surface-3), var(--surface-2));
}

:root[data-skin=cozy] .ds-card,
:root[data-skin=cozy] .ds-stat-tile { box-shadow: var(--shadow); }

/* Lift on hover. Confined to things a reader points at
   deliberately — rows, tiles, cards — and never applied to
   a tree node, because there are tens of thousands of those
   and a transform on each is exactly the cost this skin has
   to stay honest about. */
:root[data-skin=cozy] .ds-stat-tile,
:root[data-skin=cozy] .ds-folder-row,
:root[data-skin=cozy] .ds-version-row {
  transition: transform .16s ease, box-shadow .16s ease, background .16s ease;
}
:root[data-skin=cozy] .ds-stat-tile:hover,
:root[data-skin=cozy] .ds-folder-row:hover,
:root[data-skin=cozy] .ds-version-row:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-pop);
}

:root[data-skin=cozy] .ds-stat-num { font-family: var(--font-display); font-size: 27px; }
:root[data-skin=cozy] .ds-page-header h1 { font-size: 27px; }
:root[data-skin=cozy] .ds-nav-title { text-transform: none; letter-spacing: .02em; font-size: 12.5px; color: var(--accent); }

:root[data-skin=cozy] .ds-badge { border-radius: 999px; padding: 4px 12px; }
:root[data-skin=cozy] .ds-lang-btn,
:root[data-skin=cozy] .ds-mode-btn,
:root[data-skin=cozy] .ds-seg-btn { border-radius: 999px; }
:root[data-skin=cozy] .ds-langbar,
:root[data-skin=cozy] .ds-modebar,
:root[data-skin=cozy] .ds-segmented { border-radius: 999px; }
:root[data-skin=cozy] .ds-search-input,
:root[data-skin=cozy] .ds-inline-filter { border-radius: 999px; }

:root[data-skin=cozy] .ds-back-top {
  background: var(--accent-hover);
  color: #fff;
  border-color: transparent;
  width: 42px;
  height: 42px;
  font-size: 17px;
}

/* Dashed dividers, which is how the original separated
   things that were only loosely related.
   Per-SIDE on purpose: `border-style: dashed` on its own
   also activates the other three sides, whose width is the
   `medium` default the moment their style stops being
   `none` - which drew a full 3px box around the sidebar
   footer instead of a rule above it. */
:root[data-skin=cozy] .ds-footer,
:root[data-skin=cozy] .ds-sidebar-footer { border-top-color: var(--border-strong); border-top-style: dashed; }
:root[data-skin=cozy] .ds-kv-line,
:root[data-skin=cozy] .ds-info-line { border-bottom-color: var(--border-strong); border-bottom-style: dashed; }
:root[data-skin=cozy] .ds-kv-line:last-child,
:root[data-skin=cozy] .ds-info-line:last-child { border-bottom-style: none; }
:root[data-skin=cozy] .ds-tree ul { border-inline-start-style: dashed; }
:root[data-skin=cozy] .ds-detail { border-top-color: var(--border-strong); border-top-style: dashed; }

:root[data-skin=cozy] .ds-empty-note { font-style: italic; }

/* ---------- The mascot ---------- */
/* A slow bob, the boba pearls drifting inside the cup, and
   a sparkle that twinkles. One element each, all transform
   and opacity only, so the whole thing is a compositor job
   that never touches layout. Sat still in the lite skin. */
@keyframes ds-bob {
  0%, 100% { transform: translateY(0) rotate(-1.5deg); }
  50%      { transform: translateY(-3px) rotate(1.5deg); }
}
@keyframes ds-boba-drift {
  0%, 100% { transform: translateY(0); }
  50%      { transform: translateY(-2.5px); }
}
@keyframes ds-twinkle {
  0%, 100% { opacity: .45; transform: scale(.9) rotate(0deg); }
  50%      { opacity: 1;   transform: scale(1.12) rotate(18deg); }
}

:root[data-skin=cozy] .ds-logo {
  animation: ds-bob 4.5s ease-in-out infinite;
  transform-origin: 50% 70%;
  will-change: transform;
}
:root[data-skin=cozy] .ds-logo .ds-boba {
  animation: ds-boba-drift 3.2s ease-in-out infinite;
  transform-origin: 50% 78%;
}
:root[data-skin=cozy] .ds-logo .ds-sparkle {
  animation: ds-twinkle 2.6s ease-in-out infinite;
  transform-origin: 16px 74px;
}

/* Someone who has asked their OS for less motion has already
   answered this question. */
@media (prefers-reduced-motion: reduce) {
  :root[data-skin=cozy] .ds-logo,
  :root[data-skin=cozy] .ds-logo .ds-boba,
  :root[data-skin=cozy] .ds-logo .ds-sparkle { animation: none; }
}

/* ==========================================
   Skin switch + the override warning
   ========================================== */
.ds-skinbar { display: flex; gap: 2px; padding: 2px; margin: 0 4px 12px; background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius-md); }
.ds-skin-btn {
  flex: 1;
  font-size: 11.5px;
  font-weight: 600;
  padding: 5px 4px;
  border: none;
  background: transparent;
  color: var(--text-dim);
  border-radius: var(--radius-sm);
  cursor: pointer;
  white-space: nowrap;
}
.ds-skin-btn:hover { color: var(--text); background: var(--surface-3); }
.ds-skin-btn.is-active { background: var(--surface); color: var(--text); box-shadow: var(--shadow); }
:root[data-theme=dark] .ds-skin-btn.is-active { background: var(--surface-3); }
:root[data-skin=cozy] .ds-skinbar,
:root[data-skin=cozy] .ds-skin-btn { border-radius: 999px; }

/* Shown only when the reader turns the cozy skin ON against
   a lite verdict. It carries the actual measurements rather
   than a vague ""this may be slow"", because a warning without
   numbers is a shrug. */
.ds-skin-warning {
  margin: 0 4px 12px;
  padding: 9px 11px;
  border-radius: var(--radius-md);
  background: var(--danger-soft);
  border: 1px solid var(--danger-border);
  font-size: 11.5px;
  color: var(--text);
}
.ds-skin-warning[hidden] { display: none; }
.ds-skin-warning strong { display: block; margin-bottom: 3px; color: var(--danger); }
.ds-skin-warning ul { margin: 4px 0 0; padding-inline-start: 16px; }
.ds-skin-warning li { margin: 1px 0; }
.ds-skin-specs {
  margin-top: 5px;
  font-family: var(--font-mono);
  font-size: 10px;
  color: var(--text-dim);
  direction: ltr;
  unicode-bidi: isolate;
  overflow-wrap: anywhere;
}
";

        // ==========================================
        // AppJs - theme/app.js contents
        // ==========================================
        public const string AppJs = @"// ==========================================
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
  // attached, because ""this might be slow"" without them is
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
      html += '<div class=""ds-skin-specs"">' + esc(specs.join('  ·  ')) + '</div>';
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
      var countEl = document.querySelector('[data-tree-filter-count=""' + cssEscape(input.getAttribute('data-tree-filter')) + '""]');

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
    return String(value === null || value === undefined ? '' : value).replace(/[""\\]/g, '\\$&');
  }

  // ==========================================
  // Issues page filtering
  // The kind tiles and the text box narrow the same
  // list. Both are pre-applied from the query string so
  // the dashboard can link straight to ""just the broken
  // references in MainMenu"".
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
      .split('""').join('&quot;')
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
        panel.innerHTML = '<div class=""ds-search-empty"">' + esc(t('No matches', 'ヒットなし', 'موردی پیدا نشد')) + '</div>';
        return;
      }
      var html = '';
      for (var i = 0; i < items.length; i++) {
        var r = items[i].r;
        html += '<a class=""ds-search-result"" href=""' + esc(prefix + (r.u || '')) + '"">'
          + '<span class=""r-top""><span class=""r-name"">' + highlight(r.n, q) + '</span>'
          + '<span class=""r-cat"">' + esc(r.c) + '</span></span>'
          + '<span class=""r-sub"">' + highlight(r.s, q) + '</span></a>';
      }
      if (matched > items.length) {
        html += '<div class=""ds-search-more"">+' + (matched - items.length) + ' '
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
";

        public const string LogoMarkSvg = @"<svg class=""ds-logo"" viewBox=""0 0 100 100"" xmlns=""http://www.w3.org/2000/svg"" role=""img"" aria-label=""Unity DocSnap mascot"">
  <polygon points=""62,10 84,32 68,30 66,15"" fill=""#ffe08a"" stroke=""#4a3b52"" stroke-width=""2.5"" stroke-linejoin=""round""/>
  <line x1=""59"" y1=""34"" x2=""72"" y2=""12"" stroke=""#4a3b52"" stroke-width=""4.5"" stroke-linecap=""round""/>
  <line x1=""59"" y1=""34"" x2=""72"" y2=""12"" stroke=""#ff8fa3"" stroke-width=""3"" stroke-linecap=""round""/>
  <rect x=""53"" y=""30"" width=""7"" height=""7"" rx=""2"" transform=""rotate(45 56.5 33.5)"" fill=""#fff""/>
  <polygon points=""30,38 70,38 62,86 38,86"" fill=""#ffdbeb"" stroke=""#4a3b52"" stroke-width=""3.2"" stroke-linejoin=""round""/>
  <g class=""ds-boba"">
    <circle cx=""42"" cy=""74"" r=""4"" fill=""#563a42"" stroke=""#4a3b52"" stroke-width=""1.4""/>
    <circle cx=""52"" cy=""78"" r=""4.4"" fill=""#563a42"" stroke=""#4a3b52"" stroke-width=""1.4""/>
    <circle cx=""61"" cy=""73"" r=""3.8"" fill=""#563a42"" stroke=""#4a3b52"" stroke-width=""1.4""/>
  </g>
  <rect x=""26"" y=""26"" width=""48"" height=""14"" rx=""7"" fill=""#ffb6c1"" stroke=""#4a3b52"" stroke-width=""3.2""/>
  <ellipse cx=""50"" cy=""32"" rx=""4"" ry=""3"" fill=""#4a3b52""/>
  <circle cx=""39"" cy=""52"" r=""3.4"" fill=""#4a3b52""/>
  <circle cx=""61"" cy=""52"" r=""3.4"" fill=""#4a3b52""/>
  <circle cx=""37.6"" cy=""50.6"" r=""1.1"" fill=""#fff""/>
  <circle cx=""59.6"" cy=""50.6"" r=""1.1"" fill=""#fff""/>
  <ellipse cx=""33"" cy=""58"" rx=""4.5"" ry=""3.2"" fill=""#ff8fa3"" opacity="".55""/>
  <ellipse cx=""67"" cy=""58"" rx=""4.5"" ry=""3.2"" fill=""#ff8fa3"" opacity="".55""/>
  <path d=""M45 58 Q50 63 55 58"" stroke=""#4a3b52"" stroke-width=""2.2"" fill=""none"" stroke-linecap=""round""/>
  <polygon class=""ds-sparkle"" points=""16,64 19,71 26,72 19,76 18,83 13,78 6,79 10,72 8,65 15,68"" fill=""#b19cd9"" stroke=""#4a3b52"" stroke-width=""1.6"" stroke-linejoin=""round""/>
</svg>";
    }
}
