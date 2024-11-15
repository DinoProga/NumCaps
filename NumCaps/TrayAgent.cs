/*
 * REM DinoProga!!!
 *
 * (c) COPYRIGHT, B. Samchuk (DinoProga), 2023-2024.
 */

using NumCaps.Properties;

using System.ComponentModel;
using System.Text;
using System.Globalization;

namespace NumCaps;

internal class TrayAgent : Form
{
    #region Private Fields

    private const string capsTitle = "Capslock";

    private const string enKeys = "`qwertyuiop[]asdfghjkl;'zxcvbnm,./~@#$%^&QWERTYUIOP{}|ASDFGHJKL:\"ZXCVBNM<>?";

    private const string numTitle = "Numlock";

    private const string uaKeys = "'йцукенгшщзхїфівапролджєячсмитьбю.₴\"№;%:?ЙЦУКЕНГШЩЗХЇ/ФІВАПРОЛДЖЄЯЧСМИТЬБЮ,";

    private const int WideLim = 40;

    private static readonly string enExclusive = new(enKeys.Except(uaKeys).ToArray());

    private static readonly string uaExclusive = new(uaKeys.Except(enKeys).ToArray());

    private readonly NotifyIcon capsIcon = new();

    private readonly ToolStripMenuItem indicateMenu = new() { Text = "Indicate" };

    private readonly ContextMenuStrip menu = new();

    private readonly NotifyIcon numIcon = new();

    private readonly ToolStripMenuItem recapMenu = new() { Text = "cAPSLOCK => Capslock" };

    private readonly ToolStripMenuItem relayoutMenu = new() { Text = "Дфнщге => Layout" };

    private readonly System.Windows.Forms.Timer updateTimer = new();

    private bool capsState = false;

    private ShowFlags flags = ShowFlags.Num | ShowFlags.Caps;

    private bool numState = false;

    #endregion Private Fields

    #region Public Constructors

    public TrayAgent()
    {
        menu.Opening += Menu_Opening;
        _ = menu.Items.Add(BuildAboutMenu());
        _ = menu.Items.Add(InitializeIndicateMenu());
        _ = menu.Items.Add(new ToolStripSeparator());
        _ = menu.Items.Add(InitializeMenu(recapMenu));
        _ = menu.Items.Add(InitializeMenu(relayoutMenu));
        _ = menu.Items.Add(new ToolStripSeparator());
        _ = menu.Items.Add(BuildExitMenu());
        InitializeIcon(ref capsState, capsIcon, capsTitle, Resources.CapsOn, Resources.CapsOff, flags.HasFlag(ShowFlags.Caps));
        InitializeIcon(ref numState, numIcon, numTitle, Resources.NumOn, Resources.NumOff, flags.HasFlag(ShowFlags.Num));
        Start();
    }

    #endregion Public Constructors

    #region Private Enums

    private enum ShowFlags

    {
        None = 0,

        Num = 1 << 0,

        Caps = 1 << 1, Both = Num | Caps
    }

    #endregion Private Enums

    #region Protected Methods

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            capsIcon?.Dispose();
            numIcon?.Dispose();
            menu?.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        numIcon.Visible = false;
        capsIcon.Visible = false;
        base.OnFormClosed(e);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Visible = false;
    }

    #endregion Protected Methods

    #region Private Methods

    private static ToolStripMenuItem BuildItem(string? prefix, string value)
    {
        return new ToolStripMenuItem() { Text = $"{prefix}: {TrimLong(value)}", Tag = value };
    }

    private static void Complette(ToolStripMenuItem menu, string raw, string empty)
    {
        menu.Enabled = menu.DropDownItems.Count > 0;
        if (!menu.Enabled)
        {
            menu.ToolTipText = empty;
        }
        else
        {
            if (menu.DropDownItems.Count == 1)
            {
                menu.DropDownItems[0].Text = TrimLong((string?)menu.DropDownItems[0].Tag);
            }
            menu.DropDownItems.Insert(0, new ToolStripSeparator());
            menu.DropDownItems.Insert(0, new ToolStripMenuItem() { Text = TrimLong(raw), Enabled = false });
        }
    }

    private static string? GetRelayout(string raw, string inKeys, string outKeys, string neverOccurs)
    {
        if (raw.Any(neverOccurs.Contains))
        {
            return null;
        }
        StringBuilder Builder = new();
        foreach (char ch in raw)
        {
            int Index = inKeys.IndexOf(ch);
            _ = Builder.Append(Index < 0 ? ch : outKeys[Index]);
        }
        string RelayoutString = Builder.ToString();
        return RelayoutString == raw ? null : RelayoutString;
    }

    private static string OnOff(bool state) => state ? "On" : "Off";

    private static void Prepare(ToolStripMenuItem menu)
    {
        menu.Enabled = Clipboard.ContainsText(TextDataFormat.Text) || Clipboard.ContainsText(TextDataFormat.UnicodeText);
        menu.ToolTipText = menu.Enabled ? string.Empty : "No suitable data find in the clipboard";
        menu.DropDownItems.Clear();
    }

    private static string TrimLong(string? s) => s?.Length <= WideLim ? s ?? string.Empty : $"{s?[..WideLim]}…";

    private static void Update(ref bool flag, NotifyIcon icon, bool value, string title, Icon on, Icon off)
    {
        flag = value;
        icon.Icon = flag ? on : off;
        icon.Text = $"{title}: {OnOff(flag)}";
    }

    private void About_Click(object? sender, EventArgs e)
    {
        _ = MessageBox.Show(Resources.About, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private ToolStripMenuItem BuildAboutMenu()
    {
        ToolStripMenuItem Menu = new() { Text = "About" };
        Menu.Click += About_Click;
        return Menu;
    }

    private ToolStripMenuItem BuildExitMenu()
    {
        ToolStripMenuItem Menu = new() { Text = "Exit" };
        Menu.Click += MenuExit_Click;
        return Menu;
    }

    private void DropDownItemClicked(object? sender, ToolStripItemClickedEventArgs e)
    {
        if (e.ClickedItem?.Tag is string Recap)
        {
            Clipboard.SetText(Recap);
        }
    }

    private string? GetRecap(string raw, InputLanguage? language)
    {
        if (language == null)
        {
            return null;
        }
        StringBuilder Builder = new();
        TextInfo Info = language.Culture.TextInfo;
        foreach (char ch in raw)
        {
            if (char.IsUpper(ch))
            {
                _ = Builder.Append(Info.ToLower(ch));
            }
            else
            {
                _ = char.IsLower(ch) ? Builder.Append(Info.ToUpper(ch)) : Builder.Append(ch);
            }
        }
        string RecapString = Builder.ToString();
        foreach (ToolStripMenuItem? c in recapMenu.DropDownItems)
        {
            if (c?.Tag is string s && s == RecapString)
            {
                return null;
            }
        }
        return RecapString == raw ? null : RecapString;
    }

    private void Indicate_DropDownItemClicked(object? sender, ToolStripItemClickedEventArgs e)
    {
        if (e.ClickedItem == null)
        {
            return;
        }
        foreach (ToolStripMenuItem i in indicateMenu.DropDownItems)
        {
            i.Checked = e.ClickedItem.Equals(i);
            if (i.Checked && i.Tag is ShowFlags NewFlags && NewFlags != flags)
            {
                flags = NewFlags;
                numIcon.Visible = flags.HasFlag(ShowFlags.Num);
                capsIcon.Visible = flags.HasFlag(ShowFlags.Caps);
                Settings.Default.ShowFlags = (int)flags;
                Settings.Default.Save();
            }
        }
    }

    private void InitializeIcon(ref bool state, NotifyIcon icon, string title, Icon on, Icon off, bool visible)
    {
        Update(ref state, icon, IsKeyLocked(Keys.NumLock), title, on, off);
        icon.ContextMenuStrip = menu;
        icon.MouseClick += OnMouseClick;
        icon.Visible = visible;
    }

    private ToolStripMenuItem InitializeIndicateMenu()
    {
        int IntFlags = Settings.Default.ShowFlags;
        flags = Enum.IsDefined(typeof(ShowFlags), IntFlags) ? (ShowFlags)IntFlags : ShowFlags.Both;
        if (flags == ShowFlags.None)
        {
            flags = ShowFlags.Both;
        }
        _ = indicateMenu.DropDownItems.Add(new ToolStripMenuItem()
        {
            Text = numTitle, Tag = ShowFlags.Num, Checked = flags == ShowFlags.Num 
        });
        _ = indicateMenu.DropDownItems.Add(new ToolStripMenuItem()
        {
            Text = capsTitle, Tag = ShowFlags.Caps, Checked = flags == ShowFlags.Caps 
        });
        _ = indicateMenu.DropDownItems.Add(new ToolStripMenuItem()
        {
            Text = $"{numTitle} && {capsTitle}", Tag = ShowFlags.Both, Checked = flags == ShowFlags.Both
        });
        indicateMenu.DropDownItemClicked += Indicate_DropDownItemClicked;
        return indicateMenu;
    }

    private ToolStripMenuItem InitializeMenu(ToolStripMenuItem menu)
    {
        menu.DropDownItemClicked += DropDownItemClicked;
        return menu;
    }

    private void Menu_Opening(object? sender, CancelEventArgs e)
    {
        Prepare(recapMenu);
        Prepare(relayoutMenu);
        if (recapMenu.Enabled)
        {
            string RawString = Clipboard.GetText();
            foreach (InputLanguage? l in InputLanguage.InstalledInputLanguages) // Recap
            {
                if (GetRecap(RawString, l) is string RecapString)
                {
                    _ = recapMenu.DropDownItems.Add(BuildItem(l?.LayoutName, RecapString));
                }
            }
            Complette(recapMenu, RawString, "No letters find in the clipboard");
            if (GetRelayout(RawString, uaKeys, enKeys, enExclusive) is string En)
            {
                _ = relayoutMenu.DropDownItems.Add(BuildItem("Ua => En", En));
            }
            if (GetRelayout(RawString, enKeys, uaKeys, uaExclusive) is string Ua)
            {
                _ = relayoutMenu.DropDownItems.Add(BuildItem("En => Ua", Ua));
            }
            Complette(relayoutMenu, RawString, "No layout-dependent characters find in the clipboard");
        }
    }

    private void MenuExit_Click(object? sender, EventArgs e) => Close();

    private void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }
        if (sender is NotifyIcon Notify)
        {
            Notify.ShowBalloonTip(5000,
                "Current status",
                $"{numTitle}: {OnOff(numState)}\n{capsTitle}: {OnOff(capsState)}\n\nFor more features use the context menu.",
                ToolTipIcon.Info);
        }
    }

    private void Start()
    {
        updateTimer.Interval = 100;
        updateTimer.Tick += UpdateTimer_Tick;
        updateTimer.Enabled = true;
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (numState != IsKeyLocked(Keys.NumLock))
        {
            Update(ref numState, numIcon, !numState, numTitle, Resources.NumOn, Resources.NumOff);
        }
        if (capsState != IsKeyLocked(Keys.CapsLock))
        {
            Update(ref capsState, capsIcon, !capsState, capsTitle, Resources.CapsOn, Resources.CapsOff);
        }
    }

    #endregion Private Methods
}