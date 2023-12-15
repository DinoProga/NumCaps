/*
 * REM DinoProga!!!
 *
 * (c) COPYRIGHT, B. Samchuk (DinoProga), 2023.
 */

using NumCaps.Properties;
using System.ComponentModel;
using System.Text;

namespace NumCaps;

using System;
using System.Globalization;

internal class TrayAgent : Form
{
	#region Private Fields

	private const string CapsTitle = "Capslock";

	private const string EnKeys = "`qwertyuiop[]asdfghjkl;'zxcvbnm,./~@#$%^&QWERTYUIOP{}|ASDFGHJKL:\"ZXCVBNM<>?";

	private const string NumTitle = "Numlock";

	private const string UaKeys = "'йцукенгшщзхїфівапролджєячсмитьбю.₴\"№;%:?ЙЦУКЕНГШЩЗХЇ/ФІВАПРОЛДЖЄЯЧСМИТЬБЮ,";

	private const int WideLim = 40;

	private static readonly string EnExclusive = new(EnKeys.Except(UaKeys).ToArray());

	private static readonly string UaExclusive = new(UaKeys.Except(EnKeys).ToArray());

	private readonly NotifyIcon CapsIcon = new();

	private readonly ToolStripMenuItem IndicateMenu = new() { Text = "Indicate" };

	private readonly ContextMenuStrip Menu = new();

	private readonly NotifyIcon NumIcon = new();

	private readonly ToolStripMenuItem RecapMenu = new() { Text = "cAPSLOCK => Capslock" };

	private readonly ToolStripMenuItem RelayoutMenu = new() { Text = "Дфнщге => Layout" };

	private readonly System.Windows.Forms.Timer UpdateTimer = new();

	private bool CapsState = false;

	private ShowFlags Flags = ShowFlags.Num | ShowFlags.Caps;

	private bool NumState = false;

	#endregion Private Fields

	#region Public Constructors

	public TrayAgent()
	{
		Menu.Opening += Menu_Opening;
		Menu.Items.Add(BuildAboutMenu());
		Menu.Items.Add(InitializeIndicateMenu());
		Menu.Items.Add(new ToolStripSeparator());
		Menu.Items.Add(InitializeMenu(RecapMenu));
		Menu.Items.Add(InitializeMenu(RelayoutMenu));
		Menu.Items.Add(new ToolStripSeparator());
		Menu.Items.Add(BuildExitMenu());
		InitializeIcon(ref CapsState, CapsIcon, CapsTitle, Resources.CapsOn, Resources.CapsOff, Flags.HasFlag(ShowFlags.Caps));
		InitializeIcon(ref NumState, NumIcon, NumTitle, Resources.NumOn, Resources.NumOff, Flags.HasFlag(ShowFlags.Num));
		Start();
	}

	#endregion Public Constructors

	#region Private Enums

	private enum ShowFlags

	{ None = 0, Num = 1, Caps = 2, Both = Num | Caps }

	#endregion Private Enums

	#region Protected Methods

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		NumIcon.Visible = false;
		CapsIcon.Visible = false;
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
	{ return new ToolStripMenuItem() { Text = $"{prefix}: {TrimLong(value)}", Tag = value }; }

	private static void Complette(ToolStripMenuItem menu, string raw, string empty)
	{
		menu.Enabled = menu.DropDownItems.Count > 0;
		if (!menu.Enabled)
		{ menu.ToolTipText = empty; }
		else
		{
			if (menu.DropDownItems.Count == 1) menu.DropDownItems[0].Text = TrimLong((string?)menu.DropDownItems[0].Tag);
			menu.DropDownItems.Insert(0, new ToolStripSeparator());
			menu.DropDownItems.Insert(0, new ToolStripMenuItem() { Text = TrimLong(raw), Enabled = false });
		}
	}

	private static string? GetRelayout(string raw, string inKeys, string outKeys, string neverOccurs)
	{
		if (raw.Any(neverOccurs.Contains)) return null;
		StringBuilder Builder = new();
		foreach (char ch in raw)
		{
			int Index = inKeys.IndexOf(ch);
			Builder.Append(Index < 0 ? ch : outKeys[Index]);
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
	{ MessageBox.Show(Resources.About, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information); }

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
	{ if (e.ClickedItem?.Tag is string Recap) Clipboard.SetText(Recap); }

	private string? GetRecap(string raw, InputLanguage? language)
	{
		if (language == null) return null;
		StringBuilder Builder = new();
		TextInfo Info = language.Culture.TextInfo;
		foreach (char ch in raw)
		{
			if (char.IsUpper(ch)) Builder.Append(Info.ToLower(ch));
			else if (char.IsLower(ch)) Builder.Append(Info.ToUpper(ch));
			else Builder.Append(ch);
		}
		string RecapString = Builder.ToString();
		foreach (ToolStripMenuItem? c in RecapMenu.DropDownItems)
		{ if (c?.Tag is string s && s == RecapString) return null; }
		return RecapString == raw ? null : RecapString;
	}

	private void Indicate_DropDownItemClicked(object? sender, ToolStripItemClickedEventArgs e)
	{
		if (e.ClickedItem == null) return;
		foreach (ToolStripMenuItem i in IndicateMenu.DropDownItems)
		{
			i.Checked = e.ClickedItem.Equals(i);
			if (i.Checked && i.Tag is ShowFlags NewFlags && NewFlags != Flags)
			{
				Flags = NewFlags;
				NumIcon.Visible = Flags.HasFlag(ShowFlags.Num);
				CapsIcon.Visible = Flags.HasFlag(ShowFlags.Caps);
				Settings.Default.ShowFlags = (int)Flags;
				Settings.Default.Save();
			}
		}
	}

	private void InitializeIcon(ref bool state, NotifyIcon icon, string title, Icon on, Icon off, bool visible)
	{
		Update(ref state, icon, IsKeyLocked(Keys.NumLock), title, on, off);
		icon.ContextMenuStrip = Menu;
		icon.MouseClick += OnMouseClick;
		icon.Visible = visible;
	}

	private ToolStripMenuItem InitializeIndicateMenu()
	{
		int IntFlags = Settings.Default.ShowFlags;
		Flags = Enum.IsDefined(typeof(ShowFlags), IntFlags) ? (ShowFlags)IntFlags : ShowFlags.Both;
		if (Flags == ShowFlags.None) Flags = ShowFlags.Both;
		IndicateMenu.DropDownItems.Add(new ToolStripMenuItem()
		{ Text = NumTitle, Tag = ShowFlags.Num, Checked = Flags == ShowFlags.Num });
		IndicateMenu.DropDownItems.Add(new ToolStripMenuItem()
		{ Text = CapsTitle, Tag = ShowFlags.Caps, Checked = Flags == ShowFlags.Caps });
		IndicateMenu.DropDownItems.Add(new ToolStripMenuItem()
		{ Text = $"{NumTitle} && {CapsTitle}", Tag = ShowFlags.Both, Checked = Flags == ShowFlags.Both });
		IndicateMenu.DropDownItemClicked += Indicate_DropDownItemClicked;
		return IndicateMenu;
	}

	private ToolStripMenuItem InitializeMenu(ToolStripMenuItem menu)
	{
		menu.DropDownItemClicked += DropDownItemClicked;
		return menu;
	}

	private void Menu_Opening(object? sender, CancelEventArgs e)
	{
		Prepare(RecapMenu);
		Prepare(RelayoutMenu);
		if (RecapMenu.Enabled)
		{
			string RawString = Clipboard.GetText();
			foreach (InputLanguage? l in InputLanguage.InstalledInputLanguages) // Recap
			{
				if (GetRecap(RawString, l) is string RecapString)
				{ RecapMenu.DropDownItems.Add(BuildItem(l?.LayoutName, RecapString)); }
			}
			Complette(RecapMenu, RawString, "No letters find in the clipboard");
			if (GetRelayout(RawString, UaKeys, EnKeys, EnExclusive) is string En)
			{ RelayoutMenu.DropDownItems.Add(BuildItem("Ua => En", En)); }
			if (GetRelayout(RawString, EnKeys, UaKeys, UaExclusive) is string Ua)
			{ RelayoutMenu.DropDownItems.Add(BuildItem("En => Ua", Ua)); }
			Complette(RelayoutMenu, RawString, "No layout-dependent characters find in the clipboard");
		}
	}

	private void MenuExit_Click(object? sender, EventArgs e) => Close();

	private void OnMouseClick(object? sender, MouseEventArgs e)
	{
		if (e.Button != MouseButtons.Left) return;
		if (sender is NotifyIcon Notify)
		{
			Notify.ShowBalloonTip(5000,
				"Current status",
				$"{NumTitle}: {OnOff(NumState)}\n{CapsTitle}: {OnOff(CapsState)}\n\nFor more features use the context menu.",
				ToolTipIcon.Info);
		}
	}

	private void Start()
	{
		UpdateTimer.Interval = 100;
		UpdateTimer.Tick += UpdateTimer_Tick;
		UpdateTimer.Enabled = true;
	}

	private void UpdateTimer_Tick(object? sender, EventArgs e)
	{
		if (NumState != IsKeyLocked(Keys.NumLock))
		{ Update(ref NumState, NumIcon, !NumState, NumTitle, Resources.NumOn, Resources.NumOff); }
		if (CapsState != IsKeyLocked(Keys.CapsLock))
		{ Update(ref CapsState, CapsIcon, !CapsState, CapsTitle, Resources.CapsOn, Resources.CapsOff); }
	}

	#endregion Private Methods
}