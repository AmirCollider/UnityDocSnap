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
        // ==========================================
        private string CustomVersionProblem()
        {
            string name = _customVersion == null ? "" : _customVersion.Trim();
            if (name.Length == 0) { return null; }

            if (!DocSnapVersioning.IsValidCustomName(name))
            {
                return L("That name can't be used as a folder name, so the export would be auto-numbered instead. Avoid / \\ : * ? \" < > |",
                         "その名前はフォルダ名に使えないため、自動採番になります。/ \\ : * ? \" < > | は使えません。",
                         "این اسم به‌عنوان نام پوشه قابل استفاده نیست و خروجی به‌جایش خودکار شماره‌گذاری می‌شود. از / \\ : * ? \" < > | استفاده نکن.");
            }

            string baseRoot = DocSnapSettings.ResolveOutputRootAbsolute();
            if (DocSnapVersioning.ExistingVersionNames(baseRoot, _registry).Contains(name))
            {
                return L("A version with that name already exists, so the export would be auto-numbered instead.",
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
            int newUiLang = EditorGUILayout.Popup(_uiLang, LangNames);
            if (newUiLang != _uiLang) { _uiLang = newUiLang; DocSnapSettings.WindowLanguage = DocSnapLanguages.CodeAt(_uiLang); }
            EditorGUILayout.EndHorizontal();

            DrawSeparator();

            // ---- Site language + theme ----
            Section(L("Generated site", "生成されるサイト", "سایت تولیدشده"));

            _siteLang = LabeledPopup(
                L("Default language", "デフォルト言語", "زبان پیش‌فرض"),
                L("The language the site opens in the first time it's viewed.", "初回表示時にサイトが開く言語。", "زبانی که سایت بار اول با آن باز می‌شود."),
                _siteLang, LangNames);

            _siteTheme = LabeledPopup(
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
                _existingIndex = EditorGUILayout.Popup(L("Target version", "対象バージョン", "نسخه‌ی هدف"), _existingIndex, _existingVersions);

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
                if (problem != null) { EditorGUILayout.HelpBox(problem, MessageType.Warning); }

                // Said BEFORE the export rather than after it. The
                // Free shelf limit changes where the output lands,
                // and finding that out from the completion dialog -
                // after ten minutes of scanning - is finding it out
                // too late to have chosen differently.
                if (!DocSnapLicense.Has(DocSnapFeature.UnlimitedVersions)
                    && _existingVersions.Length >= DocSnapEditionLimits.FreeVersionFolders)
                {
                    EditorGUILayout.HelpBox(
                        L("The free edition keeps " + DocSnapEditionLimits.FreeVersionFolders + " snapshots, and you have that many. "
                          + "This export will refresh " + (_existingVersions.Length > 0 ? _existingVersions[0] : "the newest version")
                          + " instead of adding a new folder. Nothing is deleted.",
                          "無料版のスナップショット保持数は " + DocSnapEditionLimits.FreeVersionFolders + " 件で、既に上限に達しています。"
                          + "今回は新しいフォルダを作らず " + (_existingVersions.Length > 0 ? _existingVersions[0] : "最新バージョン") + " を更新します。削除は行われません。",
                          "نسخه‌ی رایگان " + DocSnapEditionLimits.FreeVersionFolders + " اسنپ‌شات نگه می‌دارد و همین حالا همین‌قدر داری. "
                          + "این خروجی به‌جای ساختن فولدر جدید، " + (_existingVersions.Length > 0 ? _existingVersions[0] : "جدیدترین نسخه")
                          + " را بروزرسانی می‌کند. هیچ چیزی پاک نمی‌شود."),
                        MessageType.Info);
                }
            }
            EditorGUI.indentLevel--;

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
                _changesBaseIndex = EditorGUILayout.Popup(L("Compare against", "比較元", "مقایسه با"), _changesBaseIndex, _existingVersions);
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
            var big = new GUIStyle(GUI.skin.button) { fontSize = 14, fixedHeight = 40 };
            if (GUILayout.Button("🚀  " + L("Export now", "今すぐエクスポート", "همین حالا خروجی بگیر"), big))
            {
                RunExport();
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField(
                L("Output: ", "出力先: ", "خروجی در: ") + DocSnapConstants.DefaultOutputFolderName + "/<version>/",
                EditorStyles.centeredGreyMiniLabel);

            DrawProPanel();

            GUILayout.Space(8);
            EditorGUILayout.EndScrollView();
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
                bool proceed = EditorUtility.DisplayDialog(
                    DocSnapConstants.ToolName + "  ⚠",
                    L("Exporting a whole-project .unitypackage backup is a very heavy operation.\n\nIt is recommended to save your project first (File > Save Project / Ctrl+S).\n\nThe documentation site is exported completely first, and the backup is built and added at the very end - so even if the backup step fails or crashes, the exported site stays intact.",
                      "プロジェクト全体の .unitypackage バックアップの作成は非常に重い処理です。\n\n先にプロジェクトを保存することをおすすめします(File > Save Project / Ctrl+S)。\n\nサイトのエクスポートが先に完了し、バックアップは最後に追加されます。途中で失敗してもサイトは無事です。",
                      "خروجی گرفتن بک‌آپ ‎.unitypackage از کل پروژه کار به‌شدت سنگینی است.\n\nپیشنهاد می‌شود اول پروژه‌ی یونیتی را سیو کنید (File > Save Project / Ctrl+S).\n\nاول کل سایت خروجی گرفته می‌شود و بک‌آپ در انتها اضافه می‌شود؛ پس اگر باگ یا کرشی هم رخ بدهد، خروجی سایت سالم می‌ماند."),
                    L("Continue", "続行", "ادامه"),
                    L("Cancel", "キャンセル", "انصراف"));
                if (!proceed) { return; }
            }

            // A custom name that would be silently rejected is now
            // reported before anything is written, instead of after
            // the export landed in an auto-numbered folder.
            string problem = CustomVersionProblem();
            if (!_ontoExisting && problem != null)
            {
                EditorUtility.DisplayDialog(DocSnapConstants.ToolName, problem, L("OK", "OK", "باشه"));
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
            DocSnapExportService.ExportWithOptions(options);
        }

        // ==========================================
        // Small localisation + layout helpers.
        // ==========================================
        private string L(string en, string ja, string fa)
        {
            return DocSnapText.Resolve(DocSnapLanguages.CodeAt(_uiLang), en, ja, fa);
        }

        private static int LangIndex(string code)
        {
            return DocSnapLanguages.IndexOf(code);
        }

        private int LabeledPopup(string label, string tooltip, int value, string[] options)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(label, tooltip), GUILayout.Width(160));
            int result = EditorGUILayout.Popup(value, options);
            EditorGUILayout.EndHorizontal();
            return result;
        }

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

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ToggleLeft(label, false);
            }
            if (GUILayout.Button(new GUIContent("PRO",
                    L("A Unity DocSnap Pro feature — click to see what else is in it.",
                      "Unity DocSnap Pro の機能です。クリックすると内容を確認できます。",
                      "این قابلیت مال Unity DocSnap Pro است — کلیک کن ببین چه چیزهای دیگری دارد.")),
                EditorStyles.miniButton, GUILayout.Width(40)))
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
            if (DocSnapLicense.IsPro) { return; }

            DrawSeparator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("✨ " + DocSnapProPitch.Headline(UiLangCode), EditorStyles.boldLabel);
            if (GUILayout.Button(DocSnapLicenseStore.UpsellCollapsed ? "▾" : "▴", EditorStyles.miniButton, GUILayout.Width(24)))
            {
                DocSnapLicenseStore.UpsellCollapsed = !DocSnapLicenseStore.UpsellCollapsed;
            }
            EditorGUILayout.EndHorizontal();

            if (DocSnapLicenseStore.UpsellCollapsed) { return; }

            EditorGUILayout.LabelField(DocSnapProPitch.Subhead(UiLangCode), EditorStyles.miniLabel);
            GUILayout.Space(4);

            // Only the locked lines. Listing something the reader
            // already has reads as padding and costs the rest of
            // the list its credibility.
            foreach (DocSnapProPitchLine line in DocSnapProPitch.Locked())
            {
                EditorGUILayout.LabelField(line.Emoji + "  " + line.Title(UiLangCode), EditorStyles.miniBoldLabel);
            }

            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Get Pro — " + DocSnapConstants.ProPriceDisplay,
                                   "Pro を購入 — " + DocSnapConstants.ProPriceDisplay,
                                   "خرید Pro — " + DocSnapConstants.ProPriceDisplay)))
            {
                Application.OpenURL(DocSnapConstants.BuyUrl);
            }
            if (GUILayout.Button(L("I have a key", "キーを持っています", "کد دارم"), GUILayout.Width(120)))
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
