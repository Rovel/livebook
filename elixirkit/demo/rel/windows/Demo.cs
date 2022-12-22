namespace Demo;

using System.Windows.Forms;

static class Demo
{
    [STAThread]
    static void Main(String[] args)
    {
        string? url = null;
        if (args.Length > 0 && !String.IsNullOrEmpty(args[0]))
        {
            var obj = new System.Uri(args[0]);
            url = obj.AbsoluteUri;
        }

        var api = new ElixirKit.API("com.example.Demo");

        if (api.IsMainInstance)
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new App(api, url));
            api.Stop();
        }
        else
        {
            if (url != null)
            {
                api.Publish("echo", "Got url: " + url);
            }
        }
    }
}

public class App : Form
{
    private ElixirKit.Release release;
    private NotifyIcon trayIcon;
    private TextBox label;

    public App(ElixirKit.API api, String? input)
    {
        this.release = api.StartRelease(
            eventHandler: this.handleReleaseEvent,
            exitHandler: this.handleReleaseExit
        );

        Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)!;

        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 400);
        this.Text = "Demo";
        this.Icon = icon;
        this.FormClosing += this.handleFormClosing;
        Button button = new Button();
        button.Location = new Point(50, 50);
        button.Size = new Size(200, 80);
        button.Text = "Press me!";
        button.Click += this.handleButtonClicked;
        this.Controls.Add(button);
        this.label = new TextBox();
        this.label.Multiline = true;
        this.label.ReadOnly = true;
        this.label.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.label.Text = $"Started with: {input}";
        this.label.Location = new Point(50, 180);
        this.label.Size = new Size(700, 200);
        this.Controls.Add(label);

        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("Exit", null, this.handleExitClicked);
        this.trayIcon = new NotifyIcon()
        {
            Text = "Demo",
            Visible = true,
            Icon = icon,
            ContextMenuStrip = menu
        };
        this.trayIcon.Click += this.handleIconClicked;
    }

    private void handleReleaseEvent(string name, string data)
    {
        if (name == "log")
        {
            log(data);
        }
        else
        {
            throw new ApplicationException($"unknown event {name}");
        }
    }

    private void handleReleaseExit(int code)
    {
        if (code != 0)
        {
            MessageBox.Show(
                $"Release exited with code: {code}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        Application.Exit();
    }

    private void handleFormClosing(object? sender, FormClosingEventArgs e)
    {
        trayIcon.Visible = false;
    }

    private void handleButtonClicked(object? sender, EventArgs e)
    {
        var data = "Button pressed!";
        log(data);
        this.release.Publish("echo", data);
    }

    private void handleIconClicked(object? sender, EventArgs e)
    {
        MouseEventArgs mouseEventArgs = (MouseEventArgs)e;

        if (mouseEventArgs.Button == MouseButtons.Left)
        {
            this.release.Publish("echo", "Notify Icon clicked!");
        }
    }

    private void handleExitClicked(object? sender, EventArgs e)
    {
        this.trayIcon.Visible = false;
        Application.Exit();
    }

    private void log(String data)
    {
        var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
        var line = $"{timestamp}Z [client] {data}";
        Console.WriteLine(line);
        this.label.AppendText("\r\n" + line);
    }

}
