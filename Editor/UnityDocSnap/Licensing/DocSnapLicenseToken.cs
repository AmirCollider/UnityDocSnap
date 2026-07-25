// ==========================================
// DocSnapLicenseToken
// The proof of purchase this machine holds, and the
// only thing the Editor trusts when deciding that an
// export may use a Pro feature.
//
// The shape is deliberately close to a JWS and
// deliberately not one:
//
//     base64url(payload JSON) "." base64url(signature)
//
// The signature is RSA-SHA256 over the ASCII bytes of
// the first part. The licence server holds the private
// key; the public key is compiled into PublicModulus
// below, so verification happens entirely offline.
//
// That last property is the whole point. A licence
// check that needs the network is a licence check that
// fails on a plane, behind a corporate proxy, on a
// build agent with no egress, and at 2am when the
// server is down - and it fails by taking away
// something the customer paid for, at the exact moment
// they cannot do anything about it. So the network is
// used to OBTAIN a token and never to use one: once
// activated, this machine can verify its own licence
// with no connection at all, for as long as the token
// is valid.
//
// The token still expires (DocSnapLicense renews it
// well before it does, whenever the Editor happens to
// be online). An expiry is what makes revocation
// possible at all - a refunded or charged-back key has
// to stop working eventually - and it is set long
// enough that a normal offline stretch never reaches
// it. When one does, the tool drops to Free and says
// why; it does not lock the Editor or refuse to
// export.
// ==========================================
using System;
using System.Security.Cryptography;
using System.Text;
using AmirCollider.UnityDocSnap.Editor.Json;

namespace AmirCollider.UnityDocSnap.Editor.Licensing
{
    // ==========================================
    // DocSnapTokenVerdict
    // Why a token was or was not accepted. Separate
    // values rather than a bool because the UI says
    // something different for each: an expired token
    // asks the Editor to reconnect, a machine mismatch
    // asks the customer to activate here, and a bad
    // signature means the stored value is not ours at
    // all and should simply be discarded.
    // ==========================================
    internal enum DocSnapTokenVerdict
    {
        Valid,
        Missing,
        Malformed,
        BadSignature,
        WrongProduct,
        WrongMachine,
        Expired
    }

    internal sealed class DocSnapLicenseTokenInfo
    {
        public DocSnapTokenVerdict Verdict = DocSnapTokenVerdict.Missing;
        public DocSnapEdition Edition = DocSnapEdition.Free;

        // The masked key ("DSNAP-7QK4-…-M2XZ"), for display only.
        // The full key is never inside the token: the token proves
        // an activation happened, and a stolen token should not
        // also hand over the key that produced it.
        public string KeyLabel = "";

        public string MachineId = "";
        public DateTime IssuedUtc;
        public DateTime ExpiresUtc;

        public bool IsValid { get { return Verdict == DocSnapTokenVerdict.Valid; } }

        // How long is left before the Editor must have been
        // online again. Negative once expired.
        public TimeSpan Remaining { get { return ExpiresUtc - DateTime.UtcNow; } }
    }

    internal static class DocSnapLicenseToken
    {
        // ==========================================
        // The licence server's public key.
        //
        // Public in the strict sense: it verifies signatures
        // and cannot produce them, so shipping it in a
        // readable source file gives nothing away. The
        // matching private key lives only in the Worker's
        // encrypted environment (DOCSNAP_LICENSE_PRIVATE_KEY)
        // and never leaves it.
        //
        // Both values are base64url, matching the JWK
        // encoding the Worker signs with, so the two sides
        // are literally reading the same numbers.
        // ==========================================
        private const string PublicModulus =
            "0RTS7voRCDGilW8l-QXPns9a317YfZ7Sg-xMCwK8DyjQPpuC9jZlqzQF_KIwAtcs0B9fhKgxOVn1ZAbc6f4eKA4U" +
            "SqJAtsnpO3eYEjX4SAI_GdUxLCTaXuyp7oBvWxsiPuzgBN4K7JM80Trl8WVuhYB29k-du5ToW9eING9Wzw4lSiHl" +
            "lkPGyPuJzXPPwW3eQlNIHSAK8OysLVvf-kicoEynuiUdUHbUvILMtKJ_zZzIwoOzWVuX-E0rchLtTPd_daKMXrNp" +
            "8x1wpbYz4MnN-_bFkrYc6hMY2urxfN-srQ0JbZ25VFo4ZdsvxRy8rC2I3YY9OpEzNrl2fdrBClMG5Q";

        private const string PublicExponent = "AQAB";   // 65537

        // The product this token must name. A key issued for a
        // different AmirCollider product is a valid, correctly
        // signed token that says nothing about this one, and
        // must not unlock it.
        public const string ProductId = "unity-docsnap";

        // ==========================================
        // Verify
        // Reads a stored token and says whether this machine
        // may run as Pro right now.
        //
        // Every failure path returns a verdict rather than
        // throwing. A malformed token is a thing that will
        // happen - a truncated EditorPrefs write, a value
        // hand-edited by somebody curious - and none of those
        // is a reason to break the Editor for a documentation
        // tool. The answer to "I cannot read this" is Free.
        // ==========================================
        public static DocSnapLicenseTokenInfo Verify(string token, string expectedMachineId)
        {
            var info = new DocSnapLicenseTokenInfo();

            if (string.IsNullOrEmpty(token))
            {
                info.Verdict = DocSnapTokenVerdict.Missing;
                return info;
            }

            int dot = token.IndexOf('.');
            if (dot <= 0 || dot >= token.Length - 1)
            {
                info.Verdict = DocSnapTokenVerdict.Malformed;
                return info;
            }

            string encodedPayload = token.Substring(0, dot);
            string encodedSignature = token.Substring(dot + 1);

            byte[] payloadBytes;
            byte[] signature;
            try
            {
                payloadBytes = FromBase64Url(encodedPayload);
                signature = FromBase64Url(encodedSignature);
            }
            catch
            {
                info.Verdict = DocSnapTokenVerdict.Malformed;
                return info;
            }

            // Signature first, before a single field is read. The
            // payload is attacker-supplied until this passes, and
            // trusting any of it earlier - even to decide which
            // error to report - is how a check gets talked out of
            // running.
            if (!SignatureIsValid(Encoding.ASCII.GetBytes(encodedPayload), signature))
            {
                info.Verdict = DocSnapTokenVerdict.BadSignature;
                return info;
            }

            JsonValue payload;
            try { payload = JsonValue.Parse(Encoding.UTF8.GetString(payloadBytes)); }
            catch { payload = null; }

            if (payload == null)
            {
                info.Verdict = DocSnapTokenVerdict.Malformed;
                return info;
            }

            info.KeyLabel = payload.Get("k").AsString("");
            info.MachineId = payload.Get("m").AsString("");
            info.IssuedUtc = FromUnixSeconds(payload.Get("iat").AsNumber(0));
            info.ExpiresUtc = FromUnixSeconds(payload.Get("exp").AsNumber(0));
            info.Edition = payload.Get("t").AsString("") == "pro" ? DocSnapEdition.Pro : DocSnapEdition.Free;

            if (payload.Get("p").AsString("") != ProductId)
            {
                info.Verdict = DocSnapTokenVerdict.WrongProduct;
                return info;
            }

            // A token copied from another machine verifies
            // perfectly - it was genuinely signed by us - and
            // binding it to a machine id is the only thing that
            // stops one purchase from unlocking a studio. This
            // check is the licence.
            if (string.IsNullOrEmpty(info.MachineId)
                || !string.Equals(info.MachineId, expectedMachineId, StringComparison.Ordinal))
            {
                info.Verdict = DocSnapTokenVerdict.WrongMachine;
                return info;
            }

            // A little tolerance for clock skew, in the direction
            // that costs the customer nothing. A machine whose
            // clock is five minutes fast should not lose the
            // features it paid for.
            if (DateTime.UtcNow > info.ExpiresUtc.AddMinutes(5))
            {
                info.Verdict = DocSnapTokenVerdict.Expired;
                return info;
            }

            info.Verdict = DocSnapTokenVerdict.Valid;
            return info;
        }

        // ==========================================
        // SignatureIsValid
        // RSA-SHA256 (PKCS#1 v1.5) against the embedded
        // public key.
        //
        // Wrapped whole in a try/catch: cryptography
        // availability varies across the Mono and IL2CPP
        // profiles Unity has shipped, and a platform where
        // RSA cannot be constructed must degrade to Free
        // rather than throw out of an export.
        // ==========================================
        private static bool SignatureIsValid(byte[] signedBytes, byte[] signature)
        {
            try
            {
                var parameters = new RSAParameters
                {
                    Modulus = FromBase64Url(PublicModulus),
                    Exponent = FromBase64Url(PublicExponent)
                };

                using (RSA rsa = RSA.Create())
                {
                    rsa.ImportParameters(parameters);
                    return rsa.VerifyData(signedBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
            catch
            {
                return false;
            }
        }

        // ==========================================
        // FromBase64Url
        // base64url has no padding and swaps two characters;
        // .NET's decoder wants both back.
        // ==========================================
        internal static byte[] FromBase64Url(string value)
        {
            string padded = (value ?? "").Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
                case 1: throw new FormatException("Not a valid base64url string.");
            }
            return Convert.FromBase64String(padded);
        }

        private static DateTime FromUnixSeconds(double seconds)
        {
            if (seconds <= 0) { return DateTime.MinValue; }
            try { return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds); }
            catch { return DateTime.MinValue; }
        }
    }
}
