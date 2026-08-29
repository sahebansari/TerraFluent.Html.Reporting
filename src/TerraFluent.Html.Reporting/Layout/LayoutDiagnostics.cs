namespace TerraFluent.Html.Reporting.Layout;

/// <summary>
/// Per-<see cref="LayoutEngine.Paginate"/>-call buffer for warnings raised
/// from inside an element's <c>Measure</c>/<c>Split</c> (e.g.
/// <see cref="Model.Elements.Table"/>/<see cref="Model.Elements.Row"/>
/// reporting a collapsed auto-width column). A fresh instance is created per
/// <see cref="LayoutEngine.Paginate"/> call and threaded through
/// <see cref="LayoutContext"/>, so it is never shared across concurrent
/// <c>Paginate</c> calls (even for the same <c>ReportDocument</c>) and needs
/// no internal synchronization.
/// </summary>
/// <remarks>
/// <see cref="LayoutEngine"/> drains <see cref="DrainPending"/> after every
/// <c>Measure</c> call and only turns the drained entries into real
/// <see cref="LayoutWarning"/>s when that particular measurement is the one
/// actually acted upon (placed, or used as the basis for a force-place) -
/// a measurement whose result is discarded in favor of a split, or a whole
/// element deferred whole to a fresh page, has its drained entries dropped,
/// since a fresh <c>Measure</c> call on the same or a successor element will
/// report again if the condition still applies.
/// </remarks>
internal sealed class LayoutDiagnostics
{
    /// <summary>One buffered, not-yet-attributed diagnostic entry.</summary>
    public readonly record struct PendingWarning(LayoutWarningReason Reason, string Message, string ElementType);

    private readonly List<PendingWarning> _pending = new();

    /// <summary>
    /// Records a diagnostic. When <paramref name="strict"/> is true (an
    /// opt-in the caller resolves from its own style/document settings),
    /// throws instead of buffering, so a consumer can fail fast on the same
    /// condition that would otherwise only produce a warning.
    /// </summary>
    public void Report(LayoutWarningReason reason, string message, string elementType, bool strict)
    {
        if (strict)
        {
            throw new InvalidOperationException(message);
        }

        _pending.Add(new PendingWarning(reason, message, elementType));
    }

    /// <summary>Returns everything buffered since the last drain, clearing the buffer.</summary>
    public IReadOnlyList<PendingWarning> DrainPending()
    {
        if (_pending.Count == 0) return Array.Empty<PendingWarning>();
        var drained = _pending.ToArray();
        _pending.Clear();
        return drained;
    }
}
