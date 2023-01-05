namespace Livebook;

static class LivebookMain
{
    [STAThread]
    static void Main(string[] args)
    {
        ElixirKit.Utils.DebugAttachConsole();

        var api = new ElixirKit.API(id: "dev.livebook.Livebook");

        if (api.MainInstance)
        {
            api.Start(name: "app", exited: (sender, args) =>
            {
                Application.Exit();
            });

            Application.ApplicationExit += (sender, args) =>
            {
                api.Stop();
            };

            ApplicationConfiguration.Initialize();
            Application.Run(new LivebookApp(api));
        }
        else
        {
            if (args.Length == 1 && args[0].StartsWith("open:"))
            {
                var url = new System.Uri(args[0].Remove(0, "open:".Length));
                api.Publish("open", url.AbsoluteUri);
            }
            else
            {
                api.Publish("open", "");
            }
        }
    }
}

class LivebookApp : ApplicationContext
{
    private ElixirKit.API api;
    private NotifyIcon notifyIcon;

    public LivebookApp(ElixirKit.API api)
    {
        ThreadExit += threadExit;

        this.api = api;
        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, openClicked);
        menu.Items.Add("Quit", null, quitClicked);
        notifyIcon = new NotifyIcon()
        {
            Text = "Livebook",
            Visible = true,
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)!,
            ContextMenuStrip = menu
        };
        notifyIcon.Click += notifyIconClicked;
    }

    private void threadExit(object? sender, EventArgs e)
    {
        notifyIcon.Visible = false;
    }

    private void notifyIconClicked(object? sender, EventArgs e)
    {
        MouseEventArgs mouseEventArgs = (MouseEventArgs)e;

        if (mouseEventArgs.Button == MouseButtons.Left)
        {
            open();
        }
    }

    private void openClicked(object? sender, EventArgs e)
    {
        open();
    }

    private void quitClicked(object? sender, EventArgs e)
    {
        notifyIcon.Visible = false;
        Application.Exit();
    }

    private void open() {
        api.Publish("open", "");
    }
}
