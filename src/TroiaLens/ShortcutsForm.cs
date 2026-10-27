using System;
using System.Drawing;
using System.Windows.Forms;
using TroiaLens.Npp;

namespace TroiaLens
{
    /// <summary>
    /// Lists TroiaLens commands with their shortcuts. Notepad++ owns plugin shortcuts (shortcuts.xml) and only its
    /// Shortcut Mapper can change them while it runs, so "Değiştir" opens the mapper at these commands.
    /// </summary>
    internal sealed class ShortcutsForm : Form
    {
        private readonly ListView _list;

        public ShortcutsForm()
        {
            Text = "TroiaLens klavye kısayolları";
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(560, 440);
            MinimumSize = new Size(420, 320);
            ShowIcon = false;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 4 };
            // The column keeps the window's width, so long labels wrap instead of widening it
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label
            {
                // Anchored to both sides, an auto-sized label wraps at the column's width
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 0, 0, 8),
                Text = "TroiaLens komutlarının kısayolları. Notepad++ bunları kendi ayar dosyasında (shortcuts.xml) saklar.",
            });

            _list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                HideSelection = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
            };
            _list.Columns.Add("Komut", 360);
            _list.Columns.Add("Kısayol", 150);
            _list.DoubleClick += (s, e) => Change();
            _list.Resize += (s, e) => FitColumns();
            layout.Controls.Add(_list);

            layout.Controls.Add(new Label
            {
                // Anchored to both sides, an auto-sized label wraps at the column's width
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 8, 0, 8),
                Text = "Değiştir, Notepad++'ın Kısayol Düzenleyicisi'ni bu komutla açar. Orada satıra çift tıklayıp yeni tuşu seçin; " +
                       "değişiklik hemen geçerli olur ve çakışan kısayollar kırmızı görünür.",
            });

            var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Margin = Padding.Empty };
            var close = new Button { Text = "Kapat", AutoSize = true, DialogResult = DialogResult.Cancel };
            close.Click += (s, e) => Close();
            var change = new Button { Text = "Değiştir…", AutoSize = true };
            change.Click += (s, e) => Change();
            buttons.Controls.Add(close);
            buttons.Controls.Add(change);
            layout.Controls.Add(buttons);

            Controls.Add(layout);
            AcceptButton = change;
            CancelButton = close;
            Fill();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (Plugin.IsDarkMode()) ApplyDarkMode();
            FitColumns();
            if (_list.Items.Count > 0)
            {
                _list.Items[0].Selected = true;
                _list.Focus();
            }
        }

        /// <summary>
        /// Notepad++'s dark colours. Its own theming (NPPM_DARKMODESUBCLASSANDTHEME) knows only Win32 control classes,
        /// not WinForms ones, so the colours are set here.
        /// </summary>
        private void ApplyDarkMode()
        {
            // NppDarkMode::Colors: background, softerBackground, hotBackground, pureBackground, errorBackground, text,
            // darkerText, disabledText, linkText, edge, hotEdge, disabledEdge (COLORREF)
            var c = new int[12];
            bool got = Win32.SendMessage(Plugin.Npp.NppHandle, NppMsg.NPPM_GETDARKMODECOLORS, (IntPtr)(c.Length * 4), c) != IntPtr.Zero;
            Color Ref(int i, Color fallback) => got ? ColorTranslator.FromWin32(c[i]) : fallback;
            Color back = Ref(0, Color.FromArgb(32, 32, 32)), softer = Ref(1, Color.FromArgb(43, 43, 43)), hot = Ref(2, Color.FromArgb(69, 69, 69));
            Color text = Ref(5, Color.FromArgb(224, 224, 224)), edge = Ref(9, Color.FromArgb(100, 100, 100));

            BackColor = back;
            ForeColor = text;
            _list.BackColor = softer;
            _list.ForeColor = text;
            foreach (Control control in Controls[0].Controls)
                if (control is FlowLayoutPanel buttons)
                    foreach (Button b in buttons.Controls)
                    {
                        b.FlatStyle = FlatStyle.Flat;
                        b.BackColor = softer;
                        b.ForeColor = text;
                        b.FlatAppearance.BorderColor = edge;
                        b.FlatAppearance.MouseOverBackColor = hot;
                    }
            // Dark title bar, scrollbars and column header (Windows 10 1809 and later; ignored before)
            int on = 1;
            Win32.DwmSetWindowAttribute(Handle, Win32.DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int));
            Win32.SetWindowTheme(_list.Handle, "DarkMode_Explorer", null);
            Win32.SetWindowTheme(Win32.SendMessage(_list.Handle, Win32.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero), "DarkMode_ItemsView", null);
        }

        private void Fill()
        {
            int selected = _list.SelectedIndices.Count > 0 ? _list.SelectedIndices[0] : 0;
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var (name, shortcut) in Plugin.ShortcutList())
                _list.Items.Add(new ListViewItem(new[] { name, shortcut == "" ? "—" : shortcut }) { Tag = name });
            if (_list.Items.Count > 0) _list.Items[Math.Min(selected, _list.Items.Count - 1)].Selected = true;
            _list.EndUpdate();
        }

        private void FitColumns()
        {
            if (_list.Columns.Count < 2) return;
            int shortcut = _list.Columns[1].Width;
            _list.Columns[0].Width = Math.Max(160, _list.ClientSize.Width - shortcut - 4);
        }

        /// <summary>Opens the Shortcut Mapper at the selected command and shows the shortcuts again once it closes.</summary>
        private void Change()
        {
            string name = _list.SelectedItems.Count > 0 ? (string)_list.SelectedItems[0].Tag : "";
            Plugin.OpenShortcutMapper(name);
            Fill();
            Activate();
        }
    }
}
