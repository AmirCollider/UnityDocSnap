// ==========================================
// DocSnapLicense
// The question "may this Editor use Pro features?"
// asked and answered in one place.
//
// Everything else in the tool calls Has(...) and gets
// a bool. Nothing else parses a token, talks to the
// server, or knows what an edition is - which is what
// keeps the split honest: there is exactly one way to
// be Pro, and it is verifiable.
//
// Two properties this file is built around, both of
// which are about not punishing the customer:
//
//   • It fails towards Free, never towards broken.
//     Every error path - no token, an unreadable one,
//     a server that cannot be reached, a platform
//     without RSA - ends at "run as Free" and never at
//     an exception escaping into somebody's export. A
//     documentation tool that refuses to document
//     because it could not phone home is worse than
//     one that quietly does the free half.
//
//   • It re-checks in the background, rarely, and only
//     while the Editor is idle. A licence check on the
//     export path would put a network call in front of
//     the one action the customer actually wanted, and
//     an offline machine would then feel like a broken
//     one. So the export path reads a cached verdict
//     and nothing else; the renewal happens hours
//     before it matters, if at all.
// ==========================================
using System;
using UnityEditor;
using UnityEngine;

namespace AmirCollider.UnityDocSnap.Editor.Licensing
{
    // ==========================================
    // DocSnapLicenseStatus
    // Everything the UI needs to describe the current
    // state in one sentence, without asking four
    // separate questions and assembling them itself.
    // ==========================================
    internal sealed class DocSnapLicenseStatus
    {
        public DocSnapEdition Edition = DocSnapEdition.Free;
        public DocSnapTokenVerdict Verdict = DocSnapTokenVerdict.Missing;
        public string KeyLabel = "";
        public DateTime ExpiresUtc;

        public bool IsPro { get { return Edition == DocSnapEdition.Pro; } }

        // True when a key IS stored but is not currently
        // unlocking anything - expired, activated on a
        // different machine, or corrupted. This is the state
        // that needs explaining rather than selling to: the
        // customer already paid, and telling them about the
        // 49.99 upgrade would be insulting.
        public bool NeedsAttention
        {
            get
            {
                return !IsPro
                    && Verdict != DocSnapTokenVerdict.Missing
                    && !string.IsNullOrEmpty(DocSnapLicenseStore.LicenseKey);
            }
        }
    }

    [InitializeOnLoad]
    internal static class DocSnapLicense
    {
        // How long after the last successful server contact
        // the Editor tries again. The token outlives this
        // many times over, so a machine that is offline for a
        // fortnight never notices.
        private static readonly TimeSpan RenewInterval = TimeSpan.FromHours(20);

        // Renew once the token has less than this left, not
        // when it has already expired. Expiring is the
        // failure; this is the margin that makes it not
        // happen to somebody who opens Unity every day.
        private static readonly TimeSpan RenewWhenRemainingBelow = TimeSpan.FromDays(20);

        private static DocSnapLicenseTokenInfo _cached;
        private static bool _renewalInFlight;

        // ==========================================
        // Static constructor
        // Runs on every domain reload, which is as often as
        // this needs to happen: it costs one signature
        // verification (microseconds, no I/O) and it means
        // the very first Has(...) call of a session is
        // already answered.
        //
        // The renewal itself is deferred to the first update
        // tick rather than run here - a domain reload is
        // Unity's busiest moment and starting a web request
        // inside it is a good way to make script compilation
        // feel slow.
        // ==========================================
        static DocSnapLicense()
        {
            EditorApplication.delayCall += MaybeRenew;
        }

        // ==========================================
        // Info
        // The verified token, computed once per domain
        // reload and after any change.
        // ==========================================
        private static DocSnapLicenseTokenInfo Info
        {
            get
            {
                if (_cached == null)
                {
                    _cached = DocSnapLicenseToken.Verify(DocSnapLicenseStore.Token, DocSnapMachineId.Current);

                    // A token that cannot ever verify is not worth
                    // keeping: it will fail identically forever and
                    // its only remaining effect is to make the UI say
                    // "something is wrong" every time it is drawn. The
                    // KEY is kept, so a re-activation is one click
                    // rather than a search through an old receipt.
                    if (_cached.Verdict == DocSnapTokenVerdict.BadSignature
                        || _cached.Verdict == DocSnapTokenVerdict.WrongProduct)
                    {
                        DocSnapLicenseStore.Token = "";
                    }
                }
                return _cached;
            }
        }

        // ==========================================
        // Invalidate — drops the cached verdict so the next
        // read re-verifies. Called after anything that could
        // change the answer.
        // ==========================================
        public static void Invalidate()
        {
            _cached = null;
        }

        // ==========================================
        // Edition / IsPro / Has
        // The three things the rest of the tool asks.
        // ==========================================
        public static DocSnapEdition Edition
        {
            get { return Info.IsValid ? Info.Edition : DocSnapEdition.Free; }
        }

        public static bool IsPro
        {
            get { return Edition == DocSnapEdition.Pro; }
        }

        public static bool Has(DocSnapFeature feature)
        {
            return DocSnapEditionMatrix.Allows(Edition, feature);
        }

        // ==========================================
        // Status — the same facts, shaped for the UI.
        // ==========================================
        public static DocSnapLicenseStatus Status()
        {
            DocSnapLicenseTokenInfo info = Info;
            return new DocSnapLicenseStatus
            {
                Edition = Edition,
                Verdict = info.Verdict,
                KeyLabel = info.KeyLabel,
                ExpiresUtc = info.ExpiresUtc
            };
        }

        // ==========================================
        // DescribeVerdict
        // Why the tool is running as Free despite a key
        // being present, in words that say what to do next.
        //
        // "Invalid licence" is not a message, it is a shrug.
        // Each of these names the actual situation, because
        // each has a different fix and the customer cannot
        // guess which one they are in.
        // ==========================================
        public static string DescribeVerdict(DocSnapTokenVerdict verdict)
        {
            switch (verdict)
            {
                case DocSnapTokenVerdict.Expired:
                    return "This machine's licence needs re-checking. Connect to the internet and press Refresh — "
                         + "your key is still yours, the offline period simply ran out.";
                case DocSnapTokenVerdict.WrongMachine:
                    return "This licence was activated on a different machine. Activate it here to move it across "
                         + "(the other machine's seat is released automatically).";
                case DocSnapTokenVerdict.BadSignature:
                case DocSnapTokenVerdict.Malformed:
                    return "The stored activation could not be read, so it was discarded. Press Activate to redo it — "
                         + "nothing was lost.";
                case DocSnapTokenVerdict.WrongProduct:
                    return "That key belongs to a different product.";
                default:
                    return "";
            }
        }

        // ==========================================
        // Activate
        // Binds a key to this machine and stores the token
        // it returns.
        //
        // The key is stored only on success. Storing it
        // first would mean a mistyped key permanently
        // replaces a working one, and the UI would then
        // report the customer's own good key as the broken
        // thing.
        // ==========================================
        public static void Activate(string licenseKey, Action<DocSnapLicenseResponse> onDone)
        {
            string normalized = DocSnapLicenseClient.NormalizeKey(licenseKey);
            if (string.IsNullOrEmpty(normalized))
            {
                onDone(new DocSnapLicenseResponse
                {
                    Ok = false,
                    ErrorCode = "empty_key",
                    Message = "Paste the licence key from your purchase email first."
                });
                return;
            }

            DocSnapLicenseClient.Activate(normalized, response =>
            {
                if (response.Ok && !string.IsNullOrEmpty(response.Token))
                {
                    DocSnapLicenseStore.LicenseKey = normalized;
                    DocSnapLicenseStore.Token = response.Token;
                    DocSnapLicenseStore.LastCheckUtcTicks = DateTime.UtcNow.Ticks;
                    Invalidate();
                }
                onDone(response);
            });
        }

        // ==========================================
        // Refresh
        // Re-checks an already-stored key on demand (the
        // Refresh button), as opposed to the quiet renewal
        // below.
        // ==========================================
        public static void Refresh(Action<DocSnapLicenseResponse> onDone)
        {
            string key = DocSnapLicenseStore.LicenseKey;
            if (string.IsNullOrEmpty(key))
            {
                onDone(new DocSnapLicenseResponse
                {
                    Ok = false,
                    ErrorCode = "no_key",
                    Message = "There is no licence key stored on this machine yet."
                });
                return;
            }

            DocSnapLicenseClient.Validate(key, response =>
            {
                if (response.Ok && !string.IsNullOrEmpty(response.Token))
                {
                    DocSnapLicenseStore.Token = response.Token;
                    DocSnapLicenseStore.LastCheckUtcTicks = DateTime.UtcNow.Ticks;
                    Invalidate();
                }
                onDone(response);
            });
        }

        // ==========================================
        // Deactivate
        // Releases this machine's seat, so the same key can
        // be activated somewhere else.
        //
        // Self-service on purpose. A one-machine licence
        // with no way to move it turns every new laptop into
        // a support email, and a customer who cannot use
        // what they bought while they wait for a reply is a
        // customer who tells other people about it.
        //
        // The local state is cleared whatever the server
        // says. If the seat could not be released remotely
        // the customer is told, but leaving this machine
        // looking activated when they explicitly asked it
        // not to be would be the wrong half to keep.
        // ==========================================
        public static void Deactivate(Action<DocSnapLicenseResponse> onDone)
        {
            string key = DocSnapLicenseStore.LicenseKey;
            if (string.IsNullOrEmpty(key))
            {
                DocSnapLicenseStore.Clear();
                Invalidate();
                onDone(new DocSnapLicenseResponse { Ok = true, Message = "This machine is not activated." });
                return;
            }

            DocSnapLicenseClient.Deactivate(key, response =>
            {
                DocSnapLicenseStore.Clear();
                Invalidate();
                onDone(response);
            });
        }

        // ==========================================
        // MaybeRenew
        // The quiet background renewal.
        //
        // Does nothing at all in the overwhelmingly common
        // cases: no key stored, a token with weeks left, or
        // a check already made today. When it does run it is
        // one small POST whose failure is ignored entirely -
        // the existing token keeps working, and the next
        // domain reload will try again.
        //
        // Never runs in batch mode. A CI job is not a place
        // to make an unannounced outbound request, and a
        // build agent behind a firewall should not spend
        // twenty seconds timing out on one.
        // ==========================================
        private static void MaybeRenew()
        {
            if (_renewalInFlight || Application.isBatchMode) { return; }

            string key = DocSnapLicenseStore.LicenseKey;
            if (string.IsNullOrEmpty(key)) { return; }

            var lastCheck = new DateTime(DocSnapLicenseStore.LastCheckUtcTicks, DateTimeKind.Utc);
            if (DateTime.UtcNow - lastCheck < RenewInterval) { return; }

            DocSnapLicenseTokenInfo info = Info;

            // An expired or wrong-machine token is worth retrying:
            // the first will be replaced, and the second means the
            // customer moved machines and the server may well hand
            // this one the seat. A valid token with plenty of time
            // left is not.
            bool worthRenewing = !info.IsValid || info.Remaining < RenewWhenRemainingBelow;
            if (!worthRenewing) { return; }

            _renewalInFlight = true;
            DocSnapLicenseClient.Validate(key, response =>
            {
                _renewalInFlight = false;

                // Only a successful answer touches anything. A
                // refusal is recorded as "we did reach the server"
                // so the Editor does not retry every reload, but the
                // stored token is left exactly as it was: a server
                // that is having a bad afternoon must not be able to
                // downgrade a paying customer.
                if (response.Ok && !string.IsNullOrEmpty(response.Token))
                {
                    DocSnapLicenseStore.Token = response.Token;
                    Invalidate();
                }
                if (response.ErrorCode != "offline")
                {
                    DocSnapLicenseStore.LastCheckUtcTicks = DateTime.UtcNow.Ticks;
                }
            });
        }
    }
}
