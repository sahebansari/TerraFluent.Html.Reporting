# Contributing to TerraFluent.Html.Reporting

Thanks for helping improve TerraFluent.Html.Reporting. This project is a fluent,
dependency-free .NET library that turns a report model into paginated,
print-ready HTML. Contributions should preserve predictable page breaks,
self-contained output, and a clean public API.

## Ways to Contribute

- Report bugs with a minimal reproduction and the page layout you expected.
- Improve documentation, samples, and troubleshooting notes.
- Add focused tests for pagination, rendering, and text measurement behaviour.
- Propose API improvements before doing large implementation work.

## Development Setup

Install the .NET 10 SDK, then restore and build:

```shell
dotnet restore
dotnet build -c Release
```

Run the unit and pagination tests:

```shell
dotnet test tests/TerraFluent.Html.Reporting.Tests/TerraFluent.Html.Reporting.Tests.csproj
```

### Browser print-layout tests

The browser tests render reports in Chromium, Firefox, and WebKit via Playwright
and assert real print pagination. Install the browser engines once:

```shell
dotnet build tests/TerraFluent.Html.Reporting.BrowserTests/TerraFluent.Html.Reporting.BrowserTests.csproj -c Release
pwsh tests/TerraFluent.Html.Reporting.BrowserTests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium firefox webkit
dotnet test tests/TerraFluent.Html.Reporting.BrowserTests/TerraFluent.Html.Reporting.BrowserTests.csproj -c Release
```

### Samples

Run the sample project when your change affects generated output, then open the
generated files in a browser and check the print preview:

```shell
dotnet run --project samples/TerraFluent.Html.Reporting.Sample
```

## Making Changes

1. **Fork** the repository and create a branch from `master`:
   ```shell
   git checkout -b feature/my-feature
   ```
2. Make your changes. Keep commits focused and atomic.
3. Ensure existing tests still pass and add new tests for any behaviour you
   introduce or change.
4. Open a Pull Request against `master`.

## Code Guidelines

- Keep the core package **zero-dependency**. New runtime `PackageReference`
  entries in `src/` need a strong justification.
- Preserve `netstandard2.0` compatibility. Language features are fine, but
  APIs that only exist on modern .NET need a shim in `Compatibility/` or a
  guarded fallback.
- Escape all caller-supplied text with `CssFormat.Encode` / `CssFormat.Attribute`
  when emitting markup. `AddRawHtml` is the single deliberate exception and
  must stay explicit.
- Keep public APIs fluent, discoverable, and consistent with existing builders.
- Keep nullable reference types clean — `WarningsAsErrors` includes `nullable`.
- Keep changes small and focused. Avoid unrelated reformatting.

## Pagination Changes

Pagination is the most delicate part of the library. If you change measurement
or splitting logic:

- Add a unit test that pins the expected page count and break positions.
- Add or update a browser test if the change should be visible in real print
  output.
- Note any behaviour change in `CHANGELOG.md` — page breaks moving is a
  breaking change for consumers even when the API is unchanged.

## Reporting Security Issues

Do not open a public issue. Follow [SECURITY.md](SECURITY.md).

## Release Process

1. Bump `<Version>` in `Directory.Build.props`.
2. Update `CHANGELOG.md` — move `[Unreleased]` items into a new versioned
   section, and update the `PackageReleaseNotes` anchor in
   `Directory.Build.props` to match.
3. Refresh the "What's New" section and version in `README.md` and
   `src/TerraFluent.Html.Reporting/README.md` (the NuGet-facing README).
4. Commit and push to `master`.
5. Push a matching tag, e.g. `git tag v1.2.0 && git push origin v1.2.0`.
   CI verifies the tag matches the packed version, then publishes to nuget.org.
6. Create a GitHub Release for the tag with the changelog section as notes.

See [docs/13-release-checklist.md](docs/13-release-checklist.md) for the full
checklist.
