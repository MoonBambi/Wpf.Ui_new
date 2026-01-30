// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.Effects;
using Wpf.Ui.Gallery.Models;
using Wpf.Ui.Gallery.ViewModels.Pages.Text;

namespace Wpf.Ui.Gallery.Views.Pages.Text;

public partial class TextPage : INavigableView<TextViewModel>
{
    private readonly INavigationService _navigationService;
    private SnowflakeEffect? _snowflake;
    private bool _terminalRowInitialized;
    private bool _isTerminalCollapsed;

    public TextViewModel ViewModel { get; }

    public TextPage(TextViewModel viewModel, INavigationService navigationService)
    {
        ViewModel = viewModel;
        DataContext = this;
        _navigationService = navigationService;

        InitializeComponent();
        CommandsPresenter.SetValue(
            Wpf.Ui.Gallery.Controls.GalleryNavigationPresenter.TemplateButtonCommandProperty,
            new Wpf.Ui.Input.RelayCommand<NavigationCard?>(async navigationCard =>
            {
                if (navigationCard is null)
                {
                    return;
                }

                await ViewModel.RunCommandAsync(navigationCard);
            })
        );
        Loaded += HandleLoaded;
        Unloaded += HandleUnloaded;
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        INavigationView? navigationControl = _navigationService.GetNavigationControl();
        if (
            navigationControl?.BreadcrumbBar != null
            && navigationControl.BreadcrumbBar.Visibility != Visibility.Collapsed
        )
        {
            navigationControl.BreadcrumbBar.SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
        }

        INavigationViewItem? selectedItem = navigationControl?.SelectedItem;
        if (selectedItem != null)
        {
            string? newTitle = selectedItem.Content?.ToString();
            if (MainTitle.Text != newTitle)
            {
                MainTitle.SetCurrentValue(System.Windows.Controls.TextBlock.TextProperty, newTitle);
            }

            if (selectedItem.Icon is SymbolIcon selectedIcon && MainSymbolIcon.Symbol != selectedIcon.Symbol)
            {
                MainSymbolIcon.SetCurrentValue(SymbolIcon.SymbolProperty, selectedIcon.Symbol);
            }
        }

        _snowflake ??= new(MainCanvas);
        _snowflake.Start();
    }

    private void HandleUnloaded(object sender, RoutedEventArgs e)
    {
        INavigationView? navigationControl = _navigationService.GetNavigationControl();
        if (
            navigationControl?.BreadcrumbBar != null
            && navigationControl.BreadcrumbBar.Visibility != Visibility.Visible
        )
        {
            navigationControl.BreadcrumbBar.SetCurrentValue(VisibilityProperty, Visibility.Visible);
        }

        _snowflake?.Stop();
        _snowflake = null;
        Loaded -= HandleLoaded;
        Unloaded -= HandleUnloaded;
    }

    private void TerminalHeaderGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_terminalRowInitialized)
        {
            return;
        }

        TerminalRow.Height = new GridLength(e.NewSize.Height + 10, GridUnitType.Pixel);
        _terminalRowInitialized = true;
        _isTerminalCollapsed = true;
    }

    private void ToggleTerminalButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_terminalRowInitialized)
        {
            return;
        }

        if (_isTerminalCollapsed)
        {
            TerminalRow.Height = new GridLength(1, GridUnitType.Star);
            _isTerminalCollapsed = false;
        }
        else
        {
            var headerHeight = TerminalHeaderGrid.ActualHeight;
            TerminalRow.Height = new GridLength(headerHeight + 10, GridUnitType.Pixel);
            _isTerminalCollapsed = true;
        }
    }
}
