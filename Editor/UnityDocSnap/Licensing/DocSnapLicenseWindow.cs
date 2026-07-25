// ==========================================
// DocSnapLicenseWindow
// Where a customer activates, checks or moves their
// licence, and where somebody still evaluating reads
// what Pro would add.
//
// One window doing both jobs, because they are the
// same screen at two moments in a person's life with
// this tool, and splitting them would mean the
// evaluator never sees the activation flow (so buying
// feels like a leap) and the customer has to hunt for
// where their key goes.
//
// The window is drawn in whatever language the export
// window is set to, for the same reason that one is:
// somebody who set it to Japanese is telling us
// something, and a purchase screen is the worst place
// to stop listening.
// ==========================================
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor.Licensing
{
    internal sealed class DocSnapLicenseWindow : EditorWindow
    {
        private string _keyInput = "";
        private Vector2 _scroll;

        // The last thing the server said, kept on screen until
        // the next action replaces it. A message that vanishes
        // on the next repaint is a message nobody read.
        private string _message = "";
        private MessageType _messageType = MessageType.None;
        private bool _busy;

        // ==========================================
        // ShowWindow
        // ==========================================
        public static void ShowWindow()
        {
            var window = GetWindow<DocSnapLicenseWindow>(true, DocSnapConstants.ToolName + " — Licence", true);
            window.minSize = new Vector2(460, 560);
            window.Show();
        }

        private void OnEnable()
        {
            // Pre-filled with the stored key so "is this the key I
            // think it is?" is answerable without digging through
            // an email, and so Refresh has something to act on.
            _keyInput = DocSnapLicenseStore.LicenseKey;
        }

        private static string Lang { get { return DocSnapSettings.WindowLanguage; } }

        private static string L(string en, string ja, string fa)
        {
            return DocSnapText.Resolve(Lang, en, ja, fa);
        }

        // ==========================================
        // OnGUI
        // ==========================================
        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            GUILayout.Space(10);

            DocSnapLicenseStatus status = DocSnapLicense.Status();

            DrawStatusHeader(status);
            GUILayout.Space(10);

            if (status.IsPro) { DrawActivated(status); }
            else { DrawNotActivated(status); }

            if (!string.IsNullOrEmpty(_message))
            {
                GUILayout.Space(8);
                EditorGUILayout.HelpBox(_message, _messageType);
            }

            GUILayout.Space(12);
            DrawFeatureList(status);

            GUILayout.Space(12);
            DrawMachineFooter();

            GUILayout.Space(10);
            EditorGUILayout.EndScrollView();
        }

        // ==========================================
        // DrawStatusHeader
        // The edition, stated plainly and first. Somebody
        // opening this window is almost always answering
        // "am I Pro right now?" and should not have to read
        // to find out.
        // ==========================================
        private static void DrawStatusHeader(DocSnapLicenseStatus status)
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 17 };
            GUILayout.Label("🧋 " + DocSnapConstants.ToolName, titleStyle);

            var editionStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            GUILayout.Label(status.IsPro
                ? "✨ " + L("Pro — activated on this machine", "Pro — このマシンで有効", "‏Pro — روی این سیستم فعال است")
                : "🆓 " + L("Free edition", "無料版", "نسخه‌ی رایگان"), editionStyle);

            GUILayout.Label("v" + DocSnapConstants.Version, EditorStyles.miniLabel);
        }

        // ==========================================
        // DrawActivated
        // What an existing customer needs: proof it worked,
        // when the offline period runs out, and the two
        // buttons they might want.
        // ==========================================
        private void DrawActivated(DocSnapLicenseStatus status)
        {
            EditorGUILayout.HelpBox(
                L("Every Pro feature is unlocked in every Unity project on this machine.",
                  "このマシン上のすべての Unity プロジェクトで Pro 機能が有効です。",
                  "همه‌ی قابلیت‌های Pro، توی همه‌ی پروژه‌های یونیتی این سیستم فعال است."),
                MessageType.Info);

            if (!string.IsNullOrEmpty(status.KeyLabel))
            {
                EditorGUILayout.LabelField(L("Key", "キー", "کد"), status.KeyLabel);
            }

            // Stated rather than hidden. An offline deadline
            // nobody was told about is how a licence "randomly
            // stops working" on a plane; this way it is a date on
            // a screen and the Editor renews it silently long
            // before it matters.
            if (status.ExpiresUtc > DateTime.MinValue)
            {
                int days = Mathf.Max(0, (int)(status.ExpiresUtc - DateTime.UtcNow).TotalDays);
                EditorGUILayout.LabelField(
                    L("Works offline for", "オフライン有効期間", "کارکرد آفلاین تا"),
                    days + " " + L("more days (renews itself whenever you're online)",
                                   "日(オンライン時に自動更新)",
                                   "روز دیگر (هر وقت آنلاین باشی خودش تمدید می‌شود)"));
            }

            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(_busy))
            {
                if (GUILayout.Button(L("Re-check now", "今すぐ再確認", "همین حالا دوباره بررسی کن")))
                {
                    RunRefresh();
                }
                if (GUILayout.Button(L("Release this machine", "このマシンを解除", "این سیستم را آزاد کن")))
                {
                    RunDeactivate();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                L("Releasing frees the seat so you can activate the same key on a different machine.",
                  "解除すると席が空き、同じキーを別のマシンで有効化できます。",
                  "با آزاد کردن، ظرفیت خالی می‌شود و همان کد را می‌توانی روی سیستم دیگری فعال کنی."),
                EditorStyles.wordWrappedMiniLabel);
        }

        // ==========================================
        // DrawNotActivated
        // The two audiences this half serves: somebody with
        // a key that is not working (explain, do not sell)
        // and somebody without one (sell, do not nag).
        // ==========================================
        private void DrawNotActivated(DocSnapLicenseStatus status)
        {
            if (status.NeedsAttention)
            {
                string why = DocSnapLicense.DescribeVerdict(status.Verdict);
                if (!string.IsNullOrEmpty(why))
                {
                    EditorGUILayout.HelpBox(why, MessageType.Warning);
                    GUILayout.Space(6);
                }
            }

            EditorGUILayout.LabelField(L("Licence key", "ライセンスキー", "کد لایسنس"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                L("Paste the key from your purchase email — spacing and capitalisation don't matter.",
                  "購入メールのキーを貼り付けてください。空白や大文字小文字は問いません。",
                  "کدی که توی ایمیل خریدت هست را پیست کن — فاصله و بزرگ/کوچک بودن حروف مهم نیست."),
                EditorStyles.wordWrappedMiniLabel);

            _keyInput = EditorGUILayout.TextField(_keyInput);

            GUILayout.Space(8);
            using (new EditorGUI.DisabledScope(_busy || string.IsNullOrEmpty(_keyInput)))
            {
                var big = new GUIStyle(GUI.skin.button) { fontSize = 13, fixedHeight = 34 };
                if (GUILayout.Button(_busy
                        ? L("Working…", "処理中…", "در حال انجام…")
                        : "🔓  " + L("Activate on this machine", "このマシンで有効化", "روی این سیستم فعال کن"), big))
                {
                    RunActivate();
                }
            }

            GUILayout.Space(10);
            DrawSeparator();
            GUILayout.Space(6);

            EditorGUILayout.LabelField(DocSnapProPitch.Headline(Lang), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(DocSnapProPitch.Subhead(Lang), EditorStyles.miniLabel);

            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("✨  " + L("Get Pro — " + DocSnapConstants.ProPriceDisplay,
                                            "Pro を購入 — " + DocSnapConstants.ProPriceDisplay,
                                            "خرید Pro — " + DocSnapConstants.ProPriceDisplay),
                    GUILayout.Height(28)))
            {
                Application.OpenURL(DocSnapConstants.BuyUrl);
            }
            if (GUILayout.Button(L("What's in it?", "内容を見る", "چی داخلشه؟"), GUILayout.Height(28), GUILayout.Width(130)))
            {
                Application.OpenURL(DocSnapConstants.ProductUrl);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(DocSnapProPitch.WhatFreeKeeps(Lang), EditorStyles.wordWrappedMiniLabel);
        }

        // ==========================================
        // DrawFeatureList
        // The whole matrix, with a tick or a lock against
        // each line.
        //
        // Both halves are shown even to a Pro customer -
        // for them it is a receipt, a reminder of what they
        // now have and where to find it, which is worth more
        // than an empty panel.
        // ==========================================
        private static void DrawFeatureList(DocSnapLicenseStatus status)
        {
            EditorGUILayout.LabelField(
                status.IsPro
                    ? L("Unlocked", "有効な機能", "قابلیت‌های باز شده")
                    : L("In Pro", "Pro の内容", "توی نسخه‌ی Pro"),
                EditorStyles.boldLabel);

            foreach (DocSnapProPitchLine line in DocSnapProPitch.Lines)
            {
                bool unlocked = DocSnapLicense.Has(line.Feature);
                EditorGUILayout.LabelField(
                    (unlocked ? "✅ " : "🔒 ") + line.Emoji + "  " + line.Title(Lang),
                    EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(line.Body(Lang), EditorStyles.wordWrappedMiniLabel);
                EditorGUI.indentLevel--;
                GUILayout.Space(2);
            }
        }

        // ==========================================
        // DrawMachineFooter
        // Which machine this is, as the server sees it.
        //
        // Shown because the one confusing thing about a
        // machine-bound licence is not knowing which machine
        // the server thinks you are - especially with two
        // similar laptops. The identifier is truncated: the
        // full hash is not a secret, but it is forty
        // meaningless characters and the first eight are
        // enough to match a row in the web device list.
        // ==========================================
        private static void DrawMachineFooter()
        {
            DrawSeparator();
            EditorGUILayout.LabelField(L("This machine", "このマシン", "این سیستم"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(DocSnapMachineId.Label(), EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                L("ID ", "ID ", "شناسه ") + Truncate(DocSnapMachineId.Current, 12) + "…",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                L("Only this identifier is sent — never anything about your project.",
                  "送信されるのはこの識別子のみで、プロジェクトの情報は一切送信されません。",
                  "فقط همین شناسه ارسال می‌شود — هیچ اطلاعاتی از پروژه‌ات فرستاده نمی‌شود."),
                EditorStyles.wordWrappedMiniLabel);

            GUILayout.Space(6);
            if (GUILayout.Button(L("Manage my devices in the browser", "ブラウザでデバイスを管理", "مدیریت دستگاه‌ها توی مرورگر")))
            {
                Application.OpenURL(DocSnapConstants.LicenseManageUrl);
            }
        }

        // ==========================================
        // Actions
        //
        // Each sets _busy, and each clears it in the
        // callback whatever the answer was - including the
        // failures. A window stuck on "Working…" because one
        // error path forgot to unset a flag is a window
        // somebody has to close and reopen to try again.
        // ==========================================
        private void RunActivate()
        {
            _busy = true;
            _message = "";
            DocSnapLicense.Activate(_keyInput, response =>
            {
                _busy = false;
                ReportResponse(response,
                    L("Activated. Every Pro feature is now unlocked on this machine.",
                      "有効化されました。このマシンで Pro 機能がすべて使えます。",
                      "فعال شد. حالا همه‌ی قابلیت‌های Pro روی این سیستم باز است."));
                Repaint();
            });
        }

        private void RunRefresh()
        {
            _busy = true;
            _message = "";
            DocSnapLicense.Refresh(response =>
            {
                _busy = false;
                ReportResponse(response,
                    L("Re-checked — your licence is current.",
                      "再確認しました。ライセンスは有効です。",
                      "دوباره بررسی شد — لایسنست معتبر است."));
                Repaint();
            });
        }

        private void RunDeactivate()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                DocSnapConstants.ToolName,
                L("Release this machine's seat?\n\nPro features stop here until you activate again. Your key is unaffected and can be used on another machine straight away.",
                  "このマシンの席を解除しますか?\n\n再度有効化するまで Pro 機能は使えなくなります。キー自体は影響を受けず、すぐに別のマシンで使えます。",
                  "ظرفیت این سیستم آزاد شود؟\n\nتا وقتی دوباره فعال نکنی، قابلیت‌های Pro اینجا خاموش می‌شود. خودِ کد سالم می‌ماند و بلافاصله روی سیستم دیگری قابل استفاده است."),
                L("Release", "解除する", "آزاد کن"),
                L("Cancel", "キャンセル", "انصراف"));
            if (!confirmed) { return; }

            _busy = true;
            _message = "";
            DocSnapLicense.Deactivate(response =>
            {
                _busy = false;
                _keyInput = "";
                ReportResponse(response,
                    L("This machine is no longer activated. The same key can now be used elsewhere.",
                      "このマシンの有効化を解除しました。同じキーを別の場所で使用できます。",
                      "این سیستم دیگر فعال نیست. همان کد را حالا می‌توانی جای دیگری استفاده کنی."));
                Repaint();
            });
        }

        // ==========================================
        // ReportResponse
        // One place that turns a server answer into what the
        // window says.
        //
        // A refusal carries the server's own message, which
        // is written to be read by the customer ("this key
        // is already active on DESKTOP-A1B2"). Only when
        // there is nothing usable does this substitute
        // something generic - and being unable to reach the
        // server is called that, rather than being reported
        // as a bad key, because those two need opposite
        // reactions.
        // ==========================================
        private void ReportResponse(DocSnapLicenseResponse response, string successMessage)
        {
            if (response.Ok)
            {
                _messageType = MessageType.Info;
                _message = successMessage;
                if (response.SeatsTotal > 0)
                {
                    _message += "\n" + L("Devices in use: ", "使用中のデバイス: ", "دستگاه‌های در حال استفاده: ")
                             + response.SeatsUsed + " / " + response.SeatsTotal;
                }
                return;
            }

            _messageType = MessageType.Warning;
            _message = response.ErrorCode == "offline"
                ? L("Couldn't reach the licence server. Check your connection and try again — nothing was changed.",
                    "ライセンスサーバーに接続できませんでした。接続を確認して再試行してください。変更は行われていません。",
                    "به سرور لایسنس نرسیدیم. اتصالت را چک کن و دوباره بزن — هیچ چیزی تغییر نکرد.")
                : response.Message;
        }

        private static string Truncate(string value, int length)
        {
            if (string.IsNullOrEmpty(value)) { return ""; }
            return value.Length <= length ? value : value.Substring(0, length);
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
