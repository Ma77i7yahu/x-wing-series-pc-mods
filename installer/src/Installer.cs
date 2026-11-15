// Mod installer for the X-Wing series source ports (OpenXW, OpenTIE, OpenXvT, OpenXWA).
// One source file for all four games; the build script compiles it with Delta.cs, a generated
// GameInfo class (port name, exe, build id, required release, XvT files to copy) and embeds
// the patch as the "payload.zip" resource:
//   manifest.txt   "release <name>", then one line per file, tab separated:
//                  patch <path> <release sha256> <modded sha256>   delta in delta/<path>
//                  add   <path> <sha256>                           copy in files/<path>
//                  need  <path> <sha256>                           release file left as is
//   delta/..., files/...
// The patch only works on one upstream release (GameInfo.ReleaseName). Every "patch" and
// "need" file must match that release (or, for "patch", the modded result of an earlier
// install); otherwise the installer refuses and links to the release download.
//
// Install: the player's existing port folder (e.g. "Openxw") is copied to "<name>-original"
// once, then the patch is applied in place, so files the player added (ROMs, SoundFonts)
// stay. X-Wing and TIE Fighter features that use X-Wing vs TIE Fighter art and sounds get
// those files from the player's own XvT install; they are never shipped.
// Restore: the modded folder is renamed aside and the backup renamed back.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

class InstallerForm : Form {
    const string MarkerName = "mod-install.txt";

    readonly TextBox folder = new TextBox(), xvtFolder = new TextBox();
    readonly Label status = new Label(), xvtStatus = new Label();
    readonly LinkLabel releaseLink = new LinkLabel();
    readonly Button install = new Button(), restore = new Button();
    readonly ProgressBar progress = new ProgressBar();
    string portDir;          // the port's folder (holds GameInfo.PortExe), or null
    string backupDir;        // full path of this install's backup, when modded
    bool modded;

    class Entry { public string Kind, Path, OldSha, NewSha; }
    static List<Entry> manifest;
    static string manifestRelease;

    InstallerForm() {
        Text = GameInfo.Title + " - Mod Installer";
        Font = new Font("Segoe UI", 9f);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(10);

        var root = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false };
        Controls.Add(root);

        var about = new Label { AutoSize = true, MaximumSize = new Size(640, 0), Margin = new Padding(0, 0, 0, 8) };
        about.Text = "Patches " + GameInfo.ReleaseName + " into a modified build (" + GameInfo.BuildId + "). It only works on that " +
                     GameInfo.PortName + " release, unchanged. Every change can be switched on or off in " + GameInfo.PortName +
                     "'s settings; see the README for the list.";
        root.Controls.Add(about);

        releaseLink.AutoSize = true; releaseLink.Margin = new Padding(0, 0, 0, 8);
        releaseLink.Text = "Download " + GameInfo.ReleaseName + ": " + GameInfo.ReleaseUrl;
        releaseLink.LinkArea = new LinkArea(releaseLink.Text.Length - GameInfo.ReleaseUrl.Length, GameInfo.ReleaseUrl.Length);
        releaseLink.LinkClicked += (s, e) => { try { Process.Start(GameInfo.ReleaseUrl); } catch { } };
        root.Controls.Add(releaseLink);

        root.Controls.Add(FolderRow("Game folder:", folder, "Select the " + GameInfo.Title + " folder (the one holding your " +
                                    GameInfo.PortName + " folder), or the " + GameInfo.PortName + " folder itself", LoadFolder));
        status.AutoSize = true; status.MaximumSize = new Size(640, 0); status.Margin = new Padding(0, 4, 0, 8);
        root.Controls.Add(status);

        if (GameInfo.XvtFiles.Length > 0) {
            var note = new Label { AutoSize = true, MaximumSize = new Size(640, 0), Margin = new Padding(0, 4, 0, 4) };
            note.Text = GameInfo.XvtNote;
            root.Controls.Add(note);
            root.Controls.Add(FolderRow("XvT folder:", xvtFolder, "Select your Star Wars: X-Wing vs TIE Fighter folder (the one holding \"wave\" and \"BalanceOfPower\")", CheckXvt));
            xvtStatus.AutoSize = true; xvtStatus.MaximumSize = new Size(640, 0); xvtStatus.Margin = new Padding(0, 4, 0, 8);
            root.Controls.Add(xvtStatus);
        }

        progress.Width = 640; progress.Visible = false;
        root.Controls.Add(progress);

        var bl = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
        install.Text = "Install"; install.AutoSize = true;
        install.Click += (s, e) => DoInstall();
        restore.Text = "Restore original " + GameInfo.PortName; restore.AutoSize = true;
        restore.Click += (s, e) => DoRestore();
        var close = new Button { Text = "Close", AutoSize = true };
        close.Click += (s, e) => Close();
        bl.Controls.AddRange(new Control[] { install, restore, close });
        root.Controls.Add(bl);

        folder.Text = Candidates().FirstOrDefault(d => FindPortDir(d) != null) ?? "";
        LoadFolder();
        if (GameInfo.XvtFiles.Length > 0) {
            // the best match: TIE Fighter's folder also has a MICRO64.FNT
            var best = XvtCandidates().Distinct(StringComparer.OrdinalIgnoreCase).Select(d => new { d, n = CountXvt(d) })
                                      .Where(c => c.n > 0).OrderByDescending(c => c.n).FirstOrDefault();
            xvtFolder.Text = best == null ? "" : best.d;
            CheckXvt();
        }
    }

    Control FolderRow(string label, TextBox box, string prompt, Action changed) {
        var fl = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(0) };
        fl.Controls.Add(new Label { Text = label, AutoSize = true, MinimumSize = new Size(80, 0), Margin = new Padding(0, 7, 4, 0) });
        box.Width = 460;
        box.Margin = new Padding(0, 3, 4, 0);
        box.Leave += (s, e) => changed();
        fl.Controls.Add(box);
        var browse = new Button { Text = "Browse...", AutoSize = true };
        browse.Click += (s, e) => {
            using (var d = new FolderBrowserDialog { Description = prompt }) {
                if (Directory.Exists(box.Text)) d.SelectedPath = box.Text;
                if (d.ShowDialog(this) == DialogResult.OK) { box.Text = d.SelectedPath; changed(); }
            }
        };
        fl.Controls.Add(browse);
        return fl;
    }

    // ---- finding folders ----

    static IEnumerable<string> SteamCommonDirs() {
        var libs = new List<string>();
        try {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")) {
                var sp = k == null ? null : k.GetValue("SteamPath") as string;
                if (sp != null) {
                    sp = sp.Replace('/', '\\');
                    libs.Add(sp);
                    string vdf = Path.Combine(sp, @"steamapps\libraryfolders.vdf");
                    if (File.Exists(vdf))
                        foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                            libs.Add(m.Groups[1].Value.Replace(@"\\", @"\"));
                }
            }
        } catch { }
        foreach (var l in libs.Distinct(StringComparer.OrdinalIgnoreCase)) {
            string c = Path.Combine(l, @"steamapps\common");
            if (Directory.Exists(c)) yield return c;
        }
    }

    static IEnumerable<string> GogDirs() {
        var r = new List<string>();
        foreach (var root in new[] { @"SOFTWARE\WOW6432Node\GOG.com\Games", @"SOFTWARE\GOG.com\Games" }) {
            try {
                using (var k = Registry.LocalMachine.OpenSubKey(root)) {
                    if (k == null) continue;
                    foreach (var id in k.GetSubKeyNames())
                        using (var g = k.OpenSubKey(id)) {
                            var p = g == null ? null : g.GetValue("path") as string;
                            if (!string.IsNullOrEmpty(p)) r.Add(p);
                        }
                }
            } catch { }
        }
        return r;
    }

    // folders that might be the game: next to the installer, GOG installs, Steam installs
    static IEnumerable<string> Candidates() {
        string here = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        yield return here;
        var parent = Directory.GetParent(here);
        if (parent != null) yield return parent.FullName;
        foreach (var g in GogDirs()) yield return g;
        foreach (var c in SteamCommonDirs()) {
            string[] subs;
            try { subs = Directory.GetDirectories(c); } catch { continue; }
            foreach (var s in subs) yield return s;
        }
    }

    static IEnumerable<string> XvtCandidates() {
        foreach (var c in Candidates()) yield return c;
        // e.g. "...\Star Wars X-Wing" next to "...\Star Wars X-Wing vs TIE Fighter"
        string here = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        foreach (var d in new[] { here, Path.GetDirectoryName(here) ?? "" }) {
            var p = Directory.GetParent(d);
            if (p == null) continue;
            string[] subs;
            try { subs = Directory.GetDirectories(p.FullName); } catch { continue; }
            foreach (var s in subs) yield return s;
        }
    }

    // the port folder: the folder itself, or a subfolder holding the port's exe
    // (an exact name first, then any folder without a dash, so old "Openxw-xxxx" backups are skipped)
    static string FindPortDir(string dir) {
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;
        if (File.Exists(Path.Combine(dir, GameInfo.PortExe))) return dir;
        string[] subs;
        try { subs = Directory.GetDirectories(dir); } catch { return null; }
        var hits = subs.Where(s => File.Exists(Path.Combine(s, GameInfo.PortExe))).ToList();
        return hits.FirstOrDefault(s => Path.GetFileName(s).Equals(GameInfo.PortName, StringComparison.OrdinalIgnoreCase))
            ?? hits.FirstOrDefault(s => !Path.GetFileName(s).Contains("-"));
    }

    static Dictionary<string, string> ReadMarker(string dir) {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string p = Path.Combine(dir, MarkerName);
        if (!File.Exists(p)) return null;
        foreach (var line in File.ReadAllLines(p)) {
            int i = line.IndexOf(':');
            if (i > 0) d[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
        }
        return d;
    }

    void LoadFolder() {
        portDir = null; backupDir = null; modded = false;
        install.Enabled = restore.Enabled = false;
        string dir = folder.Text.Trim().Trim('"');
        portDir = FindPortDir(dir);
        if (portDir == null) {
            status.ForeColor = Color.DarkRed;
            status.Text = dir.Length == 0
                ? "Choose the " + GameInfo.Title + " folder that holds your " + GameInfo.PortName + " folder."
                : "No " + GameInfo.PortName + " folder (with " + GameInfo.PortExe + ") was found there. Install " + GameInfo.ReleaseName +
                  " first (link above), check that it runs, then choose its folder or the game folder holding it.";
            return;
        }
        var marker = ReadMarker(portDir);
        string name = Path.GetFileName(portDir);
        if (marker != null) {
            modded = true;
            string b;
            if (marker.TryGetValue("Backup", out b) && b.Length > 0) backupDir = Path.Combine(Path.GetDirectoryName(portDir), b);
            string built;
            marker.TryGetValue("Build", out built);
            status.ForeColor = Color.DarkGreen;
            status.Text = "The mods are installed in \"" + portDir + "\" (" + (built ?? "unknown build") + "). Install again to update or repair them.";
            if (backupDir != null && Directory.Exists(backupDir)) {
                status.Text += " Your original " + GameInfo.PortName + " is kept in \"" + Path.GetFileName(backupDir) + "\".";
                restore.Enabled = true;
            } else {
                status.Text += " The backup of your original " + GameInfo.PortName + " is missing, so it can't be restored automatically.";
            }
        } else {
            status.ForeColor = Color.DarkGreen;
            status.Text = "Found " + GameInfo.PortName + " in \"" + portDir + "\". It will be copied to \"" + NewBackupName(portDir) +
                          "\" before the mods are written over it.";
        }
        var wrong = WrongFiles(portDir);
        if (wrong.Count > 0) {
            status.ForeColor = Color.DarkRed;
            status.Text = "\"" + portDir + "\" isn't an unchanged " + GameInfo.ReleaseName + ", so the patch can't be applied (" +
                          string.Join(", ", wrong.Take(4)) + (wrong.Count > 4 ? " and " + (wrong.Count - 4) + " more" : "") +
                          (wrong.Count == 1 ? " differs" : " differ") + "). Download " + GameInfo.ReleaseName + " with the link above, unzip it " +
                          "into the game folder (keeping your old " + GameInfo.PortName + " folder elsewhere if you want it), check that it runs, " +
                          "then choose its folder." + (modded && restore.Enabled ? " You can still restore your original " + GameInfo.PortName + "." : "");
            return;
        }
        install.Enabled = true;
    }

    // ---- the patch ----

    static Stream Payload() { return Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"); }

    static void ReadManifest() {
        if (manifest != null) return;
        manifest = new List<Entry>();
        using (var z = new ZipArchive(Payload(), ZipArchiveMode.Read))
        using (var r = new StreamReader(z.GetEntry("manifest.txt").Open())) {
            string line;
            while ((line = r.ReadLine()) != null) {
                var f = line.Split('\t');
                if (f[0] == "release") { manifestRelease = f[1]; continue; }
                var e = new Entry { Kind = f[0], Path = f[1] };
                if (e.Kind == "patch") { e.OldSha = f[2]; e.NewSha = f[3]; }
                else if (e.Kind == "need") e.OldSha = f[2];
                else if (e.Kind == "add") e.NewSha = f[2];
                else throw new InvalidDataException("Bad manifest line: " + line);
                manifest.Add(e);
            }
        }
    }

    static string Sha(byte[] b) {
        using (var s = System.Security.Cryptography.SHA256.Create()) return BitConverter.ToString(s.ComputeHash(b)).Replace("-", "").ToLowerInvariant();
    }

    static string FileSha(string path) {
        using (var s = System.Security.Cryptography.SHA256.Create())
        using (var f = File.OpenRead(path)) return BitConverter.ToString(s.ComputeHash(f)).Replace("-", "").ToLowerInvariant();
    }

    // files that are neither the release's (nor, for patched files, already the modded result)
    static List<string> WrongFiles(string dir) {
        ReadManifest();
        var wrong = new List<string>();
        foreach (var e in manifest) {
            if (e.Kind == "add") continue;
            string p = Path.Combine(dir, e.Path);
            string sha = File.Exists(p) ? FileSha(p) : null;
            if (sha == null) wrong.Add(e.Path + " (missing)");
            else if (sha != e.OldSha && sha != e.NewSha) wrong.Add(e.Path);
        }
        return wrong;
    }

    static string NewBackupName(string dir) {
        string parent = Path.GetDirectoryName(dir), name = Path.GetFileName(dir);
        string b = name + "-original";
        for (int i = 2; Directory.Exists(Path.Combine(parent, b)); i++) b = name + "-original-" + i;
        return b;
    }

    // ---- X-Wing vs TIE Fighter files ----

    // source -> destination (relative to the port folder); a source like "dir\*.lfd" copies every match into the destination folder
    static List<KeyValuePair<string, string>> XvtCopies(string xvt) {
        var list = new List<KeyValuePair<string, string>>();
        foreach (var f in GameInfo.XvtFiles) {
            string src = f[0], dest = f[1];
            if (src.Contains("*")) {
                string d = Path.Combine(xvt, Path.GetDirectoryName(src));
                if (!Directory.Exists(d)) continue;
                foreach (var file in Directory.GetFiles(d, Path.GetFileName(src)))
                    list.Add(new KeyValuePair<string, string>(file, Path.Combine(dest, Path.GetFileName(file))));
            } else {
                string file = Path.Combine(xvt, src);
                if (File.Exists(file)) list.Add(new KeyValuePair<string, string>(file, dest));
            }
        }
        return list;
    }

    static int CountXvt(string xvt) {
        if (string.IsNullOrEmpty(xvt) || !Directory.Exists(xvt)) return 0;
        try { return GameInfo.XvtFiles.Count(f => f[0].Contains("*") ? Directory.Exists(Path.Combine(xvt, Path.GetDirectoryName(f[0]))) : File.Exists(Path.Combine(xvt, f[0]))); }
        catch { return 0; }
    }

    void CheckXvt() {
        string x = xvtFolder.Text.Trim().Trim('"');
        int n = CountXvt(x), all = GameInfo.XvtFiles.Length;
        if (x.Length == 0) {
            xvtStatus.ForeColor = Color.DarkOrange;
            xvtStatus.Text = "No X-Wing vs TIE Fighter folder chosen. The mods still install; the features listed above stay off until you run the installer again with XvT.";
        } else if (n == 0) {
            xvtStatus.ForeColor = Color.DarkRed;
            xvtStatus.Text = "None of the X-Wing vs TIE Fighter files were found there.";
        } else if (n < all) {
            xvtStatus.ForeColor = Color.DarkOrange;
            xvtStatus.Text = "Found " + n + " of the " + all + " X-Wing vs TIE Fighter files or folders needed. The missing ones' features stay off. (" + GameInfo.XvtMissingHint + ")";
        } else {
            xvtStatus.ForeColor = Color.DarkGreen;
            xvtStatus.Text = "Found X-Wing vs TIE Fighter. Its files will be copied into " + GameInfo.PortName + "'s resources folder.";
        }
    }

    // ---- install / restore ----

    bool PortRunning() {
        string exe = Path.GetFileNameWithoutExtension(GameInfo.PortExe);
        if (Process.GetProcessesByName(exe).Length == 0) return false;
        MessageBox.Show(this, GameInfo.PortName + " is running. Close it and try again.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }

    static void CopyDir(string from, string to) {
        Directory.CreateDirectory(to);
        foreach (var f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)));
        foreach (var d in Directory.GetDirectories(from)) CopyDir(d, Path.Combine(to, Path.GetFileName(d)));
    }

    // zips the port's settings and pilots (%APPDATA%\TotallyOpen\<port>), logs left out
    static string BackupSettings() {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TotallyOpen", GameInfo.AppDataName);
        if (!Directory.Exists(dir)) return null;
        string zip = dir + "-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".zip";
        using (var z = ZipFile.Open(zip, ZipArchiveMode.Create))
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) {
                string rel = f.Substring(dir.Length + 1);
                if (rel.StartsWith("logs\\", StringComparison.OrdinalIgnoreCase)) continue;
                z.CreateEntryFromFile(f, rel.Replace('\\', '/'));
            }
        return zip;
    }

    static byte[] ReadEntry(ZipArchive z, string name) {
        var e = z.GetEntry(name);
        if (e == null) throw new InvalidDataException("The installer is missing " + name + ".");
        using (var s = e.Open()) using (var m = new MemoryStream()) { s.CopyTo(m); return m.ToArray(); }
    }

    // writes next to the target first, so a failure never leaves a half-written file
    static void WriteFile(string dest, byte[] data) {
        Directory.CreateDirectory(Path.GetDirectoryName(dest));
        string tmp = dest + ".mod-tmp";
        File.WriteAllBytes(tmp, data);
        if (File.Exists(dest)) File.Delete(dest);
        File.Move(tmp, dest);
    }

    // returns how many files were written
    int ApplyPatch() {
        ReadManifest();
        string full = Path.GetFullPath(portDir).TrimEnd('\\') + "\\";
        // all patched files are worked out before anything is written
        var writes = new List<KeyValuePair<string, byte[]>>();
        using (var z = new ZipArchive(Payload(), ZipArchiveMode.Read)) {
            progress.Maximum = manifest.Count; progress.Value = 0; progress.Visible = true;
            foreach (var e in manifest) {
                progress.Value++; Application.DoEvents();
                if (e.Kind == "need") continue;
                string dest = Path.GetFullPath(Path.Combine(portDir, e.Path));
                if (!dest.StartsWith(full, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Bad path in the patch: " + e.Path);
                byte[] data;
                if (e.Kind == "patch") {
                    byte[] old = File.ReadAllBytes(dest);
                    string sha = Sha(old);
                    if (sha == e.NewSha) continue;   // already modded (reinstall)
                    if (sha != e.OldSha) throw new InvalidDataException(e.Path + " isn't the " + GameInfo.ReleaseName + " file.");
                    data = Delta.Apply(old, ReadEntry(z, "delta/" + e.Path.Replace('\\', '/')));
                } else {
                    if (File.Exists(dest) && FileSha(dest) == e.NewSha) continue;
                    data = ReadEntry(z, "files/" + e.Path.Replace('\\', '/'));
                }
                if (Sha(data) != e.NewSha) throw new InvalidDataException("The patched " + e.Path + " came out wrong; nothing was changed.");
                writes.Add(new KeyValuePair<string, byte[]>(dest, data));
            }
        }
        foreach (var w in writes) WriteFile(w.Key, w.Value);
        return writes.Count;
    }

    void DoInstall() {
        if (portDir == null || PortRunning()) return;
        var wrongNow = WrongFiles(portDir);
        if (wrongNow.Count > 0) {
            MessageBox.Show(this, "This " + GameInfo.PortName + " folder isn't an unchanged " + GameInfo.ReleaseName + " (" + string.Join(", ", wrongNow.Take(4)) +
                "). Nothing was changed.\r\n\r\nDownload " + GameInfo.ReleaseName + " from " + GameInfo.ReleaseUrl + " and choose that folder.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            LoadFolder();
            return;
        }
        var report = new StringBuilder();
        UseWaitCursor = true; install.Enabled = restore.Enabled = false;
        try {
            if (!modded) {
                string b = NewBackupName(portDir);
                status.Text = "Backing up " + GameInfo.PortName + " to \"" + b + "\"..."; status.Refresh();
                CopyDir(portDir, Path.Combine(Path.GetDirectoryName(portDir), b));
                backupDir = Path.Combine(Path.GetDirectoryName(portDir), b);
                report.AppendLine("Your original " + GameInfo.PortName + " was copied to \"" + b + "\".");
            }
            status.Text = "Backing up your " + GameInfo.PortName + " settings and pilots..."; status.Refresh();
            string zip = BackupSettings();
            if (zip != null) report.AppendLine("Your settings and pilots were backed up to \"" + zip + "\".");

            status.Text = "Patching " + GameInfo.PortName + "..."; status.Refresh();
            int written = ApplyPatch();
            report.AppendLine("Patched " + GameInfo.ReleaseName + ": " + written + " files written.");

            int copied = 0;
            if (GameInfo.XvtFiles.Length > 0) {
                string x = xvtFolder.Text.Trim().Trim('"');
                if (x.Length > 0 && Directory.Exists(x)) {
                    foreach (var c in XvtCopies(x)) {
                        string dest = Path.Combine(portDir, c.Value);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        File.Copy(c.Key, dest, true);
                        copied++;
                    }
                }
                if (copied > 0) report.AppendLine("Copied " + copied + " files from X-Wing vs TIE Fighter.");
                else report.AppendLine("No X-Wing vs TIE Fighter files were copied, so the features that need them stay off.");
            }

            File.WriteAllLines(Path.Combine(portDir, MarkerName), new[] {
                "Game: " + GameInfo.Title,
                "Build: " + GameInfo.BuildId,
                "Release: " + GameInfo.ReleaseName,
                "Backup: " + (backupDir == null ? "" : Path.GetFileName(backupDir)),
                "Installed: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                "XvT files: " + copied,
            });
        } catch (Exception ex) {
            progress.Visible = false; UseWaitCursor = false;
            string hint = ex is UnauthorizedAccessException
                ? "Windows denied access to the game folder. Right-click the installer and choose \"Run as administrator\"."
                : "Something went wrong. Close " + GameInfo.PortName + " if it's running and try again.";
            MessageBox.Show(this, hint + "\r\n\r\n" + ex.Message + (report.Length > 0 ? "\r\n\r\nDone before the error:\r\n" + report : ""),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            LoadFolder();
            return;
        }
        progress.Visible = false; UseWaitCursor = false;
        MessageBox.Show(this, "The mods are installed.\r\n\r\n" + report + "\r\nStart " + GameInfo.PortName + " as usual. " + GameInfo.AfterInstall,
            Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadFolder();
    }

    void DoRestore() {
        if (portDir == null || backupDir == null || !Directory.Exists(backupDir) || PortRunning()) return;
        string parent = Path.GetDirectoryName(portDir), name = Path.GetFileName(portDir);
        string aside = name + "-mods-removed";
        for (int i = 2; Directory.Exists(Path.Combine(parent, aside)); i++) aside = name + "-mods-removed-" + i;
        if (MessageBox.Show(this, "Put your original " + GameInfo.PortName + " back?\r\n\r\nThe modded folder is renamed to \"" + aside +
                "\" (delete it when you no longer need it), and \"" + Path.GetFileName(backupDir) + "\" becomes \"" + name + "\" again.\r\n\r\n" +
                "Your settings and pilots in %APPDATA%\\TotallyOpen\\" + GameInfo.AppDataName + " are not changed. If the original " +
                GameInfo.PortName + " can't read them, use the backup zip the installer made next to that folder.",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try {
            Directory.Move(portDir, Path.Combine(parent, aside));
            Directory.Move(backupDir, portDir);
        } catch (Exception ex) {
            MessageBox.Show(this, "Couldn't rename the folders. Close " + GameInfo.PortName + " and any window showing those folders, then try again.\r\n\r\n" + ex.Message,
                Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            LoadFolder();
            return;
        }
        MessageBox.Show(this, "Your original " + GameInfo.PortName + " has been restored. The modded copy is in \"" + aside + "\".",
            Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadFolder();
    }

    [STAThread]
    static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InstallerForm());
    }
}
