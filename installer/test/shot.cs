// shot.exe <installer.exe> <out.png>: renders the installer's window to a PNG.
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

static class Shot {
    [STAThread]
    static void Main(string[] a) {
        Application.EnableVisualStyles();
        var form = (Form)Activator.CreateInstance(Assembly.LoadFrom(a[0]).GetType("InstallerForm"), true);
        form.Shown += (s, e) => {
            Application.DoEvents();
            using (var b = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(b, new Rectangle(0, 0, form.Width, form.Height)); b.Save(a[1]); }
            form.Close();
        };
        Application.Run(form);
    }
}
