namespace rEFIndConfigEditor.UI;

internal static class FormPlacement
{
    internal static void CenterOnDisplay(Form form, Control? owner)
    {
        if (owner is not { IsHandleCreated: true })
            return;

        var area = Screen.FromControl(owner).WorkingArea;
        var size = form.Size;
        if (size.Width <= 0 || size.Height <= 0)
            size = form.ClientSize;

        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(
            area.Left + Math.Max(0, (area.Width - size.Width) / 2),
            area.Top + Math.Max(0, (area.Height - size.Height) / 2));
    }
}
