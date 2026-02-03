// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Extensions.Localization;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.Resources;
using Wpf.Ui.Gallery.Views.Pages;
using Wpf.Ui.Gallery.Views.Pages.BasicInput;
using Wpf.Ui.Gallery.Views.Pages.Collections;
using Wpf.Ui.Gallery.Views.Pages.DateAndTime;
using Wpf.Ui.Gallery.Views.Pages.DesignGuidance;
using Wpf.Ui.Gallery.Views.Pages.DialogsAndFlyouts;
using Wpf.Ui.Gallery.Views.Pages.Layout;
using Wpf.Ui.Gallery.Views.Pages.Media;
using Wpf.Ui.Gallery.Views.Pages.Navigation;
using Wpf.Ui.Gallery.Views.Pages.OpSystem;
using Wpf.Ui.Gallery.Views.Pages.StatusAndInfo;
using Wpf.Ui.Gallery.Views.Pages.Text;
using Wpf.Ui.Gallery.Views.Pages.Windows;

namespace Wpf.Ui.Gallery.ViewModels.Windows;

public partial class MainWindowViewModel(IStringLocalizer<Translations> localizer) : ViewModel
{
    [ObservableProperty]
    private string _applicationTitle = localizer["WPF UI Gallery"];

    [ObservableProperty]
    private ObservableCollection<object> _menuItems =
    [
        new NavigationViewItem("元件管理", SymbolRegular.Apps24, typeof(TextPage))
        {
            MenuItemsSource = new object[]
            {
                new NavigationViewItem("元件制作", typeof(TextBoxPage)),
            },
        },
    ];

    [ObservableProperty]
    private ObservableCollection<object> _footerMenuItems =
    [
        new NavigationViewItem("Settings", SymbolRegular.Settings24, typeof(SettingsPage)),
    ];

    [ObservableProperty]
    private ObservableCollection<Control> _trayMenuItems =
    [
        new Wpf.Ui.Controls.MenuItem()
        {
            Header = "Home",
            Tag = "tray_home",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
        },
        new Wpf.Ui.Controls.MenuItem()
        {
            Header = "Settings",
            Tag = "tray_settings",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
        },
        new Separator(),
        new Wpf.Ui.Controls.MenuItem()
        {
            Header = "Close",
            Tag = "tray_close",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Dismiss24 },
        },
    ];
}
