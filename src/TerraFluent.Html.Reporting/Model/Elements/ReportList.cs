using TerraFluent.Html.Reporting.Layout;
using TerraFluent.Html.Reporting.Compatibility;
using TerraFluent.Html.Reporting.Model.Styling;
using TerraFluent.Html.Reporting.Rendering;

namespace TerraFluent.Html.Reporting.Model.Elements;

/// <summary>A bulleted or numbered list. Splits at item boundaries (an individual item's wrapped lines are never separated).</summary>
public sealed class ReportList : IReportElement
{
    private double _itemSpacingPx = 4;
    private double _indentPx = 24;
    private int _startIndex;

    /// <summary>Bulleted or numbered.</summary>
    public ListStyle Style { get; }

    /// <summary>The list items, in order.</summary>
    public IReadOnlyList<string> Items { get; }

    /// <summary>The style item text is measured and rendered with.</summary>
    public TextStyle TextStyle { get; }

    /// <summary>Vertical gap between items, in pixels.</summary>
    public double ItemSpacingPx
    {
        get => _itemSpacingPx;
        init => _itemSpacingPx = Guard.NonNegative(value, nameof(ItemSpacingPx));
    }

    /// <summary>Horizontal indent reserved for the bullet/number marker, in pixels.</summary>
    public double IndentPx
    {
        get => _indentPx;
        init => _indentPx = Guard.NonNegative(value, nameof(IndentPx));
    }

    /// <summary>
    /// The ordinal (0-based) of <see cref="Items"/>[0] within the original,
    /// unsplit list. A fragment produced by <see cref="Split"/> carries this
    /// forward so numbered-list markers keep counting correctly across a page
    /// break instead of restarting at 1.
    /// </summary>
    public int StartIndex
    {
        get => _startIndex;
        init => _startIndex = value < 0 ? throw new ArgumentOutOfRangeException(nameof(StartIndex)) : value;
    }

    /// <summary>Creates a list.</summary>
    public ReportList(ListStyle style, IReadOnlyList<string> items, TextStyle? textStyle = null)
    {
        Style = style;
        Items = Guard.Snapshot(items, nameof(items));
        TextStyle = textStyle ?? Styling.TextStyle.Default.With(marginBottomPx: 0);
    }

    private double ItemWidth(LayoutContext context) => Math.Max(1, context.ContentWidthPx - IndentPx);

    private double MeasureItemHeight(string item, LayoutContext context)
    {
        var measured = context.TextMeasurer.Measure(item, TextStyle.ToFontSpecification(), ItemWidth(context));
        return measured.TotalHeightPx + ItemSpacingPx;
    }

    /// <inheritdoc />
    public ElementMeasurement Measure(LayoutContext context)
    {
        var total = 0.0;
        foreach (var item in Items)
        {
            total += MeasureItemHeight(item, context);
        }

        return new ElementMeasurement(total);
    }

    /// <inheritdoc />
    public SplitResult Split(double availableHeightPx, LayoutContext context)
    {
        var headItems = new List<string>();
        var used = 0.0;
        var index = 0;

        for (; index < Items.Count; index++)
        {
            var height = MeasureItemHeight(Items[index], context);
            if (used + height > availableHeightPx) break;
            used += height;
            headItems.Add(Items[index]);
        }

        if (headItems.Count == 0) return SplitResult.Unsplittable(this);

        var head = new ReportList(Style, headItems, TextStyle) { ItemSpacingPx = ItemSpacingPx, IndentPx = IndentPx, StartIndex = StartIndex };
        if (index >= Items.Count) return SplitResult.Partial(head, null);

        var tailItems = Items.Skip(index).ToList();
        var tail = new ReportList(Style, tailItems, TextStyle)
        {
            ItemSpacingPx = ItemSpacingPx,
            IndentPx = IndentPx,
            StartIndex = StartIndex + headItems.Count,
        };
        return SplitResult.Partial(head, tail);
    }

    /// <inheritdoc />
    public string RenderHtml(ElementPlacement placement, RenderContext context)
    {
        // Rendered as a native <ul>/<ol> rather than manually-positioned items:
        // the browser's own flow layout stacks the markers and wrapped item
        // text for us, which avoids re-measuring text at render time with a
        // measurer that might not match whatever ITextMeasurer the document
        // was actually laid out with.
        var tag = Style == ListStyle.Numbered ? "ol" : "ul";
        var listStyleType = Style == ListStyle.Numbered ? "decimal" : "disc";
        var startAttribute = Style == ListStyle.Numbered ? $" start=\"{StartIndex + 1}\"" : string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.Append("<div style=\"position:absolute;left:").Append(CssFormat.Px(placement.XPx))
          .Append(";top:").Append(CssFormat.Px(placement.YPx))
          .Append(";width:").Append(CssFormat.Px(placement.WidthPx))
          .Append(";height:").Append(CssFormat.Px(placement.HeightPx)).Append(";\">");

        sb.Append('<').Append(tag).Append(startAttribute)
          .Append(" style=\"margin:0;padding-left:").Append(CssFormat.Px(IndentPx))
          .Append(";list-style-type:").Append(listStyleType)
          .Append(";font-family:").Append(CssFormat.Attribute(TextStyle.FontFamily))
          .Append(";font-size:").Append(CssFormat.Px(TextStyle.FontSizePx))
          .Append(";font-weight:").Append(CssFormat.FontWeightCss(TextStyle.FontWeight))
          .Append(";font-style:").Append(CssFormat.FontStyleCss(TextStyle.FontStyle))
          .Append(";line-height:").Append(CssFormat.Number(TextStyle.LineHeightMultiplier))
          .Append(";color:").Append(CssFormat.Attribute(TextStyle.Color)).Append(";\">");

        foreach (var item in Items)
        {
            sb.Append("<li style=\"margin-bottom:").Append(CssFormat.Px(ItemSpacingPx))
              .Append(";white-space:pre-wrap;\">").Append(CssFormat.Encode(item)).Append("</li>");
        }

        sb.Append("</").Append(tag).Append("></div>");
        return sb.ToString();
    }
}
