using System.Globalization;
using System.Net;
using TerraFluent.Html.Reporting.Model;

namespace TerraFluent.Html.Reporting.Rendering;

/// <summary>
/// Tiny formatting helpers shared by every <c>IReportElement.RenderHtml</c>
/// implementation. Centralizing these avoids two easy-to-repeat mistakes:
/// formatting a <see langword="double"/> with the current culture (a comma
/// decimal separator silently breaks every CSS length on machines set to
/// e.g. French/German locales), and forgetting to HTML-encode user text.
/// </summary>
internal static class CssFormat
{
    /// <summary>Formats a pixel length for use in an inline CSS value, e.g. "123.45px".</summary>
    public static string Px(double value) => value.ToString("0.##", CultureInfo.InvariantCulture) + "px";

    /// <summary>Formats a plain number (e.g. a unitless line-height) for an inline CSS value.</summary>
    public static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Formats a CSS padding/margin shorthand value, e.g. "4px 8px 4px 8px" (top right bottom left).</summary>
    public static string Box(double topPx, double rightPx, double bottomPx, double leftPx) =>
        Px(topPx) + " " + Px(rightPx) + " " + Px(bottomPx) + " " + Px(leftPx);

    /// <summary>HTML-encodes text for safe inclusion in markup (also escapes user content to prevent injection).</summary>
    public static string Encode(string text) => WebUtility.HtmlEncode(text);

    /// <summary>
    /// HTML-encodes a dynamic value written inside a quoted HTML attribute.
    /// This is required even for CSS values: quotes in a font/color value
    /// must not be able to terminate the surrounding <c>style</c> attribute.
    /// </summary>
    public static string Attribute(string value) => WebUtility.HtmlEncode(value);

    /// <summary>Maps a <see cref="TextAlignment"/> to its CSS keyword.</summary>
    public static string TextAlign(TextAlignment alignment) => alignment switch
    {
        TextAlignment.Left => "left",
        TextAlignment.Center => "center",
        TextAlignment.Right => "right",
        TextAlignment.Justify => "justify",
        _ => "left",
    };

    /// <summary>Maps a <see cref="FontWeight"/> to its CSS keyword.</summary>
    public static string FontWeightCss(FontWeight weight) => weight == Model.FontWeight.Bold ? "bold" : "normal";

    /// <summary>Maps a <see cref="FontStyle"/> to its CSS keyword.</summary>
    public static string FontStyleCss(FontStyle style) => style == Model.FontStyle.Italic ? "italic" : "normal";
}
