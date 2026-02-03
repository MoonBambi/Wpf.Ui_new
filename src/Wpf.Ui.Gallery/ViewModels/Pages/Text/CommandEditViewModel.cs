namespace Wpf.Ui.Gallery.ViewModels.Pages.Text;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui;

public partial class CommandEditViewModel : ViewModel, INavigationAware
{
    private readonly INavigationService _navigationService;

    public CommandEditViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private string _originalTitle = string.Empty;

    [ObservableProperty]
    private string _titleText = string.Empty;

    [ObservableProperty]
    private string _commandText = string.Empty;

    [ObservableProperty]
    private string _terminalText = string.Empty;

    private sealed class CommandEntry
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("terminal")]
        public string Terminal { get; set; } = string.Empty;

        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;
    }

    [RelayCommand]
    private void Save(object sender)
    {
        if (sender is not FrameworkElement sourceElement)
        {
            return;
        }

        FrameworkElement? root = GetRoot(sourceElement);
        if (root is null)
        {
            return;
        }

        RadioButton? cmdRadio = FindChildByName<RadioButton>(root, "CmdRadio");
        RadioButton? psRadio = FindChildByName<RadioButton>(root, "PsRadio");
        RadioButton? bashRadio = FindChildByName<RadioButton>(root, "BashRadio");

        string title = TitleText?.Trim() ?? string.Empty;
        string command = CommandText?.Trim() ?? string.Empty;
        string terminal =
            psRadio?.IsChecked == true
                ? "PS"
                : bashRadio?.IsChecked == true
                    ? "Bash"
                    : "CMD";

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(command))
        {
            _ = MessageBox.Show("标题或命令不能为空", "提示");
            return;
        }

        string filePath = GetStoragePath();
        EnsureDirectoryExists(Path.GetDirectoryName(filePath)!);

        List<CommandEntry> entries = [];
        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                List<CommandEntry>? existing =
                    JsonSerializer.Deserialize<List<CommandEntry>>(json) ?? [];
                entries = existing.ToList();
            }
            catch
            {
                entries = [];
            }
        }

        CommandEntry? current = entries.FirstOrDefault(
            e => string.Equals(e.Title, OriginalTitle, StringComparison.OrdinalIgnoreCase)
        );

        if (current is null)
        {
            _ = MessageBox.Show("未找到要编辑的命令", "提示");
            return;
        }

        if (
            entries.Any(
                e =>
                    !ReferenceEquals(e, current)
                    && string.Equals(e.Title, title, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(e.Terminal, terminal, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            _ = MessageBox.Show("该终端类型的标题已存在，不能重复", "提示");
            return;
        }

        current.Title = title;
        current.Command = command;
        current.Terminal = terminal;

        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(filePath, JsonSerializer.Serialize(entries, options));

        _ = MessageBox.Show("已保存修改", "成功");

        _ = _navigationService.GoBack();
    }

    public void OnNavigatedTo()
    {
    }

    public void OnNavigatedFrom()
    {
    }

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is not string title || string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        OriginalTitle = title.Trim();

        string filePath = GetStoragePath();
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            List<CommandEntry>? entries =
                JsonSerializer.Deserialize<List<CommandEntry>>(json) ?? [];

            CommandEntry? current = entries.FirstOrDefault(
                e => string.Equals(e.Title, OriginalTitle, StringComparison.OrdinalIgnoreCase)
            );

            if (current is null)
            {
                return;
            }

            TitleText = current.Title;
            CommandText = current.Command;
            TerminalText = current.Terminal;
        }
        catch
        {
        }
    }

    private static FrameworkElement? GetRoot(FrameworkElement element)
    {
        FrameworkElement current = element;
        while (current.Parent is FrameworkElement parent)
        {
            current = parent;
        }

        return current;
    }

    private static T? FindChildByName<T>(DependencyObject parent, string name)
        where T : FrameworkElement
    {
        if (parent == null)
        {
            return null;
        }

        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T frameworkElement && frameworkElement.Name == name)
            {
                return frameworkElement;
            }

            T? result = FindChildByName<T>(child, name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static string GetStoragePath()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WpfUiGallery"
        );

        return Path.Combine(folder, "commands.json");
    }

    private static void EnsureDirectoryExists(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}

