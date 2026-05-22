using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor.Forms;

internal abstract class StanzaEditorBase : Form
{
    protected readonly ToolTip ToolTip = new();
    protected readonly List<StanzaEditorRow> Rows = [];

    protected StanzaEditorBase()
    {
        AppFormIcon.Apply(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
    }

    protected void AddRow(string token, Control ctrl, bool tall = false)
    {
        var (friendly, tok) = CreateLabels(token);
        Controls.AddRange([friendly, tok, ctrl]);
        Rows.Add(new StanzaEditorRow(friendly, tok, ctrl, tall));
        ApplyTooltip(token, friendly, tok, ctrl);
    }

    protected void AddRowLabelOnly(string token, Control associatedControl)
    {
        var (friendly, tok) = CreateLabels(token);
        Controls.AddRange([friendly, tok]);
        Rows.Add(new StanzaEditorRow(friendly, tok, associatedControl, false));
        ApplyTooltip(token, friendly, tok, associatedControl);
    }

    private (Label Friendly, Label Token) CreateLabels(string token)
    {
        var info = StanzaFieldHelp.Get(token);
        var friendly = new Label { Text = info.Label, AutoSize = true };
        var tok = TokenDocLinks.CreateLabel(token, DeviceDpi > 0 ? DeviceDpi : UiMetrics.BaselineDpi);
        return (friendly, tok);
    }

    private void ApplyTooltip(string token, params Control[] ctrls)
    {
        var text = StanzaFieldHelp.Tooltip(token);
        foreach (var c in ctrls)
            ToolTip.SetToolTip(c, text);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ToolTip.Dispose();
        base.Dispose(disposing);
    }
}

internal sealed record StanzaEditorRow(Label Friendly, Label Token, Control Field, bool Tall);
