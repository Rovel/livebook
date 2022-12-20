namespace Demo;

using System.Windows.Forms;
using System.IO.Pipes;

static class Demo
{
    const String pipeName = "ElixirKit.Demo";

    [STAThread]
    static void Main(String[] args)
    {
        String? input = null;
        if (args.Length > 0)
        {
            input = args[0];
        }

        try
        {
            Console.WriteLine("I'm a server.");
            var pipe = new NamedPipeServerStream(pipeName, PipeDirection.Out, 10);
            ApplicationConfiguration.Initialize();
            Application.Run(new App(pipe, input));
        }
        catch (IOException e)
        {
            Console.WriteLine(e);
            Console.WriteLine("I'm a client.");
            // server pipe already exists, connect to it.
            /* var client = new NamedPipeClientStream(pipeName); */
            /* client.Connect(); */
            /* var writer = new StreamWriter(client); */
            /* writer.WriteLine("hello"); */
            /* writer.Flush(); */
        }
    }
}

public class App : Form
{
    private NotifyIcon trayIcon;
    private ElixirKit.Release release;

    public App(NamedPipeServerStream pipe, String? input)
    {
        /* pipe.BeginWaitForConnection(this.HandleClientConnected, pipe); */

        this.release = new ElixirKit.Release(
            exited: ReleaseExited
        );
        Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)!;

        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Text = "Demo";
        this.Icon = icon;
        this.FormClosing += this.HandleFormClosing;
        Button button = new Button();
        button.Location = new Point(50, 50);
        button.Size = new Size(200, 80);
        button.Text = $"Press me! ({input})";
        button.Click += this.HandleButtonClicked;
        this.Controls.Add(button);

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

    private void HandleClientConnected(IAsyncResult result)
    {
    }

    private void ReleaseExited(object? sender, EventArgs e)
    {
        System.Diagnostics.Process process = (System.Diagnostics.Process)sender!;
        if (process.ExitCode != 0) {
            MessageBox.Show(
                $"release exited with code: {process.ExitCode}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        Application.Exit();
    }

    private void UrlOpened(System.Uri uri)
    {
        release.Publish("dbg", uri.AbsoluteUri);
    }

    private void HandleFormClosing(object? sender, FormClosingEventArgs e)
    {
        trayIcon.Visible = false;
        release.Terminate();
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
