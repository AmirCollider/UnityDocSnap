# Security Policy

## Supported versions

The newest released version is the one that gets fixes. There are no long-term support branches; fixes ship in the next release and older tags are left as they were.

## Reporting a vulnerability

Please report privately through [GitHub's security advisory form](https://github.com/AmirCollider/UnityDocSnap/security/advisories/new) rather than opening a public issue.

Include what you did, what happened, and the Unity version if it matters. A minimal reproduction helps more than anything else.

The package ships as a compiled Editor assembly. Decompiling it in order to find or confirm a problem, and reporting what you find here, is explicitly permitted by the licence (§1.4 and §4) — you are not breaching anything by looking.

## What is actually in scope

Unity DocSnap is an Editor-only tool with no runtime component, no network access and no third-party dependencies. It reads your project and writes HTML, JSON and Markdown. That leaves a small but real surface, and these are the parts of it worth reporting:

**The generated site is code.** Every GameObject name, asset path and string field value in your project is written into HTML — and into `theme/search-index.js`, which the page loads with a `<script>` tag. If any input from a project can escape its context in the output — break out of an attribute, close an inline `<script>`, or execute when the page is opened — that is a vulnerability, not a cosmetic bug. The escaping all funnels through `HtmlPageBuilder.Escape`, `HtmlPageBuilder.JsString` and `FieldRenderer.EncodeUrlPath`, which is where a report should point; a test suite in the source repository pins their behaviour.

**A custom logo is deliberately not inlined.** Logos of every format are embedded as a `data:` URI inside an `<img>`, where a browser renders the picture with scripting and external loads disabled by spec. An SVG whose contents reached the page as markup would be a finding.

**Exports write and delete files.** `PruneStaleOutput` is the only code that deletes anything, and it refuses to run in a folder it cannot prove it created. Anything that gets it to delete outside a DocSnap version folder is a finding.

**Exports leave the machine.** An export is meant to be shared, and it can contain more than metadata: thumbnails (on by default) are real image data, and `Export Full Project With Files` copies asset bytes. Both are documented in the README and both are opt-out. A case where an export contains project content that no setting announced is a finding.

## What is not in scope

- Anything requiring an attacker to already have write access to your Unity project. If they can add a script, they do not need this tool.
- A project name or asset path appearing in the output. That is what a documentation tool is for.
- The generated site being readable by anyone who has the folder. The output is a static site with no authentication and is not intended to have any — treat an export the way you would treat the project it documents.
- **A way to unlock Plus or Pro without paying.** That is a commercial problem, not a vulnerability, and it is not what this process is for. It is also not a secret that one exists: the licence check runs on your machine, on code you have a copy of, which means it can be defeated by anyone determined enough. What the check is for is to keep the split unambiguous for the people who are not trying — see §3 of the [licence](LICENSE) for what is actually forbidden. Reports of this kind will be closed without a fix; the answer is the licence, not a patch.

  The parts of the licensing that **are** in scope are the ones that could hurt a customer rather than the author: anything that lets one person's licence key, machine identifier or activation token be read, guessed or reused from another machine, and anything in the activation request that carries more about a project than the README says it does (the key, a salted hash of `deviceUniqueIdentifier`, and the package version — nothing else).
