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
using System.Collections.Generic;
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

        [Test]
        public void Free_AllowsNoPaidFeature()
        {
            foreach (DocSnapFeature feature in System.Enum.GetValues(typeof(DocSnapFeature)))
            {
                if (DocSnapEditionMatrix.Required(feature) == DocSnapEdition.Free) { continue; }
                Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Free, feature),
                    feature + " is priced above Free but Free is allowed to use it.");
            }
        }

        // The tier ladder itself. Allows() is a >= comparison over
        // the enum's numeric order, so this is what stops somebody
        // renumbering the enum and silently handing Plus everything
        // Pro has.
        [Test]
        public void TiersAreOrderedCheapestFirst()
        {
            Assert.Less((int)DocSnapEdition.Free, (int)DocSnapEdition.Plus);
            Assert.Less((int)DocSnapEdition.Plus, (int)DocSnapEdition.Pro);
        }

        [Test]
        public void ProInheritsEverythingPlusHas()
        {
            foreach (DocSnapFeature feature in System.Enum.GetValues(typeof(DocSnapFeature)))
            {
                if (!DocSnapEditionMatrix.Allows(DocSnapEdition.Plus, feature)) { continue; }
                Assert.IsTrue(DocSnapEditionMatrix.Allows(DocSnapEdition.Pro, feature),
                    "Plus has " + feature + " but Pro does not - the tiers are not a ladder.");
            }
        }

        // ==========================================
        // What each tier is actually sold as
        //
        // Named individually rather than looped over the matrix. A
        // loop over "the Plus features" would be a loop over the
        // same table the implementation reads, so it would pass
        // whichever way the table was edited - including by moving
        // a $49.99 feature into the $19.99 tier, which is exactly
        // the mistake worth catching.
        // ==========================================

        [Test]
        public void Plus_IsExactlyAiSummariesAndChanges()
        {
            Assert.AreEqual(DocSnapEdition.Plus, DocSnapEditionMatrix.Required(DocSnapFeature.AiSummaries));
            Assert.AreEqual(DocSnapEdition.Plus, DocSnapEditionMatrix.Required(DocSnapFeature.ChangesPage));

            // And nothing else. Plus is sold on those two; a third
            // feature quietly landing in it is revenue moving from
            // the $49.99 tier to the $19.99 one.
            var inPlus = new List<DocSnapFeature>();
            foreach (DocSnapFeature feature in System.Enum.GetValues(typeof(DocSnapFeature)))
            {
                if (DocSnapEditionMatrix.Required(feature) == DocSnapEdition.Plus) { inPlus.Add(feature); }
            }
            Assert.AreEqual(2, inPlus.Count,
                "Plus should hold exactly AiSummaries and ChangesPage; it holds " + string.Join(", ", inPlus.ConvertAll(f => f.ToString()).ToArray()));
        }

        [Test]
        public void Plus_DoesNotGetTheProOnlyFeatures()
        {
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Plus, DocSnapFeature.UnlimitedVersions));
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Plus, DocSnapFeature.IncrementalUpdate));
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Plus, DocSnapFeature.IncludeFiles));
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Plus, DocSnapFeature.ProjectBackup));
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Plus, DocSnapFeature.Automation));
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Plus, DocSnapFeature.CustomLogo));
        }

        [Test]
        public void Free_GetsNeitherOfThePlusFeatures()
        {
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Free, DocSnapFeature.AiSummaries));
            Assert.IsFalse(DocSnapEditionMatrix.Allows(DocSnapEdition.Free, DocSnapFeature.ChangesPage));
        }

        // The badge is deliberately NOT a feature flag. Telling a
        // Plus customer on every page they generate that they are
        // running the free edition would be false, and this is the
        // check that keeps the two rules apart.
        [Test]
        public void OnlyFreeCarriesTheFreeEditionBadge()
        {
            Assert.IsTrue(DocSnapEditionMatrix.ShowsFreeEditionBadge(DocSnapEdition.Free));
            Assert.IsFalse(DocSnapEditionMatrix.ShowsFreeEditionBadge(DocSnapEdition.Plus));
            Assert.IsFalse(DocSnapEditionMatrix.ShowsFreeEditionBadge(DocSnapEdition.Pro));
        }

        // The free shelf limit is a number the export window quotes,
        // the gate enforces, and the README publishes. Changing it is
        // allowed; changing it by accident is what this catches.
        [Test]
        public void Free_KeepsThreeVersionFolders()
        {
            Assert.AreEqual(3, DocSnapEditionLimits.FreeVersionFolders);
        }

        [Test]
        public void DisplayName_IsStable()
        {
            // Written into export-info.json, which is a file people
            // parse. "Free", "Plus" and "Pro" - not "free" or "PRO".
            Assert.AreEqual("Free", DocSnapEditionMatrix.DisplayName(DocSnapEdition.Free));
            Assert.AreEqual("Plus", DocSnapEditionMatrix.DisplayName(DocSnapEdition.Plus));
            Assert.AreEqual("Pro", DocSnapEditionMatrix.DisplayName(DocSnapEdition.Pro));
        }

        // Parse and TokenTier have to be exact inverses, or a token
        // the Worker signs as "plus" is read back as Free and a
        // paying customer silently loses what they bought.
        [Test]
        public void TokenTier_RoundTrips()
        {
            foreach (DocSnapEdition edition in System.Enum.GetValues(typeof(DocSnapEdition)))
            {
                Assert.AreEqual(edition,
                    DocSnapEditionMatrix.Parse(DocSnapEditionMatrix.TokenTier(edition)),
                    "Tier string for " + edition + " does not round-trip.");
            }
        }

        [Test]
        public void Parse_TreatsAnUnknownTierAsFree()
        {
            // A token from a future version naming a tier this build
            // has never heard of must not be guessed at generously.
            Assert.AreEqual(DocSnapEdition.Free, DocSnapEditionMatrix.Parse("enterprise"));
            Assert.AreEqual(DocSnapEdition.Free, DocSnapEditionMatrix.Parse("PRO"));
            Assert.AreEqual(DocSnapEdition.Free, DocSnapEditionMatrix.Parse(""));
            Assert.AreEqual(DocSnapEdition.Free, DocSnapEditionMatrix.Parse(null));
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
            foreach (DocSnapPitchLine line in DocSnapUpgradePitch.Lines)
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
                foreach (DocSnapPitchLine line in DocSnapUpgradePitch.Lines)
                {
                    if (line.Feature == feature) { advertised = true; break; }
                }
                Assert.IsTrue(advertised, feature + " is a Pro feature with no line in DocSnapUpgradePitch.");
            }
        }

        // The single most expensive thing to get wrong on an upsell
        // surface: quoting $49.99 for something that costs $19.99
        // loses the sale outright when the customer only wanted the
        // cheaper thing.
        [Test]
        public void NextTier_QuotesTheCheapestTierThatUnlocksSomething()
        {
            Assert.AreEqual(DocSnapEdition.Plus, DocSnapUpgradePitch.NextTierFor(DocSnapEdition.Free),
                "A Free user must be offered Plus first - the AI summaries and Changes page cost less than Pro.");
            Assert.AreEqual(DocSnapEdition.Pro, DocSnapUpgradePitch.NextTierFor(DocSnapEdition.Plus));
            Assert.AreEqual(DocSnapEdition.Free, DocSnapUpgradePitch.NextTierFor(DocSnapEdition.Pro),
                "Nothing is locked for Pro, so there is no next tier to sell.");
        }

        [Test]
        public void LockedFor_ShrinksAsTheTierRises()
        {
            int free = DocSnapUpgradePitch.LockedFor(DocSnapEdition.Free).Count;
            int plus = DocSnapUpgradePitch.LockedFor(DocSnapEdition.Plus).Count;
            int pro = DocSnapUpgradePitch.LockedFor(DocSnapEdition.Pro).Count;

            Assert.AreEqual(0, pro, "Pro has nothing left locked.");
            Assert.AreEqual(free - 2, plus, "Plus should unlock exactly the two features it is sold on.");
        }

        [Test]
        public void EveryPaidTierHasAPrice()
        {
            // An empty price renders as "Get Plus — " with nothing
            // after it, which looks like a bug on a buy button.
            Assert.IsNotEmpty(DocSnapUpgradePitch.Price(DocSnapEdition.Plus));
            Assert.IsNotEmpty(DocSnapUpgradePitch.Price(DocSnapEdition.Pro));
            Assert.AreNotEqual(DocSnapUpgradePitch.Price(DocSnapEdition.Plus),
                               DocSnapUpgradePitch.Price(DocSnapEdition.Pro),
                "Two tiers at the same price is not two tiers.");
        }

        [Test]
        public void PitchLinesAreTranslatedInEveryLanguage()
        {
            foreach (DocSnapPitchLine line in DocSnapUpgradePitch.Lines)
            {
                foreach (string code in DocSnapLanguages.Codes())
                {
                    Assert.IsNotEmpty(line.Title(code), line.Feature + " has no title in " + code);
                    Assert.IsNotEmpty(line.Body(code), line.Feature + " has no body in " + code);
                }
            }
        }

        [Test]
        public void PitchIsOrderedCheapestTierFirst()
        {
            var seen = DocSnapEdition.Free;
            foreach (DocSnapPitchLine line in DocSnapUpgradePitch.Lines)
            {
                Assert.GreaterOrEqual((int)line.Tier, (int)seen,
                    "The pitch lists " + line.Feature + " (" + line.Tier + ") after a more expensive tier. "
                    + "Cheapest first, or the two Plus features are buried in a Pro list.");
                seen = line.Tier;
            }
        }

        private static string Base64Url(string text)
        {
            string value = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
            return value.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
    }
}
