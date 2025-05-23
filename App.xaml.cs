using System.Windows;

namespace HandleIt
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length != 1)
            {
                MessageBox.Show("Usage: HandleIt.exe <process_name>", "Error");
                Shutdown();
                return;
            }

            var window = new MainWindow();
            window.Show();
        }
    }
}