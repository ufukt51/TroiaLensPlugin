using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using TroiaLens.Npp;

namespace TroiaLens
{
    /// <summary>
    /// Dockable panel hosting the analysis UI (web\index.html) in WebView2.
    /// Page → host: ready, gotoLine, mark, clearMarks, reload, setPrefs, need, saveCsv. Host → page: theme, prefs, show, loadUrl, notTrace,
    /// caret, forget.
    /// </summary>
    internal sealed class PanelForm : Form
    {
        private const string AppHost = "troialens.local";
        private const string DataHost = "troialens-data.local";

        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        private readonly WebView2 _web;
        private readonly string _webDir;
        private readonly string _loaderDir;
        private readonly string _userDataDir;
        private readonly bool _devTools;
        private bool _dark;
        private bool _slideOver;
        private bool _rowHighlight;
        private string _parentView;
        private bool _pageReady;

        // Last document message; sent again whenever the page (re)loads
        private string _state;
        // Trace file the page streams from https://troialens-data.local/<name>?v=<token>, served by OnDataRequested:
        // the saved document itself, or a temporary copy (deleted when the panel moves on). Nothing of it stays in memory.
        private string _dataPath;
        private string _dataToken;
        private bool _dataTemp;
        // Open while WebView2 reads it; closed when the panel moves on, so a temporary copy can be deleted
        private Stream _dataStream;

        public PanelForm(string webDir, string loaderDir, string userDataDir, bool dark, bool devTools, bool slideOver, bool rowHighlight, string parentView)
        {
            _webDir = webDir;
            _loaderDir = loaderDir;
            _userDataDir = userDataDir;
            _devTools = devTools;
            _dark = dark;
            _slideOver = slideOver;
            _rowHighlight = rowHighlight;
            _parentView = parentView;

            Text = Plugin.Name;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = Background(dark);

            _web = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Background(dark) };
            // Keys with Ctrl or Alt arrive here while the page has focus; TroiaLens shortcuts run, the rest go on to the page
            _web.KeyDown += (s, e) =>
            {
                if (Plugin.RunShortcut(e.KeyData)) e.Handled = true;
            };
            Controls.Add(_web);
        }

        public async void Start()
        {
            try
            {
                if (!File.Exists(Path.Combine(_webDir, "index.html")))
                    throw new FileNotFoundException("Arayüz dosyaları bulunamadı: " + _webDir);

                try
                {
                    CoreWebView2Environment.SetLoaderDllFolderPath(_loaderDir);
                }
                catch (Exception)
                {
                    // Allowed once per process; another panel instance may already have set it
                }

                Directory.CreateDirectory(_userDataDir);
                var env = await CoreWebView2Environment.CreateAsync(null, _userDataDir, new CoreWebView2EnvironmentOptions());
                await _web.EnsureCoreWebView2Async(env);

                CoreWebView2 core = _web.CoreWebView2;
                core.Settings.AreDevToolsEnabled = _devTools;
                core.Settings.AreBrowserAcceleratorKeysEnabled = _devTools;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.IsGeneralAutofillEnabled = false;
                core.Settings.IsPasswordAutosaveEnabled = false;

                // file:// cannot load module workers, so the UI is served from a virtual https host
                core.SetVirtualHostNameToFolderMapping(AppHost, _webDir, CoreWebView2HostResourceAccessKind.Allow);
                // Not a folder mapping as well: WebView2 applies those only to pages loaded after them, so a trace
                // from another folder than the first would not be found
                core.AddWebResourceRequestedFilter($"https://{DataHost}/*", CoreWebView2WebResourceContext.All);
                core.WebResourceRequested += OnDataRequested;
                core.WebMessageReceived += OnWebMessage;
                core.NewWindowRequested += (s, e) => e.Handled = true;
                core.NavigationStarting += (s, e) =>
                {
                    if (!e.Uri.StartsWith($"https://{AppHost}/", StringComparison.OrdinalIgnoreCase)) e.Cancel = true;
                    else _pageReady = false;
                };

                core.Navigate($"https://{AppHost}/index.html?theme={(_dark ? "dark" : "light")}");
            }
            catch (Exception ex)
            {
                ShowFailure(ex);
            }
        }

        /// <summary>
        /// Shows a trace document the page may still have analysed: doc is the Notepad++ buffer, gen the version of
        /// its text. Without that analysis the page answers need, and the plugin sends the text with ShowTrace.
        /// </summary>
        public void ShowDocument(IntPtr doc, int gen, string name)
        {
            DropData();
            _state = Json.Serialize(new Dictionary<string, object> { ["type"] = "show", ["name"] = name, ["doc"] = DocKey(doc), ["gen"] = gen });
            Post(_state);
        }

        /// <summary>Shows a trace file; a temporary one is deleted once another document replaces it.</summary>
        public void ShowTrace(IntPtr doc, int gen, string name, string path, bool temporary)
        {
            DropData();
            _dataPath = path;
            _dataTemp = temporary;
            // A new token each time: the same file name may have new content
            _dataToken = Guid.NewGuid().ToString("N");
            _state = Json.Serialize(new Dictionary<string, object>
            {
                ["type"] = "loadUrl",
                ["name"] = name,
                ["doc"] = DocKey(doc),
                ["gen"] = gen,
                ["url"] = $"https://{DataHost}/{Uri.EscapeDataString(Path.GetFileName(path))}?v={_dataToken}",
            });
            Post(_state);
        }

        public void ShowNotTrace(IntPtr doc, string name)
        {
            DropData();
            _state = Json.Serialize(new Dictionary<string, object> { ["type"] = "notTrace", ["name"] = name, ["doc"] = DocKey(doc) });
            Post(_state);
        }

        /// <summary>The buffer was closed; the page drops its kept view state.</summary>
        public void Forget(IntPtr doc) => Post(Json.Serialize(new Dictionary<string, object> { ["type"] = "forget", ["doc"] = DocKey(doc) }));

        private static string DocKey(IntPtr doc) => doc.ToInt64().ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>The data host serves the shown file, streamed from disk; an older request finds nothing.</summary>
        private void OnDataRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            CoreWebView2Environment env = _web.CoreWebView2.Environment;
            string path = _dataPath;
            if (path != null && e.Request.Uri.EndsWith("?v=" + _dataToken, StringComparison.Ordinal))
            {
                try
                {
                    // Others may write or delete the file meanwhile: Notepad++ saving it, DropData removing a copy
                    var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1 << 16, FileOptions.SequentialScan);
                    // The page reloaded and asks again
                    _dataStream?.Dispose();
                    _dataStream = stream;
                    e.Response = env.CreateWebResourceResponse(stream, 200, "OK", DataHeaders($"Content-Type: text/plain\r\nContent-Length: {stream.Length}"));
                    return;
                }
                catch (Exception)
                {
                    // Gone or locked; the page reports it
                }
            }
            e.Response = env.CreateWebResourceResponse(null, 404, "Not Found", DataHeaders(null));
        }

        // The page is another origin (the app host), so the response has to allow it, or the fetch fails
        private static string DataHeaders(string extra) =>
            $"Access-Control-Allow-Origin: https://{AppHost}\r\nCache-Control: no-store" + (extra == null ? "" : "\r\n" + extra);

        private void DropData()
        {
            // A read still going belongs to a load the page drops anyway
            _dataStream?.Dispose();
            _dataStream = null;
            if (_dataTemp && _dataPath != null)
            {
                try
                {
                    Directory.Delete(Path.GetDirectoryName(_dataPath), true);
                }
                catch (Exception)
                {
                    // Locked by another program; the plugin cleans up old copies on its next start
                }
            }
            _dataPath = null;
            _dataToken = null;
            _dataTemp = false;
        }

        /// <summary>Only with DevTools=1 in the settings; until WebView2 has started there is nothing to inspect.</summary>
        public void OpenDevTools() => _web.CoreWebView2?.OpenDevToolsWindow();

        /// <summary>The editor caret moved to another trace line of the shown document.</summary>
        public void PostCaret(int line) => Post(Json.Serialize(new Dictionary<string, object> { ["type"] = "caret", ["line"] = line }));

        public void ApplyTheme(bool dark)
        {
            _dark = dark;
            BackColor = Background(dark);
            _web.DefaultBackgroundColor = Background(dark);
            Post(ThemeMessage());
        }

        public void Shutdown()
        {
            try
            {
                _web.Dispose();
            }
            catch (Exception)
            {
                // Notepad++ is closing anyway
            }
            DropData();
        }

        /// <summary>Statement detail as a slide-over from the right instead of a pane under the views.</summary>
        public void SetDetailSlideOver(bool on)
        {
            _slideOver = on;
            Post(PrefsMessage());
        }

        private string ThemeMessage() => Json.Serialize(new Dictionary<string, object> { ["type"] = "theme", ["dark"] = _dark });

        /// <summary>The editor tints the selected row's lines.</summary>
        public void SetRowHighlight(bool on)
        {
            _rowHighlight = on;
            Post(PrefsMessage());
        }

        /// <summary>How the call tree shows a row's parent call.</summary>
        public void SetParentView(string view)
        {
            _parentView = view;
            Post(PrefsMessage());
        }

        private string PrefsMessage() =>
            Json.Serialize(new Dictionary<string, object>
            {
                ["type"] = "prefs", ["detailSlideOver"] = _slideOver, ["rowHighlight"] = _rowHighlight, ["parentView"] = _parentView,
            });

        private void Post(string json)
        {
            if (_pageReady && _web.CoreWebView2 != null) _web.CoreWebView2.PostWebMessageAsJson(json);
        }

        private void OnWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            Dictionary<string, object> msg;
            try
            {
                msg = Json.Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);
            }
            catch (Exception)
            {
                return;
            }
            if (msg == null || !msg.TryGetValue("type", out object type)) return;

            switch (type as string)
            {
                case "ready":
                    _pageReady = true;
                    Post(ThemeMessage());
                    Post(PrefsMessage());
                    if (_state != null) Post(_state);
                    break;
                case "gotoLine":
                    if (msg.TryGetValue("line", out object line))
                    {
                        bool focus = msg.TryGetValue("focus", out object f) && f is bool b && b;
                        Plugin.GotoLine(Convert.ToInt32(line), focus);
                    }
                    break;
                case "mark":
                    Plugin.Mark(Int(msg, "from"), Int(msg, "to"), Int(msg, "line"), Int(msg, "lineEnd"), msg.TryGetValue("word", out object word) ? word as string : null);
                    break;
                case "clearMarks":
                    Plugin.ClearMarks();
                    break;
                case "reload":
                    Plugin.ReloadRequested();
                    break;
                case "need":
                    if (msg.TryGetValue("doc", out object needDoc) && long.TryParse(needDoc as string, out long buffer))
                        Plugin.DocumentRequested(new IntPtr(buffer), Int(msg, "gen"));
                    break;
                case "setPrefs":
                    if (msg.TryGetValue("detailSlideOver", out object slide) && slide is bool on) Plugin.SetDetailSlideOver(on);
                    if (msg.TryGetValue("rowHighlight", out object tint) && tint is bool tinted) Plugin.SetRowHighlight(tinted);
                    if (msg.TryGetValue("parentView", out object pv) && pv is string view) Plugin.SetParentView(view);
                    break;
                case "saveCsv":
                    if (msg.TryGetValue("name", out object csvName) && msg.TryGetValue("text", out object csv))
                        Plugin.SaveCsv(csvName as string, csv as string ?? "");
                    break;
                case "saveXlsx":
                    if (msg.TryGetValue("name", out object xlsxName) && msg.TryGetValue("data", out object xlsx))
                        Plugin.SaveXlsx(xlsxName as string, xlsx as string ?? "");
                    break;
            }
        }

        private static int Int(Dictionary<string, object> msg, string key) =>
            msg.TryGetValue(key, out object value) && value != null ? Convert.ToInt32(value) : 0;

        private void ShowFailure(Exception ex)
        {
            string text = ex is WebView2RuntimeNotFoundException
                ? "Microsoft Edge WebView2 Runtime bulunamadı.\r\n\r\nhttps://developer.microsoft.com/microsoft-edge/webview2 adresinden \"Evergreen Runtime\" kurun ve Notepad++'ı yeniden başlatın."
                : "TroiaLens paneli başlatılamadı:\r\n\r\n" + ex.Message;
            Controls.Clear();
            Controls.Add(new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Text = text,
                BackColor = Background(_dark),
                ForeColor = _dark ? Color.Gainsboro : Color.Black,
            });
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Win32.WM_NOTIFY && m.LParam != IntPtr.Zero)
            {
                // NMHDR { HWND hwndFrom; UINT_PTR idFrom; UINT code; }
                int code = Marshal.ReadInt32(m.LParam, IntPtr.Size * 2);
                if (code == NppMsg.DMN_CLOSE) Plugin.OnPanelClosed();
            }
            base.WndProc(ref m);
        }

        // Tailwind slate-950 / slate-50, the page background, so nothing flashes while it loads
        private static Color Background(bool dark) => dark ? Color.FromArgb(2, 6, 23) : Color.FromArgb(248, 250, 252);
    }
}
