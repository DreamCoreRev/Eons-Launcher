using System.Windows;

namespace EonsLauncher;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, ex) =>
        {
            System.Windows.MessageBox.Show(
                $"Erreur inattendue :\n{ex.Exception.Message}",
                "Erreur - Eons Launcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ex.Handled = true;
        };
    }
}
