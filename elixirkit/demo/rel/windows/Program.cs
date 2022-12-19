namespace Demo;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new App());
    }
}

public class App : Form
{
    private NotifyIcon trayIcon;
    private ElixirKit.Release release;

    public App()
    {
        release = new ElixirKit.Release();

        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Text = "Demo";
        FormClosing += HandleFormClosing;
        Button button = new Button();
        button.Location = new Point(50, 50);
        button.Size = new Size(200, 80);
        button.Text = "Press me!";
        button.Click += HandleButtonClicked;
        Controls.Add(button);

        ContextMenuStrip menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Exit", null, HandleExitClicked);
        Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)!;

        trayIcon = new NotifyIcon()
        {
            Text = "Demo",
            Visible = true,
            Icon = icon,
            ContextMenuStrip = menu
        };
        trayIcon.Click += HandleIconClicked;
    }

    private void HandleFormClosing(object? sender, FormClosingEventArgs e)
    {
        trayIcon.Visible = false;
    }

    private void HandleButtonClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("Button pressed!");
    }

    private void HandleIconClicked(object? sender, EventArgs e)
    {
        MouseEventArgs mouseEventArgs = (MouseEventArgs)e;

        if (mouseEventArgs.Button == MouseButtons.Left) {
            Console.WriteLine("Notify Icon clicked!");
        }
    }

    private void HandleExitClicked(object? sender, EventArgs e)
    {
        trayIcon.Visible = false;
        Application.Exit();
    }
}
