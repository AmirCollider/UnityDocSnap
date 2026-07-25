// ==========================================
// DocSnapEditionTests
// The edition split, pinned.
//
// These tests exist because the failure modes on both
// sides of a paid gate are expensive and quiet.
//
// Gate too tight and a paying customer loses something
// they bought - and finds out in the middle of a job, in
// a way that reads as the tool being broken. Gate too
// loose and the whole free/paid distinction is decided by
// whichever line somebody last edited, which is not a
// distinction at all.
//
// What is deliberately NOT tested here is activation: it
// needs a network and a server, and a unit test that
// mocked both would only be checking that the mock
// matches the code. The parts that CAN be pinned without
// leaving the process are the matrix itself, the key
// normalisation that has to agree byte-for-byte with the
// Worker, and the token verdicts - which is where the
// interesting mistakes live anyway.
// ==========================================
using AmirCollider.UnityDocSnap.Editor.Licensing;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public sealed class DocSnapEditionTests
    {
        // ==========================================
        // The matrix
        // ==========================================

        [Test]
        public void Pro_AllowsEveryFeature()
        {
            foreach (DocSnapFeature feature in System.Enum.GetValues(typeof(DocSnapFeature)))
            {
                Assert.IsTrue(DocSnapEditionMatrix.Allows(DocSnapEdition.Pro, feature),
                    "Pro must allow every feature; it refused " + feature + ".");
            }
        }

        // Each of these is a paid feature, and each is named
        // individually rather than looped over a list. A loop over
        // "the Pro features" would be a loop over the same array
        // the implementation reads, so it would pass no matter
        // which way the array was edited - including by deleting
        // an entry, which is exactly the mistake that gives a paid
        // feature away.
        [Test]
        public void Free_DoesNotGetAiSummaries()
        {
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Free, DocSnapFeature.AiSummaries));
        }

        [Test]
        public void Free_DoesNotGetChangesPage()
        {
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Free, DocSnapFeature.ChangesPage));
        }

        [Test]
        public void Free_DoesNotGetUnlimitedVersions()
        {
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Free, DocSnapFeature.UnlimitedVersions));
        }

        [Test]
        public void Free_DoesNotGetIncrementalUpdate()
        {
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Free, DocSnapFeature.IncrementalUpdate));
        }

        [Test]
        public void Free_DoesNotGetFileCopies()
        {
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Free, DocSnapFeature.IncludeFiles));
        }

        [Test]
        public void Free_DoesNotGetProjectBackup()
        {
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Free, DocSnapFeature.ProjectBackup));
        }

        [Test]
        public void Free_DoesNotGetAutomation()
        {
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Free, DocSnapFeature.Automation));
        }

        [Test]
        public void Free_DoesNotGetWhitelabel()
        {
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Free, DocSnapFeature.Whitelabel));
        }

        // The free shelf limit is a number the export window
        // quotes, the gate enforces, and the README publishes.
        // Changing it is allowed; changing it by accident is what
        // this catches.
        [Test]
        public void Free_KeepsThreeVersionFolders()
        {
            Assert.AreEqual(3, DocSnapEditionLimits.FreeVersionFolders);
        }

        [Test]
        public void DisplayName_IsStable()
        {
            // Written into export-info.json, which is a file people
            // parse. "Free" and "Pro", not "free" or "PRO".
            Assert.AreEqual("Free", DocSnapEditionMatrix.DisplayName(DocSnapEdition.Free));
            Assert.AreEqual("Pro", DocSnapEditionMatrix.DisplayName(DocSnapEdition.Pro));
        }

        // ==========================================
        // Key normalisation
        //
        // This has to produce exactly what licensing/keys.js
        // produces in the Worker, because the server hashes the
        // normalised form and compares hashes. A divergence here
        // does not fail loudly - it tells a customer holding a
        // perfectly good key that their key is invalid.
        // ==========================================

        private const string Canonical = "DSNAP-7QK4M-2XZH9-B3TFR";

        [Test]
        public void NormalizeKey_LeavesACanonicalKeyAlone()
        {
            Assert.AreEqual(Canonical, DocSnapLicenseClient.NormalizeKey(Canonical));
        }

        [Test]
        public void NormalizeKey_RepairsHowKeysActuallyArrive()
        {
            // Each of these is a real way a key reaches the text
            // field: an email client that lowercased it, a receipt
            // whose line wrap ate the hyphens, a password manager
            // that appended whitespace, and a chat window that
            // inserted spaces.
            Assert.AreEqual(Canonical, DocSnapLicenseClient.NormalizeKey("dsnap-7qk4m-2xzh9-b3tfr"));
            Assert.AreEqual(Canonical, DocSnapLicenseClient.NormalizeKey("DSNAP7QK4M2XZH9B3TFR"));
            Assert.AreEqual(Canonical, DocSnapLicenseClient.NormalizeKey("  DSNAP-7QK4M-2XZH9-B3TFR\n"));
            Assert.AreEqual(Canonical, DocSnapLicenseClient.NormalizeKey("DSNAP 7QK4M 2XZH9 B3TFR"));
        }

        [Test]
        public void NormalizeKey_HandlesEmptyInput()
        {
            Assert.AreEqual("", DocSnapLicenseClient.NormalizeKey(null));
            Assert.AreEqual("", DocSnapLicenseClient.NormalizeKey(""));
            Assert.AreEqual("", DocSnapLicenseClient.NormalizeKey("---"));
        }

        [Test]
        public void NormalizeKey_DoesNotInventStructureForAWrongLengthKey()
        {
            // A value that is not key-shaped must reach the server
            // and be refused with a real message, not be padded into
            // something that looks canonical. Re-grouping a short
            // string would turn "I mistyped" into a lookup miss with
            // no explanation.
            Assert.AreEqual("DSNAP123", DocSnapLicenseClient.NormalizeKey("dsnap-123"));
            Assert.AreEqual("NOTAKEY", DocSnapLicenseClient.NormalizeKey("not-a-key!"));
        }

        // ==========================================
        // Token verification
        //
        // Only the paths that need no server. A token this test
        // can construct is by definition unsigned, so every case
        // here is a rejection - which is the right half to pin:
        // an accepted forgery is the failure that matters.
        // ==========================================

        [Test]
        public void Verify_MissingTokenIsMissingNotBroken()
        {
            // The distinction drives the UI: "missing" sells Pro,
            // anything else explains a problem to somebody who
            // already paid.
            DocSnapLicenseTokenInfo info = DocSnapLicenseToken.Verify(null, "machine");
            Assert.AreEqual(DocSnapTokenVerdict.Missing, info.Verdict);
            Assert.IsFalse(info.IsValid);

            info = DocSnapLicenseToken.Verify("", "machine");
            Assert.AreEqual(DocSnapTokenVerdict.Missing, info.Verdict);
        }

        [Test]
        public void Verify_RejectsSomethingThatIsNotAToken()
        {
            foreach (string junk in new[] { "not-a-token", ".", "a.", ".b", "no-dot-at-all" })
            {
                DocSnapLicenseTokenInfo info = DocSnapLicenseToken.Verify(junk, "machine");
                Assert.IsFalse(info.IsValid, "Accepted junk token: " + junk);
            }
        }

        [Test]
        public void Verify_RejectsAForgedPayload()
        {
            // The whole gate in one test: a payload claiming Pro,
            // correctly encoded, with a signature that is merely
            // plausible-looking bytes. If this ever passes, the
            // licence is decorative.
            string payload = Base64Url("{\"v\":1,\"p\":\"unity-docsnap\",\"t\":\"pro\","
                + "\"k\":\"DSNAP-FAKE\",\"m\":\"machine\",\"iat\":1,\"exp\":99999999999}");
            string forged = payload + "." + Base64Url("not really a signature");

            DocSnapLicenseTokenInfo info = DocSnapLicenseToken.Verify(forged, "machine");
            Assert.AreEqual(DocSnapTokenVerdict.BadSignature, info.Verdict);
            Assert.IsFalse(info.IsValid);
            Assert.AreEqual(DocSnapEdition.Free, info.Edition,
                "A rejected token must not leave Pro on the info object.");
        }

        [Test]
        public void Verify_ChecksTheSignatureBeforeReadingAnyField()
        {
            // A payload that is not even JSON still has to come back
            // as BadSignature rather than Malformed. The order
            // matters: reading attacker-supplied bytes to decide
            // which error to report is how a check gets talked into
            // running on untrusted input.
            string forged = Base64Url("this is not json at all") + "." + Base64Url("xxxx");
            Assert.AreEqual(DocSnapTokenVerdict.BadSignature,
                DocSnapLicenseToken.Verify(forged, "machine").Verdict);
        }

        [Test]
        public void ProductId_MatchesTheWorker()
        {
            // Hard-coded in licensing/keys.js and pages/license.js on
            // the server side. A rename on one side only would make
            // every activation succeed and every Editor reject the
            // token it just received.
            Assert.AreEqual("unity-docsnap", DocSnapLicenseToken.ProductId);
        }

        // ==========================================
        // The pitch
        // ==========================================

        [Test]
        public void EveryPitchLineSellsAFeatureThatIsActuallyLocked()
        {
            // The panel must never advertise something Free already
            // has. Tied to the matrix rather than to a copy of the
            // list, so a feature moved into Free stops being
            // advertised without anybody remembering to edit the
            // marketing copy.
            foreach (DocSnapProPitchLine line in DocSnapProPitch.Lines)
            {
                Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Free, line.Feature),
                    "The Pro pitch sells " + line.Feature + ", which the Free edition already has.");
            }
        }

        [Test]
        public void EveryProFeatureIsAdvertisedSomewhere()
        {
            // The other direction: a paid feature nobody is told
            // about is one nobody upgrades for.
            foreach (DocSnapFeature feature in System.Enum.GetValues(typeof(DocSnapFeature)))
            {
                if (DocSnapEditionMatrix.Allows(DocSnapEdition.Free, feature)) { continue; }

                bool advertised = false;
                foreach (DocSnapProPitchLine line in DocSnapProPitch.Lines)
                {
                    if (line.Feature == feature) { advertised = true; break; }
                }
                Assert.IsTrue(advertised, feature + " is a Pro feature with no line in DocSnapProPitch.");
            }
        }

        [Test]
        public void PitchLinesAreTranslatedInEveryLanguage()
        {
            foreach (DocSnapProPitchLine line in DocSnapProPitch.Lines)
            {
                foreach (string code in DocSnapLanguages.Codes())
                {
                    Assert.IsNotEmpty(line.Title(code), line.Feature + " has no title in " + code);
                    Assert.IsNotEmpty(line.Body(code), line.Feature + " has no body in " + code);
                }
            }
        }

        private static string Base64Url(string text)
        {
            string value = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
            return value.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
    }
}
