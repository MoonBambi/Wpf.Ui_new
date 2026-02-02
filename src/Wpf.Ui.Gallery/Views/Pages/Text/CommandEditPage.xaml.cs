using System.Windows;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.ControlsLookup;
using Wpf.Ui.Gallery.ViewModels.Pages.Text;

namespace Wpf.Ui.Gallery.Views.Pages.Text;

[GalleryPage("Edit command.", SymbolRegular.TextColor24)]
public partial class CommandEditPage : INavigableView<CommandEditViewModel>
{
    public CommandEditViewModel ViewModel { get; }

    public CommandEditPage(CommandEditViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is string title)
        {
            ViewModel.OnNavigatedTo(title);
        }

        DataContext = this;
        Loaded -= OnLoaded;
    }
}
