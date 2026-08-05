// ==========================================
// DocSnapExportWindow
// The one small window that drives a full export.
// It replaces reaching for a fixed English menu:
// language, theme, version, whether to include the
// file bytes, whether to make a whole-project
// backup, and whether to record a Changes page (and
// against which earlier version) are all chosen
// here. Every label is drawn in the window's own
// language (English / 日本語 / فارسی) so it is as
// usable for a Japanese or Persian user as for an
// English one.
// ==========================================
using System.Collections.Generic;
using AmirCollider.UnityDocSnap.Editor.Export;
using AmirCollider.UnityDocSnap.Editor.Licensing;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal sealed class DocSnapExportWindow : EditorWindow
    {
        // Window UI language: 0 = en, 1 = ja, 2 = fa.
        private int _uiLang;

        // Site defaults.
        private int _siteLang;   // an index into DocSnapLanguages.All
        private int _siteTheme;  // 0 light, 1 dark
        private int _siteSkin;   // 0 auto, 1 cozy, 2 lite

        // Version target.
        private bool _ontoExisting;
        private int _existingIndex;
        private string _customVersion = "";

        // Content options.
        private bool _includeFiles;
        private bool _scenesInBuildOnly;
        private bool _makeBackup;
        private bool _recordChanges;
        private int _changesBaseIndex;

        // Exclude rules, edited here as well as in Project
        // Settings so the choice is in front of the person at
        // the moment it matters - about to spend ten minutes
        // documenting somebody else's imported plugin folder.
        private string _excludes = "";

        // Set by the Export button, acted on after the scroll view
        // has been closed. Not serialized: a request that survived a
        // domain reload would fire an export nobody just asked for.
        [System.NonSerialized] private bool _exportRequested;

        // Snapshot house-keeping. Like the export itself, a deletion
        // is only RECORDED during OnGUI: it opens a confirmation
        // dialog, and a dialog between BeginScrollView and
        // EndScrollView leaves Unity's layout stack unbalanced.
        private int _manageIndex;
        [System.NonSerialized] private string _deleteRequested;
        [System.NonSerialized] private bool _clearRequested;

        private Vector2 _scroll;
        private VersionsState _registry;
        private string[] _existingVersions = new string[0];
        private string _nextAutoVersion = "V1.0.0";

        // The popup labels, and the index<->code mapping that
        // goes with them, both come from DocSnapLanguages - so a
        // language added to that registry appears in both popups
        // here without this window being touched.
        //
        // An index is turned back into a code with CodeAt rather
        // than by indexing this array: the popup index survives a
        // domain reload, and a registry that gained or lost a
        // language in between would otherwise be indexed out of
        // range.
        private static string[] LangNames { get { return DocSnapLanguages.EditorNames(); } }

        // ==========================================
        // ShowWindow
        // ==========================================
        public static void ShowWindow()
        {
            var window = GetWindow<DocSnapExportWindow>(true, DocSnapConstants.ToolName + " — Export", true);
            window.minSize = new Vector2(430, 540);
            window.Refresh();
            window.Show();
        }

        private void OnDisable()
        {
            // A choice parked by a dropdown that outlived its window
            // must not be handed to the next one that opens.
            DocSnapEditorGui.Forget(PopupKey);
        }

        private void OnEnable()
        {
            _uiLang = LangIndex(DocSnapSettings.WindowLanguage);
            _siteLang = LangIndex(DocSnapSettings.DefaultSiteLanguage);
            _siteTheme = DocSnapSettings.DefaultSiteTheme == "dark" ? 1 : 0;
            _siteSkin = DocSnapSettings.SiteSkin == DocSnapCapability.SkinCozy ? 1
                : DocSnapSettings.SiteSkin == DocSnapCapability.SkinLite ? 2 : 0;
            _excludes = DocSnapSettings.ExcludePatterns;
            Refresh();
        }

        // ==========================================
        // CustomVersionProblem
        // Why the typed version name would be rejected, or
        // null when it is fine. Both the live warning and the
        // pre-export check read this, so they can never
        // disagree about what is allowed.
        //
        // Returned RAW - unshaped, unreordered. One of its two
        // readers is an IMGUI HelpBox and the other is an
        // EditorUtility dialog, which the platform draws and
        // prepares itself; raw is the form only one of them has to
        // convert, and converting for the wrong one is what turns a
        // Persian sentence into rubble.
        // ==========================================
        private string CustomVersionProblem()
        {
            string name = _customVersion == null ? "" : _customVersion.Trim();
            if (name.Length == 0) { return null; }

            if (!DocSnapVersioning.IsValidCustomName(name))
            {
                return N("That name can't be used as a folder name, so the export would be auto-numbered instead. Avoid / \\ : * ? \" < > |",
                         "その名前はフォルダ名に使えないため、自動採番になります。/ \\ : * ? \" < > | は使えません。",
                         "این اسم به‌عنوان نام پوشه قابل استفاده نیست و خروجی به‌جایش خودکار شماره‌گذاری می‌شود. از / \\ : * ? \" < > | استفاده نکن.");
            }

            string baseRoot = DocSnapSettings.ResolveOutputRootAbsolute();
            if (DocSnapVersioning.ExistingVersionNames(baseRoot, _registry).Contains(name))
            {
                return N("A version with that name already exists, so the export would be auto-numbered instead.",
                         "その名前のバージョンは既に存在するため、自動採番になります。",
                         "نسخه‌ای با این اسم از قبل وجود دارد و خروجی به‌جایش خودکار شماره‌گذاری می‌شود.");
            }
            return null;
        }

        // ==========================================
        // Refresh — reloads the version registry so the
        // existing/base-version popups stay current.
        // ==========================================
        private void Refresh()
        {
            string baseRoot = DocSnapSettings.ResolveOutputRootAbsolute();
            _registry = DocSnapVersioning.LoadRegistry();

            var names = new List<string>(_registry.versions.Count);
            // Newest first for a friendlier popup order.
            var ordered = new List<VersionSnapshot>(_registry.versions);
            ordered.Sort((a, b) => DocSnapVersioning.CompareVersions(b.version, a.version));
            foreach (VersionSnapshot v in ordered) { names.Add(v.version); }
            _existingVersions = names.ToArray();
            _nextAutoVersion = DocSnapVersioning.NextVersion(baseRoot, _registry);

            _existingIndex = Mathf.Clamp(_existingIndex, 0, Mathf.Max(0, _existingVersions.Length - 1));
            _changesBaseIndex = Mathf.Clamp(_changesBaseIndex, 0, Mathf.Max(0, _existingVersions.Length - 1));
            if (_existingVersions.Length == 0) { _ontoExisting = false; }
        }

        // ==========================================
        // OnGUI
        // ==========================================
        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            GUILayout.Space(8);

            // ---- Title + window language ----
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            GUILayout.Label("🧋 " + L("Export", "エクスポート", "خروجی"), titleStyle);
            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(L("Window language", "ウィンドウの言語", "زبان پنجره"), GUILayout.Width(160));
            int newUiLang = DocSnapEditorGui.Popup(PopupKey + "uiLang", UiLangCode, _uiLang, LangNames);
            if (newUiLang != _uiLang) { _uiLang = newUiLang; DocSnapSettings.WindowLanguage = DocSnapLanguages.CodeAt(_uiLang); }
            EditorGUILayout.EndHorizontal();

            DrawDirectTmpNotice();

            DrawSeparator();

            // ---- Site language + theme ----
            Section(L("Generated site", "生成されるサイト", "سایت تولیدشده"));

            _siteLang = LabeledPopup(
                "siteLang",
                L("Default language", "デフォルト言語", "زبان پیش‌فرض"),
                L("The language the site opens in the first time it's viewed.", "初回表示時にサイトが開く言語。", "زبانی که سایت بار اول با آن باز می‌شود."),
                _siteLang, LangNames);

            _siteTheme = LabeledPopup(
                "theme",
                L("Theme", "テーマ", "تم"),
                L("Light or dark colour theme the site opens in.", "サイトが開くときの明/暗テーマ。", "تم روشن یا تاریک سایت هنگام باز شدن."),
                _siteTheme, new[] { L("Light", "ライト", "روشن"), L("Dark", "ダーク", "تاریک") });

            // Which of the two visual skins the site opens with. Auto
            // measures this machine (RAM / cores / GPU) and how heavy the
            // project is: the cozy skin is the nicer thing to look at and
            // strictly more paint work per row, so on a huge project or a
            // tight machine the site opens light instead. Readers can
            // switch inside the site either way - this is the start point.
            _siteSkin = LabeledPopup(
                "skin",
                L("Visual style", "見た目", "ظاهر سایت"),
                L("Auto picks the cozy look when this machine and project have room for it, and the light one when they do not.",
                  "自動: このマシンとプロジェクトに余裕があればコージー、なければライトを選びます。",
                  "خودکار: اگه این سیستم و پروژه جا داشته باشن ظاهر دنج، وگرنه ظاهر سبک انتخاب می‌شه."),
                _siteSkin, new[]
                {
                    L("Auto (measure this machine)", "自動(この環境を計測)", "خودکار (سنجش این سیستم)"),
                    L("✨ Cozy — gradients + animation", "✨ コージー(グラデーション+アニメ)", "✨ دنج — گرادیانت و انیمیشن"),
                    L("⚡ Lite — flat and fast", "⚡ ライト(フラットで軽量)", "⚡ سبک — تخت و سریع")
                });

            if (_siteSkin == 0)
            {
                DocSnapCapabilityReport probe = DocSnapCapability.Measure(0, 0);
                EditorGUILayout.LabelField(" ",
                    probe.SystemMemoryMb + " MB RAM \u00B7 " + probe.ProcessorCount + " cores \u00B7 "
                        + (string.IsNullOrEmpty(probe.GraphicsDeviceName) ? "GPU unknown" : probe.GraphicsDeviceName),
                    EditorStyles.miniLabel);
            }

            DrawSeparator();

            // ---- Version ----
            Section(L("Version", "バージョン", "نسخه"));

            using (new EditorGUI.DisabledScope(_existingVersions.Length == 0))
            {
                // A radio group, not two independent toggles. The old
                // pair read each Toggle's return value straight back
                // into _ontoExisting, and GUILayout.Toggle UNCHECKS an
                // already-checked toggle when it is clicked - so
                // clicking the option that was already selected flipped
                // you to the other one.
                if (GUILayout.Toggle(!_ontoExisting,
                        "  " + L("New export (new version folder)", "新規エクスポート(新しいバージョンフォルダ)", "خروجی جدید (فولدر نسخه‌ی جدید)"),
                        EditorStyles.radioButton))
                {
                    _ontoExisting = false;
                }
                if (GUILayout.Toggle(_ontoExisting,
                        "  " + L("Export onto a previous version", "以前のバージョンに上書き", "خروجی روی یکی از نسخه‌های قبلی"),
                        EditorStyles.radioButton))
                {
                    _ontoExisting = true;
                }
            }

            EditorGUI.indentLevel++;
            if (_ontoExisting && _existingVersions.Length > 0)
            {
                _existingIndex = DocSnapEditorGui.LabeledPopup(PopupKey + "target", UiLangCode,
                    L("Target version", "対象バージョン", "نسخه‌ی هدف"), null, _existingIndex, _existingVersions);

                // Two different true sentences, because the two
                // editions genuinely do different work here. Free
                // refreshes the folder by re-scanning everything,
                // which is what the tool always did; Pro reuses what
                // has not changed. Saying "reusing anything
                // unchanged" to a Free user would be a promise the
                // export does not keep.
                EditorGUILayout.HelpBox(
                    DocSnapLicense.Has(DocSnapFeature.IncrementalUpdate)
                        ? L("This refreshes that version in place, reusing anything unchanged.",
                            "そのバージョンをその場で更新し、変更のない項目は再利用します。",
                            "این نسخه همانجا بروزرسانی می‌شود و موارد تغییرنکرده دوباره استفاده می‌شوند.")
                        : L("This refreshes that version in place. Pro reuses the Scenes that have not changed instead of re-scanning them all.",
                            "そのバージョンをその場で更新します。Pro では変更のないシーンを再スキャンせず再利用します。",
                            "این نسخه همانجا بروزرسانی می‌شود. نسخه‌ی Pro سین‌های تغییرنکرده را دوباره اسکن نمی‌کند."),
                    MessageType.None);
            }
            else
            {
                _customVersion = EditorGUILayout.TextField(
                    new GUIContent(L("Version name", "バージョン名", "نام نسخه"),
                        L("Leave empty to auto-number.", "空欄で自動採番。", "خالی بگذار تا خودکار شماره‌گذاری شود.")),
                    _customVersion);
                EditorGUILayout.LabelField(" ", string.IsNullOrEmpty(_customVersion)
                    ? L("Auto: ", "自動: ", "خودکار: ") + _nextAutoVersion
                    : L("Custom: ", "カスタム: ", "دلخواه: ") + _customVersion, EditorStyles.miniLabel);

                // A rejected custom name used to fall through to
                // auto-numbering in total silence, so typing "v1/2"
                // produced a folder called V1.0.3 and the user was
                // left to work out why on their own.
                string problem = CustomVersionProblem();
                if (problem != null)
                {
                    EditorGUILayout.HelpBox(DocSnapEditorText.Draw(UiLangCode, problem), MessageType.Warning);
                }

                // Said BEFORE the export rather than after it. The
                // Free shelf limit changes where the output lands,
                // and finding that out from the completion dialog -
                // after ten minutes of scanning - is finding it out
                // too late to have chosen differently.
                int shelfLimit = DocSnapEditionLimits.VersionFolders(DocSnapLicense.Edition);
                if (shelfLimit != DocSnapEditionLimits.Unlimited && _existingVersions.Length >= shelfLimit)
                {
                    string target = _existingVersions.Length > 0 ? _existingVersions[0] : "";
                    EditorGUILayout.HelpBox(
                        L("Your edition keeps " + shelfLimit + " snapshots, and you have that many. "
                          + "This export will refresh " + (target.Length > 0 ? target : "the newest version")
                          + " instead of adding a new folder. Nothing is deleted.",
                          "現在のエディションのスナップショット保持数は " + shelfLimit + " 件で、既に上限に達しています。"
                          + "今回は新しいフォルダを作らず " + (target.Length > 0 ? target : "最新バージョン") + " を更新します。削除は行われません。",
                          "نسخه‌ی فعلی‌ات " + shelfLimit + " اسنپ‌شات نگه می‌دارد و همین حالا همین‌قدر داری. "
                          + "این خروجی به‌جای ساختن فولدر جدید، " + (target.Length > 0 ? target : "جدیدترین نسخه")
                          + " را بروزرسانی می‌کند. هیچ چیزی پاک نمی‌شود."),
                        MessageType.Info);
                }
            }
            EditorGUI.indentLevel--;

            DrawManageVersions();

            DrawSeparator();

            // ---- Content ----
            Section(L("Contents", "内容", "محتوا"));

            _scenesInBuildOnly = EditorGUILayout.ToggleLeft(
                L("Only Scenes listed in Build Settings", "Build Settings に登録されたシーンのみ", "فقط سین‌هایی که توی Build Settings هستند"),
                _scenesInBuildOnly);
            if (_scenesInBuildOnly)
            {
                int inBuild = DocSnapExportService.FindBuildSettingsScenePaths().Count;
                EditorGUILayout.HelpBox(
                    L(inBuild + " scene(s) are enabled in Build Settings. Test and sample Scenes are skipped, which is usually the slowest part of an export.",
                      "Build Settings で有効なシーンは " + inBuild + " 件です。テスト用やサンプルのシーンは除外されます(エクスポートで最も時間がかかる部分です)。",
                      "‏" + inBuild + " سین توی Build Settings فعال است. سین‌های تستی و نمونه رد می‌شوند — که معمولاً سنگین‌ترین بخش خروجی‌گیری‌اند."),
                    inBuild == 0 ? MessageType.Warning : MessageType.None);
            }

            _includeFiles = ProToggle(DocSnapFeature.IncludeFiles, _includeFiles,
                L("Include file copies (bytes, not just info)", "ファイル本体もコピー(情報だけでなく)", "کپی خود فایل‌ها هم گرفته شود (نه فقط اطلاعات)"));

            _makeBackup = ProToggle(DocSnapFeature.ProjectBackup, _makeBackup,
                L("Also export a whole-project .unitypackage backup", "プロジェクト全体の .unitypackage バックアップも作成", "یک بک‌آپ .unitypackage از کل پروژه هم گرفته شود"));
            if (_makeBackup)
            {
                EditorGUILayout.HelpBox(
                    L("Restores the entire project even if it was deleted.",
                      "プロジェクトが削除されても丸ごと復元できます。",
                      "حتی اگر پروژه پاک شود، کل آن را برمی‌گرداند."),
                    MessageType.None);
            }

            DrawSeparator();

            // ---- Excludes ----
            Section(L("Exclude", "除外", "حذف از خروجی"));
            EditorGUILayout.LabelField(
                L("One path or pattern per line, e.g. Assets/Plugins or *.psd",
                  "1行につき1つのパスまたはパターン(例: Assets/Plugins、*.psd)",
                  "هر خط یک مسیر یا الگو، مثل Assets/Plugins یا ‎*.psd"),
                EditorStyles.miniLabel);
            _excludes = EditorGUILayout.TextArea(_excludes, GUILayout.MinHeight(52));

            DocSnapExcludeFilter preview = DocSnapExcludeFilter.Parse(_excludes);
            if (!preview.IsEmpty)
            {
                EditorGUILayout.LabelField(" ",
                    L("Active rules: ", "有効なルール: ", "قوانین فعال: ") + string.Join(" · ", preview.Patterns.ToArray()),
                    EditorStyles.miniLabel);
            }

            DrawSeparator();

            // ---- Changes ----
            Section(L("Changes", "変更点", "تغییرات"));

            using (new EditorGUI.DisabledScope(_existingVersions.Length == 0))
            {
                _recordChanges = ProToggle(DocSnapFeature.ChangesPage, _recordChanges && _existingVersions.Length > 0,
                    L("Record changes vs a previous version", "以前のバージョンとの変更点を記録", "ثبت تغییرات نسبت به یک نسخه‌ی قبلی"));
            }
            if (_recordChanges && _existingVersions.Length > 0)
            {
                EditorGUI.indentLevel++;
                _changesBaseIndex = DocSnapEditorGui.LabeledPopup(PopupKey + "changesBase", UiLangCode,
                    L("Compare against", "比較元", "مقایسه با"), null, _changesBaseIndex, _existingVersions);
                EditorGUI.indentLevel--;
            }
            else if (_existingVersions.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    L("Available once you have at least one earlier export.",
                      "以前のエクスポートが1つ以上あると利用できます。",
                      "پس از داشتن حداقل یک خروجی قبلی در دسترس است."),
                    MessageType.None);
            }

            GUILayout.Space(14);

            // ---- Export button ----
            //
            // The click is only RECORDED here. RunExport opens modal
            // dialogs and closes this window, and doing either between
            // BeginScrollView and EndScrollView leaves Unity's layout
            // stack with a group nobody closed - which is the
            // "EndLayoutGroup: BeginLayoutGroup must be called first"
            // every single export used to print. It runs below, after
            // the scroll view has been ended and there is nothing left
            // on the stack to unbalance.
            var big = new GUIStyle(GUI.skin.button) { fontSize = 14, fixedHeight = 40 };
            if (GUILayout.Button("🚀  " + L("Export now", "今すぐエクスポート", "همین حالا خروجی بگیر"), big))
            {
                _exportRequested = true;
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField(
                L("Output: ", "出力先: ", "خروجی در: ") + DocSnapConstants.DefaultOutputFolderName + "/<version>/",
                EditorStyles.centeredGreyMiniLabel);

            DrawProPanel();

            GUILayout.Space(8);
            EditorGUILayout.EndScrollView();

            // Outside every layout group, and out of the GUI pass
            // altogether: see the Export button above. RunExport opens
            // modal dialogs and closes this window, and neither belongs
            // inside OnGUI at all.
            if (_exportRequested)
            {
                _exportRequested = false;
                EditorApplication.delayCall += RunExportDeferred;
            }

            if (_deleteRequested != null)
            {
                string target = _deleteRequested;
                _deleteRequested = null;
                RunDeleteVersion(target);
            }

            if (_clearRequested)
            {
                _clearRequested = false;
                RunClearOutput();
            }
        }

        private void RunExportDeferred()
        {
            // The window can be closed between the click and this tick.
            // Unity's fake-null makes that checkable and worth checking.
            if (this == null) { return; }
            RunExport();
        }

        // ==========================================
        // DrawDirectTmpNotice
        //
        // Persian in this window is drawn by IMGUI, and IMGUI
        // joins nothing and reorders nothing - so without help the
        // translation on screen is a row of disconnected letters
        // running backwards. Unity DirectTMP supplies that help
        // and DocSnapEditorText uses it automatically the moment
        // the package is in the project.
        //
        // The one line below is for the project that does not have
        // it. Without the line, somebody who switched this window
        // to فارسی sees mangled text and concludes the translation
        // is broken - which is both wrong and unfixable from where
        // they are standing. Shown ONLY in that state: an English
        // or Japanese user never sees it, and neither does a
        // Persian user whose project already has the package.
        // ==========================================
        private void DrawDirectTmpNotice()
        {
            if (!DocSnapEditorText.NeedsDirectTmp(UiLangCode)) { return; }

            GUILayout.Space(4);
            EditorGUILayout.HelpBox(
                L("Unity's own windows cannot join Arabic-script letters or lay out right-to-left text, so this window's Persian is drawn unjoined and reversed."
                  + " Installing Unity DirectTMP fixes it here and in your game.",
                  "Unity のウィンドウはアラビア文字の連結も右から左へのレイアウトも行わないため、このウィンドウのペルシャ語は分離した状態で逆順に描画されます。"
                  + " Unity DirectTMP を導入すると、ここでもゲーム内でも解決します。",
                  "ویندوزهای خود یونیتی نه حروف عربی/فارسی رو به هم وصل می‌کنن و نه متن راست‌به‌چپ رو درست می‌چینن،"
                  + " برای همین متن فارسی این پنجره جدا‌جدا و برعکس دیده می‌شه. با نصب Unity DirectTMP، هم اینجا و هم داخل بازی‌ات درست می‌شه."),
                MessageType.Info);

            if (GUILayout.Button(L("Get Unity DirectTMP (free)", "Unity DirectTMP を入手(無料)", "دریافت Unity DirectTMP (رایگان)")))
            {
                Application.OpenURL(DocSnapConstants.DirectTmpUrl);
            }
        }

        // ==========================================
        // DrawManageVersions
        //
        // Deleting a snapshot, and - only once none are left - the
        // output folder's contents.
        //
        // ONE AT A TIME, on purpose. A "delete all" button is a
        // different proposition from this one, and the difference
        // is that nobody can undo it: the shelf is the only copy of
        // every earlier export, and a single mis-click would take
        // the lot. Emptying the shelf is still reachable - it is
        // reachable by deleting each snapshot deliberately, having
        // read which one it is - and the folder-clearing button
        // only wakes up at the end of that road.
        //
        // Pro only, and the reason is the shelf cap rather than the
        // deleting: on an edition that keeps three snapshots, a
        // delete button turns the cap into a speed bump. Pro has no
        // cap, so there is nothing here for it to undermine.
        // ==========================================
        private void DrawManageVersions()
        {
            bool licensed = DocSnapLicense.Has(DocSnapFeature.ManageVersions);
            bool hasVersions = _existingVersions.Length > 0;

            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                L("Manage snapshots", "スナップショットの管理", "مدیریت اسنپ‌شات‌ها"),
                EditorStyles.miniBoldLabel);

            if (!licensed)
            {
                DocSnapEdition tier = DocSnapEditionMatrix.Required(DocSnapFeature.ManageVersions);
                string badge = DocSnapEditionMatrix.DisplayName(tier).ToUpperInvariant();
                if (GUILayout.Button(new GUIContent(badge,
                        L("In Unity DocSnap " + badge + " (" + DocSnapUpgradePitch.Price(tier) + ").",
                          "Unity DocSnap " + badge + "(" + DocSnapUpgradePitch.Price(tier) + ")の機能です。",
                          "این قابلیت توی Unity DocSnap " + badge + " (" + DocSnapUpgradePitch.Price(tier) + ") هست.")),
                    EditorStyles.miniButton, GUILayout.Width(46)))
                {
                    Application.OpenURL(DocSnapConstants.ProductUrl);
                }
            }
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(!licensed))
            {
                EditorGUI.indentLevel++;

                using (new EditorGUI.DisabledScope(!hasVersions))
                {
                    EditorGUILayout.BeginHorizontal();
                    _manageIndex = DocSnapEditorGui.LabeledPopup(PopupKey + "manage", UiLangCode,
                        L("Snapshot", "スナップショット", "اسنپ‌شات"), null,
                        Mathf.Clamp(_manageIndex, 0, Mathf.Max(0, _existingVersions.Length - 1)),
                        hasVersions ? _existingVersions : EmptyShelf);

                    if (GUILayout.Button("🗑  " + L("Delete", "削除", "حذف"), GUILayout.Width(90))
                        && licensed && hasVersions)
                    {
                        _deleteRequested = _existingVersions[Mathf.Clamp(_manageIndex, 0, _existingVersions.Length - 1)];
                    }
                    EditorGUILayout.EndHorizontal();
                }

                // The whole folder, and only from an empty shelf.
                using (new EditorGUI.DisabledScope(hasVersions))
                {
                    if (GUILayout.Button("🧹  " + L("Clear the output folder", "出力フォルダを空にする", "پاک کردن پوشه‌ی خروجی"))
                        && licensed && !hasVersions)
                    {
                        _clearRequested = true;
                    }
                }

                EditorGUILayout.LabelField(" ", hasVersions
                    ? L("Deleting a snapshot removes its folder from disk for good. The output folder can be cleared once no snapshots are left.",
                        "スナップショットを削除すると、そのフォルダはディスクから完全に消えます。出力フォルダはスナップショットが 1 つも残っていないときに空にできます。",
                        "حذف یک اسنپ‌شات، پوشه‌اش را برای همیشه از دیسک پاک می‌کند. پوشه‌ی خروجی وقتی می‌شود پاک کرد که هیچ اسنپ‌شاتی باقی نمانده باشد.")
                    : L("No snapshots on the shelf. Only the files Unity DocSnap wrote are removed; the folder itself and anything else in it are left alone.",
                        "スナップショットはありません。Unity DocSnap が書き出したファイルのみを削除し、フォルダ自体や他のファイルはそのまま残します。",
                        "هیچ اسنپ‌شاتی روی قفسه نیست. فقط فایل‌هایی که Unity DocSnap نوشته پاک می‌شوند؛ خودِ پوشه و بقیه‌ی محتوایش دست‌نخورده می‌مانند."),
                    EditorStyles.miniLabel);

                EditorGUI.indentLevel--;
            }
        }

        // A popup needs something to show while the shelf is empty,
        // and an empty array would draw nothing at all.
        private static readonly string[] EmptyShelf = { "—" };

        // ==========================================
        // RunDeleteVersion / RunClearOutput
        // Both run OUTSIDE OnGUI (see the deferred block at the end
        // of OnGUI) because both open a confirmation dialog, and
        // both are the only things in this window that destroy
        // something a user cannot get back.
        // ==========================================
        private void RunDeleteVersion(string version)
        {
            if (!DocSnapLicense.Has(DocSnapFeature.ManageVersions) || string.IsNullOrEmpty(version)) { return; }

            // N, not L: EditorUtility draws this and prepares its own
            // text. See DocSnapEditorText.Native.
            bool confirmed = EditorUtility.DisplayDialog(
                DocSnapConstants.ToolName + "  ⚠",
                N("Delete the snapshot \"" + version + "\" for good?\n\nIts folder and everything in it is removed from disk. This cannot be undone.",
                  "スナップショット「" + version + "」を完全に削除しますか?\n\nそのフォルダと中身がディスクから削除されます。元に戻せません。",
                  "اسنپ‌شات «" + version + "» برای همیشه حذف شود؟\n\nپوشه‌اش و هر چه داخلش هست از روی دیسک پاک می‌شود. این کار برگشت‌پذیر نیست."),
                N("Delete", "削除する", "حذف کن"),
                N("Cancel", "キャンセル", "انصراف"));
            if (!confirmed) { return; }

            string error;
            string baseRoot = DocSnapSettings.ResolveOutputRootAbsolute();
            bool ok = DocSnapVersioning.DeleteVersion(baseRoot, _registry, version, out error);

            if (!ok)
            {
                EditorUtility.DisplayDialog(DocSnapConstants.ToolName,
                    N("\"" + version + "\" was not deleted.\n\n" + error,
                      "「" + version + "」は削除されませんでした。\n\n" + error,
                      "«" + version + "» حذف نشد.\n\n" + error),
                    N("OK", "OK", "باشه"));
            }
            else
            {
                DocSnapVersioning.SaveRegistry(_registry);
                Debug.Log("[Unity DocSnap] Deleted snapshot \"" + version + "\".");
            }

            _manageIndex = 0;
            Refresh();
            Repaint();
        }

        private void RunClearOutput()
        {
            if (!DocSnapLicense.Has(DocSnapFeature.ManageVersions)) { return; }
            if (!DocSnapVersioning.CanClearOutputRoot(_registry)) { return; }

            string baseRoot = DocSnapSettings.ResolveOutputRootAbsolute();

            bool confirmed = EditorUtility.DisplayDialog(
                DocSnapConstants.ToolName + "  ⚠",
                N("Clear the output folder?\n\n" + baseRoot + "\n\nEverything Unity DocSnap wrote there is removed. The folder itself, and anything else inside it, is left alone.",
                  "出力フォルダを空にしますか?\n\n" + baseRoot + "\n\nUnity DocSnap が書き出したものはすべて削除されます。フォルダ自体と、それ以外の中身はそのまま残ります。",
                  "پوشه‌ی خروجی پاک شود؟\n\n" + baseRoot + "\n\nهر چیزی که Unity DocSnap آنجا نوشته پاک می‌شود. خودِ پوشه و هر چیز دیگری که داخلش باشد دست‌نخورده می‌ماند."),
                N("Clear", "空にする", "پاک کن"),
                N("Cancel", "キャンセル", "انصراف"));
            if (!confirmed) { return; }

            string error;
            if (!DocSnapVersioning.ClearOutputRoot(baseRoot, _registry, out error))
            {
                EditorUtility.DisplayDialog(DocSnapConstants.ToolName,
                    N("The output folder was not cleared.\n\n" + error,
                      "出力フォルダは空にできませんでした。\n\n" + error,
                      "پوشه‌ی خروجی پاک نشد.\n\n" + error),
                    N("OK", "OK", "باشه"));
            }
            else
            {
                DocSnapVersioning.SaveRegistry(_registry);
                Debug.Log("[Unity DocSnap] Cleared the output folder at \"" + baseRoot + "\".");
            }

            Refresh();
            Repaint();
        }

        // ==========================================
        // RunExport — collects the choices, persists the
        // defaults, closes the window, and runs the export.
        // ==========================================
        private void RunExport()
        {
            // Building a whole-project .unitypackage is by far the
            // heaviest thing an export can do, so it gets its own
            // explicit confirmation. Cancelling keeps the window
            // open with every choice intact.
            if (_makeBackup)
            {
                // N, not L: this is an EditorUtility dialog. The platform
                // draws it and does its own joining and reordering, so text
                // this tool prepared first comes out prepared twice - which
                // is a far more broken-looking line than the one shaping
                // was added to fix.
                bool proceed = EditorUtility.DisplayDialog(
                    DocSnapConstants.ToolName + "  ⚠",
                    N("Exporting a whole-project .unitypackage backup is a very heavy operation.\n\nIt is recommended to save your project first (File > Save Project / Ctrl+S).\n\nThe documentation site is exported completely first, and the backup is built and added at the very end - so even if the backup step fails or crashes, the exported site stays intact.",
                      "プロジェクト全体の .unitypackage バックアップの作成は非常に重い処理です。\n\n先にプロジェクトを保存することをおすすめします(File > Save Project / Ctrl+S)。\n\nサイトのエクスポートが先に完了し、バックアップは最後に追加されます。途中で失敗してもサイトは無事です。",
                      "خروجی گرفتن بک‌آپ ‎.unitypackage از کل پروژه کار به‌شدت سنگینی است.\n\nپیشنهاد می‌شود اول پروژه‌ی یونیتی را سیو کنید (File > Save Project / Ctrl+S).\n\nاول کل سایت خروجی گرفته می‌شود و بک‌آپ در انتها اضافه می‌شود؛ پس اگر باگ یا کرشی هم رخ بدهد، خروجی سایت سالم می‌ماند."),
                    N("Continue", "続行", "ادامه"),
                    N("Cancel", "キャンセル", "انصراف"));
                if (!proceed) { return; }
            }

            // A custom name that would be silently rejected is now
            // reported before anything is written, instead of after
            // the export landed in an auto-numbered folder.
            string problem = CustomVersionProblem();
            if (!_ontoExisting && problem != null)
            {
                EditorUtility.DisplayDialog(DocSnapConstants.ToolName, problem, N("OK", "OK", "باشه"));
                return;
            }

            DocSnapSettings.DefaultSiteLanguage = DocSnapLanguages.CodeAt(_siteLang);
            DocSnapSettings.DefaultSiteTheme = _siteTheme == 1 ? "dark" : "light";
            DocSnapSettings.SiteSkin = _siteSkin == 1 ? DocSnapCapability.SkinCozy
                : _siteSkin == 2 ? DocSnapCapability.SkinLite : "auto";
            DocSnapSettings.ExcludePatterns = _excludes ?? "";

            var options = new DocSnapExportOptions
            {
                defaultLanguage = DocSnapLanguages.CodeAt(_siteLang),
                defaultTheme = _siteTheme == 1 ? "dark" : "light",
                includeFiles = _includeFiles,
                scenesInBuildOnly = _scenesInBuildOnly,
                makeBackup = _makeBackup,
                recordChanges = _recordChanges && _existingVersions.Length > 0
            };

            if (_ontoExisting && _existingVersions.Length > 0)
            {
                options.versionTarget = VersionTarget.ExistingVersion;
                options.existingVersion = _existingVersions[Mathf.Clamp(_existingIndex, 0, _existingVersions.Length - 1)];
            }
            else
            {
                options.versionTarget = VersionTarget.NewVersion;
                options.customVersionName = _customVersion == null ? "" : _customVersion.Trim();
            }

            if (options.recordChanges)
            {
                options.changesBaseVersion = _existingVersions[Mathf.Clamp(_changesBaseIndex, 0, _existingVersions.Length - 1)];
            }

            Close();

            // One more tick, past Close(). The export runs for minutes
            // behind a progress bar of its own; starting it while this
            // window is still being torn down is how a closed window
            // ends up drawing.
            EditorApplication.delayCall += () => DocSnapExportService.ExportWithOptions(options);
        }

        // ==========================================
        // Small localisation + layout helpers.
        // ==========================================
        // For text the PLATFORM draws - an EditorUtility dialog, a
        // GenericMenu item. See DocSnapEditorText.Native.
        private string N(string en, string ja, string fa)
        {
            return DocSnapEditorText.Native(UiLangCode, en, ja, fa);
        }

        private string L(string en, string ja, string fa)
        {
            // DocSnapEditorText, not DocSnapText: this string is about
            // to be DRAWN by IMGUI, which joins nothing and reorders
            // nothing, so Persian arrives on screen backwards and
            // unjoined unless something shapes it first.
            return DocSnapEditorText.L(DocSnapLanguages.CodeAt(_uiLang), en, ja, fa);
        }

        private static int LangIndex(string code)
        {
            return DocSnapLanguages.IndexOf(code);
        }

        // `key` distinguishes one popup from another across frames -
        // a GenericMenu answers after OnGUI has returned, so the choice
        // has to find its way back to the control that opened it.
        private int LabeledPopup(string key, string label, string tooltip, int value, string[] options)
        {
            return DocSnapEditorGui.LabeledPopup(PopupKey + key, UiLangCode, label, tooltip, value, options);
        }

        // Namespaced so two windows cannot collide on "theme".
        private const string PopupKey = "docsnap.export.";

        // ==========================================
        // ProToggle
        // A checkbox that is only a checkbox when the feature
        // behind it is licensed.
        //
        // In the Free edition it draws disabled, keeps the
        // label, gains a "PRO" tag, and returns false whatever
        // is stored - so the option cannot be smuggled past the
        // UI by a stale field surviving a domain reload.
        //
        // Shown rather than hidden, on purpose. A hidden feature
        // is one nobody knows they could buy; a visible, greyed
        // one with a tag is the single most effective thing on
        // this window, because it turns up at the exact moment
        // somebody wanted the thing. Clicking the row opens the
        // product page, so the tag is not a dead end either.
        // ==========================================
        private bool ProToggle(DocSnapFeature feature, bool value, string label)
        {
            if (DocSnapLicense.Has(feature))
            {
                return EditorGUILayout.ToggleLeft(label, value);
            }

            // The badge names the tier that actually unlocks THIS
            // control, not the top one. A Free user looking at the
            // Changes checkbox is one $19.99 purchase away from it,
            // and a "PRO" tag there would quote them $49.99 for
            // something cheaper - which costs the sale outright if
            // they only wanted this, and reads as a bait and switch
            // if they find out afterwards.
            DocSnapEdition tier = DocSnapEditionMatrix.Required(feature);
            string badge = DocSnapEditionMatrix.DisplayName(tier).ToUpperInvariant();
            string price = DocSnapUpgradePitch.Price(tier);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ToggleLeft(label, false);
            }
            if (GUILayout.Button(new GUIContent(badge,
                    L("In Unity DocSnap " + badge + " (" + price + ") — click to see what else is in it.",
                      "Unity DocSnap " + badge + "(" + price + ")の機能です。クリックすると内容を確認できます。",
                      "این قابلیت توی Unity DocSnap " + badge + " (" + price + ") هست — کلیک کن ببین چه چیزهای دیگری دارد.")),
                EditorStyles.miniButton, GUILayout.Width(46)))
            {
                Application.OpenURL(DocSnapConstants.ProductUrl);
            }
            EditorGUILayout.EndHorizontal();
            return false;
        }

        // ==========================================
        // DrawProPanel
        // The pitch, at the bottom of the window, collapsed by
        // default after the first dismissal.
        //
        // Bottom rather than top: somebody opening this window
        // came to export something, and putting an
        // advertisement above the controls they came for is how
        // a tool starts feeling like adware. They reach this
        // after the Export button, on the way past.
        //
        // It is also genuinely dismissible - the choice is
        // remembered across projects and sessions - because an
        // upsell that cannot be turned off gets the whole tool
        // resented rather than the panel.
        // ==========================================
        private void DrawProPanel()
        {
            // Nothing left to sell somebody on the top tier.
            List<DocSnapPitchLine> locked = DocSnapUpgradePitch.Locked();
            if (locked.Count == 0) { return; }

            DrawSeparator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("✨ " + DocSnapUpgradePitch.Headline(UiLangCode), EditorStyles.boldLabel);
            if (GUILayout.Button(DocSnapLicenseStore.UpsellCollapsed ? "▾" : "▴", EditorStyles.miniButton, GUILayout.Width(24)))
            {
                DocSnapLicenseStore.UpsellCollapsed = !DocSnapLicenseStore.UpsellCollapsed;
            }
            EditorGUILayout.EndHorizontal();

            if (DocSnapLicenseStore.UpsellCollapsed) { return; }

            EditorGUILayout.LabelField(DocSnapUpgradePitch.Subhead(UiLangCode), EditorStyles.miniLabel);
            GUILayout.Space(4);

            // Only the locked lines, each tagged with the tier that
            // unlocks it. Listing something the reader already has
            // reads as padding and costs the rest of the list its
            // credibility; listing everything at one price hides the
            // fact that two of these cost less on their own.
            foreach (DocSnapPitchLine line in locked)
            {
                EditorGUILayout.LabelField(
                    line.Emoji + "  " + line.Title(UiLangCode)
                        + "   (" + DocSnapEditionMatrix.DisplayName(line.Tier) + ")",
                    EditorStyles.miniBoldLabel);
            }

            GUILayout.Space(6);

            // The cheapest tier that would unlock anything currently
            // locked gets the primary button. For a Free user with
            // the Changes checkbox in front of them that is Plus, not
            // Pro - and offering Pro first there is how a middle tier
            // ends up never being sold.
            DocSnapEdition next = DocSnapUpgradePitch.NextTier();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(DocSnapUpgradePitch.BuyLabel(next, UiLangCode)))
            {
                Application.OpenURL(DocSnapUpgradePitch.BuyUrl(next));
            }
            if (next != DocSnapEdition.Pro
                && GUILayout.Button(DocSnapUpgradePitch.BuyLabel(DocSnapEdition.Pro, UiLangCode)))
            {
                Application.OpenURL(DocSnapUpgradePitch.BuyUrl(DocSnapEdition.Pro));
            }
            if (GUILayout.Button(L("I have a key", "キーを持っています", "کد دارم"), GUILayout.Width(110)))
            {
                DocSnapLicenseWindow.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();
        }

        // The window's own language as a code rather than a popup
        // index, which is what everything outside this window
        // speaks.
        private string UiLangCode { get { return DocSnapLanguages.CodeAt(_uiLang); } }

        private static void Section(string title)
        {
            GUILayout.Space(4);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static void DrawSeparator()
        {
            GUILayout.Space(8);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.25f));
            GUILayout.Space(6);
        }
    }
}
