using WpfApplication = System.Windows.Application;
using ShutdownMode = System.Windows.ShutdownMode;

namespace WhipCursor;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        try
        {
            var app = new WpfApplication
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };

            using var controller = new WhipController(app);
            app.Run();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                ex.Message,
                "Whip Cursor could not start",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
