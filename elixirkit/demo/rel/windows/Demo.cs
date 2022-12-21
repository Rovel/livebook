namespace Demo;

using System.Windows.Forms;
using System.Threading.Tasks;

static class Demo
{
    private static Mutex? mutex = null;

    [STAThread]
    static void Main(String[] args)
    {
        bool isMainInstance = false;
        mutex = new Mutex(true, "com.example.Demo", out isMainInstance);
        var release = new ElixirKit.ReleaseScript();

        String? input = null;
        if (args.Length > 0 && !String.IsNullOrEmpty(args[0]))
        {
            input = args[0];
        }

        if (isMainInstance)
        {
            var process = release.Start();

            var t = Task.Run(() => {
                process.WaitForExit();

                if (process.ExitCode != 0) {
                    MessageBox.Show(
                        $"release exited with code: {process.ExitCode}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }

                Application.Exit();
            });

            ApplicationConfiguration.Initialize();
            Application.Run(new App(release, input));

            if (!t.IsCompleted)
            {
                Console.WriteLine($"stopping release");
                release.Stop();
            }
        }
        else
        {
            release.Publish("dbg", "reopened app");
        }
    }
}

public class App : Form
{
    private ElixirKit.ReleaseScript release;
    private NotifyIcon trayIcon;
    private Label label;

    public App(ElixirKit.ReleaseScript release, String? input)
    {
        this.release = release;
        Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)!;

        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 250);
        this.Text = "Demo";
        this.Icon = icon;
        this.FormClosing += this.HandleFormClosing;
        Button button = new Button();
        button.Location = new Point(50, 50);
        button.Size = new Size(200, 80);
        button.Text = "Press me!";
        button.Click += this.HandleButtonClicked;
        this.Controls.Add(button);
        this.label = new Label();
        this.label.Text = $"Started with: {input}";
        this.label.Location = new Point(50, 180);
        label.Size = new Size(label.PreferredWidth, label.PreferredHeight);
        this.Controls.Add(label);

        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("Exit", null, this.HandleExitClicked);

        this.trayIcon = new NotifyIcon()
        {
            Text = "Demo",
            Visible = true,
            Icon = icon,
            ContextMenuStrip = menu
        };
        this.trayIcon.Click += this.HandleIconClicked;
    }

    private void UrlOpened(System.Uri uri)
    {
        release.Publish("dbg", uri.AbsoluteUri);
    }

    private void HandleFormClosing(object? sender, FormClosingEventArgs e)
    {
        trayIcon.Visible = false;
    }

    private void HandleButtonClicked(object? sender, EventArgs e)
    {
        release.Publish("dbg", "Button pressed!");
    }

    private void HandleIconClicked(object? sender, EventArgs e)
    {
        MouseEventArgs mouseEventArgs = (MouseEventArgs)e;

        if (mouseEventArgs.Button == MouseButtons.Left) {
            release.Publish("dbg", "Notify Icon clicked!");
        }
    }

    private void HandleExitClicked(object? sender, EventArgs e)
    {
        trayIcon.Visible = false;
        Application.Exit();
    }
}
