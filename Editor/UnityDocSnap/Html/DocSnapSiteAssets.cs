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
        // StyleCss - theme/style.css contents
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

/* Set by the tiny boot script in <head> when the
   reader's stored language differs from the baked
   one: the body stays invisible until app.js swaps
   the text (applyLanguage removes the class), so
   there is never a flash of the wrong language or
   direction. A timeout in the boot script clears it
   after 1.5s even if app.js never runs. */
html.ds-lang-pending body { visibility: hidden; }

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

/* ==========================================
   Detail-level switch (Simple / Advanced)
   Simple hides every element tagged .ds-adv
   (engine-component fields, GUIDs, import
   settings), leaving a light, skimmable page;
   Advanced shows the complete export. The
   chosen mode is remembered in localStorage by
   app.js, exactly like the UI language.
   ========================================== */
.ds-modebar { display: flex; gap: 6px; margin-bottom: 20px; }
.ds-mode-btn {
  flex: 1;
  font-family: var(--font-body);
  font-weight: 600;
  font-size: 12px;
  padding: 7px 4px;
  border: 1.5px solid var(--lavender);
  background: #fff;
  color: var(--ink);
  border-radius: 999px;
  cursor: pointer;
  transition: transform .15s ease, background .15s ease, color .15s ease;
}
.ds-mode-btn:hover { transform: translateY(-1px); background: #f1eaFB; }
.ds-mode-btn.is-active { background: var(--lavender-strong); color: #fff; border-color: var(--lavender-strong); }

body.ds-mode-simple .ds-adv { display: none !important; }

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

/* ==========================================
   Collapsed asset cards — every asset card is a
   <details>: closed it is one compact header row
   (name + type + size), open it reveals the full
   info. A folder with hundreds of files therefore
   lays out a list of light headers instead of
   hundreds of full cards, and content-visibility
   lets the browser skip work for cards that are
   off-screen entirely. This is what keeps the
   Assets page usable in Advanced mode.
   ========================================== */
details.ds-asset-card {
  margin-bottom: 0;
  content-visibility: auto;
  contain-intrinsic-size: auto 52px;
}
details.ds-asset-card[open] { contain-intrinsic-size: auto 480px; }
details.ds-asset-card > summary.ds-asset-card-head {
  cursor: pointer;
  list-style: none;
  padding: 10px 14px;
  justify-content: flex-start;
  flex-wrap: nowrap;
  overflow: hidden;
}
details.ds-asset-card > summary.ds-asset-card-head::-webkit-details-marker { display: none; }
details.ds-asset-card > summary.ds-asset-card-head::before {
  content: '▸';
  color: var(--lavender);
  font-size: 11px;
  transition: transform .15s ease;
  flex: none;
}
details.ds-asset-card[open] > summary.ds-asset-card-head::before { transform: rotate(90deg); }
details.ds-asset-card > summary.ds-asset-card-head:hover h3 { color: var(--pink-strong); }
details.ds-asset-card > summary.ds-asset-card-head h3 {
  font-size: 13.5px;
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
  gap: 6px;
  font-family: var(--font-mono);
  font-size: 10.5px;
  color: var(--ink-soft);
  direction: ltr;
  unicode-bidi: isolate;
  white-space: nowrap;
}
.ds-asset-head-meta .t {
  background: rgba(255,255,255,.55);
  border: 1px solid var(--line);
  border-radius: 999px;
  padding: 1px 8px;
}

/* Component cards can also be skipped while
   off-screen; a rough intrinsic height keeps the
   scrollbar stable. */
.ds-component { content-visibility: auto; contain-intrinsic-size: auto 140px; }

.ds-transform-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 10px; margin: 14px 0; }
.ds-vec3 { display: flex; flex-wrap: wrap; gap: 6px; font-family: var(--font-mono); font-size: 12.5px; }
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

/* ==========================================
   Narrow-container fallback for .ds-field-grid
   MUST stay last in this section. Every selector
   is prefixed with .ds-field-grid so it outranks
   the unprefixed base rules above on specificity
   rather than relying on source order alone.
   Field name and type share the top line, the
   value gets the line below, one divider per
   field instead of one per cell.
   ========================================== */
@container (max-width: 340px) {
  .ds-field-grid.ds-field-grid { grid-template-columns: minmax(0, 1fr); }
  .ds-field-grid .ds-field-grid-head { display: none; }
  .ds-field-grid .ds-field-grid-row > div { border-bottom: none; padding: 0; }
  .ds-field-grid .ds-field-grid-row > .ds-field-name { padding-top: 9px; }
  .ds-field-grid .ds-field-grid-row > .ds-field-type { padding-bottom: 3px; }
  .ds-field-grid .ds-field-grid-row > .ds-field-value {
    padding-bottom: 9px;
    border-bottom: 1px solid var(--line);
  }
  .ds-field-grid .ds-field-grid-row:last-child > .ds-field-value { border-bottom: none; }
}

/* Same fallback for browsers without container
   query support: a phone-width viewport is the
   only case where it matters there. */
@supports not (container-type: inline-size) {
  @media (max-width: 620px) {
    .ds-field-grid.ds-field-grid { grid-template-columns: minmax(0, 1fr); }
    .ds-field-grid .ds-field-grid-head { display: none; }
    .ds-field-grid .ds-field-grid-row > div { border-bottom: none; padding: 0; }
    .ds-field-grid .ds-field-grid-row > .ds-field-name { padding-top: 9px; }
    .ds-field-grid .ds-field-grid-row > .ds-field-type { padding-bottom: 3px; }
    .ds-field-grid .ds-field-grid-row > .ds-field-value {
      padding-bottom: 9px;
      border-bottom: 1px solid var(--line);
    }
  }
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
/* 420px, not 320px: minus the card body's 40px of
   horizontal padding a 320px card leaves a 280px
   container, which sat below the narrow-layout
   breakpoint on every screen size - so the three
   column Field / Type / Value grid never rendered
   anywhere. 420px leaves ~380px, comfortably above
   it, while min(…, 100%) still lets the card
   collapse to full width on a phone. */
.ds-asset-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(340px, 100%), 1fr));
  gap: 10px;
  align-items: start;
}
.ds-asset-grid > * { min-width: 0; }
/* An open card takes the whole row, so its detail
   tables get the full page width instead of being
   squeezed into one grid column. */
.ds-asset-grid > .ds-asset-card[open] { grid-column: 1 / -1; }

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

/* ==========================================
   Site search (sidebar)
   ========================================== */
.ds-search { position: relative; margin-bottom: 18px; }
.ds-search-input {
  width: 100%;
  font-family: var(--font-body);
  font-size: 13px;
  padding: 9px 12px;
  border: 1.5px solid var(--lavender);
  border-radius: 999px;
  background: #fff;
  color: var(--ink);
  outline: none;
}
.ds-search-input:focus { border-color: var(--pink-strong); box-shadow: var(--shadow-soft); }
.ds-search-input::placeholder { color: var(--ink-faint); }

.ds-search-filters { display: flex; gap: 5px; margin-top: 8px; }
.ds-search-filter {
  flex: 1;
  font-family: var(--font-body);
  font-weight: 600;
  font-size: 11px;
  padding: 5px 4px;
  border: 1px solid var(--line);
  background: #fff;
  color: var(--ink-soft);
  border-radius: 999px;
  cursor: pointer;
}
.ds-search-filter.is-active { background: var(--lavender-strong); color: #fff; border-color: var(--lavender-strong); }

.ds-search-results {
  margin-top: 10px;
  max-height: 46vh;
  overflow-y: auto;
  background: #fff;
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-soft);
  padding: 6px;
}
.ds-search-result {
  display: block;
  padding: 8px 10px;
  border-radius: var(--radius-sm);
  color: var(--ink);
  border-bottom: 1px dashed var(--line);
}
.ds-search-result:last-child { border-bottom: none; }
.ds-search-result:hover, .ds-search-result.is-active { background: var(--pink-pale); text-decoration: none; }
.ds-search-result .r-top { display: flex; align-items: center; gap: 6px; }
.ds-search-result .r-name { font-weight: 700; font-size: 13px; overflow-wrap: anywhere; }
.ds-search-result .r-cat {
  margin-inline-start: auto;
  flex: none;
  font-family: var(--font-mono);
  font-size: 9.5px;
  font-weight: 700;
  color: var(--lavender-strong);
  background: #f1eaFB;
  border-radius: 999px;
  padding: 1px 7px;
}
.ds-search-result .r-sub {
  display: block;
  font-family: var(--font-mono);
  font-size: 10.5px;
  color: var(--ink-soft);
  margin-top: 2px;
  overflow-wrap: anywhere;
  direction: ltr;
  unicode-bidi: isolate;
}
.ds-search-empty { padding: 10px; font-size: 12px; color: var(--ink-faint); font-style: italic; text-align: center; }
.ds-search-more { padding: 8px 10px 2px; font-size: 11px; color: var(--ink-soft); font-style: italic; text-align: center; }
mark { background: var(--peach); color: var(--ink); border-radius: 3px; padding: 0 1px; }

/* ==========================================
   Prefab override markers
   ========================================== */
.ds-override-dot { color: var(--pink-strong); font-size: 9px; vertical-align: 1px; }
.ds-field-grid-row.is-override > .ds-field-name { color: var(--pink-strong); }
.ds-prefab-tag {
  font-family: var(--font-body);
  font-size: 10.5px;
  font-weight: 700;
  color: var(--lavender-strong);
  background: #f1eaFB;
  border: 1px solid var(--lavender);
  border-radius: 999px;
  padding: 1px 8px;
}
.ds-prefab-mark { font-size: 11px; opacity: .8; cursor: help; }

/* ==========================================
   Packages page
   ========================================== */
.ds-pkg-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(320px, 100%), 1fr));
  gap: 14px;
  align-items: start;
  margin-top: 12px;
}
.ds-pkg-card {
  border: 1px solid var(--line);
  border-inline-start: 5px solid var(--lavender);
  border-radius: var(--radius-md);
  background: var(--card);
  overflow: hidden;
  min-width: 0;
}
.ds-pkg-head {
  padding: 12px 16px 8px;
  background: linear-gradient(120deg, var(--pink-pale), var(--cream-deep));
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.ds-pkg-head h4 { margin: 0; font-size: 15px; overflow-wrap: anywhere; }
.ds-pkg-body { padding: 10px 16px 14px; }
.ds-pkg-id { font-size: 11.5px; color: var(--ink-soft); overflow-wrap: anywhere; direction: ltr; unicode-bidi: isolate; }
.ds-pkg-author { font-size: 11.5px; color: var(--ink-soft); margin-top: 4px; }
.ds-pkg-desc { font-size: 12.5px; color: var(--ink); margin: 8px 0 4px; }
.ds-module-list { list-style: none; margin: 8px 0 0; padding: 0; display: grid; grid-template-columns: repeat(auto-fill, minmax(min(240px, 100%), 1fr)); gap: 4px 14px; }
.ds-module-list li { display: flex; gap: 8px; align-items: baseline; font-size: 12px; }
.ds-module-name { color: var(--ink); overflow-wrap: anywhere; }
.ds-module-ver { margin-inline-start: auto; font-size: 10.5px; color: var(--ink-faint); }

/* ==========================================
   Sidebar top bar (language + theme toggle)
   ========================================== */
.ds-topbar { display: flex; align-items: stretch; gap: 6px; margin-bottom: 20px; }
.ds-topbar .ds-langbar { flex: 1; margin-bottom: 0; }
.ds-theme-toggle {
  flex: none;
  width: 40px;
  border: 1.5px solid var(--lavender);
  background: var(--card);
  color: var(--ink);
  border-radius: 999px;
  cursor: pointer;
  font-size: 15px;
  line-height: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: transform .15s ease, background .15s ease;
}
.ds-theme-toggle:hover { transform: translateY(-1px); background: var(--cream-deep); }

/* ==========================================
   Export Info card (dashboard) + Changes page
   ========================================== */
.ds-info-lines { display: flex; flex-direction: column; gap: 6px; margin-top: 6px; }
.ds-info-line {
  display: grid;
  grid-template-columns: minmax(120px, auto) minmax(0, 1fr);
  column-gap: 12px;
  align-items: baseline;
  font-size: 13px;
  padding: 5px 0;
  border-bottom: 1px dashed var(--line);
}
.ds-info-line:last-child { border-bottom: none; }
.ds-info-key { color: var(--ink-soft); font-weight: 600; }
.ds-info-val { font-family: var(--font-mono); font-size: 12.5px; overflow-wrap: anywhere; direction: ltr; unicode-bidi: isolate; text-align: start; }
.ds-info-tz { color: var(--ink-soft); font-size: 11px; }

.ds-stat-tile.ds-tile-mint .ds-stat-num { color: var(--mint-strong); }
.ds-stat-tile.ds-tile-warn .ds-stat-num { color: var(--warn); }
.ds-stat-tile.ds-tile-lav .ds-stat-num { color: var(--lavender-strong); }
.ds-stat-tile.ds-tile-pink .ds-stat-num { color: var(--pink-strong); }

.ds-diff-list {
  list-style: none;
  margin: 6px 0 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
  /* Very long change lists scroll inside their own
     section instead of stretching the page. */
  max-height: 440px;
  overflow-y: auto;
}
.ds-diff-item {
  font-family: var(--font-mono);
  font-size: 12px;
  padding: 5px 10px;
  border-radius: var(--radius-sm);
  border-inline-start: 3px solid var(--line);
  background: var(--cream-deep);
  direction: ltr;
  unicode-bidi: isolate;
  text-align: start;
  overflow-wrap: anywhere;
  display: flex;
  align-items: baseline;
  gap: 10px;
  flex-wrap: wrap;
}
.ds-diff-pathwrap { flex: 1 1 auto; min-width: 0; overflow-wrap: anywhere; }
.ds-diff-dir { color: var(--ink-soft); }
.ds-diff-file { font-weight: 700; }
.ds-diff-size {
  flex: none;
  margin-inline-start: auto;
  font-size: 11px;
  color: var(--ink-soft);
  white-space: nowrap;
}
.ds-diff-size .plus { color: var(--mint-strong); font-weight: 700; }
.ds-diff-size .minus { color: var(--warn); font-weight: 700; }
/* Per-entry download chips: the file as it was in the
   compared version and as it is in this export. */
.ds-diff-links { flex: none; display: inline-flex; gap: 4px; }
.ds-diff-item .ds-file-link { font-size: 10.5px; padding: 2px 8px; }
.ds-diff-item.ds-diff-added { border-inline-start-color: var(--mint-strong); }
.ds-diff-item.ds-diff-removed { border-inline-start-color: var(--warn); }
.ds-diff-item.ds-diff-changed { border-inline-start-color: var(--lavender); }
.ds-diff-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 22px;
  height: 20px;
  padding: 0 6px;
  border-radius: 999px;
  font-family: var(--font-mono);
  font-size: 11px;
  font-weight: 700;
  color: #fff;
}
.ds-diff-badge.ds-diff-added { background: var(--mint-strong); }
.ds-diff-badge.ds-diff-removed { background: var(--warn); }
.ds-diff-badge.ds-diff-changed { background: var(--lavender-strong); }

/* ==========================================
   Root version-picker landing page
   ========================================== */
.ds-versions-page { max-width: 720px; margin: 0 auto; padding: 40px 24px 60px; }
.ds-versions-page h1 { font-size: 26px; display: flex; align-items: center; gap: 10px; }
.ds-version-list { list-style: none; margin: 22px 0 0; padding: 0; display: flex; flex-direction: column; gap: 10px; }
.ds-version-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 18px;
  border-radius: var(--radius-md);
  border: 1px solid var(--line);
  background: var(--card);
  box-shadow: var(--shadow-soft);
  transition: transform .15s ease, box-shadow .15s ease;
}
.ds-version-row:hover { transform: translateY(-2px); box-shadow: var(--shadow-lift); text-decoration: none; }
.ds-version-tag { font-family: var(--font-display); font-size: 18px; color: var(--pink-strong); }
.ds-version-when { margin-inline-start: auto; font-size: 12px; color: var(--ink-soft); font-family: var(--font-mono); direction: ltr; unicode-bidi: isolate; }
.ds-version-latest { font-size: 10.5px; font-weight: 700; color: #fff; background: var(--mint-strong); border-radius: 999px; padding: 2px 9px; }

/* ==========================================
   Dark theme
   Overrides the design tokens (so everything
   built on var(--…) recolours automatically),
   plus the few surfaces that hard-coded a light
   value (#fff panels, light pill backgrounds).
   Driven by <html data-theme=""dark"">, which the
   exporter sets as the default and app.js toggles
   / remembers per reader.
   ========================================== */
:root[data-theme=""dark""] {
  --pink: #ff9db0;
  --pink-strong: #ff86a0;
  --pink-pale: #3b2a33;
  --lavender: #b9a6e0;
  --lavender-strong: #c9b8f0;
  --mint: #2e3d2e;
  --mint-strong: #8fd98c;
  --peach: #6a4a2f;
  --cream: #1c1922;
  --cream-deep: #2a2533;
  --card: #241f2d;
  --ink: #ece7f2;
  --ink-soft: #b3a8c2;
  --ink-faint: #7c7189;
  --line: #38313f;
  --warn: #ff8f8f;
  --warn-ink: #ffb4b4;
  --warn-bg: #3a2626;

  --shadow-soft: 0 6px 20px rgba(0, 0, 0, 0.35);
  --shadow-lift: 0 10px 28px rgba(0, 0, 0, 0.45);
}
:root[data-theme=""dark""] body { background: var(--cream); color: var(--ink); }
:root[data-theme=""dark""] .ds-lang-btn,
:root[data-theme=""dark""] .ds-mode-btn,
:root[data-theme=""dark""] .ds-search-input,
:root[data-theme=""dark""] .ds-search-filter,
:root[data-theme=""dark""] .ds-search-results { background: var(--card); color: var(--ink); }
:root[data-theme=""dark""] .ds-search-filter { color: var(--ink-soft); }
:root[data-theme=""dark""] .ds-nav-link:hover { background: rgba(255,255,255,.06); }
:root[data-theme=""dark""] .ds-nav-link.is-current { background: var(--card); color: var(--pink-strong); }
:root[data-theme=""dark""] .ds-nav-count { background: rgba(255,255,255,.08); }
:root[data-theme=""dark""] .ds-lang-btn.is-active { background: var(--pink); color: #241f2d; border-color: var(--pink); }
:root[data-theme=""dark""] .ds-mode-btn.is-active { background: var(--lavender-strong); color: #241f2d; }
:root[data-theme=""dark""] .ds-mode-btn:hover { background: #332b40; }
:root[data-theme=""dark""] .ds-badge.mint { background: #24331f; border-color: var(--mint-strong); color: var(--mint-strong); }
:root[data-theme=""dark""] .ds-badge.lav { background: #2c2740; border-color: var(--lavender); color: var(--lavender-strong); }
:root[data-theme=""dark""] .ds-pill.bool-true,
:root[data-theme=""dark""] .ds-component-toggle.on { background: #24331f; color: var(--mint-strong); }
:root[data-theme=""dark""] .ds-pill.bool-false,
:root[data-theme=""dark""] .ds-component-toggle.off { background: #332b34; color: var(--ink-soft); }
:root[data-theme=""dark""] .ds-pill.enum,
:root[data-theme=""dark""] .ds-ref-chip,
:root[data-theme=""dark""] .ds-prefab-tag,
:root[data-theme=""dark""] .ds-search-result .r-cat { background: #2c2740; color: var(--lavender-strong); }
:root[data-theme=""dark""] .ds-asset-head-meta .t { background: rgba(255,255,255,.07); }
";

        // ==========================================
        // AppJs - theme/app.js contents
        // ==========================================
        public const string AppJs = @"// ==========================================
// Unity DocSnap - Site Behaviour
// Language + Simple/Advanced switching, tree
// helpers, and a fast client-side search over
// the embedded index. No network calls. The only
// persisted state is the chosen UI language and
// detail mode, via a storage helper that falls
// back to in-memory when localStorage is blocked
// (some browsers deny it under a file:// origin).
// ==========================================

(function () {
  'use strict';

  var RTL_LANGS = { fa: true };
  var LANG_STORAGE_KEY = 'unityDocSnapLang';
  var MODE_STORAGE_KEY = 'unityDocSnapMode';
  var THEME_STORAGE_KEY = 'unityDocSnapTheme';
  var DEFAULTS_STORAGE_KEY = 'unityDocSnapDefaults';

  // ==========================================
  // safeStorage
  // localStorage wrapped so it can never throw,
  // with an in-memory fallback for origins (file://
  // in some browsers, private modes) that deny it.
  // Persistence across pages still needs a working
  // localStorage; the fallback simply guarantees the
  // page keeps working (no uncaught exception, no
  // broken language toggle) when it is unavailable.
  // ==========================================
  var memoryStore = {};
  var safeStorage = {
    get: function (key) {
      try {
        var v = window.localStorage.getItem(key);
        if (v !== null && v !== undefined) { return v; }
      } catch (e) { /* denied - fall through to memory */ }
      return Object.prototype.hasOwnProperty.call(memoryStore, key) ? memoryStore[key] : null;
    },
    set: function (key, value) {
      memoryStore[key] = value;
      try { window.localStorage.setItem(key, value); } catch (e) { /* denied - memory only */ }
    }
  };

  // ==========================================
  // syncExportDefaults()
  // A reader's saved language/theme choice should
  // survive reloads of the SAME export - but when a
  // NEW export is made with different defaults
  // (e.g. the exporter now chose Japanese + light),
  // the new defaults must actually show up. Without
  // this, a choice stored while viewing an older
  // export (or the older export's own defaults,
  // stored on first visit) silently overrode every
  // newer export's defaults forever. The exporter's
  // defaults are recorded; whenever they differ from
  // the record, the stored choices are reset to the
  // new defaults.
  // ==========================================
  function syncExportDefaults() {
    var lang = window.__DOCSNAP_LANG__ || 'en';
    var theme = window.__DOCSNAP_THEME__ || 'light';
    var current = lang + '|' + theme;
    if (safeStorage.get(DEFAULTS_STORAGE_KEY) !== current) {
      safeStorage.set(LANG_STORAGE_KEY, lang);
      safeStorage.set(THEME_STORAGE_KEY, theme);
      safeStorage.set(DEFAULTS_STORAGE_KEY, current);
    }
  }

  // ==========================================
  // applyLanguage(lang)
  // Swaps visible text for every element that
  // carries data-en/data-ja/data-fa, localises any
  // input placeholder tagged data-ph-*, flips
  // document direction for RTL, and remembers the
  // choice for the next page.
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

    var phNodes = document.querySelectorAll('[data-ph-en]');
    for (var p = 0; p < phNodes.length; p++) {
      var ph = phNodes[p].getAttribute('data-ph-' + lang) || phNodes[p].getAttribute('data-ph-en');
      if (ph !== null) { phNodes[p].setAttribute('placeholder', ph); }
    }

    var buttons = document.querySelectorAll('.ds-lang-btn');
    for (var b = 0; b < buttons.length; b++) {
      var isActive = buttons[b].getAttribute('data-lang') === lang;
      buttons[b].classList.toggle('is-active', isActive);
      buttons[b].setAttribute('aria-pressed', isActive ? 'true' : 'false');
    }

    safeStorage.set(LANG_STORAGE_KEY, lang);

    // The <head> boot script hides the body while a language
    // swap is pending; the swap just happened, so reveal it.
    root.classList.remove('ds-lang-pending');
  }

  function restoreLanguage() {
    // A reader's own saved choice wins; otherwise fall back to
    // the default language the exporter baked into the page.
    // The stored value is validated against the real buttons
    // (never interpolated into a selector, which could throw
    // on a corrupt stored value and break every wire-up below).
    var stored = safeStorage.get(LANG_STORAGE_KEY);
    var lang = stored || window.__DOCSNAP_LANG__ || 'en';
    var buttons = document.querySelectorAll('.ds-lang-btn');
    var valid = false;
    for (var i = 0; i < buttons.length; i++) {
      if (buttons[i].getAttribute('data-lang') === lang) { valid = true; break; }
    }
    applyLanguage(valid ? lang : (window.__DOCSNAP_LANG__ || 'en'));
  }

  // ==========================================
  // applyTheme / restoreTheme / wireThemeToggle
  // Light / dark colour theme. Sets <html data-theme>,
  // swaps the toggle icon, and remembers the choice.
  // The page ships with the exporter's default already
  // on <html>; a reader's saved choice overrides it.
  // ==========================================
  function applyTheme(theme) {
    var dark = theme === 'dark';
    document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
    var icons = document.querySelectorAll('.ds-theme-icon');
    for (var i = 0; i < icons.length; i++) {
      icons[i].textContent = dark ? '☀️' : '🌙';
    }
    var toggles = document.querySelectorAll('[data-theme-toggle]');
    for (var t = 0; t < toggles.length; t++) {
      toggles[t].setAttribute('aria-pressed', dark ? 'true' : 'false');
    }
    safeStorage.set(THEME_STORAGE_KEY, dark ? 'dark' : 'light');
  }

  function restoreTheme() {
    var stored = safeStorage.get(THEME_STORAGE_KEY);
    var theme = stored || window.__DOCSNAP_THEME__ || document.documentElement.getAttribute('data-theme') || 'light';
    applyTheme(theme);
  }

  function wireThemeToggle() {
    var toggles = document.querySelectorAll('[data-theme-toggle]');
    for (var i = 0; i < toggles.length; i++) {
      toggles[i].addEventListener('click', function () {
        var current = document.documentElement.getAttribute('data-theme');
        applyTheme(current === 'dark' ? 'light' : 'dark');
      });
    }
  }

  function wireLanguageButtons() {
    var buttons = document.querySelectorAll('.ds-lang-btn');
    for (var i = 0; i < buttons.length; i++) {
      buttons[i].addEventListener('click', function (evt) {
        applyLanguage(evt.currentTarget.getAttribute('data-lang'));
      });
    }
  }

  // ==========================================
  // applyMode / restoreMode / wireModeButtons
  // The Simple / Advanced detail-level switch.
  // ==========================================
  function applyMode(mode) {
    var simple = mode !== 'advanced';
    var body = document.body;
    body.classList.toggle('ds-mode-simple', simple);
    body.classList.toggle('ds-mode-advanced', !simple);

    var buttons = document.querySelectorAll('.ds-mode-btn');
    for (var i = 0; i < buttons.length; i++) {
      var isActive = (buttons[i].getAttribute('data-mode') === 'advanced') === !simple;
      buttons[i].classList.toggle('is-active', isActive);
      buttons[i].setAttribute('aria-pressed', isActive ? 'true' : 'false');
    }

    safeStorage.set(MODE_STORAGE_KEY, simple ? 'simple' : 'advanced');
  }

  function restoreMode() {
    var stored = safeStorage.get(MODE_STORAGE_KEY);
    if (stored) { applyMode(stored); }
  }

  function wireModeButtons() {
    var buttons = document.querySelectorAll('.ds-mode-btn');
    for (var i = 0; i < buttons.length; i++) {
      buttons[i].addEventListener('click', function (evt) {
        applyMode(evt.currentTarget.getAttribute('data-mode'));
      });
    }
  }

  // ==========================================
  // wireTreeControls()
  // Expand-all / collapse-all for any .ds-tree.
  // Expand only touches the tree nodes themselves
  // (details.ds-go - folders / GameObjects), never
  // the per-item heavy detail (asset cards, Import
  // Settings, Fields, Prefab Contents). Expanding
  // literally everything used to force layout of
  // every field table on the page at once, which
  // froze the browser on a big Assets page.
  // Collapse still closes everything, so one click
  // always returns the page to its lightest state.
  // ==========================================
  function wireTreeControls() {
    var expandButtons = document.querySelectorAll('[data-tree-expand]');
    for (var i = 0; i < expandButtons.length; i++) {
      expandButtons[i].addEventListener('click', function (evt) {
        var scope = document.getElementById(evt.currentTarget.getAttribute('data-tree-expand'));
        if (!scope) { return; }
        var open = evt.currentTarget.getAttribute('data-mode') === 'expand';
        var details = scope.querySelectorAll(open ? 'details.ds-go' : 'details');
        for (var d = 0; d < details.length; d++) { details[d].open = open; }
      });
    }
  }

  // ==========================================
  // revealHashTarget()
  // Cross-page links and search results point at
  // anchors (#asset-…, #go-…, #folder-…) that now
  // live inside collapsed <details>. A closed
  // <details> never lays out its content, so the
  // browser cannot scroll to it. This opens every
  // <details> on the path to the target (and the
  // target itself when it is a collapsed card),
  // then scrolls it into view.
  // ==========================================
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
      node = node.parentElement;
    }
    window.setTimeout(function () {
      el.scrollIntoView({ block: 'start' });
    }, 0);
  }

  // ==========================================
  // wireBackToTop()
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

  // ==========================================
  // Search
  // Filters the embedded index (window.__DOCSNAP_SEARCH__)
  // entirely in the browser: no network, works under
  // file://. Substring match on name + context, name
  // matches ranked first, results capped so even a huge
  // project stays instant and never freezes the tab.
  // Record shape: { c: category, n: name, s: sub, u: url,
  // g: group('scene'|'asset') }. Links are rewritten with
  // the page-depth prefix so they resolve from any page.
  // ==========================================
  function esc(s) {
    return String(s === null || s === undefined ? '' : s)
      .split('&').join('&amp;')
      .split('<').join('&lt;')
      .split('>').join('&gt;')
      .split(String.fromCharCode(39)).join('&#39;');
  }

  function highlight(text, q) {
    var t = String(text === null || text === undefined ? '' : text);
    if (!q) { return esc(t); }
    var idx = t.toLowerCase().indexOf(q);
    if (idx < 0) { return esc(t); }
    return esc(t.slice(0, idx)) + '<mark>' + esc(t.slice(idx, idx + q.length)) + '</mark>' + esc(t.slice(idx + q.length));
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

    var filterButtons = document.querySelectorAll('.ds-search-filter');
    for (var i = 0; i < filterButtons.length; i++) {
      filterButtons[i].addEventListener('click', function (evt) {
        filter = evt.currentTarget.getAttribute('data-search-filter');
        for (var j = 0; j < filterButtons.length; j++) {
          filterButtons[j].classList.toggle('is-active', filterButtons[j] === evt.currentTarget);
        }
        run(input.value);
      });
    }

    input.addEventListener('input', function () {
      if (debounceTimer) { clearTimeout(debounceTimer); }
      debounceTimer = setTimeout(function () { run(input.value); }, 110);
    });

    input.addEventListener('keydown', function (evt) {
      if (evt.key === 'Escape') { input.value = ''; hide(); input.blur(); }
      else if (evt.key === 'Enter') {
        var first = panel.querySelector('.ds-search-result');
        if (first) { window.location.href = first.getAttribute('href'); }
      }
    });

    document.addEventListener('click', function (evt) {
      if (!evt.target.closest('.ds-search')) { hide(); }
    });

    function hide() { panel.hidden = true; panel.innerHTML = ''; }

    function run(raw) {
      var q = (raw || '').trim().toLowerCase();
      if (q.length < 1) { hide(); return; }

      var results = [];
      var matched = 0;
      for (var i = 0; i < records.length; i++) {
        var r = records[i];
        if (filter !== 'all' && r.g !== filter) { continue; }
        var name = (r.n || '').toLowerCase();
        var sub = (r.s || '').toLowerCase();
        var inName = name.indexOf(q) >= 0;
        var inSub = sub.indexOf(q) >= 0;
        if (!inName && !inSub) { continue; }
        matched++;
        if (results.length < MAX) {
          var score = inName ? (name.indexOf(q) === 0 ? 0 : 1) : 2;
          results.push({ r: r, score: score });
        }
      }
      results.sort(function (a, b) { return a.score - b.score; });
      render(results, q, matched);
    }

    function noMatchesText() {
      var lang = document.documentElement.getAttribute('lang') || 'en';
      if (lang === 'ja') { return 'ヒットなし'; }
      if (lang === 'fa') { return 'موردی پیدا نشد'; }
      return 'No matches';
    }

    function q1(inner) { return String.fromCharCode(39) + inner + String.fromCharCode(39); }

    function render(items, q, matched) {
      panel.hidden = false;
      if (items.length === 0) {
        panel.innerHTML = '<div class=' + q1('ds-search-empty') + '>' + esc(noMatchesText()) + '</div>';
        return;
      }
      var html = '';
      for (var i = 0; i < items.length; i++) {
        var r = items[i].r;
        var href = prefix + (r.u || '');
        html += '<a class=' + q1('ds-search-result') + ' href=' + q1(esc(href)) + '>'
          + '<span class=' + q1('r-top') + '><span class=' + q1('r-name') + '>' + highlight(r.n, q) + '</span>'
          + '<span class=' + q1('r-cat') + '>' + esc(r.c) + '</span></span>'
          + '<span class=' + q1('r-sub') + '>' + highlight(r.s, q) + '</span></a>';
      }
      if (matched > items.length) {
        html += '<div class=' + q1('ds-search-more') + '>+' + (matched - items.length) + ' more' + (truncatedIndex ? ' (index capped)' : '') + '</div>';
      }
      panel.innerHTML = html;
    }
  }

  document.addEventListener('DOMContentLoaded', function () {
    syncExportDefaults();
    restoreLanguage();
    wireLanguageButtons();
    restoreTheme();
    wireThemeToggle();
    restoreMode();
    wireModeButtons();
    wireTreeControls();
    wireBackToTop();
    wireSearch();
    revealHashTarget();
    window.addEventListener('hashchange', revealHashTarget);
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
