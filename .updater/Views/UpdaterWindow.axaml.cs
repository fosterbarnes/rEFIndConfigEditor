using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using rEFIndConfigEditor.Updater.ViewModels;

namespace rEFIndConfigEditor.Updater.Views;

public partial class UpdaterWindow : Window
{
    private UpdaterWindowViewModel ViewModel => (UpdaterWindowViewModel)DataContext!;

    public UpdaterWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ViewModel.RequestClose += Close;
        Closing += (_, _) => ViewModel.OnClosing();
        await ViewModel.OnShownAsync();
    }
}
