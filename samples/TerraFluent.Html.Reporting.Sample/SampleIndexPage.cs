using System.Net;
using System.Text;
using TerraFluent.Html.Reporting.Sample.Scenarios;

namespace TerraFluent.Html.Reporting.Sample;

/// <summary>
/// Builds a landing page with a left-hand menu of every generated sample report; clicking an
/// entry loads it into a right-hand iframe instead of navigating away, so the menu stays visible.
/// </summary>
internal static class SampleIndexPage
{
    public static string Build(IReadOnlyList<ISampleScenario> scenarios)
    {
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\" />");
        html.Append("<title>TerraFluent.Html.Reporting Samples</title><style>");
        html.Append(
            "html, body { margin: 0; padding: 0; height: 100%; font-family: Segoe UI, Arial, sans-serif; } " +
            ".layout { display: flex; height: 100vh; } " +
            ".sidebar { width: 320px; flex: none; overflow-y: auto; padding: 24px; box-sizing: border-box; background: #e8e8e8; border-right: 1px solid #c9c9c9; } " +
            ".sidebar h1 { margin: 0 0 20px; font-size: 18px; } " +
            ".sidebar ul { list-style: none; margin: 0; padding: 0; } " +
            ".sidebar li { cursor: pointer; background: #ffffff; border-radius: 8px; box-shadow: 0 0 6px rgba(0,0,0,0.15); margin-bottom: 10px; padding: 12px 14px; border-left: 4px solid transparent; } " +
            ".sidebar li:hover { box-shadow: 0 0 10px rgba(0,0,0,0.3); } " +
            ".sidebar li.active { background: #2f4858; border-left-color: #5fa3c7; } " +
            ".sidebar li.active span.title, .sidebar li.active a.external, .sidebar li.active p { color: #ffffff; } " +
            ".item-row { display: flex; align-items: baseline; justify-content: space-between; gap: 8px; } " +
            ".item-row span.title { font-size: 15px; font-weight: 600; color: #2f4858; } " +
            ".item-row a.external { color: #8a8a8a; text-decoration: none; font-size: 13px; flex: none; } " +
            ".item-row a.external:hover { color: #5fa3c7; } " +
            ".sidebar p { margin: 6px 0 0; color: #4a4a4a; font-size: 12px; } " +
            ".viewer { flex: 1 1 auto; border: 0; background: #ffffff; }");
        html.Append("</style></head><body>");

        html.Append("<div class=\"layout\"><nav class=\"sidebar\"><h1>TerraFluent.Html.Reporting Samples</h1><ul>");
        foreach (var scenario in scenarios)
        {
            var fileName = WebUtility.HtmlEncode(scenario.FileName);
            html.Append("<li data-file=\"").Append(fileName).Append("\"><div class=\"item-row\">");
            html.Append("<span class=\"title\">").Append(WebUtility.HtmlEncode(scenario.Title)).Append("</span>");
            html.Append("<a class=\"external\" href=\"").Append(fileName).Append("\" target=\"_blank\" rel=\"noopener noreferrer\" title=\"Open in new tab\">&#8599;</a>");
            html.Append("</div><p>").Append(WebUtility.HtmlEncode(scenario.Description)).Append("</p></li>");
        }
        html.Append("</ul></nav>");

        var firstFileName = scenarios.Count > 0 ? WebUtility.HtmlEncode(scenarios[0].FileName) : "about:blank";
        html.Append("<iframe class=\"viewer\" name=\"viewer\" src=\"").Append(firstFileName).Append("\"></iframe>");
        html.Append("</div>");

        html.Append(
            "<script>" +
            "var viewer = document.querySelector('iframe.viewer');" +
            "var items = document.querySelectorAll('.sidebar li');" +
            "items.forEach(function (item) {" +
            "item.addEventListener('click', function () {" +
            "var active = document.querySelector('.sidebar li.active');" +
            "if (active) { active.classList.remove('active'); }" +
            "item.classList.add('active');" +
            "viewer.src = item.dataset.file;" +
            "});" +
            "});" +
            "document.querySelectorAll('.sidebar a.external').forEach(function (link) {" +
            "link.addEventListener('click', function (event) { event.stopPropagation(); });" +
            "});" +
            "if (items.length > 0) { items[0].classList.add('active'); }" +
            "</script>");

        html.Append("</body></html>");
        return html.ToString();
    }
}
