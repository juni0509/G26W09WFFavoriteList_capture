using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace G26W09WFFavoriteList;

// 캡처 전용 실행기: 실제 Form1을 띄우고 케이스별로 조작한 뒤 화면을 PNG로 저장한다.
static class CaptureProgram {
    const string OutDir = @"C:\Users\qweju\source\captures";

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string cls, string title);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h, int attr, out RECT r, int size);
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }

    static Rectangle Frame(IntPtr h) {
        DwmGetWindowAttribute(h, 9, out RECT r, Marshal.SizeOf<RECT>());
        return Rectangle.FromLTRB(r.L, r.T, r.R, r.B);
    }

    static void Shot(Rectangle area, string name) {
        area.Inflate(12, 12);
        area.Intersect(Screen.PrimaryScreen!.Bounds);
        using var bmp = new Bitmap(area.Width, area.Height);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(area.Location, Point.Empty, area.Size);
        bmp.Save(Path.Combine(OutDir, name + ".png"), ImageFormat.Png);
    }

    static void ShotForm(Form f, string name) => Shot(Frame(f.Handle), name);

    static T Ctl<T>(Form f, string name) where T : Control => (T)f.Controls.Find(name, true)[0];

    // 모달 메시지 창이 뜨면 잠시 뒤 캡처하고, 지정한 버튼 ID로 닫는다.
    static void WatchDialog(Form f, string? shotName, int buttonId) {
        var t = new System.Windows.Forms.Timer { Interval = 1500 };
        t.Tick += (s, e) => {
            t.Stop();
            IntPtr dlg = FindWindow("#32770", null);
            if (dlg != IntPtr.Zero) {
                if (shotName != null)
                    Shot(Rectangle.Union(Frame(f.Handle), Frame(dlg)), shotName);
                SendMessage(dlg, 0x0111, (IntPtr)buttonId, IntPtr.Zero); // WM_COMMAND
            }
            t.Dispose();
        };
        t.Start();
    }

    [STAThread]
    static void Main(string[] args) {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        Directory.CreateDirectory(OutDir);
        bool stage2 = args.Contains("stage2");
        if (!stage2 && File.Exists("favorites.db")) File.Delete("favorites.db");

        ApplicationConfiguration.Initialize();
        var form = new Form1 { StartPosition = FormStartPosition.Manual, Location = new Point(60, 40), TopMost = true };
        form.Shown += async (s, e) => {
            try {
                if (stage2) await Stage2(form); else await Stage1(form);
            } catch (Exception ex) {
                File.WriteAllText(Path.Combine(OutDir, "error.txt"), ex.ToString());
            }
            form.Close();
        };
        Application.Run(form);
    }

    static async Task Stage1(Form1 f) {
        var filter = Ctl<TextBox>(f, "filterText");
        var list = Ctl<ListBox>(f, "mainContainer");
        var title = Ctl<TextBox>(f, "titleField");
        var desc = Ctl<TextBox>(f, "descField");
        var link = Ctl<TextBox>(f, "linkField");
        var add = Ctl<Button>(f, "submitAction");
        var drop = Ctl<Button>(f, "dropAction");

        await Task.Delay(2500);
        ShotForm(f, "01_start");

        list.SelectedIndex = 0;
        await Task.Delay(6000);
        ShotForm(f, "02_select");

        filter.Text = "버스"; await Task.Delay(800);
        ShotForm(f, "03_search_name");
        filter.Text = "개운"; await Task.Delay(800);
        ShotForm(f, "04_search_reason");
        filter.Text = "zzzz"; await Task.Delay(800);
        ShotForm(f, "05_search_none");
        filter.Text = ""; await Task.Delay(800);

        title.Text = "산책하며 사진 찍기";
        desc.Text = "걷다가 마음에 드는 풍경을 남겨요";
        link.Text = "https://loremflickr.com/400/400/photography,street?lock=12";
        await Task.Delay(500);
        ShotForm(f, "06a_add_filled");
        add.PerformClick();
        await Task.Delay(500);
        list.SelectedIndex = 0;
        await Task.Delay(6000);
        ShotForm(f, "06b_add_done");

        title.Text = "혼자 카페 가기";
        desc.Text = "조용히 앉아 있으면 마음이 편해져요";
        link.Text = "";
        add.PerformClick();
        await Task.Delay(500);
        list.SelectedIndex = 0;
        await Task.Delay(1500);
        ShotForm(f, "07_add_no_url");

        title.Text = ""; desc.Text = ""; link.Text = "";
        WatchDialog(f, "08_empty_warning", 1); // IDOK
        add.PerformClick();
        await Task.Delay(500);

        list.SelectedIndex = 0;
        await Task.Delay(800);
        WatchDialog(f, "09_delete_confirm", 7); // IDNO
        drop.PerformClick();
        await Task.Delay(800);
        ShotForm(f, "10_delete_no");

        WatchDialog(f, null, 6); // IDYES
        drop.PerformClick();
        await Task.Delay(800);
        ShotForm(f, "11_delete_yes");

        File.WriteAllText(Path.Combine(OutDir, "stage1.done"), "ok");
        Process.Start(new ProcessStartInfo(Application.ExecutablePath, "stage2") { UseShellExecute = false });
    }

    static async Task Stage2(Form1 f) {
        var list = Ctl<ListBox>(f, "mainContainer");
        await Task.Delay(2500);
        list.SelectedIndex = 0;
        await Task.Delay(6000);
        ShotForm(f, "12_restart");
        File.WriteAllText(Path.Combine(OutDir, "stage2.done"), "ok");
    }
}
