// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.Effects;
using Wpf.Ui.Gallery.Models;
using Wpf.Ui.Gallery.ViewModels.Pages.Text;

namespace Wpf.Ui.Gallery.Views.Pages.Text;

public partial class TextPage : INavigableView<TextViewModel>
{
    private readonly INavigationService _navigationService;
    private bool _terminalRowInitialized;
    private bool _isTerminalCollapsed;
    private double _collapsedTerminalHeight;
    private double _expandedTerminalHeight;
    private bool _terminalThemeInitialized;
    private bool _terminalDocumentUpdatePending;
    private Color _commandBrushDarkColor;
    private Color _outputBrushDarkColor;
    private Color _errorBrushDarkColor;
    private Color _systemBrushDarkColor;

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
        CommandsPresenter.SetValue(
            Wpf.Ui.Gallery.Controls.GalleryNavigationPresenter.EditButtonCommandProperty,
            new Wpf.Ui.Input.RelayCommand<NavigationCard?>(navigationCard =>
            {
                if (navigationCard?.Name is null)
                {
                    return;
                }

                _navigationService.Navigate(
                    typeof(CommandEditPage),
                    navigationCard.Name
                );
            })
        );
        CommandsPresenter.SetValue(
            Wpf.Ui.Gallery.Controls.GalleryNavigationPresenter.DeleteButtonCommandProperty,
            new Wpf.Ui.Input.RelayCommand<NavigationCard?>(navigationCard =>
            {
                ViewModel.DeleteCommand(navigationCard);
            })
        );
        Loaded += HandleLoaded;
        Unloaded += HandleUnloaded;
        if (ViewModel.TerminalLines is INotifyCollectionChanged notifyCollection)
        {
            notifyCollection.CollectionChanged += TerminalLines_OnCollectionChanged;
        }
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
        }

        InitializeTerminalTheme();
        ApplyTerminalTheme(ApplicationThemeManager.GetAppTheme());
        ApplicationThemeManager.Changed += OnApplicationThemeChanged;
        UpdateTerminalDocument();
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

        ApplicationThemeManager.Changed -= OnApplicationThemeChanged;
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

    private void TerminalClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetTerminal();
    }

    private void TerminalLines_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_terminalDocumentUpdatePending)
        {
            return;
        }

        _terminalDocumentUpdatePending = true;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            _terminalDocumentUpdatePending = false;
            UpdateTerminalDocument();
        }), DispatcherPriority.Background);
    }

    private void OnApplicationThemeChanged(ApplicationTheme currentApplicationTheme, Color systemAccent)
    {
        ApplyTerminalTheme(currentApplicationTheme);
    }

    private void InitializeTerminalTheme()
    {
        if (_terminalThemeInitialized)
        {
            return;
        }

        if (Resources["CommandBrush"] is SolidColorBrush commandBrush)
        {
            _commandBrushDarkColor = commandBrush.Color;
        }

        if (Resources["OutputBrush"] is SolidColorBrush outputBrush)
        {
            _outputBrushDarkColor = outputBrush.Color;
        }

        if (Resources["ErrorBrush"] is SolidColorBrush errorBrush)
        {
            _errorBrushDarkColor = errorBrush.Color;
        }

        if (Resources["SystemBrush"] is SolidColorBrush systemBrush)
        {
            _systemBrushDarkColor = systemBrush.Color;
        }

        _terminalThemeInitialized = true;
    }

    private void ApplyTerminalTheme(ApplicationTheme theme)
    {
        if (!_terminalThemeInitialized)
        {
            InitializeTerminalTheme();
        }

        if (Resources["CommandBrush"] is not SolidColorBrush commandBrush
            || Resources["OutputBrush"] is not SolidColorBrush outputBrush
            || Resources["ErrorBrush"] is not SolidColorBrush errorBrush
            || Resources["SystemBrush"] is not SolidColorBrush systemBrush)
        {
            return;
        }

        if (theme == ApplicationTheme.Light || theme == ApplicationTheme.HighContrast)
        {
            if (Resources["CommandBrushLight"] is SolidColorBrush commandLight)
            {
                commandBrush.Color = commandLight.Color;
            }

            if (Resources["OutputBrushLight"] is SolidColorBrush outputLight)
            {
                outputBrush.Color = outputLight.Color;
            }

            if (Resources["ErrorBrushLight"] is SolidColorBrush errorLight)
            {
                errorBrush.Color = errorLight.Color;
            }

            if (Resources["SystemBrushLight"] is SolidColorBrush systemLight)
            {
                systemBrush.Color = systemLight.Color;
            }

            if (Resources["TerminalBackgroundLight"] is SolidColorBrush backgroundLight)
            {
                TerminalRichTextBox.Background = backgroundLight;
            }
        }
        else
        {
            commandBrush.Color = _commandBrushDarkColor;
            outputBrush.Color = _outputBrushDarkColor;
            errorBrush.Color = _errorBrushDarkColor;
            systemBrush.Color = _systemBrushDarkColor;
            TerminalRichTextBox.ClearValue(BackgroundProperty);
        }
    }

    private void UpdateTerminalDocument()
    {
        if (TerminalRichTextBox.Document == null)
        {
            TerminalRichTextBox.Document = new FlowDocument();
        }

        var document = TerminalRichTextBox.Document;

        document.FontFamily = TerminalRichTextBox.FontFamily;

        if (TryFindResource("TextFillColorSecondaryBrush") is Brush baseForeground)
        {
            document.Foreground = baseForeground;
        }

        document.Blocks.Clear();

        var paragraph = new Paragraph();
        document.Blocks.Add(paragraph);

        var lines = ViewModel.TerminalLines;

        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            var line = lines[i] ?? string.Empty;

            AddColoredLine(paragraph, line);
        }
    }

    private void AddColoredLine(Paragraph paragraph, string line)
    {
        const string separator = "] > ";

        var separatorIndex = line.IndexOf(separator, StringComparison.Ordinal);

        if (separatorIndex >= 0)
        {
            var prefixEnd = separatorIndex + separator.Length;

            var prefixText = line.Substring(0, prefixEnd);
            var commandText =
                line.Length > prefixEnd
                    ? line.Substring(prefixEnd)
                    : string.Empty;

            paragraph.Inlines.Add(
                new Run(prefixText)
                {
                    Foreground = GetTerminalBrush("SystemBrush"),
                }
            );

            if (!string.IsNullOrEmpty(commandText))
            {
                paragraph.Inlines.Add(
                    new Run(commandText)
                    {
                        Foreground = GetTerminalBrush("CommandBrush"),
                    }
                );
            }

            return;
        }

        if (
            line.StartsWith("终端启动失败", StringComparison.Ordinal)
            || line == "未找到匹配的命令"
            || line == "命令为空"
        )
        {
            paragraph.Inlines.Add(
                new Run(line)
                {
                    Foreground = GetTerminalBrush("ErrorBrush"),
                }
            );

            return;
        }

        paragraph.Inlines.Add(
            new Run(line)
            {
                Foreground = GetTerminalBrush("OutputBrush"),
            }
        );
    }

    private Brush GetTerminalBrush(string resourceKey)
    {
        if (Resources[resourceKey] is SolidColorBrush brush)
        {
            return brush;
        }

        return TerminalRichTextBox.Foreground;
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

        void OnStoryboardCompleted(object? sender, EventArgs e)
        {
            storyboard.Completed -= OnStoryboardCompleted;

            TerminalRow.BeginAnimation(System.Windows.Controls.RowDefinition.HeightProperty, null);

            double targetHeight = _isTerminalCollapsed
                ? _collapsedTerminalHeight
                : _expandedTerminalHeight;

            TerminalRow.Height = new GridLength(targetHeight, GridUnitType.Pixel);
        }

        storyboard.Completed += OnStoryboardCompleted;
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
