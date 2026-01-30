// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Linq;
using System.Windows.Media.Animation;
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
    private double _collapsedTerminalHeight;
    private double _expandedTerminalHeight;

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

                EnsureTerminalExpanded();
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

        double collapsedHeight = e.NewSize.Height + 10;
        TerminalRow.Height = new GridLength(collapsedHeight, GridUnitType.Pixel);
        _collapsedTerminalHeight = collapsedHeight;
        _expandedTerminalHeight = collapsedHeight * 5;
        _terminalRowInitialized = true;
        _isTerminalCollapsed = true;
    }

    private void ToggleTerminalButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        ToggleTerminal(button);
    }

    private void TerminalRichTextBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.RichTextBox richTextBox)
        {
            richTextBox.ScrollToEnd();
        }
    }

    private void EnsureTerminalExpanded()
    {
        if (!_terminalRowInitialized || ! _isTerminalCollapsed)
        {
            return;
        }

        ToggleTerminal(TerminalToggleButton);
    }

    private void ToggleTerminal(Button button)
    {
        if (!_terminalRowInitialized)
        {
            return;
        }

        string storyboardKey = _isTerminalCollapsed ? "TerminalExpandStoryboard" : "TerminalCollapseStoryboard";
        if (FindResource(storyboardKey) is not Storyboard storyboard)
        {
            if (_isTerminalCollapsed)
            {
                TerminalRow.Height = new GridLength(_expandedTerminalHeight, GridUnitType.Pixel);
            }
            else
            {
                TerminalRow.Height = new GridLength(_collapsedTerminalHeight, GridUnitType.Pixel);
            }

            _isTerminalCollapsed = !_isTerminalCollapsed;
            UpdateTerminalToggleIcon(button);
            return;
        }

        GridLengthAnimation? animation = storyboard.Children.OfType<GridLengthAnimation>().FirstOrDefault();
        if (animation == null)
        {
            if (_isTerminalCollapsed)
            {
                TerminalRow.Height = new GridLength(_expandedTerminalHeight, GridUnitType.Pixel);
            }
            else
            {
                TerminalRow.Height = new GridLength(_collapsedTerminalHeight, GridUnitType.Pixel);
            }

            _isTerminalCollapsed = !_isTerminalCollapsed;
            UpdateTerminalToggleIcon(button);
            return;
        }

        double currentHeight = TerminalRow.ActualHeight;
        if (currentHeight <= 0)
        {
            currentHeight = _isTerminalCollapsed ? _collapsedTerminalHeight : _expandedTerminalHeight;
        }

        if (_isTerminalCollapsed)
        {
            animation.From = new GridLength(currentHeight, GridUnitType.Pixel);
            animation.To = new GridLength(_expandedTerminalHeight, GridUnitType.Pixel);
        }
        else
        {
            _expandedTerminalHeight = currentHeight;
            animation.From = new GridLength(currentHeight, GridUnitType.Pixel);
            animation.To = new GridLength(_collapsedTerminalHeight, GridUnitType.Pixel);
        }

        animation.EasingFunction = new CubicEase
        {
            EasingMode = EasingMode.EaseInOut,
        };

        storyboard.Begin(this);
        _isTerminalCollapsed = !_isTerminalCollapsed;
        UpdateTerminalToggleIcon(button);
    }

    private void UpdateTerminalToggleIcon(Button button)
    {
        SymbolRegular symbol = _isTerminalCollapsed ? SymbolRegular.ChevronUp24 : SymbolRegular.ChevronDown24;
        button.Icon = new SymbolIcon
        {
            Symbol = symbol,
        };
    }
}
