// Build tool: makepatch.exe <release dir> <mod build dir> <out dir> <release name>
// Compares the mod build with the upstream release file by file and writes <out dir>:
//   manifest.txt      tab-separated lines (see Installer.cs ReadManifest)
//   delta\<rel>       Delta patches for files that differ from the release
//   files\<rel>       plain copies of files the release doesn't have
// Every DLL in the release's top folder is listed as "need", because the mod exe was linked
// against exactly those. Paths under resources\xvt_* are never included (they come from the
// player's own X-Wing vs TIE Fighter install).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

static class MakePatch {
    public static string Sha(byte[] b) {
        using (var s = SHA256.Create()) return BitConverter.ToString(s.ComputeHash(b)).Replace("-", "").ToLowerInvariant();
    }

    static Dictionary<string, string> Files(string dir) {
        dir = Path.GetFullPath(dir).TrimEnd('\\');
        return Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
            .ToDictionary(f => f.Substring(dir.Length + 1), f => f, StringComparer.OrdinalIgnoreCase);
    }

    static int Main(string[] a) {
        if (a.Length != 4) { Console.Error.WriteLine("usage: makepatch <release dir> <mod dir> <out dir> <release name>"); return 2; }
        var rel = Files(a[0]); var mod = Files(a[1]);
        string outDir = a[2];
        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);
        var lines = new List<string> { "release\t" + a[3] };
        long deltaBytes = 0, addBytes = 0;
        foreach (var r in mod.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)) {
            if (r.StartsWith(@"resources\xvt_", StringComparison.OrdinalIgnoreCase)) continue;
            byte[] nw = File.ReadAllBytes(mod[r]);
            string newSha = Sha(nw);
            string op;
            if (rel.TryGetValue(r, out op)) {
                byte[] old = File.ReadAllBytes(op);
                string oldSha = Sha(old);
                if (oldSha == newSha) continue;
                byte[] d = Delta.Create(old, nw);
                byte[] check = Delta.Apply(old, d);
                if (Sha(check) != newSha) throw new Exception("delta round trip failed for " + r);
                string dp = Path.Combine(outDir, "delta", r);
                Directory.CreateDirectory(Path.GetDirectoryName(dp));
                File.WriteAllBytes(dp, d);
                deltaBytes += d.Length;
                lines.Add("patch\t" + r + "\t" + oldSha + "\t" + newSha);
                Console.WriteLine("patch {0} ({1:N0} -> {2:N0} bytes, delta {3:N0})", r, old.Length, nw.Length, d.Length);
            } else {
                string fp = Path.Combine(outDir, "files", r);
                Directory.CreateDirectory(Path.GetDirectoryName(fp));
                File.WriteAllBytes(fp, nw);
                addBytes += nw.Length;
                lines.Add("add\t" + r + "\t" + newSha);
                Console.WriteLine("add   {0} ({1:N0} bytes)", r, nw.Length);
            }
        }
        foreach (var r in rel.Keys.Where(k => !k.Contains("\\") && k.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).OrderBy(k => k)) {
            string sha = Sha(File.ReadAllBytes(rel[r]));
            if (mod.ContainsKey(r) && Sha(File.ReadAllBytes(mod[r])) != sha) continue;   // patched above
            lines.Add("need\t" + r + "\t" + sha);
        }
        File.WriteAllLines(Path.Combine(outDir, "manifest.txt"), lines, new UTF8Encoding(false));
        Console.WriteLine("deltas {0:N0} bytes, new files {1:N0} bytes", deltaBytes, addBytes);
        return 0;
    }
}
