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
    /// Initializes a new instance of the <see cref="Main"/> class.
    /// </summary>
    public Main()
    {
        this.InitializeComponent();
        this.Visible = false;

        while (true)
        {
            try
            {
                Process.GetProcesses().Where(x => x.ProcessName.ToLower().Contains("searchui")).ToList()
                    .ForEach(x => x.Kill());
            }
            catch
            {
                // ignored
            }
        }
    }
}
