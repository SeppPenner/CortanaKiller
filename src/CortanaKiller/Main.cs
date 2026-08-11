// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Main.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   Defines the Main type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CortanaKiller;

/// <summary>
/// The main form.
/// </summary>
public partial class Main : Form
{
    /// <summary>
    /// The name of the process that hosts the Cortana background task. Matched as a substring and
    /// case insensitively, the process is called SearchUI.exe on Windows 10.
    /// </summary>
    private const string CortanaProcessName = "searchui";

    /// <summary>
    /// The time between two kill attempts in milliseconds.
    /// </summary>
    private const int KillIntervalInMilliseconds = 1000;

    /// <summary>
    /// The timer that triggers the kill attempts.
    /// </summary>
    private readonly System.Windows.Forms.Timer killTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Main"/> class.
    /// </summary>
    public Main()
    {
        this.InitializeComponent();

        // The timer is added to the container of the form so that the generated Dispose method disposes it.
        this.components ??= new Container();
        this.killTimer = new System.Windows.Forms.Timer(this.components)
        {
            Interval = KillIntervalInMilliseconds
        };
        this.killTimer.Tick += this.KillTimerTick;
        this.killTimer.Start();

        this.KillCortanaProcesses();
    }

    /// <summary>
    /// Keeps the form hidden. The application has no user interface at all, but it needs a running
    /// message loop for the timer, and <see cref="Application.Run(Form)"/> would show the form.
    /// </summary>
    /// <param name="value">True if the form should become visible; ignored on purpose.</param>
    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(false);
    }

    /// <summary>
    /// Handles the tick of the <see cref="killTimer"/>.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void KillTimerTick(object? sender, EventArgs e)
    {
        this.KillCortanaProcesses();
    }

    /// <summary>
    /// Kills all running processes that host the Cortana background task.
    /// </summary>
    private void KillCortanaProcesses()
    {
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                if (!process.ProcessName.Contains(CortanaProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    process.Kill();
                }
                catch
                {
                    // Killing fails for processes of other users and for processes that exited in
                    // the meantime. The next tick tries again.
                }
            }
        }
    }
}
