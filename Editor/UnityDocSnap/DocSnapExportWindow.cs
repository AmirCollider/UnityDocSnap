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
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor
{
    internal sealed class DocSnapExportWindow : EditorWindow
    {
        // Window UI language: 0 = en, 1 = ja, 2 = fa.
        private int _uiLang;

        // Site defaults.
        private int _siteLang;   // 0 en, 1 ja, 2 fa
        private int _siteTheme;  // 0 light, 1 dark

        // Version target.
        private bool _ontoExisting;
        private int _existingIndex;
        private string _customVersion = "";

        // Content options.
        private bool _includeFiles;
        private bool _makeBackup;
        private bool _recordChanges;
        private int _changesBaseIndex;

        private Vector2 _scroll;
        private VersionsState _registry;
        private string[] _existingVersions = new string[0];
        private string _nextAutoVersion = "V1.0.0";

        private static readonly string[] LangCodes = { "en", "ja", "fa" };

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
            Refresh();
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
            int newUiLang = EditorGUILayout.Popup(_uiLang, new[] { "English", "日本語", "فارسی" });
            if (newUiLang != _uiLang) { _uiLang = newUiLang; DocSnapSettings.WindowLanguage = LangCodes[_uiLang]; }
            EditorGUILayout.EndHorizontal();

            DrawSeparator();

            // ---- Site language + theme ----
            Section(L("Generated site", "生成されるサイト", "سایت تولیدشده"));

            _siteLang = LabeledPopup(
                L("Default language", "デフォルト言語", "زبان پیش‌فرض"),
                L("The language the site opens in the first time it's viewed.", "初回表示時にサイトが開く言語。", "زبانی که سایت بار اول با آن باز می‌شود."),
                _siteLang, new[] { "English", "日本語", "فارسی" });

            _siteTheme = LabeledPopup(
                L("Theme", "テーマ", "تم"),
                L("Light or dark colour theme the site opens in.", "サイトが開くときの明/暗テーマ。", "تم روشن یا تاریک سایت هنگام باز شدن."),
                _siteTheme, new[] { L("Light", "ライト", "روشن"), L("Dark", "ダーク", "تاریک") });

            DrawSeparator();

            // ---- Version ----
            Section(L("Version", "バージョン", "نسخه"));

            using (new EditorGUI.DisabledScope(_existingVersions.Length == 0))
            {
                _ontoExisting = !GUILayout.Toggle(!_ontoExisting,
                    "  " + L("New export (new version folder)", "新規エクスポート(新しいバージョンフォルダ)", "خروجی جدید (فولدر نسخه‌ی جدید)"),
                    EditorStyles.radioButton);
                _ontoExisting = GUILayout.Toggle(_ontoExisting,
                    "  " + L("Export onto a previous version", "以前のバージョンに上書き", "خروجی روی یکی از نسخه‌های قبلی"),
                    EditorStyles.radioButton);
            }

            EditorGUI.indentLevel++;
            if (_ontoExisting && _existingVersions.Length > 0)
            {
                _existingIndex = EditorGUILayout.Popup(L("Target version", "対象バージョン", "نسخه‌ی هدف"), _existingIndex, _existingVersions);
                EditorGUILayout.HelpBox(
                    L("This refreshes that version in place, reusing anything unchanged.",
                      "そのバージョンをその場で更新し、変更のない項目は再利用します。",
                      "این نسخه همانجا بروزرسانی می‌شود و موارد تغییرنکرده دوباره استفاده می‌شوند."),
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
            }
            EditorGUI.indentLevel--;

            DrawSeparator();

            // ---- Content ----
            Section(L("Contents", "内容", "محتوا"));

            _includeFiles = EditorGUILayout.ToggleLeft(
                L("Include file copies (bytes, not just info)", "ファイル本体もコピー(情報だけでなく)", "کپی خود فایل‌ها هم گرفته شود (نه فقط اطلاعات)"),
                _includeFiles);

            _makeBackup = EditorGUILayout.ToggleLeft(
                L("Also export a whole-project .unitypackage backup", "プロジェクト全体の .unitypackage バックアップも作成", "یک بک‌آپ .unitypackage از کل پروژه هم گرفته شود"),
                _makeBackup);
            if (_makeBackup)
            {
                EditorGUILayout.HelpBox(
                    L("Restores the entire project even if it was deleted.",
                      "プロジェクトが削除されても丸ごと復元できます。",
                      "حتی اگر پروژه پاک شود، کل آن را برمی‌گرداند."),
                    MessageType.None);
            }

            DrawSeparator();

            // ---- Changes ----
            Section(L("Changes", "変更点", "تغییرات"));

            using (new EditorGUI.DisabledScope(_existingVersions.Length == 0))
            {
                _recordChanges = EditorGUILayout.ToggleLeft(
                    L("Record changes vs a previous version", "以前のバージョンとの変更点を記録", "ثبت تغییرات نسبت به یک نسخه‌ی قبلی"),
                    _recordChanges && _existingVersions.Length > 0);
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

            GUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        // ==========================================
        // RunExport — collects the choices, persists the
        // defaults, closes the window, and runs the export.
        // ==========================================
        private void RunExport()
        {
            DocSnapSettings.DefaultSiteLanguage = LangCodes[_siteLang];
            DocSnapSettings.DefaultSiteTheme = _siteTheme == 1 ? "dark" : "light";

            var options = new DocSnapExportOptions
            {
                defaultLanguage = LangCodes[_siteLang],
                defaultTheme = _siteTheme == 1 ? "dark" : "light",
                includeFiles = _includeFiles,
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
            switch (_uiLang)
            {
                case 1: return ja;
                case 2: return fa;
                default: return en;
            }
        }

        private static int LangIndex(string code)
        {
            switch (code)
            {
                case "ja": return 1;
                case "fa": return 2;
                default: return 0;
            }
        }

        private int LabeledPopup(string label, string tooltip, int value, string[] options)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(label, tooltip), GUILayout.Width(160));
            int result = EditorGUILayout.Popup(value, options);
            EditorGUILayout.EndHorizontal();
            return result;
        }

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
