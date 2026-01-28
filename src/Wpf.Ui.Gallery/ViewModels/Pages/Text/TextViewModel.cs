// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.Models;

namespace Wpf.Ui.Gallery.ViewModels.Pages.Text;

public partial class TextViewModel : ViewModel
{
    [ObservableProperty]
    private ICollection<NavigationCard> _navigationCards = new ObservableCollection<NavigationCard>();

    [ObservableProperty]
    private string _terminalOutput = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _terminalLines = new();

    private readonly IReadOnlyList<CommandDefinition> _commands;

    public TextViewModel()
    {
        _commands = LoadCommandsFromJson();

        NavigationCards = new ObservableCollection<NavigationCard>(
            _commands.Select(
                command =>
                    new NavigationCard
                    {
                        Name = command.Title,
                        Description = command.Terminal,
                        Icon = SymbolRegular.Textbox24,
                    }
            )
        );
    }

    partial void OnTerminalOutputChanged(string value)
    {
        TerminalLines.Clear();

        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var parts = value.Replace("\r\n", "\n").Split('\n');

        foreach (var part in parts)
        {
            TerminalLines.Add(part);
        }
    }

    private static IReadOnlyList<CommandDefinition> LoadCommandsFromJson()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (string.IsNullOrWhiteSpace(localAppData))
            {
                return Array.Empty<CommandDefinition>();
            }

            var directory = Path.Combine(localAppData, "WpfUiGallery");
            var path = Path.Combine(directory, "commands.json");

            if (!File.Exists(path))
            {
                return Array.Empty<CommandDefinition>();
            }

            using var stream = File.OpenRead(path);

            var items =
                JsonSerializer.Deserialize<List<CommandDefinition>>(stream)
                ?? new List<CommandDefinition>();

            return items
                .Select(
                    i =>
                        new CommandDefinition
                        {
                            Title = (i.Title ?? string.Empty).Trim(),
                            Terminal = (i.Terminal ?? string.Empty).Trim(),
                            Command = (i.Command ?? string.Empty).Trim(),
                        }
                )
                .Where(i => !string.IsNullOrWhiteSpace(i.Title))
                .ToList();
        }
        catch
        {
            return Array.Empty<CommandDefinition>();
        }
    }

    public async Task RunCommandAsync(NavigationCard? card)
    {
        if (card is null)
        {
            return;
        }

        var commandDefinition = _commands.FirstOrDefault(
            c => string.Equals(c.Title, card.Name, StringComparison.Ordinal)
        );

        if (commandDefinition == null)
        {
            TerminalOutput = "未找到匹配的命令";
            return;
        }

        var commandText = (commandDefinition.Command ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(commandText))
        {
            TerminalOutput = "命令为空";
            return;
        }

        var terminal = NormalizeTerminal(commandDefinition.Terminal);

        try
        {
            TerminalOutput = $"> {commandText}{Environment.NewLine}";

            using var process = CreateProcess(terminal, commandText);

            var outputBuilder = new StringBuilder();

            process.Start();

            var standardOutputTask = process.StandardOutput.ReadToEndAsync();
            var standardErrorTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(standardOutputTask, standardErrorTask);
            await process.WaitForExitAsync();

            outputBuilder.Append(standardOutputTask.Result);

            if (!string.IsNullOrWhiteSpace(standardErrorTask.Result))
            {
                if (outputBuilder.Length > 0)
                {
                    outputBuilder.AppendLine();
                }

                outputBuilder.Append(standardErrorTask.Result);
            }

            if (outputBuilder.Length == 0)
            {
                outputBuilder.Append("命令执行完成");
            }

            TerminalOutput += outputBuilder.ToString();
        }
        catch (Exception ex)
        {
            TerminalOutput = $"命令执行失败: {ex.Message}";
        }
    }

    private static string NormalizeTerminal(string? terminal)
    {
        var value = (terminal ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            return "cmd";
        }

        if (
            value.Equals("ps", StringComparison.OrdinalIgnoreCase)
            || value.Equals("powershell", StringComparison.OrdinalIgnoreCase)
            || value.Equals("pwsh", StringComparison.OrdinalIgnoreCase)
        )
        {
            return "powershell";
        }

        if (value.Equals("bash", StringComparison.OrdinalIgnoreCase))
        {
            return "bash";
        }

        if (value.Equals("cmd", StringComparison.OrdinalIgnoreCase))
        {
            return "cmd";
        }

        return value;
    }

    private static Process CreateProcess(string terminal, string commandText)
    {
        var isPowerShell = terminal.Equals("pwsh", StringComparison.OrdinalIgnoreCase)
            || terminal.Equals("powershell", StringComparison.OrdinalIgnoreCase)
            || terminal.Equals("ps", StringComparison.OrdinalIgnoreCase);

        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (isPowerShell)
        {
            startInfo.FileName = "pwsh.exe";
            startInfo.Arguments = $"-NoLogo -NoProfile -Command \"{commandText}\"";
        }
        else if (terminal.Equals("bash", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = "bash";
            startInfo.Arguments = $"-c \"{commandText}\"";
        }
        else
        {
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/C {commandText}";
        }

        return new Process { StartInfo = startInfo };
    }

    private sealed class CommandDefinition
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("terminal")]
        public string? Terminal { get; set; }

        [JsonPropertyName("command")]
        public string? Command { get; set; }
    }
}
