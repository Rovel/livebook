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

// public class App : ApplicationContext
public class App : Form
{
    private NotifyIcon trayIcon;

    public App()
    {
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Text = "Demo";
        Button button = new Button();
        button.Location = new Point(50, 50);
        button.Size = new Size(200, 80);
        button.Text = "Press me!";
        button.Click += this.HandleButtonClick;
        this.Controls.Add(button);

        ContextMenuStrip menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Exit", null, this.HandleExit);
        String iconPath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;

        trayIcon = new NotifyIcon()
        {
            Text = "Demo",
            Visible = true,
            Icon = Icon.ExtractAssociatedIcon(iconPath),
            ContextMenuStrip = menu
        };
        trayIcon.Click += this.HandleIconClick;
    }

    void HandleIconClick(object? sender, EventArgs e)
    {
        MouseEventArgs mouseEventArgs = (MouseEventArgs)e;

        if (mouseEventArgs.Button == MouseButtons.Left) {
            trayIcon.Visible = false;
            Application.Exit();
        }
    }

    void HandleExit(object? sender, EventArgs e)
    {
        trayIcon.Visible = false;
        Application.Exit();
    }

    void HandleButtonClick(object? sender, EventArgs e)
    {
        Console.WriteLine("Button pressed!");
    }
}
