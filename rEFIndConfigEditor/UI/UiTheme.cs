using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.Storage;

namespace rEFIndConfigEditor.UI;

public static class UiTheme
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;
    private const int DwmColorDefault = unchecked((int)0xFFFFFFFF);

    private struct ThemeColors
    {
        public Color Form, Surface, Field, Text, Muted, LogBack, LogFore, Link;
        public Color DgvHeader, DgvLine, DgvSelect, DgvSelectFore, MenuBack, ButtonBorder;
    }

    // Spec: app dark; VS-style (existing)
    private static readonly ThemeColors VSDark = new ThemeColors
    {
        Form = Color.FromArgb(30, 30, 30),
        Surface = Color.FromArgb(45, 45, 48),
        Field = Color.FromArgb(60, 60, 60),
        Text = Color.FromArgb(220, 220, 220),
        Muted = Color.FromArgb(150, 150, 150),
        LogBack = Color.FromArgb(20, 20, 20),
        LogFore = Color.White,
        Link = Color.FromArgb(100, 180, 255),
        DgvHeader = Color.FromArgb(50, 50, 50),
        DgvLine = Color.FromArgb(60, 60, 60),
        DgvSelect = Color.FromArgb(0, 99, 177),
        DgvSelectFore = Color.White,
        MenuBack = Color.FromArgb(40, 40, 40),
        ButtonBorder = Color.FromArgb(100, 100, 100)
    };

    // Dracula Classic dark (https://draculatheme.com/spec)
    private static readonly ThemeColors DraculaDarkColors = new ThemeColors
    {
        Form = Color.FromArgb(40, 42, 54),
        Surface = Color.FromArgb(52, 55, 70),
        Field = Color.FromArgb(66, 68, 80),
        Text = Color.FromArgb(248, 248, 242),
        Muted = Color.FromArgb(98, 114, 164),
        LogBack = Color.FromArgb(33, 34, 44),
        LogFore = Color.FromArgb(241, 250, 140),
        Link = Color.FromArgb(139, 233, 253),
        DgvHeader = Color.FromArgb(68, 71, 90),
        DgvLine = Color.FromArgb(68, 71, 90),
        DgvSelect = Color.FromArgb(68, 71, 90),
        DgvSelectFore = Color.FromArgb(248, 248, 242),
        MenuBack = Color.FromArgb(25, 26, 33),
        ButtonBorder = Color.FromArgb(98, 114, 164)
    };

    // Dracula light variant (Alucard-inspired)
    private static readonly ThemeColors DraculaLightColors = new ThemeColors
    {
        Form = Color.FromArgb(245, 245, 240),
        Surface = Color.FromArgb(238, 238, 232),
        Field = Color.FromArgb(252, 252, 248),
        Text = Color.FromArgb(31, 31, 47),
        Muted = Color.FromArgb(98, 114, 164),
        LogBack = Color.FromArgb(248, 248, 242),
        LogFore = Color.FromArgb(31, 31, 47),
        Link = Color.FromArgb(80, 110, 180),
        DgvHeader = Color.FromArgb(228, 228, 222),
        DgvLine = Color.FromArgb(218, 218, 212),
        DgvSelect = Color.FromArgb(189, 147, 249),
        DgvSelectFore = Color.FromArgb(31, 31, 47),
        MenuBack = Color.FromArgb(238, 238, 232),
        ButtonBorder = Color.FromArgb(189, 147, 249)
    };

    public const string MutedLabelTag = "uiMutedText";

    public const string ToolbarGlyphInactiveTag = "uiToolbarGlyphInactive";

    public static UiThemeKind EffectiveTheme(UiThemeKind theme)
    {
        if (theme == UiThemeKind.System)
            return IsWindowsAppDark() ? UiThemeKind.Dark : UiThemeKind.Light;
        return theme;
    }

    private static bool IsWindowsAppDark()
    {
        try
        {
            using RegistryKey? k = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
            if (k?.GetValue("AppsUseLightTheme") is int i)
                return i == 0;
        }
        catch
        {
        }

        return false;
    }

    public static void ApplySaved(Control root, TextBox? logTextBox = null)
    {
        var theme = new AppConfigStore().LoadUiPreferences().Theme;
        Apply(root, theme, logTextBox);
    }

    public static void Apply(Control root, UiThemeKind theme)
    {
        ApplyInternal(root, theme, null);
    }

    public static void Apply(Control root, UiThemeKind theme, TextBox? serverLogTextBox)
    {
        ApplyInternal(root, theme, serverLogTextBox);
    }

    public static void ApplyWindowFrame(Form form, UiThemeKind theme)
    {
        if (form == null)
            return;

        UiThemeKind frameTheme = theme is UiThemeKind.DraculaLight or UiThemeKind.DraculaDark
            ? theme
            : EffectiveTheme(theme);
        if (form.IsHandleCreated)
            SetImmersiveDarkMode(form, frameTheme);
        else
        {
            void OnCreate(object? s, EventArgs a)
            {
                form.HandleCreated -= OnCreate;
                SetImmersiveDarkMode(form, frameTheme);
            }
            form.HandleCreated += OnCreate;
        }
    }

    private static void ApplyInternal(Control root, UiThemeKind theme, TextBox? serverLogTextBox)
    {
        if (root == null)
            return;

        UiThemeKind t = EffectiveTheme(theme);
        ApplyRecurse(root, t, serverLogTextBox, false);
        if (root is Form form)
        {
            if (form.MainMenuStrip is not null)
                StyleToolStrip(form.MainMenuStrip, t);
            ApplyWindowFrame(form, theme);
            ScheduleThemedChildChrome(form, t);
        }
    }

    public static void ApplyThemedChildChrome(Control root, UiThemeKind theme)
    {
        if (root == null)
            return;

        ThemedChildChromeRecurse(root, EffectiveTheme(theme));
    }

    private static void ScheduleThemedChildChrome(Form form, UiThemeKind theme)
    {
        if (form == null)
            return;

        void Run()
        {
            if (form.IsDisposed)
                return;
            ThemedChildChromeRecurse(form, theme);
        }

        if (form.IsHandleCreated)
            form.BeginInvoke((Action)Run);
        else
        {
            void OnCreate(object? s, EventArgs a)
            {
                form.HandleCreated -= OnCreate;
                if (!form.IsDisposed)
                    form.BeginInvoke((Action)Run);
            }
            form.HandleCreated += OnCreate;
        }
    }

    private static void ApplyCenteredTabHeaders(CenteredTabControl tab, bool light, ThemeColors s)
    {
        if (light)
        {
            tab.FlatChrome = false;
            tab.TabHeaderBack = SystemColors.Control;
            tab.TabHeaderFore = SystemColors.ControlText;
            tab.SelectedTabHeaderBack = SystemColors.Window;
            tab.SelectedTabHeaderFore = SystemColors.ControlText;
            tab.TabStripBack = SystemColors.Control;
            tab.BackColor = SystemColors.Control;
        }
        else
        {
            tab.FlatChrome = true;
            tab.TabHeaderBack = s.Surface;
            tab.TabHeaderFore = s.Text;
            tab.SelectedTabHeaderBack = s.Form;
            tab.SelectedTabHeaderFore = s.Text;
            tab.TabStripBack = s.Form;
            tab.BackColor = s.Form;
        }

        tab.Invalidate(true);
    }

    private static void ThemedChildChromeRecurse(Control c, UiThemeKind theme)
    {
        foreach (Control child in c.Controls)
        {
            if (child is CenteredTabControl)
            {
                child.Invalidate(true);
            }
            else if (child is TabControl tab && tab.IsHandleCreated)
            {
                SetWindowThemeSystemTabs(tab);
                tab.Invalidate(true);
            }
            if (child.HasChildren)
                ThemedChildChromeRecurse(child, theme);
        }
    }

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

    private static void SetWindowThemeSystemTabs(TabControl tab)
    {
        SetWindowTheme(tab.Handle, null, null);
    }

    public static void ApplySystemContextMenuColors(ContextMenuStrip menu)
    {
        ApplyContextMenu(menu, UiThemeKind.Light);
    }

    public static void ApplyContextMenu(ContextMenuStrip menu, UiThemeKind theme)
    {
        if (menu == null)
            return;

        if (theme == UiThemeKind.Light)
        {
            menu.BackColor = SystemColors.Menu;
            menu.ForeColor = SystemColors.MenuText;
        }
        else
        {
            ThemeColors s = GetThemeColors(theme);
            menu.BackColor = s.MenuBack;
            menu.ForeColor = s.Text;
        }

        foreach (ToolStripItem item in menu.Items)
        {
            if (item is ToolStripMenuItem tsmi)
                StyleToolStripItem(tsmi, theme);
        }
    }

    private static void StyleToolStripItem(ToolStripMenuItem item, UiThemeKind theme)
    {
        if (theme == UiThemeKind.Light)
        {
            item.BackColor = SystemColors.Menu;
            item.ForeColor = SystemColors.MenuText;
        }
        else
        {
            ThemeColors s = GetThemeColors(theme);
            item.BackColor = s.MenuBack;
            item.ForeColor = s.Text;
        }

        foreach (ToolStripItem sub in item.DropDownItems)
        {
            if (sub is ToolStripMenuItem tsmi)
                StyleToolStripItem(tsmi, theme);
        }
    }

    private static void StyleToolStrip(ToolStrip strip, UiThemeKind theme)
    {
        if (theme == UiThemeKind.Light)
        {
            strip.RenderMode = ToolStripRenderMode.System;
            strip.BackColor = SystemColors.Control;
            strip.ForeColor = SystemColors.ControlText;
            foreach (ToolStripItem item in strip.Items)
            {
                item.BackColor = Color.Empty;
                item.ForeColor = Color.Empty;
                if (item is ToolStripDropDownItem dropDown)
                    StyleToolStripDropDown(dropDown, theme);
                if (item is ToolStripControlHost host && host.Control is not null)
                    ApplyRecurse(host.Control, theme, null, false);
            }
            return;
        }

        ThemeColors s = GetThemeColors(theme);
        bool menuLike = strip is MenuStrip or StatusStrip;
        Color back = menuLike ? s.MenuBack : s.Surface;
        strip.RenderMode = ToolStripRenderMode.Professional;
        strip.Renderer = new UiToolStripRenderer(s, menuLike);
        strip.BackColor = back;
        strip.ForeColor = s.Text;
        foreach (ToolStripItem item in strip.Items)
        {
            item.BackColor = back;
            item.ForeColor = s.Text;
            if (item is ToolStripDropDownItem dropDown)
                StyleToolStripDropDown(dropDown, theme);
            if (item is ToolStripControlHost host && host.Control is not null)
                ApplyRecurse(host.Control, theme, null, false);
        }
    }

    private static void StyleToolStripDropDown(ToolStripDropDownItem item, UiThemeKind theme)
    {
        if (item.DropDown is not ToolStripDropDown menu)
            return;

        if (theme == UiThemeKind.Light)
        {
            menu.RenderMode = ToolStripRenderMode.System;
            return;
        }

        ThemeColors s = GetThemeColors(theme);
        menu.RenderMode = ToolStripRenderMode.Professional;
        menu.Renderer = new UiToolStripRenderer(s, menuLike: true);
        menu.BackColor = s.MenuBack;
        menu.ForeColor = s.Text;
        foreach (ToolStripItem sub in menu.Items)
        {
            sub.BackColor = s.MenuBack;
            sub.ForeColor = s.Text;
            if (sub is ToolStripMenuItem tsmi)
                StyleToolStripItem(tsmi, theme);
        }
    }

    private static bool IsToolbarGlyphInactiveButton(Control c)
    {
        if (c is not Button)
            return false;

        object? tag = c.Tag;
        return ReferenceEquals(tag, ToolbarGlyphInactiveTag)
               || (tag is string s && s == ToolbarGlyphInactiveTag);
    }

    private static ThemeColors GetThemeColors(UiThemeKind kind)
    {
        return kind switch
        {
            UiThemeKind.DraculaLight => DraculaLightColors,
            UiThemeKind.DraculaDark => DraculaDarkColors,
            _ => VSDark
        };
    }

    private static bool UsesDarkWindowFrame(UiThemeKind theme)
    {
        return theme is UiThemeKind.Dark or UiThemeKind.DraculaDark;
    }

    private static void ApplyRecurse(Control c, UiThemeKind theme, TextBox? serverLogTextBox, bool isLog)
    {
        isLog = isLog || (serverLogTextBox != null && ReferenceEquals(c, serverLogTextBox));
        bool light = theme == UiThemeKind.Light;
        ThemeColors s = default;
        if (!light)
            s = GetThemeColors(theme);

        switch (c)
        {
            case CenteredTabControl ctc:
                ApplyCenteredTabHeaders(ctc, light, s);
                break;
            case TabControl tc:
                if (tc.Tag is UiThemeKind)
                    tc.Tag = null;
                tc.DrawMode = TabDrawMode.Normal;
                tc.BackColor = SystemColors.Control;
                break;
            case TabPage tp:
                if (light)
                {
                    tp.BackColor = SystemColors.Control;
                    tp.UseVisualStyleBackColor = true;
                    tp.ForeColor = SystemColors.ControlText;
                }
                else
                {
                    tp.BackColor = s.Form;
                    tp.UseVisualStyleBackColor = false;
                    tp.ForeColor = s.Text;
                }
                break;
            case DataGridView dgv:
                if (light)
                {
                    dgv.BackgroundColor = SystemColors.AppWorkspace;
                    dgv.BorderStyle = BorderStyle.Fixed3D;
                    dgv.GridColor = SystemColors.Control;
                    dgv.EnableHeadersVisualStyles = true;
                    dgv.DefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = SystemColors.Window,
                        ForeColor = SystemColors.WindowText,
                        SelectionBackColor = SystemColors.Highlight,
                        SelectionForeColor = SystemColors.HighlightText
                    };
                    dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = SystemColors.Control,
                        ForeColor = SystemColors.ControlText
                    };
                }
                else
                {
                    dgv.BackgroundColor = s.Form;
                    dgv.BorderStyle = BorderStyle.None;
                    dgv.GridColor = s.DgvLine;
                    dgv.EnableHeadersVisualStyles = false;
                    dgv.DefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = s.Surface,
                        ForeColor = s.Text,
                        SelectionBackColor = s.DgvSelect,
                        SelectionForeColor = s.DgvSelectFore
                    };
                    dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = s.DgvHeader,
                        ForeColor = s.Text
                    };
                }
                break;
            case LinkLabel ll:
                if (IsTokenDocLink(ll))
                {
                    ApplyTokenDocLinkColors(ll, light, s);
                    break;
                }
                if (light)
                {
                    ll.BackColor = SystemColors.Control;
                    ll.ForeColor = SystemColors.ControlText;
                    ll.LinkColor = Color.Blue;
                    ll.ActiveLinkColor = Color.Red;
                    ll.VisitedLinkColor = Color.Purple;
                }
                else
                {
                    ll.BackColor = s.Form;
                    ll.ForeColor = s.Text;
                    ll.LinkColor = s.Link;
                    ll.ActiveLinkColor = s.Text;
                    ll.VisitedLinkColor = s.Link;
                }
                break;
            case GroupBox gb:
                if (light)
                {
                    gb.BackColor = SystemColors.Control;
                    gb.ForeColor = SystemColors.ControlText;
                    DetachGroupBoxFlatPaint(gb);
                }
                else
                {
                    gb.BackColor = s.Form;
                    gb.ForeColor = s.Text;
                    AttachGroupBoxFlatPaint(gb, s);
                }
                break;
            case Panel:
                if (light)
                {
                    c.BackColor = SystemColors.Control;
                    c.ForeColor = SystemColors.ControlText;
                }
                else
                {
                    c.BackColor = s.Form;
                    c.ForeColor = s.Text;
                }
                break;
            case Label lb:
                if (lb.BackColor == Color.Transparent)
                {
                    if (IsMutedLabel(lb))
                        lb.ForeColor = light ? SystemColors.GrayText : s.Muted;
                    else
                        lb.ForeColor = light ? SystemColors.ControlText : s.Text;
                }
                else
                {
                    if (light)
                    {
                        lb.ForeColor = IsMutedLabel(lb) ? SystemColors.GrayText : SystemColors.ControlText;
                        lb.BackColor = SystemColors.Control;
                    }
                    else
                    {
                        lb.ForeColor = IsMutedLabel(lb) ? s.Muted : s.Text;
                        lb.BackColor = s.Form;
                    }
                }
                break;
            case TextBox tb:
                if (isLog)
                {
                    if (light)
                    {
                        tb.BackColor = SystemColors.Window;
                        tb.ForeColor = SystemColors.WindowText;
                    }
                    else
                    {
                        tb.BackColor = s.LogBack;
                        tb.ForeColor = s.LogFore;
                    }
                }
                else
                {
                    if (light)
                    {
                        tb.BackColor = SystemColors.Window;
                        tb.ForeColor = SystemColors.WindowText;
                    }
                    else
                    {
                        tb.BackColor = s.Field;
                        tb.ForeColor = s.Text;
                    }
                }
                break;
            case ComboBox cb:
                if (light)
                {
                    cb.BackColor = SystemColors.Window;
                    cb.ForeColor = SystemColors.WindowText;
                }
                else
                {
                    cb.BackColor = s.Field;
                    cb.ForeColor = s.Text;
                }
                break;
            case NumericUpDown nud:
                if (light)
                {
                    nud.BackColor = SystemColors.Window;
                    nud.ForeColor = SystemColors.WindowText;
                }
                else
                {
                    nud.BackColor = s.Field;
                    nud.ForeColor = s.Text;
                }
                break;
            case CheckBox chk:
                if (light)
                {
                    chk.FlatStyle = FlatStyle.Standard;
                    chk.UseVisualStyleBackColor = true;
                    chk.BackColor = SystemColors.Control;
                    chk.ForeColor = SystemColors.ControlText;
                }
                else
                {
                    chk.BackColor = s.Form;
                    if (!chk.Enabled)
                    {
                        chk.UseVisualStyleBackColor = false;
                        chk.FlatStyle = FlatStyle.Flat;
                        chk.ForeColor = s.Muted;
                        chk.FlatAppearance.BorderSize = 0;
                        chk.FlatAppearance.MouseOverBackColor = s.Form;
                        chk.FlatAppearance.MouseDownBackColor = s.Form;
                        chk.FlatAppearance.CheckedBackColor = s.Form;
                    }
                    else
                    {
                        chk.FlatStyle = FlatStyle.Standard;
                        chk.UseVisualStyleBackColor = true;
                        chk.ForeColor = s.Text;
                    }
                }
                break;
            case RadioButton rb:
                if (light)
                {
                    rb.FlatStyle = FlatStyle.Standard;
                    rb.UseVisualStyleBackColor = true;
                    rb.BackColor = SystemColors.Control;
                    rb.ForeColor = SystemColors.ControlText;
                }
                else
                {
                    rb.BackColor = s.Form;
                    rb.ForeColor = rb.Enabled ? s.Text : s.Muted;
                    rb.FlatStyle = FlatStyle.Standard;
                    rb.UseVisualStyleBackColor = true;
                }
                break;
            case CheckedListBox clb:
                if (light)
                {
                    clb.BackColor = SystemColors.Window;
                    clb.ForeColor = SystemColors.WindowText;
                    clb.BorderStyle = BorderStyle.Fixed3D;
                }
                else
                {
                    clb.BackColor = s.Field;
                    clb.ForeColor = s.Text;
                    clb.BorderStyle = BorderStyle.FixedSingle;
                }
                break;
            case ListBox list:
                if (light)
                {
                    list.BackColor = SystemColors.Window;
                    list.ForeColor = SystemColors.WindowText;
                    list.BorderStyle = BorderStyle.Fixed3D;
                }
                else
                {
                    list.BackColor = s.Field;
                    list.ForeColor = s.Text;
                    list.BorderStyle = BorderStyle.FixedSingle;
                }
                break;
            case ToolStrip strip:
                StyleToolStrip(strip, theme);
                break;
            case Button b:
                if (light)
                {
                    b.BackColor = SystemColors.Control;
                    b.ForeColor = IsToolbarGlyphInactiveButton(b) ? SystemColors.GrayText : SystemColors.ControlText;
                    b.FlatStyle = FlatStyle.System;
                }
                else
                {
                    b.BackColor = s.Surface;
                    b.ForeColor = IsToolbarGlyphInactiveButton(b) ? s.Muted : s.Text;
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderColor = s.ButtonBorder;
                }
                break;
            case UserControl u:
                if (light)
                {
                    u.BackColor = SystemColors.Control;
                    u.ForeColor = SystemColors.ControlText;
                }
                else
                {
                    u.BackColor = s.Form;
                    u.ForeColor = s.Text;
                }
                break;
            case Form f:
                if (light)
                {
                    f.BackColor = SystemColors.Control;
                    f.ForeColor = SystemColors.ControlText;
                }
                else
                {
                    f.BackColor = s.Form;
                    f.ForeColor = s.Text;
                }
                break;
        }

        foreach (Control child in c.Controls)
        {
            bool childIsLog = isLog || (serverLogTextBox != null && ReferenceEquals(child, serverLogTextBox));
            ApplyRecurse(child, theme, serverLogTextBox, childIsLog);
        }
    }

    public static void SetMuted(Label label, bool muted)
    {
        if (label == null)
            return;
        label.Tag = muted ? MutedLabelTag : null;
    }

    private static bool IsMutedLabel(Control c)
    {
        if (c is Label lb)
        {
            if (ReferenceEquals(lb.Tag, MutedLabelTag) || (lb.Tag is string t && t == MutedLabelTag))
                return true;
        }

        return false;
    }

    private static bool IsTokenDocLink(Control c) =>
        c is LinkLabel ll && ll.Tag is string t && (t == "token" || t == "sublabel");

    private static void ApplyTokenDocLinkColors(LinkLabel ll, bool light, ThemeColors s)
    {
        ll.LinkBehavior = LinkBehavior.HoverUnderline;
        if (light)
        {
            ll.BackColor = SystemColors.Control;
            ll.ForeColor = SystemColors.ControlText;
            ll.LinkColor = Color.FromArgb(0, 102, 204);
        }
        else
        {
            ll.BackColor = s.Form;
            ll.ForeColor = s.Text;
            ll.LinkColor = s.Link;
        }
        ll.ActiveLinkColor = ll.LinkColor;
        ll.VisitedLinkColor = ll.LinkColor;
    }

    private const string GroupBoxFlatPaintTag = "uiGroupBoxFlatPainter";

    private static void AttachGroupBoxFlatPaint(GroupBox gb, ThemeColors s)
    {
        DetachGroupBoxFlatPaint(gb);
        var painter = new GroupBoxFlatPainter(gb, s);
        gb.Paint += painter.OnPaint;
        gb.Tag = painter;
        gb.Invalidate(true);
    }

    private static void DetachGroupBoxFlatPaint(GroupBox gb)
    {
        if (gb.Tag is GroupBoxFlatPainter existing)
        {
            gb.Paint -= existing.OnPaint;
            gb.Tag = null;
            gb.Invalidate(true);
        }
    }

    private sealed class GroupBoxFlatPainter
    {
        private readonly GroupBox _gb;
        private readonly Color _border;
        private readonly Color _back;
        private readonly Color _text;

        public GroupBoxFlatPainter(GroupBox gb, ThemeColors s)
        {
            _gb = gb;
            _border = s.ButtonBorder;
            _back = s.Form;
            _text = s.Text;
        }

        public void OnPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = _gb.ClientRectangle;
            using (var backBrush = new SolidBrush(_back))
                g.FillRectangle(backBrush, rect);

            string text = _gb.Text ?? string.Empty;
            var textSize = TextRenderer.MeasureText(g, text, _gb.Font, Size.Empty, TextFormatFlags.NoPadding);
            int textTop = textSize.Height / 2;
            var borderRect = new Rectangle(rect.X, rect.Y + textTop, rect.Width - 1, rect.Height - textTop - 1);

            using (var pen = new Pen(_border, 1))
                g.DrawRectangle(pen, borderRect);

            if (text.Length > 0)
            {
                int textLeft = rect.X + 8;
                var textRect = new Rectangle(textLeft, rect.Y, textSize.Width + 6, textSize.Height);
                using (var backBrush = new SolidBrush(_back))
                    g.FillRectangle(backBrush, textRect);
                TextRenderer.DrawText(g, text, _gb.Font, new Point(textLeft + 3, rect.Y), _text, TextFormatFlags.NoPadding);
            }
        }
    }

    private sealed class UiToolStripRenderer : ToolStripProfessionalRenderer
    {
        public UiToolStripRenderer(ThemeColors colors, bool menuLike)
            : base(new UiToolStripColorTable(colors, menuLike))
        {
        }
    }

    private sealed class UiToolStripColorTable : ProfessionalColorTable
    {
        private readonly ThemeColors _c;
        private readonly bool _menuLike;

        public UiToolStripColorTable(ThemeColors colors, bool menuLike)
        {
            _c = colors;
            _menuLike = menuLike;
        }

        private Color BarBack => _menuLike ? _c.MenuBack : _c.Surface;

        public override Color ToolStripGradientBegin => BarBack;
        public override Color ToolStripGradientMiddle => BarBack;
        public override Color ToolStripGradientEnd => BarBack;
        public override Color MenuStripGradientBegin => _c.MenuBack;
        public override Color MenuStripGradientEnd => _c.MenuBack;
        public override Color StatusStripGradientBegin => _c.MenuBack;
        public override Color StatusStripGradientEnd => _c.MenuBack;
        public override Color MenuBorder => _c.ButtonBorder;
        public override Color MenuItemBorder => _c.ButtonBorder;
        public override Color MenuItemSelected => _c.DgvSelect;
        public override Color MenuItemSelectedGradientBegin => _c.DgvSelect;
        public override Color MenuItemSelectedGradientEnd => _c.DgvSelect;
        public override Color MenuItemPressedGradientBegin => _c.Field;
        public override Color MenuItemPressedGradientEnd => _c.Field;
        public override Color ImageMarginGradientBegin => _c.MenuBack;
        public override Color ImageMarginGradientMiddle => _c.MenuBack;
        public override Color ImageMarginGradientEnd => _c.MenuBack;
        public override Color ToolStripBorder => _c.ButtonBorder;
        public override Color SeparatorDark => _c.ButtonBorder;
        public override Color SeparatorLight => _c.Field;
        public override Color OverflowButtonGradientBegin => _c.Surface;
        public override Color OverflowButtonGradientMiddle => _c.Surface;
        public override Color OverflowButtonGradientEnd => _c.Surface;
    }

    #region DWM

    private static void SetImmersiveDarkMode(Form form, UiThemeKind theme)
    {
        if (form == null || !form.IsHandleCreated)
            return;

        IntPtr hwnd = form.Handle;
        try
        {
            SetDwmFrameForTheme(hwnd, theme);
            if (!form.IsDisposed)
            {
                form.BeginInvoke(
                    (Action)(() =>
                    {
                        if (form.IsDisposed || !form.IsHandleCreated)
                            return;
                        SetDwmFrameForTheme(form.Handle, theme);
                    }));
            }
        }
        catch
        {
        }
    }

    private static void SetDwmFrameForTheme(IntPtr hwnd, UiThemeKind theme)
    {
        if (hwnd == IntPtr.Zero)
            return;

        int d = DwmColorDefault;
        if (UsesDarkWindowFrame(theme))
        {
            int on = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY, ref on, sizeof(int));
            ThemeColors s = GetThemeColors(theme);
            int caption = ColorToDwmColorRef(s.Form);
            int border = ColorToDwmColorRef(s.Form);
            int text = ColorToDwmColorRef(s.Text);
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref border, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref text, sizeof(int));
        }
        else
        {
            int on = 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY, ref on, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref d, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref d, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref d, sizeof(int));
        }

        RefreshWindowFrame(hwnd);
    }

    private static int ColorToDwmColorRef(Color c)
    {
        return c.R | (c.G << 8) | (c.B << 16);
    }

    private static void RefreshWindowFrame(IntPtr hwnd)
    {
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_FRAMECHANGED = 0x0020;
        SetWindowPos(
            hwnd,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attr,
        ref int attrValue,
        int attrSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    #endregion
}
