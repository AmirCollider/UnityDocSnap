// ==========================================
// DocSnapLicenseClient
// The only code in the package that talks to the
// licence server.
//
// Three calls, all POST, all JSON:
//
//   activate    bind this key to this machine and get
//               a signed token back
//   validate    renew the token for an already-bound
//               machine (the quiet background call)
//   deactivate  release this machine's seat, so the
//               customer can move to a new one without
//               emailing anybody
//
// What is sent is the key, the hashed machine id, a
// short device label, and the package version. Nothing
// about the project: not its name, not its path, not
// how many Scenes it has, not whether the export
// succeeded. A documentation tool has no business
// reporting on the project it documents, and the one
// place it would be easy to do that from is here.
//
// Every call is asynchronous with an explicit timeout
// and every failure is a message, never an exception
// that escapes into somebody's export. The Editor
// staying responsive matters more than the answer:
// the answer, when it does not arrive, is simply "keep
// using whatever token we already had".
// ==========================================
using System;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace AmirCollider.UnityDocSnap.Editor.Licensing
{
    // ==========================================
    // DocSnapLicenseResponse
    // What came back, in the shape the caller acts on.
    //
    // `Ok` and `Token` are separate on purpose: a
    // deactivation succeeds and returns no token, and a
    // refusal ("this key is already on another machine")
    // is a successful round trip carrying a message the
    // customer needs to read.
    // ==========================================
    internal sealed class DocSnapLicenseResponse
    {
        public bool Ok;
        public string Token = "";
        public string ErrorCode = "";
        public string Message = "";

        // How many of the key's seats are used, and how many
        // it has. Shown after activation so "1 of 1 devices"
        // is visible before somebody wonders where their
        // second seat went.
        public int SeatsUsed;
        public int SeatsTotal;
    }

    internal static class DocSnapLicenseClient
    {
        // Long enough for a slow connection, short enough
        // that a hung server never holds an Editor window
        // open waiting on it.
        private const int TimeoutSeconds = 20;

        public static void Activate(string licenseKey, Action<DocSnapLicenseResponse> onDone)
        {
            JsonValue body = BaseBody(licenseKey);
            body.Set("label", DocSnapMachineId.Label());
            Send("/license/activate", body, onDone);
        }

        public static void Validate(string licenseKey, Action<DocSnapLicenseResponse> onDone)
        {
            Send("/license/validate", BaseBody(licenseKey), onDone);
        }

        public static void Deactivate(string licenseKey, Action<DocSnapLicenseResponse> onDone)
        {
            Send("/license/deactivate", BaseBody(licenseKey), onDone);
        }

        // ==========================================
        // BaseBody
        // The fields every call carries. Assembled in one
        // place so a field can never be present on one
        // endpoint and missing on another.
        // ==========================================
        private static JsonValue BaseBody(string licenseKey)
        {
            JsonValue body = JsonValue.Obj();
            body.Set("product", DocSnapLicenseToken.ProductId);
            body.Set("key", NormalizeKey(licenseKey));
            body.Set("machine", DocSnapMachineId.Current);
            body.Set("version", DocSnapConstants.Version);
            return body;
        }

        // ==========================================
        // NormalizeKey
        // What somebody pasted, turned into what the server
        // stored.
        //
        // Keys arrive from an email, a receipt page or a
        // password manager, which means they arrive with
        // trailing spaces, in the wrong case, with the
        // dashes eaten by a line wrap, or with a stray
        // newline on the end. Every one of those is the same
        // key, and every one of them would fail a naive
        // lookup - producing "invalid key" for somebody
        // holding a perfectly good one, which is the worst
        // possible first impression of a paid product.
        //
        // The server normalises identically before hashing,
        // so the two always agree.
        // ==========================================
        internal static string NormalizeKey(string raw)
        {
            if (string.IsNullOrEmpty(raw)) { return ""; }

            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (c >= 'A' && c <= 'Z') { sb.Append(c); }
                else if (c >= 'a' && c <= 'z') { sb.Append(char.ToUpperInvariant(c)); }
                else if (c >= '0' && c <= '9') { sb.Append(c); }
            }

            string bare = sb.ToString();
            if (bare.Length == 0) { return ""; }

            // Re-inserted in the canonical DSNAP-XXXXX-XXXXX-XXXXX
            // grouping only when the length is exactly right;
            // anything else is passed through for the server to
            // reject with a real message rather than reshaped into
            // something that looks valid and is not.
            const string prefix = "DSNAP";
            if (!bare.StartsWith(prefix, StringComparison.Ordinal) || bare.Length != prefix.Length + 15)
            {
                return bare;
            }

            string digits = bare.Substring(prefix.Length);
            return prefix + "-" + digits.Substring(0, 5) + "-" + digits.Substring(5, 5) + "-" + digits.Substring(10, 5);
        }

        // ==========================================
        // Send
        // One POST, pumped from EditorApplication.update.
        //
        // Unity's Editor has no coroutine host that outlives
        // an EditorWindow, and a window that closes mid-flight
        // must not take the request's completion handler with
        // it - somebody clicking Activate and then closing the
        // window should still end up activated. So the pump is
        // a static update hook that unsubscribes itself, and
        // the callback is invoked exactly once whatever
        // happens.
        // ==========================================
        private static void Send(string path, JsonValue body, Action<DocSnapLicenseResponse> onDone)
        {
            UnityWebRequest request;
            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(body.ToString());

                request = new UnityWebRequest(DocSnapConstants.LicenseApiBase + path, UnityWebRequest.kHttpVerbPOST);
                request.uploadHandler = new UploadHandlerRaw(payload);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                request.timeout = TimeoutSeconds;
            }
            catch (Exception ex)
            {
                onDone(Failure("request_failed", ex.Message));
                return;
            }

            UnityWebRequestAsyncOperation operation;
            try { operation = request.SendWebRequest(); }
            catch (Exception ex)
            {
                request.Dispose();
                onDone(Failure("request_failed", ex.Message));
                return;
            }

            EditorApplication.CallbackFunction pump = null;
            pump = () =>
            {
                if (!operation.isDone) { return; }

                EditorApplication.update -= pump;

                DocSnapLicenseResponse result;
                try { result = Interpret(request); }
                catch (Exception ex) { result = Failure("bad_response", ex.Message); }
                finally { request.Dispose(); }

                onDone(result);
            };
            EditorApplication.update += pump;
        }

        // ==========================================
        // Interpret
        // Turns a finished request into an answer.
        //
        // A non-2xx status is not automatically a transport
        // problem: the server answers a refused activation
        // with 4xx AND a JSON body explaining it, and that
        // explanation is the single most useful thing the
        // customer can be shown. So the body is parsed first
        // and the status code only decides what to do when
        // there is nothing readable in it.
        // ==========================================
        private static DocSnapLicenseResponse Interpret(UnityWebRequest request)
        {
            string text = request.downloadHandler != null ? request.downloadHandler.text : "";

            JsonValue json = null;
            if (!string.IsNullOrEmpty(text))
            {
                try { json = JsonValue.Parse(text); }
                catch { json = null; }
            }

            if (json == null)
            {
                bool transportFailed = request.result != UnityWebRequest.Result.Success;
                return Failure(
                    transportFailed ? "offline" : "bad_response",
                    transportFailed ? request.error : "The licence server sent a reply this version could not read.");
            }

            var response = new DocSnapLicenseResponse
            {
                Ok = json.Get("ok").AsBool(false),
                Token = json.Get("token").AsString(""),
                ErrorCode = json.Get("error").AsString(""),
                Message = json.Get("message").AsString(""),
                SeatsUsed = (int)json.Get("seatsUsed").AsNumber(0),
                SeatsTotal = (int)json.Get("seatsTotal").AsNumber(0)
            };

            if (!response.Ok && string.IsNullOrEmpty(response.Message))
            {
                response.Message = "The licence server refused the request (" + request.responseCode + ").";
            }
            return response;
        }

        private static DocSnapLicenseResponse Failure(string code, string message)
        {
            return new DocSnapLicenseResponse
            {
                Ok = false,
                ErrorCode = code,
                Message = string.IsNullOrEmpty(message) ? "Could not reach the licence server." : message
            };
        }
    }
}
