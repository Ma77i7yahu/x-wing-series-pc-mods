// Drives an installer exe by reflection: harness.exe <installer.exe> <game folder> <xvt folder or -> <install|restore>
// Message boxes are answered with Enter (OK / the default Yes) by a watcher thread, and their text is printed.
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

static class Harness {
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc f, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h, EnumProc f, IntPtr l);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
    delegate bool EnumProc(IntPtr h, IntPtr l);

    static void Watch() {
        uint me = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        while (true) {
            Thread.Sleep(300);
            EnumWindows((h, l) => {
                uint pid; GetWindowThreadProcessId(h, out pid);
                var c = new StringBuilder(64); GetClassName(h, c, 64);
                if (pid != me || c.ToString() != "#32770" || !IsWindowVisible(h)) return true;
                var text = new StringBuilder();
                EnumChildWindows(h, (ch, l2) => {
                    var cc = new StringBuilder(64); GetClassName(ch, cc, 64);
                    if (cc.ToString() == "Static") { var t = new StringBuilder(4096); GetWindowText(ch, t, 4096); if (t.Length > 0) text.AppendLine(t.ToString()); }
                    return true;
                }, IntPtr.Zero);
                Console.WriteLine("[dialog] " + text.ToString().Trim().Replace("\r\n", " | "));
                SendMessage(h, 0x0111 /*WM_COMMAND*/, (IntPtr)6 /*IDYES*/, IntPtr.Zero);
                SendMessage(h, 0x0111, (IntPtr)1 /*IDOK*/, IntPtr.Zero);
                return true;
            }, IntPtr.Zero);
        }
    }

    [STAThread]
    static int Main(string[] a) {
        var asm = Assembly.LoadFrom(a[0]);
        var t = asm.GetType("InstallerForm");
        var form = (Form)Activator.CreateInstance(t, true);
        var bf = BindingFlags.Instance | BindingFlags.NonPublic;
        ((TextBox)t.GetField("folder", bf).GetValue(form)).Text = a[1];
        t.GetMethod("LoadFolder", bf).Invoke(form, null);
        var xvt = (TextBox)t.GetField("xvtFolder", bf).GetValue(form);
        xvt.Text = a[2] == "-" ? "" : a[2];
        var ck = t.GetMethod("CheckXvt", bf); ck.Invoke(form, null);
        Console.WriteLine("[status] " + ((Label)t.GetField("status", bf).GetValue(form)).Text);
        Console.WriteLine("[xvt] " + ((Label)t.GetField("xvtStatus", bf).GetValue(form)).Text);
        var w = new Thread(Watch) { IsBackground = true }; w.Start();
        form.Show();
        t.GetMethod(a[3] == "install" ? "DoInstall" : "DoRestore", bf).Invoke(form, null);
        Console.WriteLine("[status after] " + ((Label)t.GetField("status", bf).GetValue(form)).Text);
        form.Close();
        return 0;
    }
}
