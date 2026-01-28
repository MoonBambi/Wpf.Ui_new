// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Wpf.Ui.Gallery.ViewModels.Pages.Text;

public partial class TextBoxViewModel : ViewModel
{
    [ObservableProperty]
    private string _titleText = string.Empty;

    [ObservableProperty]
    private string _commandText = string.Empty;

    private sealed class CommandEntry
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("terminal")]
        public string Terminal { get; set; } = string.Empty;

        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    [RelayCommand]
    private void AddEntry(object sender)
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

        if (
            entries.Any(
                e => string.Equals(e.Title, title, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            _ = MessageBox.Show("标题已存在，不能重复添加", "提示");
            return;
        }

        entries.Add(
            new CommandEntry
            {
                Title = title,
                Terminal = terminal,
                Command = command,
                CreatedAt = DateTime.UtcNow,
            }
        );

        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(filePath, JsonSerializer.Serialize(entries, options));

        _ = MessageBox.Show("已追加到 JSON", "成功");
    }

    private static FrameworkElement? GetRoot(FrameworkElement element)
    {
        FrameworkElement current = element;
        while (true)
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(current);
            if (parent is FrameworkElement fe)
            {
                current = fe;
                continue;
            }

            break;
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
