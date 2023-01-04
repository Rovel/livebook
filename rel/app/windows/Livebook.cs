namespace Livebook;

static class LivebookMain
{
    [STAThread]
    static void Main()
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

            api.Publish("log", "Hello from Windows Forms!");

            ApplicationConfiguration.Initialize();
            Application.Run(new LivebookForm());
        }
        else
        {
            api.Publish("log", "Hello from another instance!");
        }
    }
}

class LivebookForm : Form
{
    public LivebookForm()
    {
        InitializeComponent();
    }

    // WinForms boilerplate below.

    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Text = "Livebook";
    }

    #endregion
}
