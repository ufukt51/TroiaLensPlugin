using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using TroiaLens.Npp;

namespace TroiaLens
{
    // Updates from the GitHub Releases page: once a day after startup (and from the menu) the latest release is looked up.
    // A newer one's setup exe is downloaded, checked against the SHA-256 GitHub gives for it, and run by a hidden
    // PowerShell once this Notepad++ has exited, so the plugin DLL is no longer locked. The setup's MSI replaces the
    // earlier install (same UpgradeCode since 0.1.0), whether that came from the setup, the MSI or the zip.
    internal static partial class Plugin
    {
        private const string LatestReleaseApi = "https://api.github.com/repos/ufukt51/TroiaLensPlugin/releases/latest";
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromDays(1);
        private static string UpdateDir => Path.Combine(Path.GetTempPath(), Name + "-update");

        // Created on the UI thread; background work comes back through it
        private static Control _ui;
        private static bool _updateBusy;

        private sealed class Release
        {
            public Version Version;
            public string Tag, Page, SetupUrl, SetupName, Sha256;
        }

        private static Version InstalledVersion => Complete(typeof(Plugin).Assembly.GetName().Version);

        /// <summary>All four parts, missing ones as 0, so 0.1.4 equals 0.1.4.0 and 0.1.4.1 is newer than both.</summary>
        private static Version Complete(Version v) => new Version(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));

        /// <summary>0.1.4 for 0.1.4.0, 0.1.4.1 as it is.</summary>
        private static string Display(Version v) => v.Revision > 0 ? v.ToString(4) : v.ToString(3);

        /// <summary>At NPPN_READY: looks for a new release in the background when the last look is a day old.</summary>
        private static void AutoCheckUpdate()
        {
            if (!_settings.CheckUpdates) return;
            if (DateTime.UtcNow - _settings.LastUpdateCheck < UpdateInterval) return;
            CheckUpdate(manual: false);
        }

        private static void CheckUpdateCommand() => CheckUpdate(manual: true);

        private static void ToggleCheckUpdates()
        {
            _settings.CheckUpdates = !_settings.CheckUpdates;
            SetCheck(_cmdCheckUpdates, _settings.CheckUpdates);
            _settings.Save();
        }

        private static void CheckUpdate(bool manual)
        {
            if (_updateBusy)
            {
                if (manual) MessageBox.Show("Güncelleme zaten denetleniyor.", Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _updateBusy = true;
            EnsureUi();
            Task.Run(() => FetchLatest()).ContinueWith(t => _ui.BeginInvoke((Action)(() => Safe(() =>
            {
                _updateBusy = false;
                _settings.LastUpdateCheck = DateTime.UtcNow;
                _settings.Save();
                if (t.IsFaulted)
                {
                    // An offline or proxied machine must not get a message every day
                    if (manual)
                        MessageBox.Show("Güncelleme denetlenemedi:\n" + t.Exception.GetBaseException().Message,
                            Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                OfferUpdate(t.Result, manual);
            }))));
        }

        private static void OfferUpdate(Release release, bool manual)
        {
            string installed = Display(InstalledVersion);
            if (release == null || release.Version <= InstalledVersion)
            {
                if (manual) MessageBox.Show($"En son sürüm kurulu: {installed}", Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string latest = Display(release.Version);
            if (!manual && _settings.SkippedVersion == latest) return;
            if (release.SetupUrl == null)
            {
                if (manual && Ask($"TroiaLens {latest} yayınlandı ama kurulum programı bulunamadı.\n\nReleases sayfası açılsın mı?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    Process.Start(release.Page);
                return;
            }

            var answer = Ask(
                $"TroiaLens {latest} yayınlandı. Kurulu sürüm: {installed}.\n\n" +
                "Evet: indir ve kur\n" +
                "Hayır: bu sürümü atla\n" +
                "İptal: sonra hatırlat\n\n" +
                "Kurulum için Notepad++ kapanır. Yönetici izni istenebilir.",
                MessageBoxButtons.YesNoCancel);
            if (answer == DialogResult.No)
            {
                _settings.SkippedVersion = latest;
                _settings.Save();
                return;
            }
            if (answer != DialogResult.Yes) return;
            // An earlier "skip" must not hide this version at startup if the install does not go through
            if (_settings.SkippedVersion == latest)
            {
                _settings.SkippedVersion = "";
                _settings.Save();
            }

            _updateBusy = true;
            Task.Run(() => Download(release)).ContinueWith(t => _ui.BeginInvoke((Action)(() => Safe(() =>
            {
                _updateBusy = false;
                if (t.IsFaulted)
                {
                    if (Ask("Güncelleme indirilemedi:\n" + t.Exception.GetBaseException().Message +
                            "\n\nReleases sayfası açılsın mı?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        Process.Start(release.Page);
                    return;
                }
                Install(t.Result, latest);
            }))));
        }

        /// <summary>Starts the setup after this Notepad++ exits; closes Notepad++ now and opens it again afterwards if the user agrees.</summary>
        private static void Install(string setup, string version)
        {
            var answer = Ask(
                $"TroiaLens {version} indirildi.\n\n" +
                "Evet: Notepad++ şimdi kapansın, kurulumdan sonra yeniden açılsın\n" +
                "Hayır: Notepad++'ı kapattığımda kurulsun",
                MessageBoxButtons.YesNo);
            bool now = answer == DialogResult.Yes;

            string exe = Process.GetCurrentProcess().MainModule.FileName;
            // The setup finds Notepad++ itself; a portable copy is only known from here
            string nppDir = Path.GetDirectoryName(exe);
            string dll = typeof(Plugin).Assembly.Location;
            var script = new StringBuilder()
                .AppendLine($"Wait-Process -Id {Process.GetCurrentProcess().Id} -ErrorAction SilentlyContinue")
                // Node.js is a first-install option; an update leaves it as the user chose then
                .AppendLine($"$p = Start-Process -FilePath {Quote(setup)} -ArgumentList '/passive', {Quote("NPPDIR=\"" + nppDir + "\"")}, 'InstallNode=0' -Wait -PassThru")
                // Plugins Admin may have put an older zip over this MSI version; the setup then sees it installed and copies nothing
                .AppendLine($"if ($p.ExitCode -eq 0 -and [Version](Get-Item {Quote(dll)}).VersionInfo.FileVersion -lt [Version]'{version}') " +
                            $"{{ Start-Process -FilePath {Quote(setup)} -ArgumentList '/repair', '/passive' -Wait }}")
                .AppendLine(now ? $"Start-Process -FilePath {Quote(exe)}" : "")
                .ToString();
            Process.Start(new ProcessStartInfo("powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " +
                Convert.ToBase64String(Encoding.Unicode.GetBytes(script)))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            // Notepad++ asks about unsaved documents itself; if the user cancels, the setup waits for the next exit
            if (now) Win32.PostMessage(Npp.NppHandle, Win32.WM_COMMAND, (IntPtr)NppMsg.IDM_FILE_EXIT, IntPtr.Zero);
        }

        // ---- Background work ----

        private static Release FetchLatest()
        {
            var json = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(Http().DownloadString(LatestReleaseApi));
            string tag = json["tag_name"] as string ?? "";
            if (!Version.TryParse(tag.TrimStart('v', 'V').Split('-', '+')[0], out var version)) return null;
            var release = new Release
            {
                Version = Complete(version),
                Tag = tag,
                Page = json["html_url"] as string,
            };
            var setup = ((json["assets"] as ArrayList) ?? new ArrayList())
                .OfType<Dictionary<string, object>>()
                .FirstOrDefault(a => a["name"] is string n && n.StartsWith("TroiaLens-Setup-", StringComparison.OrdinalIgnoreCase)
                                                           && n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            if (setup != null)
            {
                release.SetupName = (string)setup["name"];
                release.SetupUrl = setup["browser_download_url"] as string;
                if (setup.TryGetValue("digest", out var d) && d is string digest && digest.StartsWith("sha256:"))
                    release.Sha256 = digest.Substring(7);
            }
            return release;
        }

        private static string Download(Release release)
        {
            // Earlier downloads go; one still waiting for Notepad++ to exit is in use and stays
            if (Directory.Exists(UpdateDir))
                foreach (string f in Directory.GetFiles(UpdateDir))
                    try { File.Delete(f); } catch (Exception) { }
            Directory.CreateDirectory(UpdateDir);

            string file = Path.Combine(UpdateDir, Path.GetFileName(release.SetupName));
            Http().DownloadFile(release.SetupUrl, file);
            if (release.Sha256 == null) throw new InvalidDataException("Dosyanın SHA-256 özeti yayında yok.");
            using (var sha = SHA256.Create())
            using (var s = File.OpenRead(file))
            {
                string actual = string.Concat(sha.ComputeHash(s).Select(b => b.ToString("x2")));
                if (!string.Equals(actual, release.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    s.Dispose();
                    File.Delete(file);
                    throw new InvalidDataException("İndirilen dosya doğrulanamadı (SHA-256 uyuşmuyor).");
                }
            }
            return file;
        }

        private static WebClient Http()
        {
            // .NET Framework 4.8 may still default to TLS 1.0 on older Windows; GitHub needs 1.2
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var client = new WebClient { Encoding = Encoding.UTF8 };
            client.Headers[HttpRequestHeader.UserAgent] = Name + "/" + Display(InstalledVersion);
            client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
            // Company proxies usually sign in with the Windows account
            if (client.Proxy != null) client.Proxy.Credentials = CredentialCache.DefaultCredentials;
            return client;
        }

        // ---- Helpers ----

        private static void EnsureUi()
        {
            if (_ui != null) return;
            _ui = new Control();
            _ = _ui.Handle;
        }

        private static DialogResult Ask(string text, MessageBoxButtons buttons, MessageBoxIcon icon = MessageBoxIcon.Question) =>
            MessageBox.Show(new NppWindow(), text, Name, buttons, icon);

        /// <summary>PowerShell single-quoted string.</summary>
        private static string Quote(string s) => "'" + s.Replace("'", "''") + "'";

        private sealed class NppWindow : IWin32Window
        {
            public IntPtr Handle => Npp.NppHandle;
        }
    }
}
