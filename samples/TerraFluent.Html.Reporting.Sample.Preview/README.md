# Preview/Debug Harness Sample

A minimal console tool for faster iteration when tuning a report's layout:
it paginates a document, prints its page count and every
`LayoutResult.Warning` (with the structured `Reason`/`ElementType`/
`PageIndex`/`ElementIndex` fields, not just the free-form message), writes
the generated HTML to a temp file, and opens it in your system's default
browser - one command instead of re-running a full sample project and
manually opening its output folder.

This isn't a new library feature - everything it prints is already exposed
by `ReportDocument`/`LayoutResult`. It's just a better-packaged way to use
what's already there while you're iterating on a report's layout.

## Usage

Edit `BuildDemoReport()` in `Program.cs` to build (or call into) whatever
report you're actually working on, then:

```shell
dotnet run --project samples/TerraFluent.Html.Reporting.Sample.Preview
```
