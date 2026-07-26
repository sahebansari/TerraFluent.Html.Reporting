# Security Policy

TerraFluent.Html.Reporting generates HTML/CSS documents from application-supplied
content, including text, images, and — via `AddRawHtml` — caller-provided markup.
Please report security issues privately so they can be evaluated before public
disclosure.

## Supported Versions

Security fixes are considered for the latest released package version and the
current `master` branch. Older versions may receive fixes when the issue is
severe and the fix can be applied safely.

We always recommend using the latest published version on
[NuGet](https://www.nuget.org/packages/TerraFluent.Html.Reporting).

## Reporting a Vulnerability

**Please do not open a public GitHub issue for a suspected vulnerability.**

Use GitHub's private vulnerability reporting for this repository:
**[Report a vulnerability](https://github.com/sahebansari/TerraFluent.Html.Reporting/security/advisories/new)**.
This opens a private advisory visible only to you and the maintainers. See also
the [TerraFluent security page](https://terrafluent.dev/security/).

Include:

- Affected TerraFluent.Html.Reporting version or commit.
- A concise description of the issue.
- Reproduction steps or proof-of-concept code.
- Impact assessment, including whether the issue requires untrusted input.
- Any suggested mitigation or patch, if available.

## Response Expectations

Maintainers will try to acknowledge reports within 7 days. After triage, the
expected next steps are:

- Confirm whether the report is a valid security issue.
- Identify affected versions and realistic exploit conditions.
- Prepare a fix, mitigation, or advisory.
- Coordinate disclosure timing with the reporter when practical.

## Scope

Examples of in-scope issues include:

- HTML or CSS injection through APIs that are documented as escaping their
  input (`AddParagraph`, `AddText`, `AddHeading`, table cells, page-number
  templates, and similar).
- Path traversal, arbitrary file overwrite, or unexpected file access caused by
  the image-loading or file-rendering APIs.
- Denial-of-service vectors in pagination or text measurement triggered by
  crafted but realistic content (e.g. unbounded loops or runaway allocation).
- Dependency vulnerabilities that affect normal library use.

Examples usually out of scope:

- Markup passed deliberately to `AddRawHtml`. That API is an explicit escape
  hatch and does not sanitise its input — escaping is the caller's
  responsibility. See the note below.
- Issues that require already-trusted code execution by the application using
  the library.
- Denial-of-service cases involving intentionally huge trusted inputs without a
  practical mitigation.
- Vulnerabilities in browsers, .NET, or print engines that are not caused by
  TerraFluent.Html.Reporting behaviour.

## Safe Handling Guidance

Generated reports are frequently opened in a browser, so treat the output as a
web page rather than an inert file:

- Never pass untrusted, unsanitised input to `AddRawHtml`. Use the escaping
  content APIs for anything that originates from a user.
- Validate untrusted input before embedding it in a report, and avoid writing
  rendered output to attacker-controlled paths.
- Keep the .NET runtime and package dependencies current.
