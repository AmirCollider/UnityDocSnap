# Security Policy

## Supported versions

The newest released version is the one that gets fixes. Unity DocSnap is pre-1.0, so there are no long-term support branches.

## Reporting a vulnerability

Please report privately through [GitHub's security advisory form](https://github.com/AmirCollider/UnityDocSnap/security/advisories/new) rather than opening a public issue.

Include what you did, what happened, and the Unity version if it matters. A minimal reproduction helps more than anything else.

## What is actually in scope

Unity DocSnap is an Editor-only tool with no runtime component, no network access and no third-party dependencies. It reads your project and writes HTML, JSON and Markdown. That leaves a small but real surface, and these are the parts of it worth reporting:

**The generated site is code.** Every GameObject name, asset path and string field value in your project is written into HTML — and into `theme/search-index.js`, which the page loads with a `<script>` tag. If any input from a project can escape its context in the output — break out of an attribute, close an inline `<script>`, or execute when the page is opened — that is a vulnerability, not a cosmetic bug. The relevant code is `HtmlPageBuilder.Escape`, `HtmlPageBuilder.JsString` and `FieldRenderer.EncodeUrlPath`, and `Tests/Editor/HtmlEscapingTests.cs` is what pins their behaviour.

**A custom logo is deliberately not inlined.** Logos of every format are embedded as a `data:` URI inside an `<img>`, where a browser renders the picture with scripting and external loads disabled by spec. An SVG whose contents reached the page as markup would be a finding.

**Exports write and delete files.** `PruneStaleOutput` is the only code that deletes anything, and it refuses to run in a folder it cannot prove it created. Anything that gets it to delete outside a DocSnap version folder is a finding.

**Exports leave the machine.** An export is meant to be shared, and it can contain more than metadata: thumbnails (on by default) are real image data, and `Export Full Project With Files` copies asset bytes. Both are documented in the README and both are opt-out. A case where an export contains project content that no setting announced is a finding.

## What is not in scope

- Anything requiring an attacker to already have write access to your Unity project. If they can add a script, they do not need this tool.
- A project name or asset path appearing in the output. That is what a documentation tool is for.
- The generated site being readable by anyone who has the folder. The output is a static site with no authentication and is not intended to have any — treat an export the way you would treat the project it documents.
