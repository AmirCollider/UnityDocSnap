// ==========================================
// DocSnapSiteAssets
// Embedded static site assets (CSS + JS) so
// the generated output has zero external
// file dependencies at build/export time.
// Source of truth: webtemplate/style.css and
// webtemplate/app.js in the project repo tools.
// ==========================================
namespace AmirCollider.UnityDocSnap.Editor
{
    internal static class DocSnapSiteAssets
    {
        // ==========================================
        // StyleCss - assets_ui/style.css contents
        // ==========================================
        public const string StyleCss = @"/* ==========================================
   Unity DocSnap — Site Stylesheet
   Design tokens, layout, and components for
   the generated offline documentation site.
   ========================================== */

@import url('https://fonts.googleapis.com/css2?family=Baloo+2:wght@500;600;700;800&family=Quicksand:wght@400;500;600;700&family=Space+Mono:wght@400;700&family=Kosugi+Maru&family=Vazirmatn:wght@400;500;600;700&display=swap');

/* ==========================================
   Design Tokens
   ========================================== */
:root {
  --pink: #ffb6c1;
  --pink-strong: #ff8fa3;
  --pink-pale: #ffd6e8;
  --lavender: #b19cd9;
  --lavender-strong: #9678c2;
  --mint: #c8f7c5;
  --mint-strong: #8fd98c;
  --peach: #ffd8b8;
  --cream: #fffaf3;
  --cream-deep: #fff3e6;
  --card: #ffffff;
  --ink: #4a3b52;
  --ink-soft: #8a7a92;
  --ink-faint: #c3b8cb;
  --line: #f0dfe8;
  --warn: #ff9494;
  --warn-ink: #9c3b3b;
  --warn-bg: #fff0ee;

  --radius-lg: 22px;
  --radius-md: 14px;
  --radius-sm: 9px;
  --shadow-soft: 0 6px 20px rgba(177, 156, 217, 0.16);
  --shadow-lift: 0 10px 28px rgba(255, 143, 163, 0.22);

  --font-display: 'Baloo 2', 'Kosugi Maru', 'Vazirmatn', sans-serif;
  --font-body: 'Quicksand', 'Kosugi Maru', 'Vazirmatn', sans-serif;
  --font-mono: 'Space Mono', 'Kosugi Maru', monospace;

  --sidebar-w: 288px;
}

:lang(ja) {
  --font-display: 'Kosugi Maru', 'Baloo 2', sans-serif;
  --font-body: 'Kosugi Maru', 'Quicksand', sans-serif;
}

:lang(fa) {
  --font-display: 'Vazirmatn', 'Baloo 2', sans-serif;
  --font-body: 'Vazirmatn', 'Quicksand', sans-serif;
}

* { box-sizing: border-box; }

html { scroll-behavior: smooth; }

@media (prefers-reduced-motion: reduce) {
  html { scroll-behavior: auto; }
  * { animation-duration: 0.001ms !important; transition-duration: 0.001ms !important; }
}

body {
  margin: 0;
  background: var(--cream);
  color: var(--ink);
  font-family: var(--font-body);
  font-size: 15px;
  line-height: 1.6;
}

html[dir=""rtl""] body { direction: rtl; }

a { color: var(--lavender-strong); text-decoration: none; }
a:hover { text-decoration: underline; }
a:focus-visible, button:focus-visible, summary:focus-visible, .tab:focus-visible {
  outline: 3px solid var(--lavender);
  outline-offset: 2px;
  border-radius: 4px;
}

h1, h2, h3, h4 { font-family: var(--font-display); font-weight: 700; color: var(--ink); margin: 0 0 .5em; }

code, .mono { font-family: var(--font-mono); }

/* ==========================================
   Page Shell — Sidebar + Main
   ========================================== */
.ds-shell { display: flex; min-height: 100vh; align-items: stretch; }

.ds-sidebar {
  width: var(--sidebar-w);
  flex: 0 0 var(--sidebar-w);
  background: linear-gradient(180deg, var(--pink-pale) 0%, var(--cream-deep) 46%, var(--cream) 100%);
  border-inline-end: 1px solid var(--line);
  padding: 22px 18px 18px;
  position: sticky;
  top: 0;
  height: 100vh;
  overflow-y: auto;
}

.ds-brand { display: flex; align-items: center; gap: 10px; margin-bottom: 4px; }
.ds-brand svg { width: 44px; height: 44px; flex: none; }
.ds-brand-text h1 { font-size: 19px; margin: 0; line-height: 1.1; }
.ds-brand-text span { font-size: 11.5px; color: var(--ink-soft); }

.ds-tagline { font-size: 12.5px; color: var(--ink-soft); margin: 10px 2px 18px; }

.ds-langbar { display: flex; gap: 6px; margin-bottom: 20px; }
.ds-lang-btn {
  flex: 1;
  font-family: var(--font-body);
  font-weight: 600;
  font-size: 12px;
  padding: 7px 4px;
  border: 1.5px solid var(--pink);
  background: #fff;
  color: var(--ink);
  border-radius: 999px;
  cursor: pointer;
  transition: transform .15s ease, background .15s ease, color .15s ease;
}
.ds-lang-btn:hover { transform: translateY(-1px); background: var(--pink-pale); }
.ds-lang-btn.is-active { background: var(--pink); color: #fff; border-color: var(--pink); }

.ds-nav-section { margin-bottom: 16px; }
.ds-nav-title {
  font-family: var(--font-display);
  font-size: 12.5px;
  letter-spacing: .02em;
  color: var(--lavender-strong);
  margin: 0 0 6px 2px;
  display: flex;
  align-items: center;
  gap: 6px;
}
.ds-nav-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 3px; }
.ds-nav-link {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 7px 10px;
  border-radius: var(--radius-sm);
  color: var(--ink);
  font-size: 13.5px;
  font-weight: 600;
}
.ds-nav-link:hover { background: rgba(255,255,255,.7); text-decoration: none; }
.ds-nav-link.is-current { background: #fff; box-shadow: var(--shadow-soft); color: var(--pink-strong); }
.ds-nav-empty { font-size: 12px; color: var(--ink-faint); padding: 4px 10px; }
.ds-nav-count {
  margin-inline-start: auto;
  font-family: var(--font-mono);
  font-size: 10.5px;
  color: var(--ink-soft);
  background: rgba(255,255,255,.6);
  border-radius: 999px;
  padding: 1px 7px;
}

.ds-sidebar-footer {
  margin-top: 22px;
  padding-top: 14px;
  border-top: 1px dashed var(--line);
  font-size: 11px;
  color: var(--ink-faint);
  text-align: center;
}
.ds-sidebar-footer a { color: var(--ink-soft); }

.ds-main { flex: 1 1 auto; min-width: 0; padding: 30px 40px 60px; }

/* ==========================================
   Page Header
   ========================================== */
.ds-breadcrumb { font-size: 12.5px; color: var(--ink-soft); margin-bottom: 10px; }
.ds-breadcrumb a { color: var(--ink-soft); }
.ds-breadcrumb .sep { margin: 0 6px; color: var(--ink-faint); }

.ds-page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 20px;
  flex-wrap: wrap;
  margin-bottom: 22px;
}
.ds-page-header h1 { font-size: 30px; display: flex; align-items: center; gap: 10px; }
.ds-page-sub { color: var(--ink-soft); font-size: 13.5px; margin-top: 4px; }

.ds-badge-row { display: flex; gap: 8px; flex-wrap: wrap; }
.ds-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  font-weight: 600;
  padding: 5px 12px;
  border-radius: 999px;
  background: var(--cream-deep);
  color: var(--ink);
  border: 1px solid var(--line);
  white-space: nowrap;
}
.ds-badge.pink { background: var(--pink-pale); border-color: var(--pink); }
.ds-badge.mint { background: #eafcE9; border-color: var(--mint-strong); color: #396b37; }
.ds-badge.lav { background: #f1eaFB; border-color: var(--lavender); color: var(--lavender-strong); }
.ds-badge.warn { background: var(--warn-bg); border-color: var(--warn); color: var(--warn-ink); }
.ds-badge.ghost { background: transparent; }

/* ==========================================
   Cards & Stat Tiles
   ========================================== */
.ds-card {
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: var(--radius-lg);
  padding: 20px 22px;
  margin-bottom: 18px;
  box-shadow: var(--shadow-soft);
}

.ds-stat-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 14px; margin-bottom: 22px; }
.ds-stat-tile {
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
  padding: 16px 18px;
  box-shadow: var(--shadow-soft);
  transition: transform .18s ease, box-shadow .18s ease;
}
.ds-stat-tile:hover { transform: translateY(-3px); box-shadow: var(--shadow-lift); }
.ds-stat-num { font-family: var(--font-display); font-size: 28px; color: var(--pink-strong); line-height: 1; }
.ds-stat-label { font-size: 12px; color: var(--ink-soft); margin-top: 6px; font-weight: 600; }

/* ==========================================
   Hierarchy Tree (native <details>/<summary>)
   ========================================== */
.ds-tree, .ds-tree ul { list-style: none; margin: 0; padding-inline-start: 0; }
.ds-tree ul { padding-inline-start: 14px; border-inline-start: 2px dashed var(--line); margin-inline-start: 8px; }
.ds-tree ul ul ul ul { padding-inline-start: 8px; margin-inline-start: 4px; }
.ds-tree ul ul ul ul ul ul { padding-inline-start: 4px; margin-inline-start: 2px; border-inline-start-style: dotted; }
.ds-tree li { margin: 3px 0; }

.ds-go summary {
  cursor: pointer;
  list-style: none;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 10px;
  border-radius: var(--radius-sm);
  font-weight: 600;
  font-size: 13.5px;
}
.ds-go summary::-webkit-details-marker { display: none; }
.ds-go summary:hover { background: var(--cream-deep); }
.ds-go summary::before {
  content: '▸';
  color: var(--lavender);
  font-size: 11px;
  transition: transform .15s ease;
  flex: none;
}
.ds-go[open] > summary::before { transform: rotate(90deg); }
.ds-go-leaf {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 10px 6px 26px;
  font-weight: 600;
  font-size: 13.5px;
  border-radius: var(--radius-sm);
}
.ds-go-leaf:hover, .ds-go summary:target, .ds-go-leaf:target { background: var(--pink-pale); }

.ds-go-inactive summary, .ds-go-inactive.ds-go-leaf { opacity: .5; font-style: italic; }
.ds-go-tag { font-family: var(--font-mono); font-size: 10.5px; color: var(--ink-soft); background: var(--cream-deep); border-radius: 6px; padding: 1px 6px; }

/* ==========================================
   GameObject / Asset Detail Cards
   ========================================== */
.ds-go-card, .ds-asset-card {
  scroll-margin-top: 18px;
  border: 1px solid var(--line);
  border-radius: var(--radius-lg);
  background: var(--card);
  margin-bottom: 22px;
  overflow: hidden;
  box-shadow: var(--shadow-soft);
}
.ds-go-card-head, .ds-asset-card-head {
  padding: 16px 20px;
  background: linear-gradient(120deg, var(--pink-pale), var(--cream-deep));
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}
.ds-go-card-head h3, .ds-asset-card-head h3 { font-size: 18px; margin: 0; display: flex; align-items: center; gap: 8px; min-width: 0; overflow-wrap: anywhere; }
.ds-go-card-body, .ds-asset-card-body { padding: 6px 20px 18px; }

.ds-transform-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 10px; margin: 14px 0; }
.ds-vec3 { display: flex; gap: 6px; font-family: var(--font-mono); font-size: 12.5px; }
.ds-vec3 b { color: var(--lavender-strong); font-weight: 700; }
.ds-transform-tile { background: var(--cream-deep); border-radius: var(--radius-sm); padding: 10px 12px; }
.ds-transform-tile .lbl { font-family: var(--font-body); font-weight: 700; font-size: 11.5px; color: var(--ink-soft); display: block; margin-bottom: 5px; }

/* ==========================================
   Component Cards
   ========================================== */
.ds-component {
  border: 1px solid var(--line);
  border-inline-start: 5px solid var(--lavender);
  border-radius: var(--radius-md);
  margin: 14px 0;
  overflow: hidden;
}
.ds-component.is-user-script { border-inline-start-color: var(--pink-strong); }
.ds-component.is-missing { border-inline-start-color: var(--warn); background: var(--warn-bg); }
.ds-component-head {
  padding: 10px 16px;
  background: var(--cream-deep);
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 700;
  font-size: 14px;
}
.ds-component.is-missing .ds-component-head { background: transparent; color: var(--warn-ink); }
.ds-component-toggle { margin-inline-start: auto; font-size: 11px; font-weight: 700; padding: 2px 9px; border-radius: 999px; }
.ds-component-toggle.on { background: #eafce9; color: #396b37; }
.ds-component-toggle.off { background: #f1e9ea; color: var(--ink-soft); }

/* ==========================================
   Field Grid (CSS Grid, not <table>) — each
   .ds-field-grid builds its own column tracks
   from its own available width, so nesting one
   inside another (structs inside structs, an
   array's complex items inside a component's
   fields, …) can never compound into an ever-
   narrower fixed percentage the way nested
   <table>s used to. Rows use display:contents
   so their children become direct grid items of
   the surrounding .ds-field-grid.
   ========================================== */
.ds-field-grid {
  display: grid;
  grid-template-columns: minmax(0, 32%) minmax(0, 18%) minmax(0, 1fr);
  column-gap: 12px;
  width: 100%;
  min-width: 0;
  font-size: 13px;
}

/* Declares the containment context the narrow-layout
   fallback measures against. The @container block
   itself lives at the END of this section, after
   every .ds-field-grid-* rule: @container adds no
   specificity, so a block placed here loses to any
   equally-specific rule written below it. */
.ds-asset-card-body, .ds-go-card-body { container-type: inline-size; }
.ds-field-grid-head, .ds-field-grid-row { display: contents; }
.ds-field-grid-head > span {
  font-family: var(--font-display);
  font-size: 11px;
  letter-spacing: .03em;
  color: var(--ink-soft);
  padding: 8px 0;
  border-bottom: 2px solid var(--line);
}
.ds-field-grid-row > div {
  padding: 7px 0;
  border-bottom: 1px solid var(--line);
  min-width: 0;
  align-self: start;
}
.ds-field-grid-row:last-child > div { border-bottom: none; }
.ds-field-name { font-weight: 700; overflow-wrap: anywhere; }
.ds-field-type { color: var(--ink-soft); font-family: var(--font-mono); font-size: 11.5px; overflow-wrap: anywhere; }
.ds-field-value {
  font-family: var(--font-mono);
  font-size: 12.5px;
  min-width: 0;
  overflow-wrap: anywhere;
  direction: ltr;
  unicode-bidi: isolate;
  text-align: start;
}

.ds-nested-block {
  margin: 4px 0;
  padding: 8px 10px;
  background: var(--cream-deep);
  border-radius: var(--radius-sm);
  max-width: 100%;
  overflow-x: auto;
}
.ds-nested-block-title { font-weight: 700; padding: 2px 0 8px; overflow-wrap: anywhere; }

.ds-pill { display: inline-flex; align-items: center; gap: 4px; padding: 2px 9px; border-radius: 999px; font-size: 11.5px; font-weight: 700; }
.ds-pill.bool-true { background: #eafce9; color: #396b37; }
.ds-pill.bool-false { background: #f1e9ea; color: var(--ink-soft); }
.ds-pill.enum { background: #f1eaFB; color: var(--lavender-strong); }
.ds-swatch { display: inline-block; width: 13px; height: 13px; border-radius: 4px; border: 1px solid rgba(0,0,0,.15); vertical-align: -2px; margin-inline-end: 6px; }

.ds-ref-chip {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 3px 10px;
  border-radius: 999px;
  background: #f1eaFB;
  color: var(--lavender-strong);
  font-weight: 700;
  font-size: 12px;
  font-family: var(--font-body);
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.ds-ref-chip:hover { background: var(--lavender); color: #fff; text-decoration: none; }
.ds-ref-chip.is-missing { background: var(--warn-bg); color: var(--warn-ink); }
.ds-ref-chip.is-unresolved { background: var(--cream-deep); color: var(--ink-soft); }
.ds-ref-chip .type { opacity: .7; font-weight: 500; font-size: 10.5px; }

/* ==========================================
   Array Data Grid — compact scalar array
   elements (numbers, bools, enums, vectors,
   refs, …) as fixed-width cells with nowrap +
   ellipsis, instead of the old flex-wrap chips.
   A value can no longer break mid-character: it
   either fits, or it truncates with an ellipsis,
   with the full value still readable via the
   cell's title tooltip.
   ========================================== */
.ds-array-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(88px, 1fr));
  gap: 5px;
  max-height: 260px;
  overflow-y: auto;
  padding: 8px;
  background: var(--cream-deep);
  border-radius: var(--radius-sm);
}
.ds-array-cell {
  display: flex;
  flex-direction: column;
  gap: 1px;
  min-width: 0;
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: 7px;
  padding: 4px 8px;
  overflow: hidden;
}
.ds-array-cell .idx { font-size: 9.5px; font-weight: 700; color: var(--ink-faint); line-height: 1.3; }
.ds-array-cell .val {
  font-family: var(--font-mono);
  font-size: 12px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
  direction: ltr;
  unicode-bidi: isolate;
  text-align: start;
}

/* ==========================================
   Matrix Grid — arrays whose own elements are
   arrays of scalars (jagged/2D numeric data:
   vertex lists, matrices, …). Rendered as one
   real sticky-header, scrollable spreadsheet-
   style table instead of nesting array grids
   inside array blocks, which is what kept
   recreating the character-splitting bug no
   matter how many times the flat-array case
   alone got patched.
   ========================================== */
.ds-matrix-scroll {
  overflow: auto;
  max-height: 320px;
  background: var(--cream-deep);
  border-radius: var(--radius-sm);
  padding: 8px;
}
.ds-matrix-table { border-collapse: separate; border-spacing: 3px; font-family: var(--font-mono); font-size: 11.5px; }
.ds-matrix-table thead th {
  position: sticky;
  top: 0;
  background: var(--cream-deep);
  color: var(--ink-soft);
  font-size: 10px;
  font-weight: 700;
  padding: 2px 8px;
  z-index: 1;
}
.ds-matrix-table td {
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: 6px;
  padding: 3px 9px;
  white-space: nowrap;
  text-align: end;
}
.ds-matrix-row-head {
  position: sticky;
  inset-inline-start: 0;
  background: var(--cream-deep) !important;
  text-align: center !important;
  z-index: 1;
}

.ds-array-block-item { margin: 6px 0; padding: 6px 8px; background: var(--cream-deep); border-radius: var(--radius-sm); max-width: 100%; overflow-x: auto; }
.ds-array-more { color: var(--ink-soft); font-size: 11.5px; font-style: italic; margin-top: 6px; }
.ds-empty-note { color: var(--ink-faint); font-size: 12.5px; font-style: italic; padding: 8px 0; }

/* ==========================================
   Asset Grid / Thumbnails
   ========================================== */
/* minmax(min(320px, 100%), 1fr) instead of a bare
   320px floor: a bare floor makes the grid wider
   than its own container whenever the container is
   narrower than the floor, which is exactly what
   happens inside a deeply nested folder node. */
.ds-asset-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(320px, 100%), 1fr));
  gap: 16px;
  align-items: start;
}
.ds-asset-grid > * { min-width: 0; }

.ds-thumb {
  position: relative;
  width: 100%;
  aspect-ratio: 4 / 3;
  border-radius: var(--radius-sm);
  background: repeating-conic-gradient(var(--cream-deep) 0% 25%, var(--card) 0% 50%) 0 0 / 18px 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 34px;
  margin-bottom: 10px;
  border: 1px solid var(--line);
  overflow: hidden;
}
.ds-thumb img { width: 100%; height: 100%; object-fit: contain; display: block; }
.ds-thumb.is-icon { aspect-ratio: auto; min-height: 76px; }
.ds-thumb.is-icon img { width: auto; height: auto; max-width: 48px; max-height: 48px; image-rendering: pixelated; }

/* ==========================================
   Playable / downloadable media — only ever
   emitted when the export actually copied the
   file bytes into files/ (Export Full Project
   With Files).
   ========================================== */
.ds-media { width: 100%; display: block; margin: 0 0 10px; border-radius: var(--radius-sm); background: var(--cream-deep); }
video.ds-media { max-height: 260px; }
.ds-file-actions { display: flex; flex-wrap: wrap; gap: 6px; margin: 2px 0 8px; }
.ds-file-link {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 11.5px;
  font-weight: 700;
  padding: 4px 11px;
  border-radius: 999px;
  background: var(--cream-deep);
  border: 1px solid var(--line);
  color: var(--ink);
  white-space: nowrap;
}
.ds-file-link:hover { background: var(--pink-pale); border-color: var(--pink); text-decoration: none; }

/* ==========================================
   Collapsible Detail Sections — Import
   Settings, Fields, Shader Properties and
   Prefab Contents. Collapsed by default: a
   closed <details> is never laid out, so a
   folder page with hundreds of assets stays
   openable instead of hanging the browser.
   ========================================== */
.ds-detail { margin-top: 10px; border-top: 1px dashed var(--line); }
.ds-detail > summary {
  cursor: pointer;
  list-style: none;
  display: flex;
  align-items: center;
  gap: 7px;
  padding: 8px 0 6px;
  font-weight: 700;
  font-size: 13px;
  color: var(--ink);
}
.ds-detail > summary::-webkit-details-marker { display: none; }
.ds-detail > summary::before {
  content: '▸';
  color: var(--lavender);
  font-size: 11px;
  transition: transform .15s ease;
  flex: none;
}
.ds-detail[open] > summary::before { transform: rotate(90deg); }
.ds-detail > summary:hover { color: var(--pink-strong); }
.ds-detail-body { padding-bottom: 6px; min-width: 0; }

/* Two-column grid, not flex: a flex row with a
   non-shrinkable key and a long unbreakable value
   forces the card wider than its grid track. */
.ds-kv-line {
  display: grid;
  grid-template-columns: minmax(60px, auto) minmax(0, 1fr);
  column-gap: 10px;
  align-items: baseline;
  font-size: 12.5px;
  padding: 4px 0;
  border-bottom: 1px dashed var(--line);
  min-width: 0;
}
.ds-kv-line:last-child { border-bottom: none; }
.ds-kv-line .k { color: var(--ink-soft); font-weight: 600; min-width: 0; overflow-wrap: anywhere; }
.ds-kv-line .v {
  font-family: var(--font-mono);
  text-align: end;
  overflow-wrap: anywhere;
  min-width: 0;
  direction: ltr;
  unicode-bidi: isolate;
}

/* ==========================================
   Folder Tree (asset browser on dashboard)
   ========================================== */
.ds-folder-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 6px; }
.ds-folder-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  border-radius: var(--radius-md);
  border: 1px solid var(--line);
  background: var(--card);
  transition: transform .15s ease, box-shadow .15s ease;
}
.ds-folder-row:hover { transform: translateY(-2px); box-shadow: var(--shadow-soft); text-decoration: none; }
.ds-folder-path { font-family: var(--font-mono); font-size: 12.5px; color: var(--ink); }
.ds-folder-meta { margin-inline-start: auto; font-size: 11.5px; color: var(--ink-soft); }

/* ==========================================
   Misc: tabs, footer, callouts
   ========================================== */
.ds-callout {
  border-radius: var(--radius-md);
  padding: 14px 18px;
  background: var(--cream-deep);
  border: 1px dashed var(--lavender);
  font-size: 13px;
  margin-bottom: 18px;
}
.ds-callout.warn { background: var(--warn-bg); border-color: var(--warn); color: var(--warn-ink); }

.ds-footer {
  margin-top: 40px;
  padding-top: 18px;
  border-top: 1px dashed var(--line);
  color: var(--ink-soft);
  font-size: 12.5px;
  display: flex;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px;
}

.ds-back-top {
  position: fixed;
  inset-block-end: 22px;
  inset-inline-end: 26px;
  background: var(--pink);
  color: #fff;
  border: none;
  width: 42px;
  height: 42px;
  border-radius: 50%;
  font-size: 18px;
  line-height: 1;
  padding: 0;
  display: none;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  box-shadow: var(--shadow-lift);
}
.ds-back-top:hover { background: var(--pink-strong); }

[data-en] { unicode-bidi: isolate; }

/* ==========================================
   Latin data inside an RTL document — paths,
   GUIDs, type names, numbers. Without an
   explicit LTR isolate these get bidi-reordered
   into unreadable fragments the moment the UI
   language is switched to Persian.
   ========================================== */
.ds-folder-path,
.ds-go-tag,
.ds-field-type,
.ds-matrix-table,
.ds-ref-chip,
code, kbd, samp, pre {
  direction: ltr;
  unicode-bidi: isolate;
}
.ds-asset-card-head h3, .ds-go-card-head h3 { unicode-bidi: isolate; }

@media (max-width: 880px) {
  .ds-shell { flex-direction: column; }
  .ds-sidebar { width: 100%; flex-basis: auto; position: relative; height: auto; border-inline-end: none; border-bottom: 1px solid var(--line); }
  .ds-main { padding: 22px 16px 50px; }
}
";

        // ==========================================
        // AppJs - assets_ui/app.js contents
        // ==========================================
        public const string AppJs = @"// ==========================================
// Unity DocSnap — Site Behaviour
// Language switching + tree helpers.
// No network calls. The only storage used is
// localStorage, to remember the chosen UI
// language (en/ja/fa) across pages of this
// offline site - never used for project data.
// ==========================================

(function () {
  'use strict';

  var RTL_LANGS = { fa: true };
  var LANG_STORAGE_KEY = 'unityDocSnapLang';

  // ==========================================
  // readStoredLanguage() / writeStoredLanguage()
  // Best-effort persistence of the user's chosen
  // language across pages of this offline site.
  // Wrapped in try/catch since some browsers
  // restrict localStorage under a file:// origin.
  // ==========================================
  function readStoredLanguage() {
    try { return window.localStorage.getItem(LANG_STORAGE_KEY); }
    catch (e) { return null; }
  }

  function writeStoredLanguage(lang) {
    try { window.localStorage.setItem(LANG_STORAGE_KEY, lang); }
    catch (e) { /* localStorage unavailable - non-fatal */ }
  }

  // ==========================================
  // applyLanguage(lang)
  // Swaps visible text for every element that
  // carries data-en/data-ja/data-fa, flips
  // document direction for RTL languages, and
  // remembers the choice for the next page.
  // ==========================================
  function applyLanguage(lang) {
    var root = document.documentElement;
    root.setAttribute('lang', lang);
    root.setAttribute('dir', RTL_LANGS[lang] ? 'rtl' : 'ltr');

    var nodes = document.querySelectorAll('[data-en]');
    for (var i = 0; i < nodes.length; i++) {
      var el = nodes[i];
      var text = el.getAttribute('data-' + lang) || el.getAttribute('data-en');
      if (text !== null) { el.textContent = text; }
    }

    var buttons = document.querySelectorAll('.ds-lang-btn');
    for (var b = 0; b < buttons.length; b++) {
      var isActive = buttons[b].getAttribute('data-lang') === lang;
      buttons[b].classList.toggle('is-active', isActive);
      buttons[b].setAttribute('aria-pressed', isActive ? 'true' : 'false');
    }

    writeStoredLanguage(lang);
  }

  // ==========================================
  // restoreLanguage()
  // Applies the language stored from a previous
  // page, if this page has a matching lang button.
  // Called once on DOMContentLoaded.
  // ==========================================
  function restoreLanguage() {
    var stored = readStoredLanguage();
    if (!stored) { return; }
    var match = document.querySelector('.ds-lang-btn[data-lang=' + stored + ']');
    if (match) { applyLanguage(stored); }
  }

  // ==========================================
  // wireLanguageButtons()
  // Hooks up the sidebar EN / JA / FA buttons.
  // ==========================================
  function wireLanguageButtons() {
    var buttons = document.querySelectorAll('.ds-lang-btn');
    for (var i = 0; i < buttons.length; i++) {
      buttons[i].addEventListener('click', function (evt) {
        applyLanguage(evt.currentTarget.getAttribute('data-lang'));
      });
    }
  }

  // ==========================================
  // wireTreeControls()
  // Optional ""expand all / collapse all"" for
  // any .ds-tree block, operating on the native
  // <details> open attribute.
  // ==========================================
  function wireTreeControls() {
    var expandButtons = document.querySelectorAll('[data-tree-expand]');
    for (var i = 0; i < expandButtons.length; i++) {
      expandButtons[i].addEventListener('click', function (evt) {
        var scope = document.getElementById(evt.currentTarget.getAttribute('data-tree-expand'));
        if (!scope) { return; }
        var open = evt.currentTarget.getAttribute('data-mode') === 'expand';
        var details = scope.querySelectorAll('details');
        for (var d = 0; d < details.length; d++) { details[d].open = open; }
      });
    }
  }

  // ==========================================
  // wireBackToTop()
  // Shows the floating back-to-top button once
  // the page has scrolled a little.
  // ==========================================
  function wireBackToTop() {
    var btn = document.querySelector('.ds-back-top');
    if (!btn) { return; }
    var toggle = function () {
      btn.style.display = window.scrollY > 400 ? 'flex' : 'none';
    };
    window.addEventListener('scroll', toggle, { passive: true });
    btn.addEventListener('click', function () {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    });
    toggle();
  }

  document.addEventListener('DOMContentLoaded', function () {
    restoreLanguage();
    wireLanguageButtons();
    wireTreeControls();
    wireBackToTop();
  });
})();
";

        // ==========================================
        // LogoMarkSvg - default inline mascot logo,
        // used until a custom logo path is configured
        // in Unity DocSnap > Settings.
        // ==========================================
        public const string LogoMarkSvg = @"<svg viewBox=""0 0 100 100"" xmlns=""http://www.w3.org/2000/svg"" role=""img"" aria-label=""Unity DocSnap mascot"">
  <polygon points=""62,10 84,32 68,30 66,15"" fill=""#ffe08a"" stroke=""#4a3b52"" stroke-width=""2.5"" stroke-linejoin=""round""/>
  <line x1=""59"" y1=""34"" x2=""72"" y2=""12"" stroke=""#4a3b52"" stroke-width=""4.5"" stroke-linecap=""round""/>
  <line x1=""59"" y1=""34"" x2=""72"" y2=""12"" stroke=""#ff8fa3"" stroke-width=""3"" stroke-linecap=""round""/>
  <rect x=""53"" y=""30"" width=""7"" height=""7"" rx=""2"" transform=""rotate(45 56.5 33.5)"" fill=""#fff""/>
  <polygon points=""30,38 70,38 62,86 38,86"" fill=""#ffdbeb"" stroke=""#4a3b52"" stroke-width=""3.2"" stroke-linejoin=""round""/>
  <circle cx=""42"" cy=""74"" r=""4"" fill=""#563a42"" stroke=""#4a3b52"" stroke-width=""1.4""/>
  <circle cx=""52"" cy=""78"" r=""4.4"" fill=""#563a42"" stroke=""#4a3b52"" stroke-width=""1.4""/>
  <circle cx=""61"" cy=""73"" r=""3.8"" fill=""#563a42"" stroke=""#4a3b52"" stroke-width=""1.4""/>
  <rect x=""26"" y=""26"" width=""48"" height=""14"" rx=""7"" fill=""#ffb6c1"" stroke=""#4a3b52"" stroke-width=""3.2""/>
  <ellipse cx=""50"" cy=""32"" rx=""4"" ry=""3"" fill=""#4a3b52""/>
  <circle cx=""39"" cy=""52"" r=""3.4"" fill=""#4a3b52""/>
  <circle cx=""61"" cy=""52"" r=""3.4"" fill=""#4a3b52""/>
  <circle cx=""37.6"" cy=""50.6"" r=""1.1"" fill=""#fff""/>
  <circle cx=""59.6"" cy=""50.6"" r=""1.1"" fill=""#fff""/>
  <ellipse cx=""33"" cy=""58"" rx=""4.5"" ry=""3.2"" fill=""#ff8fa3"" opacity="".55""/>
  <ellipse cx=""67"" cy=""58"" rx=""4.5"" ry=""3.2"" fill=""#ff8fa3"" opacity="".55""/>
  <path d=""M45 58 Q50 63 55 58"" stroke=""#4a3b52"" stroke-width=""2.2"" fill=""none"" stroke-linecap=""round""/>
  <polygon points=""16,64 19,71 26,72 19,76 18,83 13,78 6,79 10,72 8,65 15,68"" fill=""#b19cd9"" stroke=""#4a3b52"" stroke-width=""1.6"" stroke-linejoin=""round""/>
</svg>";
    }
}
