# Release Checklist

## One-time NuGet.org setup

1. Create or choose the NuGet.org owner that will own `TerraFluent.Html.Reporting`.
2. In NuGet.org **Trusted Publishing**, create a GitHub Actions policy with:
   - Repository owner: `sahebansari`
   - Repository: `TerraFluent.Html.Reporting`
   - Workflow file: `ci.yml`
   - Environment: leave empty (the workflow does not currently use a GitHub environment).
3. Add a GitHub Actions secret named `NUGET_USER` containing the NuGet.org profile name, not an email address. No long-lived NuGet API key is used.
4. After the first package is published and ownership is established, apply for the `TerraFluent` package-ID prefix reservation if it meets NuGet.org's criteria.

## Release preparation

1. Move all intended entries from `Unreleased` into a dated version in `CHANGELOG.md`.
2. Set the same version in `Directory.Build.props`.
3. Confirm `PackageReleaseNotes` points at that changelog section.
4. Run the unit suite:

   ```shell
   dotnet test tests/TerraFluent.Html.Reporting.Tests/TerraFluent.Html.Reporting.Tests.csproj --configuration Release
   ```

5. Build and install the Playwright browser engines, then run the print-layout suite:

   ```powershell
   dotnet build tests/TerraFluent.Html.Reporting.BrowserTests/TerraFluent.Html.Reporting.BrowserTests.csproj --configuration Release
   pwsh tests/TerraFluent.Html.Reporting.BrowserTests/bin/Release/net10.0/playwright.ps1 install chromium firefox webkit
   dotnet test tests/TerraFluent.Html.Reporting.BrowserTests/TerraFluent.Html.Reporting.BrowserTests.csproj --configuration Release --no-build
   ```

   On Windows machines without PowerShell 7 (`pwsh`), run the generated script
   with Windows PowerShell instead:

   ```powershell
   powershell -File tests/TerraFluent.Html.Reporting.BrowserTests/bin/Release/net10.0/playwright.ps1 install chromium firefox webkit
   ```

6. Generate and inspect the package:

   ```shell
   dotnet pack src/TerraFluent.Html.Reporting/TerraFluent.Html.Reporting.csproj --configuration Release --output artifacts
   ```

7. Preview the package README and metadata before publishing.

## Publishing

**For now**, while releases are still frequent and low-stakes, the CI
workflow publishes to NuGet.org on **every push to `master`** whose
`<Version>` in `Directory.Build.props` hasn't already been published -
`dotnet nuget push --skip-duplicate` makes an unbumped version a no-op rather
than a failure. This means step 2 above (bumping `<Version>`) is what
actually triggers a release once merged to `master` - no extra step needed.
Tighten this back to tag-only once releases need to be more deliberate (see
the workflow's `publish` job condition in
[`ci.yml`](../.github/workflows/ci.yml)).

Tag-based publishing still works too: push a tag whose name is `v` followed
by the exact package version, for example:

```shell
git tag v1.0.0
git push origin v1.0.0
```

For a tag push, the CI workflow additionally verifies the tag/package version
match before publishing (skipped for a plain `master` push, since there's no
tag to compare against). Either way, it obtains a short-lived NuGet
credential through OIDC and publishes the package.
