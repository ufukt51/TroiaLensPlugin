using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TroiaLens.Npp;

namespace TroiaLens
{
    /// <summary>Stored in plugins\config\TroiaLens.ini.</summary>
    internal sealed class Settings
    {
        private const string Section = "TroiaLens";
        private readonly string _file;

        /// <summary>Open the panel automatically when a file with one of <see cref="Extensions"/> becomes active.</summary>
        public bool AutoOpen { get; set; } = true;

        /// <summary>Re-analyse on every tab switch while the panel is open, not only for trace extensions.</summary>
        public bool AnalyzeOnTabSwitch { get; set; } = true;

        public string[] Extensions { get; set; } = { ".trace" };

        /// <summary>Allows F12 developer tools in the panel.</summary>
        public bool DevTools { get; set; }

        /// <summary>Statement detail slides over from the right instead of docking under the views.</summary>
        public bool DetailSlideOver { get; set; }

        /// <summary>Tint the selected row's method and the row itself in the editor; off, only the margin bar and word boxes show.</summary>
        public bool RowHighlight { get; set; } = true;

        /// <summary>How the call tree shows a row's parent call: off, sticky, guides or jump.</summary>
        public string ParentView { get; set; } = "sticky";

        public static readonly string[] ParentViews = { "off", "sticky", "guides", "jump" };

        /// <summary>Colour the text of trace documents in the editor.</summary>
        public bool SyntaxColors { get; set; } = true;

        /// <summary>Look for a new release on GitHub once a day.</summary>
        public bool CheckUpdates { get; set; } = true;

        public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;

        /// <summary>Release the user chose to skip; it is not offered again unless asked for from the menu.</summary>
        public string SkippedVersion { get; set; } = "";

        public Settings(string configDir)
        {
            _file = Path.Combine(configDir, "TroiaLens.ini");
        }

        public void Load()
        {
            AutoOpen = Read("AutoOpen", AutoOpen);
            AnalyzeOnTabSwitch = Read("AnalyzeOnTabSwitch", AnalyzeOnTabSwitch);
            DevTools = Read("DevTools", DevTools);
            DetailSlideOver = Read("DetailSlideOver", DetailSlideOver);
            RowHighlight = Read("RowHighlight", RowHighlight);
            string view = Read("ParentView", ParentView).Trim().ToLowerInvariant();
            if (ParentViews.Contains(view)) ParentView = view;
            SyntaxColors = Read("SyntaxColors", SyntaxColors);
            CheckUpdates = Read("CheckUpdates", CheckUpdates);
            SkippedVersion = Read("SkippedVersion", SkippedVersion).Trim();
            if (DateTime.TryParse(Read("LastUpdateCheck", ""), CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var last))
                LastUpdateCheck = last;
            string ext = Read("Extensions", string.Join(";", Extensions));
            Extensions = ext.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.StartsWith(".") ? e : "." + e)
                .ToArray();
            // First run: the file shows what can be set. An older file gets the newer switches, on
            if (!File.Exists(_file)) Save();
            else
            {
                WriteIfMissing("CheckUpdates", CheckUpdates ? "1" : "0");
                WriteIfMissing("RowHighlight", RowHighlight ? "1" : "0");
                WriteIfMissing("ParentView", ParentView);
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_file));
                Write("AutoOpen", AutoOpen ? "1" : "0");
                Write("AnalyzeOnTabSwitch", AnalyzeOnTabSwitch ? "1" : "0");
                Write("DetailSlideOver", DetailSlideOver ? "1" : "0");
                Write("RowHighlight", RowHighlight ? "1" : "0");
                Write("ParentView", ParentView);
                Write("SyntaxColors", SyntaxColors ? "1" : "0");
                Write("CheckUpdates", CheckUpdates ? "1" : "0");
                Write("SkippedVersion", SkippedVersion);
                if (LastUpdateCheck > DateTime.MinValue)
                    Write("LastUpdateCheck", LastUpdateCheck.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
                // No menu item changes these; the file only gets them once, so an edit made while Notepad++ runs
                // (often in Notepad++ itself) is not written over
                WriteIfMissing("Extensions", string.Join(";", Extensions));
                WriteIfMissing("DevTools", DevTools ? "1" : "0");
            }
            catch (Exception)
            {
                // A read-only config folder must not break Notepad++
            }
        }

        public bool IsTraceExtension(string path) =>
            !string.IsNullOrEmpty(path) && Extensions.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase));

        private string Read(string key, string def)
        {
            var sb = new StringBuilder(1024);
            Win32.GetPrivateProfileString(Section, key, def, sb, sb.Capacity, _file);
            return sb.ToString();
        }

        private bool Read(string key, bool def) => Read(key, def ? "1" : "0").Trim() == "1";

        private void Write(string key, string value) => Win32.WritePrivateProfileString(Section, key, value, _file);

        private void WriteIfMissing(string key, string value)
        {
            const string none = "\u0001";
            if (Read(key, none) == none) Write(key, value);
        }
    }
}
