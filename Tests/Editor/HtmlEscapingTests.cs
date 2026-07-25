// ==========================================
// HtmlEscapingTests
// Every piece of project data that reaches an
// exported page - a GameObject name, a file path, a
// string field's value - goes through
// HtmlPageBuilder.Escape, JsString or Href on its
// way there. Those three functions are the entire
// boundary between "the project contains this text"
// and "the generated page does what that text
// says", and until now not one of them had a test.
//
// They are also the easiest code in the tool to
// break by accident: all three are small, all three
// look like they could be simplified, and a page
// that is subtly wrong still opens and still looks
// fine. The failure is not a crash - it is a
// GameObject called `</script>` quietly ending the
// inline script that carries the reader's language
// preference, or a Scene called `A & B` producing a
// link that 404s.
//
// A Unity project can genuinely contain every input
// below: macOS and Linux both allow quotes and
// angle brackets in asset names.
// ==========================================
using AmirCollider.UnityDocSnap.Editor.Html;
using NUnit.Framework;

namespace AmirCollider.UnityDocSnap.Editor.Tests
{
    public class HtmlEscapingTests
    {
        // ------------------------------------------
        // Escape - HTML text and attribute context
        // ------------------------------------------

        [Test]
        public void Escape_NullAndEmpty_AreEmpty()
        {
            Assert.AreEqual("", HtmlPageBuilder.Escape(null));
            Assert.AreEqual("", HtmlPageBuilder.Escape(""));
        }

        [Test]
        public void Escape_LeavesOrdinaryTextAlone()
        {
            Assert.AreEqual("StartButton", HtmlPageBuilder.Escape("StartButton"));
        }

        [Test]
        public void Escape_EscapesAllFiveMarkupCharacters()
        {
            Assert.AreEqual("&amp;&lt;&gt;&quot;&#39;", HtmlPageBuilder.Escape("&<>\"'"));
        }

        // The ampersand has to go first or every other
        // replacement's output is escaped a second time and the
        // page shows "&amp;lt;" where it meant "<".
        [Test]
        public void Escape_DoesNotDoubleEscapeItsOwnOutput()
        {
            Assert.AreEqual("&amp;lt;", HtmlPageBuilder.Escape("&lt;"));
        }

        [Test]
        public void Escape_ClosesTheScriptTagBreakout()
        {
            string escaped = HtmlPageBuilder.Escape("</script><img src=x onerror=alert(1)>");
            StringAssert.DoesNotContain("<", escaped);
            StringAssert.DoesNotContain(">", escaped);
        }

        // A quote that survives into an attribute ends it, and
        // everything after it becomes markup rather than text.
        [Test]
        public void Escape_CannotEscapeAnAttribute()
        {
            string escaped = HtmlPageBuilder.Escape("x\" onmouseover=\"alert(1)");
            StringAssert.DoesNotContain("\"", escaped);
        }

        [Test]
        public void Escape_KeepsNonLatinTextIntact()
        {
            Assert.AreEqual("سین اصلی", HtmlPageBuilder.Escape("سین اصلی"));
            Assert.AreEqual("メインシーン", HtmlPageBuilder.Escape("メインシーン"));
        }

        // ------------------------------------------
        // JsString - inside an inline <script>
        // ------------------------------------------

        [Test]
        public void JsString_NullAndEmpty_AreQuotedEmpty()
        {
            Assert.AreEqual("\"\"", HtmlPageBuilder.JsString(null));
            Assert.AreEqual("\"\"", HtmlPageBuilder.JsString(""));
        }

        [Test]
        public void JsString_WrapsInQuotes()
        {
            Assert.AreEqual("\"scenes/\"", HtmlPageBuilder.JsString("scenes/"));
        }

        [Test]
        public void JsString_EscapesQuotesAndBackslashes()
        {
            Assert.AreEqual("\"a\\\"b\"", HtmlPageBuilder.JsString("a\"b"));
            Assert.AreEqual("\"a\\\\b\"", HtmlPageBuilder.JsString("a\\b"));
        }

        [Test]
        public void JsString_EscapesLineBreaks()
        {
            // A raw newline inside a JS string literal is a syntax
            // error, so this one breaks the whole page rather than
            // one value on it.
            Assert.AreEqual("\"a\\nb\"", HtmlPageBuilder.JsString("a\nb"));
            Assert.AreEqual("\"a\\rb\"", HtmlPageBuilder.JsString("a\rb"));
            Assert.AreEqual("\"a\\tb\"", HtmlPageBuilder.JsString("a\tb"));
        }

        // The HTML parser ends an inline script at the first
        // literal "</script", wherever it appears - including
        // inside a string literal, where JavaScript itself would
        // have been perfectly happy.
        [Test]
        public void JsString_CannotCloseTheScriptElement()
        {
            string js = HtmlPageBuilder.JsString("</script><script>alert(1)</script>");
            StringAssert.DoesNotContain("<", js);
            StringAssert.DoesNotContain(">", js);
            StringAssert.Contains("\\u003c", js);
        }

        [Test]
        public void JsString_EscapesControlCharacters()
        {
            Assert.AreEqual("\"a\\u0000b\"", HtmlPageBuilder.JsString("a\0b"));
        }

        // HTML escaping is the wrong tool inside a <script>: a
        // browser does not decode entities there, so "&amp;" would
        // arrive at the script as the five characters rather than
        // as "&". This pins that JsString is not quietly doing it.
        [Test]
        public void JsString_DoesNotProduceHtmlEntities()
        {
            string js = HtmlPageBuilder.JsString("a&b");
            StringAssert.DoesNotContain("&amp;", js);
            StringAssert.Contains("\\u0026", js);
        }

        // ------------------------------------------
        // Href - a link to another page in the site
        // ------------------------------------------

        [Test]
        public void Href_LeavesAnOrdinaryPathReadable()
        {
            Assert.AreEqual("scenes/MainMenu.html", HtmlPageBuilder.Href("scenes/MainMenu.html"));
        }

        [Test]
        public void Href_KeepsSlashesAsPathSeparators()
        {
            // Percent-encoding the separators too would turn a path
            // into a single very strange file name.
            StringAssert.Contains("/", HtmlPageBuilder.Href("folders/a/b/c.html"));
        }

        [Test]
        public void Href_EncodesSpacesAndAmpersands()
        {
            string href = HtmlPageBuilder.Href("scenes/Level 1 & 2.html");
            StringAssert.DoesNotContain(" ", href);
            // Raw '&' in an href starts an entity; either encoding
            // is correct as long as it is not left bare.
            Assert.IsTrue(href.Contains("%26") || href.Contains("&amp;"), href);
        }

        [Test]
        public void Href_CannotEscapeTheAttribute()
        {
            string href = HtmlPageBuilder.Href("scenes/a\"onmouseover=x.html");
            StringAssert.DoesNotContain("\"", href);
        }

        [Test]
        public void Href_WithAnchor_JoinsWithHash()
        {
            string href = HtmlPageBuilder.Href("scenes/Main.html", "go-42");
            Assert.AreEqual("scenes/Main.html#go-42", href);
        }

        [Test]
        public void Href_WithEmptyAnchor_HasNoHash()
        {
            Assert.AreEqual("scenes/Main.html", HtmlPageBuilder.Href("scenes/Main.html", ""));
            Assert.AreEqual("scenes/Main.html", HtmlPageBuilder.Href("scenes/Main.html", null));
        }
    }
}
