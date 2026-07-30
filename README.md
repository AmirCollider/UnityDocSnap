<a id="top"></a>
<p align="center">
  <img src="Docs~/logo.png" alt="Unity DocSnap logo" width="180"/>
</p>

<h1 align="center">🧋 Unity DocSnap ✨</h1>

<p align="center"><em>Snap your whole Unity project into a cozy little website.</em></p>
<p align="center"><em>あなたのUnityプロジェクトを、まるごと可愛いWebサイトに閉じ込めます。</em></p>
<p align="center"><em>کل پروژه‌ی یونیتی‌ت رو تبدیل کن به یه وب‌سایت کوچولوی دنج.</em></p>

<p align="center">
  <a href="#english">English</a> ・
  <a href="#japanese">日本語</a> ・
  <a href="#persian">فارسی</a>
</p>

<p align="center">
  <img alt="license" src="https://img.shields.io/badge/license-MIT-ffb6c1?style=flat-square">
  <img alt="unity version" src="https://img.shields.io/badge/Unity-2021.3%2B-b19cd9?style=flat-square&logo=unity&logoColor=white">
  <img alt="editor extension" src="https://img.shields.io/badge/type-Editor%20Extension-ffd6e8?style=flat-square">
  <img alt="prs welcome" src="https://img.shields.io/badge/PRs-welcome-c8f7c5?style=flat-square">
  <img alt="kawaii level" src="https://img.shields.io/badge/kawaii-100%25-ffb6c1?style=flat-square">
</p>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<a id="english"></a>
## 🧋 English

Ever open a project after two weeks away and have absolutely no idea what's inside your own Hierarchy anymore? **Unity DocSnap** remembers so you don't have to.

It's an Editor extension that walks every Scene in your project — every GameObject, every Component, every field, every reference — and every Asset's import settings, then bakes all of it into a clean, offline HTML site you can open in any browser. No server, no build step, just double-click and go. Built for the humans who need to remember what they made, and for the AI assistants that need the full picture handed to them in one clean file instead of forty screenshots. 🍰

### ✨ Features

- 🌳 **Full Hierarchy snapshot** — every GameObject in a scene, nested exactly as it sits in the Hierarchy window, with its tag, layer, active state, and static flags.
- 🔍 **Complete Inspector export** — every Component on every GameObject, every serialized field, and its live value, exactly as the Inspector shows it.
- 🔗 **Real connections, not just names** — when a script references another GameObject, a Prefab, or a ScriptableObject, that reference becomes a clickable link in the output, so you can trace exactly how a scene is wired together.
- 🖼️ **Asset *info*, not asset files** — point DocSnap at a folder, say `Assets/Images/Backgrounds`, and it exports the metadata for every file inside: import settings, compression, max size, dimensions, format. The original file is never copied. Two opt-ins do put real content in the export and both say so: **thumbnails** (on by default) write small PNG previews of your images, and **Export Full Project With Files** copies the asset bytes themselves. Turn thumbnails off in Project Settings for a strictly metadata-only export.
- 📁 **One menu entry per Scene** — DocSnap scans your project and lists every Scene as its own menu item, so exporting one Scene is a single click.
- 🖱️ **Right-click, anywhere** — every menu action is also available from the Project window's right-click context menu on any folder or asset.
- 🌐 **An actual local website** — everything bakes into a self-contained `index.html` plus a handful of linked pages, complete with a sidebar and cross-links between objects and the assets they reference.
- 🤖 **Built for AI too** *(Plus / Pro)* — alongside the pretty HTML, DocSnap writes a structured JSON export, so handing your whole project's context to an AI assistant takes one file instead of a screen-sharing session.
- 🧩 **Editor-only** — lives entirely inside an `Editor` assembly. Zero runtime cost, zero added build size.
- 🩺 **Project health that tells you *where*** — a dedicated `issues.html` lists every missing script, broken object reference and unresolved asset **individually**, with the GameObject path it sits on (`Canvas/Menu/StartButton`), the component and field that hold it (`MenuController › targetScene`), and a link that opens the collapsed card it lives in and flashes it. The dashboard leads with the counts, and each one is a link into that report filtered to its own kind.
- 🙋 **…and whose fault it is** — findings in folders Unity or a package installed into `Assets/` (TextMesh Pro, the render-pipeline `Settings` a template creates, package Samples) are separated from your own, with a **My files / Unity & packages / All** tab defaulting to yours. "8 broken references" where none of the eight is yours to fix now reads as clean instead of alarming.
- ✨ **Two skins, and it measures before choosing** — **Cozy** (pastel gradients, soft shadows, a bobbing boba mascot) and **Lite** (flat, fast, no animation). Which one the site opens with is decided from your machine's RAM, cores and GPU plus how heavy the project is; you can switch any time, and if you pick Cozy against the measurement it shows you the numbers behind the recommendation.
- 🔎 **Filter any page in place** — every Hierarchy and folder tree has a filter box: type four letters and a Scene with 20,000 GameObjects collapses to the handful that match, ancestors kept so the path still reads.
- 🚫 **Exclude what you did not write** — one line like `Assets/Plugins` in Project Settings keeps imported Asset Store content out of the walk, the tree, the search index and the counts. Wildcards (`*`, `?`) work too, and what was excluded is stated in the output rather than silently missing.
- 🎬 **Build Settings scenes only** — optionally document just the Scenes your game actually ships, skipping the test beds and sample Scenes that arrived with a package.
- 📦 **One file for an AI** *(Plus / Pro)* — `summary/ai-bundle.md` is every summary in the export concatenated into a single document, so a whole project is one paste instead of a folder.
- ⏹️ **Cancelable** — both passes show a progress bar you can stop.
- ⚡ **Built for big projects** — the site skips layout for everything off-screen, so a Scene with tens of thousands of objects opens as fast as a small one. Minimal, modern UI in light and dark, keyboard-driven (`/` to search, `[` to collapse the sidebar), with copy-path buttons where you'd reach for them.

### 💎 Free, Plus and Pro

Unity DocSnap ships in three editions. **Free needs no key, no account and no network** — install the package and every export in the first three rows works.

| | Free | Plus $19.99 | Pro $49.99 |
| --- | :---: | :---: | :---: |
| The full offline site — hierarchy, Inspector data, references, cross-links | ✅ | ✅ | ✅ |
| Project health report (`issues.html`) — every missing script and broken reference, with the object path and field | ✅ | ✅ | ✅ |
| Packages page, search, light/dark, both skins, all three languages, thumbnails, exclude rules | ✅ | ✅ | ✅ |
| Plan page (`plan.html`) — which edition made the export, what it includes, and a link to verify the licence | ✅ | ✅ | ✅ |
| 🤖 **AI-ready summaries** — `summary/*.md`, `summary/*.json` and the one-paste `summary/ai-bundle.md` | — | ✅ | ✅ |
| 🔁 **Changes page** — what moved between two exports, old and new bytes side by side | — | ✅ | ✅ |
| No "free edition" line in the exported footer | — | ✅ | ✅ |
| 📚 **Version history** | 3 snapshots | 5 snapshots | unlimited |
| ⚡ **Incremental `Update Previous Export`** — reuse the Scenes that did not change | — | — | ✅ |
| 🤖 **CI automation** — `DocSnapAPI` and `-executeMethod` | — | — | ✅ |
| 📁 **File copies** — the real asset bytes in `source-files/` | — | — | ✅ |
| 📦 **Whole-project `.unitypackage` backup** | — | — | ✅ |
| ✨ **Your own logo** in the exported sidebar | — | — | ✅ |

**Both paid editions are one-off purchases.** Not subscriptions. One machine per key, and moving to a new computer is self-service — release the old one from `Unity DocSnap ▸ Licence & Pro Features`, or from [the licence page](https://amircollider.n95pluss.workers.dev/license) when that machine is gone. All 1.x updates are included.

→ **[Compare all three](https://amircollider.n95pluss.workers.dev/unity-docsnap)** · **[Buy Plus](https://amircollider.n95pluss.workers.dev/checkout?tier=plus&lang=en)** · **[Buy Pro](https://amircollider.n95pluss.workers.dev/checkout?tier=pro&lang=en)**

**Why Plus exists.** The AI summaries and the Changes page are what most people actually come for, and a lot of those people have no use for CI automation, file copies or project backups. Making them buy the $49.99 tier to get two features means most of them buy nothing at all. Plus is those two, on their own, at $19.99.

**A note on what the Free edition does.** Everything in the first three rows is the entire exporter, and it is not time-limited or nagged. Free hits exactly three walls in normal use: it keeps three snapshots rather than every one (Plus keeps five), it re-scans everything instead of reusing unchanged Scenes, and it writes one credit line into the footer of the site it generates. Where an edition's option is switched on and not licensed, the export still runs — the option is skipped and the completion dialog says which, and at what price. The one exception is `DocSnapAPI`, which refuses outright, because a build agent that half-succeeds publishes a docs folder missing the outputs the pipeline existed to produce and still goes green.

**What leaves your machine.** In the Free edition, nothing: it never touches the network. A paid edition sends one request when activating and one when renewing, carrying the licence key, a salted hash of Unity's `deviceUniqueIdentifier`, and the package version. Nothing about your project — not its name, not its path, not its size — is ever transmitted. After activation the licence verifies offline for 45 days against a public key compiled into the package, so there is never a network call in front of an export.

### 📋 Requirements

- Unity **2021.3 LTS** or newer (Unity 6.x supported)
- No third-party dependencies

### 📦 Installation

**Option A — Package Manager (recommended)**
1. Open **Window → Package Manager**
2. Click **+ → Add package from git URL…**
3. Paste `https://github.com/AmirCollider/UnityDocSnap.git`
4. Click **Add**

**Option B — Manual**
1. Download or clone this repository
2. Copy the `Editor/UnityDocSnap` folder into your project's `Assets` folder — including the `Site~` sub-folder, which holds the generated site's stylesheet, script and fonts
3. Unity compiles it automatically — no restart needed

### 🚀 Usage

Once installed, a new menu shows up in Unity's top menu bar: **Unity DocSnap**.

```
Unity DocSnap
├── Export Scene
│   ├── MainMenu
│   ├── Level01
│   └── Level02              ← one entry per Scene found in your project
├── Export Asset Info
│   ├── Entire Assets Folder
│   └── Selected Folder…
├── Export Full Project      (Scenes + Assets, all cross-linked)
├── Export Full Project With Files
├── Update Previous Export    (fast incremental refresh — reuses unchanged Scenes/Assets)
├── Open Output Folder
├── Licence & Pro Features
└── About Unity DocSnap
```

The generated site also has a **search box** in the sidebar (All / Scenes / Assets), a **Packages** page listing every package the project depends on, and marks **Prefab instances / variants / overridden fields** throughout.

**Exporting a single Scene**
`Unity DocSnap → Export Scene → [YourSceneName]` walks that Scene's entire Hierarchy and writes a full snapshot of every GameObject and Component into the output folder.

**Exporting asset info**
`Unity DocSnap → Export Asset Info → Selected Folder…` lets you pick a folder — for example `Assets/Images/Backgrounds` — and DocSnap exports the Inspector info for every file inside it. For an image like `bakery_street.png`, that means Texture Type, sRGB, Compression, Max Size, Filter Mode, Wrap Mode, generated mip maps, and every other import setting, captured exactly as Unity has it configured.

> **About the pixels.** The original `bakery_street.png` is never copied. With **Generate Image Thumbnails** on — which is the default — DocSnap does write a downscaled PNG preview of it into `theme/thumbs/`, so the exported page can show you what the texture actually looks like. That preview is real image data. If the export has to carry metadata and nothing else, turn thumbnails off in **Project Settings → Unity DocSnap** before exporting.

**Opening the result**
By default, output lands in `<ProjectRoot>/UnityDocSnap_Output/`. Use `Unity DocSnap → Open Output Folder` to jump straight there, then open `index.html` in any browser.

### 📁 Output Structure

Every export lands in **its own versioned folder**. The output root is a shelf of them, and each one is a complete, self-contained site — so a snapshot you took last month is still there, intact, next to today's.

The root itself holds only the two pages that lead into that shelf:

```
UnityDocSnap_Output/
├── index.html         ← redirects to the newest version
├── versions.html      ← the shelf: every export, newest first
├── V1.0.0/            ← one complete site per export
├── V1.0.1/
└── V1.1.0/
```

Inside **one version folder**, every export writes **two forms of the same information** side by side: the **full** offline site (browse it, or read the raw JSON) and a **simple** set of short summaries — Markdown *and* JSON — gathered in the `summary/` folder, small enough to paste straight into an AI assistant.

```
V1.1.0/
├── index.html         ← the full offline site (start here)
├── issues.html        ← every broken reference / missing script, linked to the exact object
├── packages.html      ← packages the project depends on (Unity + third-party)
├── changes.html       ← what changed vs an earlier version (when change tracking is on)
├── plan.html          ← the plan this export was made on: what it includes and what it does not
├── summary.md         ← project index → points into summary/
├── export-info.txt    ← what this export contains, in plain words
├── export-info.json   ← the same, machine-readable
├── summary/           ← simple, AI-friendly (short — hand these to an AI)
│   ├── ai-bundle.md                 ← ALL of the below in one file (one paste)
│   ├── scene-MainMenu.md            ← readable
│   ├── scene-MainMenu.json          ← structured (a few hundred lines)
│   ├── folder-Images_Backgrounds.md
│   └── folder-Images_Backgrounds.json
├── scenes/
│   └── MainMenu.html                ← full interactive page
├── folders/
│   └── Images_Backgrounds.html
├── data/              ← full, every-field JSON (the advanced form)
├── theme/             ← css/js + search-index.js + thumbnails for the site itself
├── source-files/      ← optional verbatim asset copies (With Files export only)
├── changes-files/     ← the old and new bytes of each changed file, for review
└── project-backup.unitypackage   ← optional whole-project backup
```

Version names run `V1.0.0 → V1.0.9 → V1.1.0 → … → V9.9.9 → V10.0.0`, or you can type your own name in the export window. **Update Previous Export** re-exports *into* the newest folder instead of making a new one, reusing whatever has not changed.

> **Re-exporting onto a folder made by a higher edition.** A version folder keeps whatever the export that built it produced, so a folder built while Plus or Pro was active still holds that tier's `summary/`, `source-files/`, `changes.html` and `project-backup.unitypackage`. When a full export lands back on that folder from an edition that does not include those, they are removed — the folder is rewritten to match the plan that produced it, and the export-complete dialog lists every file it took out. Nothing is touched in a folder this export does not write into, and a tier that still holds a feature keeps its files even when the box is left unticked on a later run.

The site itself has a **Simple / Advanced** toggle in the sidebar: *Simple* shows a clean skim (hierarchy, custom-script configuration, key asset facts), *Advanced* shows every serialized field. It opens in Simple by default and remembers your choice.

### 🤖 Automation & CI

> **Pro.** Scripted and command-line exports need a Pro licence. A Free Editor fails the call with the reason attached and exits non-zero in `-batchmode` — deliberately, rather than half-succeeding and publishing a docs folder missing the outputs the pipeline was built to produce. Every menu item works as normal in Free.

Every export is available from C# and from the command line, so documentation can be regenerated on merge instead of by remembering to click a menu item.

```csharp
using AmirCollider.UnityDocSnap.Editor;

DocSnapResult result = DocSnapAPI.ExportFullProject();
if (!result.Succeeded) { Debug.LogError(result.Message); }
```

```bash
Unity -batchmode -quit -projectPath . \
      -executeMethod AmirCollider.UnityDocSnap.Editor.DocSnapAPI.RunFromCommandLine \
      -docsnapOutput Build/Docs \
      -docsnapExclude "Assets/Plugins;Assets/ThirdParty"
```

In `-batchmode` the process exits non-zero when the export fails, so a red build means a real problem. No dialog is ever shown from an API-driven or batch export.

| Argument | Effect |
| --- | --- |
| `-docsnapUpdate` | Refresh the newest version folder in place (incremental — the one to run on a schedule) |
| `-docsnapScene <path>` | Export one Scene; repeatable |
| `-docsnapFolder <path>` | Export one folder under `Assets/`; repeatable |
| `-docsnapWithFiles` | Also copy the real asset bytes into `source-files/` |
| `-docsnapOutput <path>` | Output root, absolute or project-relative |
| `-docsnapExclude "a;b"` | Exclude patterns, `;` separated |
| `-docsnapLanguage en\|ja\|fa` | Language the site opens in |
| `-docsnapTheme light\|dark` | Theme the site opens in |
| `-docsnapSkin auto\|cozy\|lite` | Skin the site opens in |
| `-docsnapNoThumbnails` | Metadata only — no pixel previews |
| `-docsnapNoFonts` | Skip the ~570 KB of embedded web fonts |
| `-docsnapSaveSettings` | Also **write** the settings above to the committed settings file |

With no action argument it runs a full project export.

> **These settings apply to the run, not to the repository.** Everything above is applied to that one export and is *not* written to `ProjectSettings/UnityDocSnapSettings.json`, so a build agent leaves the working tree exactly as it found it and a `git diff --exit-code` step still passes. Pass `-docsnapSaveSettings` for the rare job whose whole purpose is to update that committed file.
>
> An argument that is missing its value — `-docsnapOutput` followed by another flag, or sitting last on the command line — is **refused**, with the offending argument named on the console and a non-zero exit in `-batchmode`. It is never quietly reinterpreted: an export that silently fell back to the default output folder and reported success is how a pipeline publishes nothing and still goes green.

`DocSnapAPI` is the **only** public type in the package. Everything else is `internal` on purpose, so the rest of the tool stays free to change.

### ♻️ Files Unity rewrites by itself

TextMesh Pro's dynamic font assets keep their glyph table and atlas texture inside the `.asset` file, and TMP re-serialises them whenever it renders a character the atlas does not have yet — including while the Editor draws its own UI at startup. Open Unity, close Unity, and `LiberationSans SDF - Fallback.asset` genuinely has different bytes, without anybody having touched it.

The Changes page keeps these out of the change counts and lists them in their own collapsed **Rewritten by Unity** group, with the pattern that classified each one shown beside it. They are still documented in full like any other asset — separated, never silently dropped. Add your own patterns under **Project Settings → Unity DocSnap → Rewritten-by-Unity paths**, in the same syntax as the exclude list.

Bake output — lightmaps, NavMesh data, occlusion culling — is deliberately *not* on this list. Those files change because somebody pressed Bake, which is exactly the kind of change the page exists to report.

### ⚙️ Settings, and where they live

Settings that describe the **project** — exclude patterns, not-my-code folders, rewritten-by-Unity paths, output path, the site's default language / theme / skin, thumbnails, embedded fonts — are written to:

```
ProjectSettings/UnityDocSnapSettings.json
```

**Commit that file.** It is plain, ordered JSON meant to be read in a pull request, and it is what makes one team and its CI agent produce the same export instead of one per machine. Settings that describe *you* rather than the project — the export window's own language, and the absolute path to a custom logo — stay in `EditorUserSettings` and are not written there.

Upgrading from 0.9.x migrates your existing settings into the new file automatically, once, without overwriting anything the repository already had.

### 📏 What an export leaves out

DocSnap documents a project; it is not a serialiser you could rebuild one from. A handful of caps keep a single pathological object from producing an export nobody can open, and every one of them is marked in the output (`"truncated": true` in the JSON, a note on the page) rather than silently applied:

| Cap | Limit |
| --- | --- |
| Array elements per field | 50 (10 for a nested array) |
| Fields per object | 1,000 |
| Nesting depth | 14 |
| Assets rendered per folder node | 300 |
| Health findings | 400 per Scene/folder, 2,000 rendered |
| Search index records | 20,000 |
| `ai-bundle.md` | 600,000 characters |

Also outside the walk by design: anything Unity itself ignores (hidden folders, `Foo~` folders, `CVS`), and anything your exclude patterns remove.

### 🧠 Built for Humans *and* AI

Every exported page follows the same clean, predictable structure — proper headings, labeled fields, consistent IDs. A person can skim it in a browser in under a minute. An AI assistant can be handed the `data/` folder (or a single JSON file) and immediately understand your Hierarchy, your Components, and your asset settings — without you typing out an explanation by hand.

### 🗺️ Roadmap

- [x] Optional thumbnail previews for image assets
- [x] Search across the whole exported site
- [x] Diff view between two exports
- [x] Dark mode for the generated site 🌙
- [x] Versioned exports + whole-project `.unitypackage` backup

### 🤝 Contributing

Issues and pull requests are always welcome.

**Where things live**

- `Editor/UnityDocSnap/` — the tool itself. Everything is `internal`; it is a self-contained editor tool, not a public API.
- `Editor/UnityDocSnap/Site~/` — the generated site's own `style.css`, `app.js`, `fonts.css` and `logo.svg`, as **real files**. Edit them directly; they are read at export time and written into each version folder. The trailing `~` keeps Unity from importing them, which is why they need no `.meta`.
- `Tests/Editor/` — EditMode tests (NUnit), reaching the `internal` types via `InternalsVisibleTo`.

**Before opening a PR**

```bash
python3 .github/scripts/validate_package.py   # version sync, .meta coverage, site assets
```

Then run the EditMode tests from **Window → General → Test Runner** in any project that has the package installed. CI runs both on every push, and the Unity tests against 2021.3 (the declared floor) and Unity 6.

**Releases**

`package.json`, `DocSnapConstants.Version` and the `CHANGELOG.md` heading all carry the version and must agree — CI fails if they do not. To publish, tag the commit and push the tag; the release workflow does the rest:

```bash
git tag v0.10.1 && git push origin v0.10.1
```

Tagging is what lets a user pin a version in the Package Manager (`…UnityDocSnap.git#v0.10.1`) instead of always getting whatever the default branch happens to be.

### 📜 License

MIT — see [LICENSE](LICENSE).

### 💌 Credits

Made with 🧋 by [AmirCollider](https://github.com/AmirCollider).

If Unity DocSnap saves you some digging around later, a ⭐ on the repo goes a long way.

<p align="right"><a href="#top">⬆ Back to top</a></p>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<a id="japanese"></a>
## 🍰 日本語

2週間ぶりにプロジェクトを開いて、自分のHierarchyの中身を全部忘れてしまったこと、ありませんか?**Unity DocSnap** が代わりに覚えていてくれます。

これはエディタ拡張機能で、プロジェクト内のすべてのSceneを歩き回り——すべてのGameObject、すべてのComponent、すべてのフィールド、すべての参照——そしてすべてのAssetのインポート設定までをまるごとスナップショットして、ブラウザでそのまま開けるきれいなオフラインHTMLサイトに焼き上げます。サーバーもビルドも不要、ダブルクリックで開くだけです。「自分が何を作ったか思い出せない人」のためにも、「40枚のスクリーンショットではなく1つの整理された情報が欲しいAIアシスタント」のためにも作られました。🍰

### ✨ 特徴

- 🌳 **完全なHierarchyスナップショット** — シーン内のすべてのGameObjectを、Hierarchyウィンドウそのままの入れ子構造で。タグ、レイヤー、アクティブ状態、Staticフラグも含めて。
- 🔍 **完全なInspectorエクスポート** — すべてのGameObjectのすべてのComponent、すべてのシリアライズされたフィールドとその現在値を、Inspectorに表示されている通りに。
- 🔗 **名前だけでなく、本当のつながり** — あるスクリプトが別のGameObject・Prefab・ScriptableObjectを参照している場合、出力内でクリック可能なリンクになります。シーンがどう組み立てられているか、たどることができます。
- 🖼️ **ファイルの中身ではなく、ファイルの情報** — `Assets/Images/Backgrounds` のようなフォルダを指定すると、中の全ファイルの *メタデータ*(インポート設定、圧縮方式、最大サイズ、解像度、フォーマットなど)をエクスポートします。元ファイル自体はコピーしません。ただし実際の中身が出力に入るオプションが2つあり、どちらも明示されています:**サムネイル**(既定でオン)は画像の縮小PNGプレビューを書き出し、**Export Full Project With Files** はアセットのバイト列そのものをコピーします。メタデータだけの出力にしたい場合は Project Settings でサムネイルをオフにしてください。
- 📁 **Sceneごとにメニュー項目を自動生成** — DocSnapがプロジェクトをスキャンし、すべてのSceneをそれぞれ独立したメニュー項目として表示します。
- 🖱️ **どこでも右クリック** — すべてのメニュー操作は、Projectウィンドウでフォルダやアセットを右クリックしたコンテキストメニューからも実行できます。
- 🌐 **本物のローカルWebサイト** — すべてが `index.html` と数枚のリンクされたページにまとめられ、サイドバーと、オブジェクト同士・参照アセット間の相互リンク付きです。
- 🤖 **AIのためにも** — 見やすいHTMLと一緒に、構造化されたJSONも出力します。プロジェクト全体の情報をAIアシスタントに渡すのに、画面共有ではなく1つのファイルで済みます。
- 🧩 **エディタ専用** — すべて `Editor` アセンブリの中に収まります。ランタイムコストはゼロ、ビルドサイズへの影響もゼロです。
- 🩺 **「どこが」壊れているかまで分かる健康状態** — 専用の `issues.html` が、欠落スクリプト・切れた参照・型不明アセットを**1件ずつ**列挙します。該当GameObjectのパス(`Canvas/Menu/StartButton`)、それを保持しているコンポーネントとフィールド(`MenuController › targetScene`)まで示し、リンクをたどると折りたたまれたカードが開いてハイライトされます。ダッシュボードは件数を先頭に出し、その件数自体が種類別に絞り込んだレポートへのリンクです。
- 🙋 **さらに「誰の責任か」まで** — Unity やパッケージが `Assets/` にインストールしたフォルダ内の指摘(TextMesh Pro、テンプレートが作る `Settings`、パッケージの Samples など)は自分のものと分けて表示され、**自分のファイル / Unity・パッケージ / すべて** タブは自分のファイルが既定です。8件のうち自分で直せるものが0件なら、警告ではなく「問題なし」と読めます。
- ✨ **2つのスキンと、選ぶ前の計測** — **コージー**(パステルのグラデーション、やわらかい影、揺れるタピオカマスコット)と **ライト**(フラット・高速・アニメなし)。どちらで開くかは、このマシンのRAM・コア数・GPUとプロジェクトの重さから決まります。いつでも切り替えられますし、計測結果に逆らってコージーを選んだ場合は、その根拠となった数値が表示されます。
- 🔎 **ページ内をその場で絞り込み** — Hierarchy とフォルダツリーには絞り込みボックスが付きました。4文字打てば、GameObject が2万個あるシーンでも一致する数件まで畳まれます(パスが読めるよう親は残ります)。
- 🚫 **自分が書いていないものを除外** — Project Settings に `Assets/Plugins` のような1行を書くだけで、インポートしたアセットストアの中身をファイル走査・ツリー・検索インデックス・集計のすべてから外せます(`*` `?` のワイルドカード対応)。除外した内容は出力側にも明記されます。
- 🎬 **Build Settings のシーンだけ** — 実際に出荷するシーンだけを対象にし、テスト用やパッケージ付属のサンプルシーンを飛ばせます。
- 📦 **AIに渡す1ファイル** — `summary/ai-bundle.md` はエクスポート内のすべての要約を1つにまとめたもので、フォルダごとではなく1回の貼り付けでプロジェクト全体を渡せます。
- ⏹️ **キャンセル可能** — どちらの処理もプログレスバーから中断できます。
- ⚡ **大規模プロジェクト向け** — 画面外の要素はレイアウトを省略するため、数万オブジェクトのシーンでも小さなシーンと同じ速さで開きます。ライト/ダーク対応のミニマルでモダンなUI、キーボード操作(`/` で検索、`[` でサイドバー折りたたみ)、必要な場所にパスのコピーボタン。

### 💎 無料版・Plus・Pro

Unity DocSnap には 3 つのエディションがあります。**無料版はキーもアカウントもネットワークも不要**です。インストールすれば、下表の最初の 3 行はすべて動作します。

| | 無料版 | Plus $19.99 | Pro $49.99 |
| --- | :---: | :---: | :---: |
| オフラインサイト一式 — 階層、インスペクター情報、参照、相互リンク | ✅ | ✅ | ✅ |
| プロジェクトのヘルスレポート(`issues.html`)— 欠落スクリプトと壊れた参照を、オブジェクトのパスとフィールド付きで | ✅ | ✅ | ✅ |
| パッケージページ、検索、ライト/ダーク、2 つのスキン、3 言語、サムネイル、除外ルール | ✅ | ✅ | ✅ |
| プランページ(`plan.html`)— どのエディションで作成したか、含まれる機能、ライセンス確認リンク | ✅ | ✅ | ✅ |
| 🤖 **AI 向けサマリー** — `summary/*.md`、`summary/*.json`、1 回の貼り付けで済む `summary/ai-bundle.md` | — | ✅ | ✅ |
| 🔁 **変更ページ** — 2 つのエクスポート間の差分を、変更前後のバイトと並べて | — | ✅ | ✅ |
| エクスポートのフッターに「無料版」表記なし | — | ✅ | ✅ |
| 📚 **バージョン履歴** | 3 件 | 5 件 | 無制限 |
| ⚡ **差分更新(`Update Previous Export`)** — 変更のないシーンを再利用 | — | — | ✅ |
| 🤖 **CI 自動化** — `DocSnapAPI` と `-executeMethod` | — | — | ✅ |
| 📁 **ファイル本体のコピー** — アセットの実バイトを `source-files/` へ | — | — | ✅ |
| 📦 **プロジェクト全体の `.unitypackage` バックアップ** | — | — | ✅ |
| ✨ **自社ロゴ** をエクスポートのサイドバーに | — | — | ✅ |

**有料版はいずれも買い切り**で、サブスクリプションではありません。キー 1 つにつき 1 台で、別のマシンへの移行は自分で行えます(`Unity DocSnap ▸ Licence & Pro Features` から解除するか、そのマシンが手元にない場合は[ライセンスページ](https://amircollider.n95pluss.workers.dev/license)から)。1.x のアップデートはすべて含まれます。

→ **[Compare all three](https://amircollider.n95pluss.workers.dev/unity-docsnap)** · **[Buy Plus](https://amircollider.n95pluss.workers.dev/checkout?tier=plus&lang=en)** · **[Buy Pro](https://amircollider.n95pluss.workers.dev/checkout?tier=pro&lang=en)**

**Plus がある理由。** 多くの方が実際に求めているのは AI サマリーと変更ページであり、その大半は CI 自動化・ファイルコピー・プロジェクトバックアップを必要としていません。その 2 つのために $49.99 のエディションを買わせると、結局ほとんどの方は何も購入しません。Plus はその 2 つだけを $19.99 で提供します。

**無料版でできることについて。** 上の 3 行はエクスポーターのすべてであり、期限も催促もありません。通常の利用で無料版が制限に触れるのは 3 か所だけです。スナップショットを 3 件までしか保持しないこと(Plus は 5 件)、変更のないシーンを再利用せず毎回すべて再スキャンすること、そして生成されるサイトのフッターにクレジット行が 1 行入ることです。ライセンスのないオプションを有効にしていても、エクスポート自体は実行されます。該当のオプションがスキップされ、完了ダイアログにその内容と価格が表示されます。唯一の例外が `DocSnapAPI` で、こちらは明示的に失敗します。ビルドエージェントが中途半端に成功すると、パイプラインが本来生成するはずの出力を欠いたまま成功として扱われてしまうためです。

**送信される情報。** 無料版では一切ありません(ネットワークに接続しません)。有料版は有効化時と更新時にそれぞれ 1 回リクエストを送り、ライセンスキー、Unity の `deviceUniqueIdentifier` のソルト付きハッシュ、パッケージのバージョンのみを含みます。プロジェクトの名前・パス・規模など、プロジェクトに関する情報は一切送信されません。有効化後はパッケージに埋め込まれた公開鍵で 45 日間オフライン検証されるため、エクスポートの前にネットワーク通信が入ることはありません。

### 📋 必要環境

- Unity **2021.3 LTS** 以降(Unity 6系にも対応)
- サードパーティ製の依存関係なし

### 📦 インストール

**方法A — Package Manager(推奨)**
1. **Window → Package Manager** を開く
2. **+ → Add package from git URL…** をクリック
3. `https://github.com/AmirCollider/UnityDocSnap.git` を貼り付ける
4. **Add** をクリック

**方法B — 手動インストール**
1. このリポジトリをダウンロードまたはクローン
2. `Editor/UnityDocSnap` フォルダをプロジェクトの `Assets` フォルダにコピー(生成サイトのCSS・JS・フォントが入っている `Site~` サブフォルダも忘れずに)
3. Unityが自動的にコンパイルします。再起動は不要です

### 🚀 使い方

インストール後、Unityの上部メニューバーに **Unity DocSnap** という新しいメニューが追加されます。

```
Unity DocSnap
├── Export Scene
│   ├── MainMenu
│   ├── Level01
│   └── Level02              ← プロジェクト内のSceneごとに項目が追加されます
├── Export Asset Info
│   ├── Entire Assets Folder
│   └── Selected Folder…
├── Export Full Project      (Scene + Assetをまとめて、すべて相互リンク済みで)
├── Export Full Project With Files
├── Update Previous Export    (増分更新 — 変更のないScene/Assetは再利用)
├── Open Output Folder
├── Licence & Pro Features
└── About Unity DocSnap
```

生成されたサイトには、サイドバーの**検索ボックス**(All / Scenes / Assets)、依存パッケージ一覧の**Packages**ページ、そして**Prefabインスタンス/バリアント/上書きされたフィールド**の表示も追加されました。

**Sceneを1つだけエクスポートする**
`Unity DocSnap → Export Scene → [Scene名]` を選ぶと、そのSceneのHierarchy全体を歩き、すべてのGameObjectとComponentのスナップショットを出力フォルダに書き出します。

**アセット情報をエクスポートする**
`Unity DocSnap → Export Asset Info → Selected Folder…` でフォルダを選べます。例えば `Assets/Images/Backgrounds` を選ぶと、中の全ファイルのInspector情報がエクスポートされます。`bakery_street.png` のような画像なら、Texture Type、sRGB、Compression、Max Size、Filter Mode、Wrap Mode、Generate Mip Mapsなど、Unityに設定されているインポート設定がそのまま記録されます。画像のピクセルデータそのものはプロジェクトの外に出ません。

**結果を開く**
デフォルトでは出力先は `<プロジェクトルート>/UnityDocSnap_Output/` です。`Unity DocSnap → Open Output Folder` で直接そのフォルダを開き、`index.html` をブラウザで開いてください。

### 📁 出力構造

エクスポートは毎回**専用のバージョンフォルダ**に書き出されます。出力ルートはその棚であり、それぞれが完全に独立した1つのサイトです。先月撮ったスナップショットは、今日のものと並んでそのまま残ります。

ルート直下にあるのは、その棚へ案内する2つのページだけです。

```
UnityDocSnap_Output/
├── index.html         ← 最新バージョンへリダイレクト
├── versions.html      ← 棚:全エクスポートを新しい順に一覧
├── V1.0.0/            ← エクスポート1回につき、完全なサイト1つ
├── V1.0.1/
└── V1.1.0/
```

**1つのバージョンフォルダ**の中には、**同じ情報が2つの形**で並んで書き出されます。**フル版**のオフラインサイト(ブラウザで見る、または生のJSONを読む)と、AIアシスタントにそのまま貼り付けられる**シンプル版**の短い要約(MarkdownとJSONの両方)で、後者はすべて `summary/` フォルダにまとまっています。

```
V1.1.0/
├── index.html         ← フル版のオフラインサイト(まずここから)
├── issues.html        ← 切れた参照・欠落スクリプトを1件ずつ、該当オブジェクトへのリンク付きで
├── packages.html      ← プロジェクトが依存するパッケージ(Unity + サードパーティ)
├── changes.html       ← 以前のバージョンとの差分(変更履歴を有効にした場合)
├── plan.html          ← このエクスポートを作成したプラン。含まれるもの・含まれないもの
├── summary.md         ← プロジェクト索引 → summary/ への案内
├── export-info.txt    ← このエクスポートの内容を平易な文章で
├── export-info.json   ← 同じ内容の機械可読版
├── summary/           ← シンプル / AI向け(短い。AIにはこれを渡す)
│   ├── ai-bundle.md                 ← 下記すべてを1ファイルに(貼り付け1回)
│   ├── scene-MainMenu.md            ← 読みやすい版
│   ├── scene-MainMenu.json          ← 構造化版(数百行)
│   ├── folder-Images_Backgrounds.md
│   └── folder-Images_Backgrounds.json
├── scenes/
│   └── MainMenu.html                ← フルの対話ページ
├── folders/
│   └── Images_Backgrounds.html
├── data/              ← 完全な構造化JSON(全フィールドを含む詳細版)
├── theme/             ← サイト自体のcss/js + search-index.js + サムネイル
├── source-files/      ← アセット実体のコピー(任意 / With Files エクスポート時のみ)
├── changes-files/     ← 変更された各ファイルの新旧の実体(確認用)
└── project-backup.unitypackage   ← プロジェクト全体のバックアップ(任意)
```

バージョン名は `V1.0.0 → V1.0.9 → V1.1.0 → … → V9.9.9 → V10.0.0` と進みます。エクスポートウィンドウで独自の名前を付けることもできます。**Update Previous Export** は新しいフォルダを作らず、最新のフォルダに**上書きで**再エクスポートし、変更のないものは再利用します。

> **上位エディションで作成されたフォルダへの再エクスポートについて。** バージョンフォルダには、それを作成したエクスポートの生成物がそのまま残ります。Plus / Pro が有効な状態で作成されたフォルダには、そのエディションの `summary/`、`source-files/`、`changes.html`、`project-backup.unitypackage` が残っています。これらを含まないエディションからプロジェクト全体を同じフォルダに再エクスポートすると、それらは削除されます。フォルダは作成したプランに合わせて書き直され、削除されたファイルはエクスポート完了ダイアログにすべて表示されます。今回書き込まないフォルダには一切触れません。また、機能を保持しているエディションでは、チェックを外して再エクスポートしてもファイルは残ります。

サイトにはサイドバーに **Simple / Advanced** の切り替えがあります。*Simple* はすっきりした概要(ヒエラルキー、カスタムスクリプトの設定、アセットの要点)を、*Advanced* はすべてのシリアライズ済みフィールドを表示します。初期状態は Simple で、選択は記憶されます。

### 🤖 自動化とCI

> **Pro 機能です。** スクリプトおよびコマンドラインからのエクスポートには Pro ライセンスが必要です。無料版では理由を添えて失敗し、`-batchmode` では非ゼロで終了します。中途半端に成功してパイプラインが本来生成するはずの出力を欠いたまま公開されることを避けるための意図的な動作です。メニュー項目は無料版でも通常どおり利用できます。

すべてのエクスポートはC#とコマンドラインから実行できるので、メニューを押し忘れることなくマージのたびにドキュメントを再生成できます。

```bash
Unity -batchmode -quit -projectPath . \
      -executeMethod AmirCollider.UnityDocSnap.Editor.DocSnapAPI.RunFromCommandLine \
      -docsnapOutput Build/Docs
```

`-batchmode` ではエクスポート失敗時にプロセスが非ゼロで終了するので、ビルドが赤くなるのは本当に問題があるときだけです。API経由・バッチ経由のエクスポートではダイアログは一切表示されません。主な引数: `-docsnapUpdate`(最新バージョンを差分更新)、`-docsnapScene <path>`、`-docsnapFolder <path>`、`-docsnapWithFiles`、`-docsnapOutput <path>`、`-docsnapExclude "a;b"`、`-docsnapLanguage`、`-docsnapTheme`、`-docsnapSkin`、`-docsnapNoThumbnails`、`-docsnapNoFonts`、`-docsnapSaveSettings`。引数なしならプロジェクト全体をエクスポートします。

> **これらの設定はその実行にだけ適用され、リポジトリには書き込まれません。** `ProjectSettings/UnityDocSnapSettings.json` は変更されないので、ビルドエージェントは作業ツリーをそのまま残し、`git diff --exit-code` も通ります。コミット対象のファイル自体を更新したい場合だけ `-docsnapSaveSettings` を付けてください。
>
> 値が欠けた引数(`-docsnapOutput` の直後が別のフラグ、あるいはコマンドラインの末尾にある場合)は**拒否**され、該当の引数名がコンソールに出力され、`-batchmode` では非ゼロで終了します。黙って解釈し直すことはありません — 既定の出力先にエクスポートして成功と報告するのは、パイプラインが何も公開しないまま緑になる原因そのものだからです。

公開型は `DocSnapAPI` **だけ**です。それ以外はすべて意図的に `internal` のままにしてあります。

### ♻️ Unity が自分で書き換えるファイル

TextMesh Pro の動的フォントアセットは、グリフテーブルとアトラステクスチャを `.asset` の中に持っています。TMP はアトラスに無い文字を描画するたびにこれを再シリアライズするため — エディタが自身の UI を描画する起動時も含みます — Unity を開いて閉じるだけで `LiberationSans SDF - Fallback.asset` のバイト列は実際に変化します。誰も触っていないのに、です。

変更点ページはこれらを変更件数から外し、**Unityによる自動書き換え**という専用の折りたたみグループに、判定に使われたパターンを添えて表示します。エクスポートには通常どおり含まれています — 分けているだけで、黙って捨ててはいません。独自のパターンは **Project Settings → Unity DocSnap → Rewritten-by-Unity paths** に、除外リストと同じ書式で追加できます。

ベイク結果(ライトマップ、NavMesh、オクルージョンカリング)は意図的にこのリストに入れていません。それらは誰かが Bake を押したから変わるのであり、まさに変更点ページが報告すべき変更だからです。

### ⚙️ 設定の保存先

**プロジェクト**に関する設定(除外パターン、他人のコードのフォルダ、出力先、サイトの既定言語・テーマ・スキン、サムネイル、埋め込みフォント)は次のファイルに書き込まれます:

```
ProjectSettings/UnityDocSnapSettings.json
```

**このファイルはコミットしてください。** プルリクエストで読める、順序が安定したプレーンなJSONです。これがあるからチーム全員とCIが同じ出力を得られます。**あなた個人**に関する設定(エクスポートウィンドウの言語、カスタムロゴの絶対パス)は従来どおり `EditorUserSettings` に残り、このファイルには書かれません。

0.9.x からのアップグレード時には、既存の設定が一度だけ自動で移行されます。

### 📏 エクスポートに含まれないもの

DocSnapはプロジェクトを「記録」するツールであって、そこから復元できるシリアライザではありません。極端なオブジェクト1つのせいで開けない出力ができてしまわないよう、いくつか上限があります。いずれも出力側に明示されます(JSONの `"truncated": true`、ページ上の注記)。

配列の要素は1フィールドあたり50個(ネストした配列は10個)、1オブジェクトあたりのフィールドは1,000個、ネストの深さは14、フォルダノードあたりの表示アセットは300件、健康状態の指摘はシーン/フォルダあたり400件・表示2,000件、検索インデックスは20,000件、`ai-bundle.md` は600,000文字まで。

またUnity自身が無視するもの(隠しフォルダ、`Foo~` フォルダ、`CVS`)と、除外パターンで外したものは最初から対象外です。

### 🧠 人にもAIにもやさしい理由

すべてのエクスポートページは、きちんとした見出し・ラベル付きのフィールド・一貫したIDという、わかりやすい構造に従っています。人間はブラウザで1分もあれば全体を把握できますし、AIアシスタントには `data/` フォルダ(または1つのJSONファイル)を渡すだけで、Hierarchy・Component・アセット設定を、いちいち手で説明しなくても理解してもらえます。

### 🗺️ ロードマップ

- [x] 画像アセットのサムネイルプレビュー(任意)
- [x] エクスポートしたサイト全体の検索機能
- [x] 2つのエクスポート間の差分表示
- [x] 生成されたサイトのダークモード 🌙
- [x] バージョン管理付きエクスポート + プロジェクト全体の `.unitypackage` バックアップ

### 🤝 コントリビュート

IssueやPull Requestはいつでも歓迎です。

### 📜 ライセンス

MIT — 詳細は [LICENSE](LICENSE) をご覧ください。

### 💌 クレジット

🧋を込めて、[AmirCollider](https://github.com/AmirCollider) より。

Unity DocSnapが後々の手間を減らしてくれたなら、リポジトリへの ⭐ がとても励みになります。

<p align="right"><a href="#top">⬆ トップに戻る</a></p>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<a id="persian"></a>
<div dir="rtl" align="right">

## ⭐ فارسی

تا حالا شده بعد از دو هفته پروژه رو باز کنی و اصلاً یادت نیاد توی Hierarchy خودت چی ریخته بودی؟ **Unity DocSnap** جاش یادش می‌مونه.

این یه افزونه‌ی ادیتوره که توی همه‌ی سین‌های پروژه‌ت قدم می‌زنه — همه‌ی GameObject ها، همه‌ی کامپوننت‌ها، همه‌ی فیلدها، همه‌ی رفرنس‌ها — و تنظیمات ایمپورت همه‌ی فایل‌های پروژه رو هم برمی‌داره، بعد همه‌شون رو می‌ریزه توی یه سایت HTML تمیز و آفلاین که با هر مرورگری میشه بازش کرد. نه سروری لازمه نه بیلدی، فقط دابل‌کلیک کن و باز کن. هم برای آدم‌هایی ساخته شده که یادشون میره دقیقاً چی ساختن، هم برای دستیارهای هوش مصنوعی که به‌جای چهل تا اسکرین‌شات، یه فایل مرتب می‌خوان. 🍰

### ✨ ویژگی‌ها

- 🌳 **اسنپ‌شات کامل از Hierarchy** — همه‌ی GameObject های یه سین، دقیقاً با همون تودرتویی که توی پنجره‌ی Hierarchy می‌بینی، همراه با Tag، Layer، وضعیت فعال/غیرفعال و Static Flag.
- 🔍 **خروجی کامل از Inspector** — همه‌ی کامپوننت‌های روی هر GameObject، همه‌ی فیلدهای سریالایز شده و مقدار فعلی‌شون، دقیقاً همون‌طور که توی Inspector می‌بینی.
- 🔗 **اتصالات واقعی، نه فقط اسم** — اگه یه اسکریپت به یه GameObject دیگه، یه Prefab یا یه ScriptableObject رفرنس داشته باشه، توی خروجی یه لینک قابل‌کلیک میشه؛ اینجوری می‌تونی ببینی سین دقیقاً چطور به هم وصله.
- 🖼️ **اطلاعات فایل، نه خود فایل** — یه مسیر بهش بده، مثلاً `Assets/Images/Backgrounds`، اون هم *اطلاعات* همه‌ی فایل‌های اون مسیر رو خروجی می‌گیره: تنظیمات ایمپورت، فشرده‌سازی، حداکثر سایز، ابعاد، فرمت — بدون این‌که خود فایل کپی بشه. دو تا گزینه هست که واقعاً محتوا رو وارد خروجی می‌کنن و هر دوش هم اعلام می‌شه: **تصاویر بندانگشتی** (پیش‌فرض روشن) یک پیش‌نمایش PNG کوچیک از عکس‌هات می‌نویسه، و **Export Full Project With Files** خود بایت‌های فایل‌ها رو کپی می‌کنه. برای خروجی‌ای که فقط متادیتا داشته باشه، توی Project Settings تصاویر بندانگشتی رو خاموش کن.
- 📁 **یه گزینه‌ی منو برای هر سین** — DocSnap پروژه رو اسکن می‌کنه و همه‌ی سین‌ها رو جدا جدا توی منو میاره.
- 🖱️ **راست‌کلیک، هرجا که باشی** — همه‌ی گزینه‌های منو از راست‌کلیک روی هر فولدر یا فایل توی پنجره‌ی Project هم در دسترسن.
- 🌐 **یه وب‌سایت لوکال واقعی** — همه چی توی یه `index.html` و چندتا صفحه‌ی به‌هم‌وصل جمع میشه، با سایدبار و لینک‌های داخلی بین آبجکت‌ها و فایل‌هایی که بهشون رفرنس دارن.
- 🤖 **برای هوش مصنوعی هم ساخته شده** — کنار HTML قشنگش، یه خروجی JSON ساختاریافته هم می‌ده؛ دادن کل اطلاعات پروژه به یه دستیار هوش مصنوعی، به‌جای اشتراک‌گذاری صفحه، فقط یه فایل می‌خواد.
- 🧩 **فقط ادیتور** — کاملاً توی یه اسمبلی `Editor` جا می‌گیره. نه هزینه‌ای موقع اجرا داره، نه چیزی به حجم بیلد اضافه می‌کنه.
- 🩺 **سلامت پروژه که می‌گه *کجا*** — یک صفحه‌ی مستقل `issues.html` هر اسکریپت گم‌شده، ارجاع شکسته و فایل بدون نوع رو **دونه‌دونه** لیست می‌کنه: با مسیر همون GameObject (`Canvas/Menu/StartButton`)، و کامپوننت و فیلدی که نگهش داشته (`MenuController › targetScene`)، و یک لینک که کارت جمع‌شده‌ش رو باز می‌کنه و هایلایتش می‌کنه. داشبورد شمارش‌ها رو اول می‌آره و هر شمارش خودش لینکیه به همون گزارش، فیلترشده روی همون نوع.
- 🙋 **و اینکه تقصیر کیه** — ایرادهایی که داخل پوشه‌های نصب‌شده‌ی Unity یا پکیج‌ها توی `Assets/` هستن (TextMesh Pro، پوشه‌ی `Settings` که تمپلیت می‌سازه، Samples پکیج‌ها) از فایل‌های خودت جدا می‌شن، با تب **فایل‌های خودم / Unity و پکیج‌ها / همه** که پیش‌فرض روی فایل‌های خودته. «۸ ارجاع شکسته» که هیچ‌کدومش دست تو نیست، الان به‌جای هشدار، «سالم» خونده می‌شه.
- ✨ **دو تا ظاهر، با سنجش قبل از انتخاب** — **دنج** (گرادیانت‌های پاستلی، سایه‌های نرم، ماسکات چای‌حبابی که تکون می‌خوره) و **سبک** (تخت، سریع، بدون انیمیشن). این‌که سایت با کدوم باز بشه از RAM و تعداد هسته و GPU سیستمت به‌علاوه‌ی سنگینی پروژه تصمیم گرفته می‌شه؛ هر وقت بخوای عوضش کن، و اگه برخلاف سنجش «دنج» رو انتخاب کنی، عددهایی که مبنای پیشنهاد بودن رو نشونت می‌ده.
- 🔎 **فیلتر کردن همون صفحه، سرجاش** — هر Hierarchy و درخت پوشه یه باکس فیلتر داره: چهار حرف تایپ کن و یه سین با بیست‌هزار GameObject جمع می‌شه به همون چندتایی که مطابقن (والدها می‌مونن تا مسیر خونا بمونه).
- 🚫 **چیزی که خودت ننوشتی رو حذف کن** — یک خط مثل `Assets/Plugins` توی Project Settings کافیه تا محتوای ایمپورت‌شده‌ی Asset Store از پیمایش فایل‌ها، درخت پوشه‌ها، ایندکس جستجو و شمارش‌ها بیرون بمونه (وایلدکارت `*` و `?` هم کار می‌کنه). چیزی که حذف شده توی خروجی هم صریحاً نوشته می‌شه.
- 🎬 **فقط سین‌های Build Settings** — می‌تونی فقط سین‌هایی رو مستند کنی که واقعاً توی بازی هستن و سین‌های تستی و نمونه‌ی پکیج‌ها رو رد کنی.
- 📦 **یک فایل برای هوش مصنوعی** — `summary/ai-bundle.md` همه‌ی خلاصه‌های خروجی رو توی یک فایل جمع می‌کنه، پس کل پروژه با یک بار paste منتقل می‌شه نه یک پوشه.
- ⏹️ **قابل لغو** — هر دو مرحله نوار پیشرفت دارن و می‌تونی متوقفشون کنی.
- ⚡ **ساخته‌شده برای پروژه‌های بزرگ** — سایت برای هر چیزی که بیرون صفحه‌ست layout انجام نمی‌ده، پس یه سین با ده‌ها هزار آبجکت هم به‌سرعت یه سین کوچیک باز می‌شه. ظاهر مینیمال و مدرن در حالت روشن و تاریک، کار با کیبورد (`/` برای جستجو، `[` برای بستن سایدبار)، و دکمه‌ی کپی مسیر همون‌جایی که لازمش داری.

### 💎 رایگان، Plus و Pro

‏Unity DocSnap سه نسخه دارد. **نسخه‌ی رایگان هیچ کدی، هیچ حسابی و هیچ اینترنتی نمی‌خواهد** — پکیج را نصب کن و هر چیزی که در سه ردیف اول جدول است کار می‌کند.

| | رایگان | Plus ۱۹.۹۹$ | Pro ۴۹.۹۹$ |
| --- | :---: | :---: | :---: |
| کل سایت آفلاین — سلسله‌مراتب، اطلاعات اینسپکتور، رفرنس‌ها، لینک‌های متقابل | ✅ | ✅ | ✅ |
| گزارش سلامت پروژه (`issues.html`) — هر اسکریپت گم‌شده و رفرنس شکسته، با مسیر آبجکت و نام فیلد | ✅ | ✅ | ✅ |
| صفحه‌ی پکیج‌ها، جست‌وجو، تم روشن/تاریک، هر دو ظاهر، هر سه زبان، تامبنیل، قوانین حذف | ✅ | ✅ | ✅ |
| صفحه‌ی پلن (`plan.html`) — این خروجی با کدوم نسخه ساخته شده، چی توشه، و لینک بررسی درستی لایسنس | ✅ | ✅ | ✅ |
| 🤖 **خروجی آماده‌ی هوش مصنوعی** — `summary/*.md`، `summary/*.json` و `summary/ai-bundle.md` که یک پیست است | — | ✅ | ✅ |
| 🔁 **صفحه‌ی تغییرات** — بین دو خروجی چه عوض شده، با بایت‌های قبل و بعد کنار هم | — | ✅ | ✅ |
| بدون خط «نسخه‌ی رایگان» توی فوتر خروجی | — | ✅ | ✅ |
| 📚 **تاریخچه‌ی نسخه‌ها** | ۳ اسنپ‌شات | ۵ اسنپ‌شات | نامحدود |
| ⚡ **بروزرسانی افزایشی (`Update Previous Export`)** — سین‌های تغییرنکرده دوباره استفاده می‌شوند | — | — | ✅ |
| 🤖 **اتوماسیون CI** — `DocSnapAPI` و `-executeMethod` | — | — | ✅ |
| 📁 **کپی خود فایل‌ها** — بایت واقعی اسست‌ها توی `source-files/` | — | — | ✅ |
| 📦 **بک‌آپ `.unitypackage` از کل پروژه** | — | — | ✅ |
| ✨ **لوگوی خودت** توی سایدبار خروجی | — | — | ✅ |

**هر دو نسخه‌ی پولی خرید یک‌باره هستند** — اشتراک ماهانه نیست. هر کد روی یک سیستم، و رفتن به سیستم جدید کاملاً خودسرویس است: سیستم قبلی را از `Unity DocSnap ▸ Licence & Pro Features` آزاد کن، یا اگر آن سیستم دیگر در دسترس نیست از [صفحه‌ی لایسنس](https://amircollider.n95pluss.workers.dev/license). همه‌ی بروزرسانی‌های ۱.x شامل می‌شود.

← **[هر سه را مقایسه کن](https://amircollider.n95pluss.workers.dev/unity-docsnap)** · **[خرید Plus](https://amircollider.n95pluss.workers.dev/checkout?tier=plus&lang=fa)** · **[خرید Pro](https://amircollider.n95pluss.workers.dev/checkout?tier=pro&lang=fa)**

**‏Plus برای چیست؟** خروجی AI و صفحه‌ی تغییرات همان چیزی است که بیشتر آدم‌ها واقعاً برایش می‌آیند، و خیلی از همان آدم‌ها هیچ کاری با اتوماسیون CI، کپی فایل‌ها یا بک‌آپ پروژه ندارند. اگر مجبورشان کنی برای دو تا قابلیت نسخه‌ی ۴۹.۹۹ دلاری بخرند، بیشترشان هیچ چیزی نمی‌خرند. نسخه‌ی Plus همان دو تاست، تنها، با ۱۹.۹۹ دلار.

**یک توضیح درباره‌ی نسخه‌ی رایگان.** هرچه در سه ردیف اول است، تمامِ اکسپورتر است — نه محدودیت زمانی دارد نه پیام مزاحم. نسخه‌ی رایگان توی استفاده‌ی معمولی دقیقاً به سه دیوار می‌خورد: سه اسنپ‌شات نگه می‌دارد نه همه را (نسخه‌ی Plus پنج تا)، به‌جای استفاده‌ی دوباره از سین‌های تغییرنکرده همه‌چیز را دوباره اسکن می‌کند، و یک خط کردیت توی فوتر سایتی که می‌سازد می‌نویسد. اگر گزینه‌ای را روشن کنی که لایسنسش را نداری، خروجی باز هم گرفته می‌شود — آن گزینه رد می‌شود و دیالوگ پایان کار می‌گوید کدام‌ها و با چه قیمتی. تنها استثنا `DocSnapAPI` است که صریحاً رد می‌کند، چون یک بیلد اجنت که نصفه‌نیمه موفق شود، پوشه‌ی مستنداتی منتشر می‌کند که دقیقاً خروجی‌هایی را ندارد که پایپ‌لاین برایشان ساخته شده بود — و سبز هم می‌شود.

**چه چیزی از سیستمت بیرون می‌رود.** توی نسخه‌ی رایگان: هیچ‌چیز. اصلاً به شبکه وصل نمی‌شود. نسخه‌های پولی یک درخواست موقع فعال‌سازی و یکی موقع تمدید می‌فرستند که فقط شامل کد لایسنس، هش نمک‌دار `deviceUniqueIdentifier` یونیتی، و شماره‌ی نسخه‌ی پکیج است. هیچ‌چیزی از پروژه‌ات — نه اسمش، نه مسیرش، نه اندازه‌اش — هرگز فرستاده نمی‌شود. بعد از فعال‌سازی، لایسنس ۴۵ روز به‌صورت آفلاین و با یک کلید عمومی که داخل خود پکیج کامپایل شده تأیید می‌شود، پس هیچ‌وقت سر راه یک اکسپورت، تماس شبکه‌ای وجود ندارد.

### 📋 پیش‌نیازها

- یونیتی **2021.3 LTS** به بعد (یونیتی 6 هم پشتیبانی میشه)
- بدون هیچ وابستگی به کتابخونه‌ی شخص‌ثالث

### 📦 نصب

**روش الف — Package Manager (پیشنهادی)**
۱. برو به **Window → Package Manager**
۲. کلیک کن روی **+ → Add package from git URL…**
۳. این آدرس رو بچسبون: `https://github.com/AmirCollider/UnityDocSnap.git`
۴. کلیک کن روی **Add**

**روش ب — نصب دستی**
۱. این ریپازیتوری رو دانلود یا کلون کن
۲. پوشه‌ی `Editor/UnityDocSnap` رو بریز توی پوشه‌ی `Assets` پروژه‌ت — همراه با زیرپوشه‌ی `Site~` که استایل و اسکریپت و فونت‌های سایت خروجی توشه
۳. یونیتی خودش کامپایلش می‌کنه؛ نیازی به ری‌استارت نیست

### 🚀 نحوه‌ی استفاده

بعد از نصب، توی نوار بالای یونیتی یه منوی جدید به اسم **Unity DocSnap** اضافه میشه.

```
Unity DocSnap
├── Export Scene
│   ├── MainMenu
│   ├── Level01
│   └── Level02              ← به ازای هر سین توی پروژه، یه گزینه
├── Export Asset Info
│   ├── Entire Assets Folder
│   └── Selected Folder…
├── Export Full Project      (سین‌ها + فایل‌ها، همه به هم لینک‌شده)
├── Export Full Project With Files
├── Update Previous Export    (بروزرسانی افزایشی و سریع — موارد تغییرنکرده دوباره استفاده می‌شن)
├── Open Output Folder
├── Licence & Pro Features
└── About Unity DocSnap
```

سایت تولیدشده حالا یه **باکس جستجو** توی سایدبار داره (همه / سین‌ها / فایل‌ها)، یه صفحه‌ی **Packages** که همه‌ی پکیج‌های پروژه رو لیست می‌کنه، و **نمونه‌ها/واریانت‌های Prefab و فیلدهای بازنویسی‌شده** رو همه‌جا مشخص می‌کنه.

**اکسپورت گرفتن از یه سین**
با زدن `Unity DocSnap → Export Scene → [اسم سین]`، کل Hierarchy همون سین رو قدم می‌زنه و اسنپ‌شات کامل همه‌ی GameObject ها و کامپوننت‌هاشون رو توی پوشه‌ی خروجی می‌نویسه.

**اکسپورت گرفتن اطلاعات فایل‌ها**
با `Unity DocSnap → Export Asset Info → Selected Folder…` می‌تونی یه پوشه انتخاب کنی — مثلاً `Assets/Images/Backgrounds` — و DocSnap اطلاعات Inspector همه‌ی فایل‌های اون پوشه رو اکسپورت می‌کنه. برای یه عکس مثل `bakery_street.png`، یعنی Texture Type، sRGB، Compression، Max Size، Filter Mode، Wrap Mode، Generate Mip Maps و بقیه‌ی تنظیمات ایمپورتش، دقیقاً همون‌طور که توی یونیتی تنظیم شده.

> **درباره‌ی پیکسل‌ها.** خود فایل `bakery_street.png` هیچ‌وقت کپی نمی‌شه. اما وقتی **Generate Image Thumbnails** روشنه — که پیش‌فرضه — یک پیش‌نمایش کوچیک‌شده‌ی PNG ازش داخل `theme/thumbs/` نوشته می‌شه تا صفحه‌ی خروجی نشون بده تکسچر واقعاً چه شکلیه. اون پیش‌نمایش، داده‌ی تصویری واقعیه. اگه خروجی باید فقط متادیتا داشته باشه، قبل از اکسپورت از **Project Settings → Unity DocSnap** تصاویر بندانگشتی رو خاموش کن.

**باز کردن نتیجه**
به‌صورت پیش‌فرض، خروجی توی مسیر `<ریشه‌ی پروژه>/UnityDocSnap_Output/` قرار می‌گیره. با `Unity DocSnap → Open Output Folder` مستقیم می‌ری اونجا، بعد `index.html` رو با هر مرورگری باز کن.

### 📁 ساختار خروجی

هر اکسپورت توی **پوشه‌ی نسخه‌ی خودش** نوشته می‌شه. ریشه‌ی خروجی یه قفسه از این پوشه‌هاست و هرکدوم یه سایت کامل و مستقله — پس اسنپ‌شاتی که ماه پیش گرفتی هنوز دست‌نخورده کنار اسنپ‌شات امروز هست.

خودِ ریشه فقط دو تا صفحه داره که راه رو به اون قفسه نشون می‌دن:

```
UnityDocSnap_Output/
├── index.html         ← ریدایرکت به جدیدترین نسخه
├── versions.html      ← قفسه: همه‌ی اکسپورت‌ها، از جدید به قدیم
├── V1.0.0/            ← به ازای هر اکسپورت، یک سایت کامل
├── V1.0.1/
└── V1.1.0/
```

داخل **یک پوشه‌ی نسخه**، هر اکسپورت **دو شکل از یک اطلاعات** رو کنار هم می‌نویسه: نسخه‌ی **کامل** یعنی سایت آفلاین (توی مرورگر ببینش یا JSON خامش رو بخون) و نسخه‌ی **ساده** یعنی چند فایل خلاصه‌ی کوتاه — هم Markdown هم JSON — که همه توی پوشه‌ی `summary/` جمع شدن و می‌تونی مستقیم بچسبونی توی یه دستیار هوش مصنوعی.

```
V1.1.0/
├── index.html         ← سایت آفلاین کامل (از اینجا شروع کن)
├── issues.html        ← هر ارجاع شکسته و اسکریپت گم‌شده، دونه‌دونه، با لینک به همون آبجکت
├── packages.html      ← پکیج‌هایی که پروژه بهشون وابسته‌ست (یونیتی + شخص‌ثالث)
├── changes.html       ← تفاوت‌ها نسبت به یک نسخه‌ی قبلی (وقتی ثبت تغییرات روشن باشه)
├── plan.html          ← پلنی که این خروجی باهاش ساخته شده: چی توشه و چی نیست
├── summary.md         ← فهرست پروژه → راهنما به summary/
├── export-info.txt    ← این اکسپورت شامل چیه، به زبان ساده
├── export-info.json   ← همون، ولی ماشین‌خوان
├── summary/           ← ساده / مناسب هوش مصنوعی (کوتاه — اینا رو به AI بده)
│   ├── ai-bundle.md                 ← همه‌ی موارد زیر در یک فایل (یک paste)
│   ├── scene-MainMenu.md            ← نسخه‌ی خوانا
│   ├── scene-MainMenu.json          ← نسخه‌ی ساختاریافته (چند صد خط)
│   ├── folder-Images_Backgrounds.md
│   └── folder-Images_Backgrounds.json
├── scenes/
│   └── MainMenu.html                ← صفحه‌ی کامل و تعاملی
├── folders/
│   └── Images_Backgrounds.html
├── data/              ← JSON کامل و ساختاریافته (نسخه‌ی پیشرفته، همه‌ی فیلدها)
├── theme/             ← css/js و search-index.js و تصاویر بندانگشتی خود سایت
├── source-files/      ← کپی خام فایل‌ها (اختیاری / فقط در اکسپورت With Files)
├── changes-files/     ← بایت‌های قدیم و جدید هر فایل تغییرکرده، برای بررسی
└── project-backup.unitypackage   ← بک‌آپ کل پروژه (اختیاری)
```

نام نسخه‌ها این‌طور جلو می‌ره: `V1.0.0 → V1.0.9 → V1.1.0 → … → V9.9.9 → V10.0.0`. توی پنجره‌ی اکسپورت می‌تونی اسم دلخواه خودت رو هم بذاری. گزینه‌ی **Update Previous Export** به‌جای ساختن پوشه‌ی جدید، **روی** جدیدترین پوشه دوباره اکسپورت می‌کنه و هرچی تغییر نکرده رو دوباره استفاده می‌کنه.

> **خروجی گرفتن دوباره روی پوشه‌ای که با نسخه‌ی بالاتر ساخته شده.** هر پوشه‌ی نسخه هرچی رو که اکسپورت سازنده‌اش تولید کرده نگه می‌داره؛ پس پوشه‌ای که موقع فعال بودن Plus یا Pro ساخته شده، هنوز `summary/` و `source-files/` و `changes.html` و `project-backup.unitypackage` همون نسخه رو داره. وقتی یک اکسپورت کامل از نسخه‌ای که اینا رو نداره دوباره روی همون پوشه بیفته، این فایل‌ها حذف می‌شن — پوشه مطابق پلنی که ساختتش بازنویسی می‌شه و پنجره‌ی پایان اکسپورت دونه‌دونه می‌گه چی برداشته شده. به پوشه‌ای که این اکسپورت توش نمی‌نویسه دست زده نمی‌شه، و نسخه‌ای که هنوز قابلیت رو داره، حتی با تیک‌نزدن گزینه توی اجرای بعدی هم فایل‌هاش سر جاشون می‌مونن.

خود سایت توی سایدبار یه کلید **Simple / Advanced** داره: حالت *Simple* یه نمای تمیز و سریع نشون می‌ده (Hierarchy، تنظیمات اسکریپت‌های خودت، نکات کلیدی فایل‌ها) و حالت *Advanced* همه‌ی فیلدهای سریالایز‌شده رو. به‌صورت پیش‌فرض روی Simple باز میشه و انتخابت رو یادش می‌مونه.

### 🤖 اتوماسیون و CI

> **مخصوص Pro.** خروجی گرفتن از طریق اسکریپت و خط فرمان به لایسنس Pro نیاز دارد. نسخه‌ی رایگان با ذکر دلیل شکست می‌خورد و در `-batchmode` با کد غیرصفر خارج می‌شود — عمداً، به‌جای اینکه نصفه‌نیمه موفق شود و پوشه‌ی مستنداتی منتشر کند که خروجی‌های موردنظر پایپ‌لاین را ندارد. همه‌ی آیتم‌های منو در نسخه‌ی رایگان مثل همیشه کار می‌کنند.

هر اکسپورت از C# و از خط فرمان هم قابل اجراست، پس می‌شه مستندات رو روی هر merge دوباره ساخت به‌جای اینکه یادت بمونه یه منو رو کلیک کنی.

```bash
Unity -batchmode -quit -projectPath . \
      -executeMethod AmirCollider.UnityDocSnap.Editor.DocSnapAPI.RunFromCommandLine \
      -docsnapOutput Build/Docs \
      -docsnapExclude "Assets/Plugins;Assets/ThirdParty"
```

توی `-batchmode` اگه اکسپورت شکست بخوره پروسه با کد غیرصفر خارج می‌شه، پس بیلد قرمز یعنی یه مشکل واقعی. توی اکسپورتی که از API یا از batch اجرا شده هیچ دیالوگی نشون داده نمی‌شه.

آرگومان‌ها: `-docsnapUpdate` (به‌روزرسانی افزایشی آخرین نسخه)، `-docsnapScene <path>`، `-docsnapFolder <path>`، `-docsnapWithFiles`، `-docsnapOutput <path>`، `-docsnapExclude "a;b"`، `-docsnapLanguage`، `-docsnapTheme`، `-docsnapSkin`، `-docsnapNoThumbnails`، `-docsnapNoFonts`، `-docsnapSaveSettings`. بدون هیچ آرگومانی، اکسپورت کامل پروژه اجرا می‌شه.

> **این تنظیمات فقط روی همون اجرا اعمال می‌شن، نه روی ریپازیتوری.** فایل `ProjectSettings/UnityDocSnapSettings.json` دست‌نخورده می‌مونه، پس بیلد سرور working tree رو همون‌طور که تحویل گرفته باقی می‌ذاره و مرحله‌ی `git diff --exit-code` هم پاس می‌شه. فقط برای اون جاب خاصی که کارش دقیقاً به‌روز کردن همون فایل کامیت‌شده‌ست `-docsnapSaveSettings` رو اضافه کن.
>
> آرگومانی که مقدارش جا افتاده باشه — مثلاً `-docsnapOutput` که بعدش یه فلگ دیگه اومده، یا آخرین آرگومان خط فرمان باشه — **رد می‌شه**: اسم همون آرگومان توی کنسول نوشته می‌شه و توی `-batchmode` پروسه با کد غیرصفر خارج می‌شه. هیچ‌وقت بی‌صدا جور دیگه‌ای تفسیر نمی‌شه — چون اکسپورتی که بی‌خبر توی مسیر پیش‌فرض بنویسه و بگه موفق بودم، دقیقاً همون چیزیه که باعث می‌شه یه pipeline هیچی منتشر نکنه و باز هم سبز بشه.

`DocSnapAPI` **تنها** تایپ public پکیجه؛ بقیه عمداً `internal` موندن.

### ♻️ فایل‌هایی که خود یونیتی بازنویسی می‌کنه

فونت‌اَسِت‌های داینامیک TextMesh Pro، جدول گلیف و تکسچر اطلسشون رو داخل خود فایل `.asset` نگه می‌دارن، و TMP هر بار که کاراکتری رندر بشه که توی اطلس نیست اون فایل رو دوباره ذخیره می‌کنه — از جمله موقع بالا اومدن ادیتور که داره رابط کاربری خودش رو می‌کشه. یونیتی رو باز کن و ببند، و بایت‌های `LiberationSans SDF - Fallback.asset` واقعاً فرق کرده. بدون این‌که کسی دستش بهش خورده باشه.

صفحه‌ی تغییرات این‌ها رو از شمارش تغییرات بیرون می‌ذاره و توی یه گروه جمع‌شده‌ی جدا به اسم **بازنویسی‌شده توسط یونیتی** فهرست می‌کنه، با همون الگویی که هر فایل رو تشخیص داده کنارش. این فایل‌ها مثل هر اَسِت دیگه‌ای کامل مستند می‌شن — فقط جدا شدن، بی‌صدا حذف نشدن. الگوهای خودت رو می‌تونی از **Project Settings → Unity DocSnap → Rewritten-by-Unity paths** اضافه کنی، با همون سینتکس لیست exclude.

خروجی Bake — لایت‌مپ، NavMesh، occlusion culling — عمداً توی این لیست نیست. اون فایل‌ها وقتی عوض می‌شن که یکی دکمه‌ی Bake رو زده باشه، و این دقیقاً همون تغییریه که صفحه‌ی تغییرات برای گزارشش ساخته شده.

### ⚙️ تنظیمات کجا ذخیره می‌شن

تنظیماتی که **پروژه** رو توصیف می‌کنن — الگوهای exclude، پوشه‌های «مال من نیست»، مسیرهای بازنویسی‌شده توسط یونیتی، مسیر خروجی، زبان/تم/اسکین پیش‌فرض سایت، تصاویر بندانگشتی، فونت‌های embed — اینجا نوشته می‌شن:

```
ProjectSettings/UnityDocSnapSettings.json
```

**این فایل رو commit کن.** یه JSON ساده و مرتبه که توی pull request خونده می‌شه، و همینه که باعث می‌شه کل تیم و CI یک خروجی یکسان بگیرن نه هر نفر یکی. تنظیماتی که **خودت** رو توصیف می‌کنن — زبان پنجره‌ی اکسپورت و مسیر مطلق لوگوی سفارشی — همچنان توی `EditorUserSettings` می‌مونن و اینجا نوشته نمی‌شن.

موقع آپدیت از 0.9.x، تنظیمات فعلی‌ت یک بار به‌صورت خودکار منتقل می‌شن، بدون اینکه چیزی که از قبل توی مخزن بوده بازنویسی بشه.

### 📏 چیزهایی که توی خروجی نمیان

DocSnap پروژه رو مستند می‌کنه؛ یه serializer نیست که بشه پروژه رو ازش بازسازی کرد. چند تا سقف وجود داره تا یک آبجکت غیرعادی باعث نشه خروجی‌ای ساخته بشه که اصلاً باز نمی‌شه — و همه‌شون توی خروجی علامت‌گذاری می‌شن (`"truncated": true` توی JSON، و یادداشت روی صفحه) نه اینکه بی‌صدا اعمال بشن:

عناصر آرایه در هر فیلد ۵۰ تا (آرایه‌ی تودرتو ۱۰ تا)، فیلد در هر آبجکت ۱۰۰۰ تا، عمق تودرتویی ۱۴، اسست نمایش‌داده‌شده در هر نود پوشه ۳۰۰ تا، ایرادهای سلامت ۴۰۰ در هر سین/پوشه و ۲۰۰۰ نمایش‌داده‌شده، رکوردهای ایندکس جستجو ۲۰٬۰۰۰ تا، و `ai-bundle.md` تا ۶۰۰٬۰۰۰ کاراکتر.

اینها هم عمداً بیرونن: هرچیزی که خود یونیتی نادیده می‌گیره (پوشه‌های مخفی، پوشه‌های `Foo~`، `CVS`) و هرچیزی که الگوهای exclude خودت حذف کردن.

### 🧠 چرا هم برای آدم‌ها هم برای هوش مصنوعی؟

هر صفحه‌ی اکسپورت‌شده یه ساختار تمیز و قابل‌پیش‌بینی داره — تیترهای درست، فیلدهای برچسب‌دار، آی‌دی‌های یکدست. یه آدم می‌تونه توی کمتر از یه دقیقه توی مرورگر کل ماجرا رو بفهمه، و به یه دستیار هوش مصنوعی هم کافیه پوشه‌ی `data/` (یا یه فایل JSON) رو بدی تا Hierarchy، کامپوننت‌ها و تنظیمات فایل‌هات رو، بدون این‌که مجبور باشی دستی توضیح بدی، بفهمه.

### 🗺️ نقشه‌ی راه

- [x] پیش‌نمایش کوچیک (Thumbnail) برای فایل‌های عکس (اختیاری)
- [x] جستجو توی کل سایت اکسپورت‌شده
- [x] نمایش تفاوت بین دو تا خروجی مختلف
- [x] حالت تاریک (Dark Mode) برای سایت تولیدشده 🌙
- [x] خروجی نسخه‌بندی‌شده + بک‌آپ `.unitypackage` از کل پروژه

### 🤝 مشارکت

Issue و Pull Request همیشه خوش‌اومدن.

### 📜 لایسنس

MIT — جزئیات توی فایل [LICENSE](LICENSE).

### 💌 با تشکر از

با 🧋 ساخته شده توسط [AmirCollider](https://github.com/AmirCollider).

اگه Unity DocSnap یه‌کم از دردسر بعدیت کم کرد، یه ⭐ روی ریپو خیلی دلگرم‌کننده‌ست.

<p align="right"><a href="#top">⬆ برگشت به بالا</a></p>

</div>

<p align="center">━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</p>

<p align="center"><sub>Made with 🧋 🍰 ⭐ for Unity — <a href="https://github.com/AmirCollider">AmirCollider</a></sub></p>
