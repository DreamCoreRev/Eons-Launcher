using System.Windows;
using System.Windows.Input;

namespace EonsLauncher.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = (MainViewModel)DataContext;

        _vm.Logs.CollectionChanged += (_, _) =>
        {
            LogScrollViewer.ScrollToBottom();
        };
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await _vm.InitializeAsync();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.IsDownloading)
        {
            var result = System.Windows.MessageBox.Show(
                "Un téléchargement est en cours.\nVoulez-vous quand même fermer le launcher ?",
                "Fermeture", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            _vm.CancelDownload();
        }
        Close();
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private async void Play_Click(object sender, RoutedEventArgs e) =>
        await Task.Run(() => System.Windows.Application.Current.Dispatcher.Invoke(_vm.LaunchGame));

    private async void Update_Click(object sender, RoutedEventArgs e) =>
        await _vm.StartDownloadAsync();

    private void Cancel_Click(object sender, RoutedEventArgs e) =>
        _vm.CancelDownload();

    private async void Refresh_Click(object sender, RoutedEventArgs e) =>
        await _vm.CheckFilesAsync();

    private void Website_Click(object sender, RoutedEventArgs e)  => _vm.OpenWebsite();
    private void Register_Click(object sender, RoutedEventArgs e) => _vm.OpenRegister();
    private void BrowsePath_Click(object sender, RoutedEventArgs e) => _vm.BrowseClientPath();
}
