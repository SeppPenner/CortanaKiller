using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

public partial class Main : Form
{
    public Main()
    {
        InitializeComponent();
        Visible = false;
        while (true)
            try
            {
                Process.GetProcesses()
                    .Where(x => x.ProcessName.ToLower().Contains("searchui"))
                    .ToList()
                    .ForEach(x => x.Kill());
            }
            catch
            {
                // ignored
            }
        // ReSharper disable once FunctionNeverReturns
    }
}