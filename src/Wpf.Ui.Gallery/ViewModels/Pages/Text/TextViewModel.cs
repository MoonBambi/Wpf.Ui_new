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

    private readonly List<CommandDefinition> _commands;
    private readonly string _initialWorkingDirectory;
    private string _currentWorkingDirectory;
    private TerminalSession? _session;

    public TextViewModel()
    {
        _commands = LoadCommandsFromJson().ToList();

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        _initialWorkingDirectory = string.IsNullOrWhiteSpace(userProfile)
            ? Directory.GetCurrentDirectory()
            : userProfile;

        _currentWorkingDirectory = _initialWorkingDirectory;

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

    public void ResetTerminal()
    {
        _session?.Dispose();
        _session = null;
        _currentWorkingDirectory = _initialWorkingDirectory;
        TerminalOutput = string.Empty;
    }

    public void DeleteCommand(NavigationCard? card)
    {
        if (card is null)
        {
            return;
        }

        var title = (card.Name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"确定删除命令“{title}”吗？",
            "确认删除",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question
        );

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        if (NavigationCards is ObservableCollection<NavigationCard> collection)
        {
            var existingCard = collection.FirstOrDefault(
                c => string.Equals(c.Name, title, StringComparison.Ordinal)
            );

            if (existingCard != null)
            {
                collection.Remove(existingCard);
            }
        }

        var commandDefinition = _commands.FirstOrDefault(
            c => string.Equals(c.Title, title, StringComparison.Ordinal)
        );

        if (commandDefinition != null)
        {
            _commands.Remove(commandDefinition);
        }

        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (string.IsNullOrWhiteSpace(localAppData))
            {
                return;
            }

            var directory = Path.Combine(localAppData, "WpfUiGallery");
            var path = Path.Combine(directory, "commands.json");

            if (!Directory.Exists(directory))
            {
                return;
            }

            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(_commands, options));
        }
        catch
        {
        }
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

    public Task RunCommandAsync(NavigationCard? card)
    {
        if (card is null)
        {
            return Task.CompletedTask;
        }

        var commandDefinition = _commands.FirstOrDefault(
            c => string.Equals(c.Title, card.Name, StringComparison.Ordinal)
        );

        if (commandDefinition == null)
        {
            if (!string.IsNullOrEmpty(TerminalOutput))
            {
                TerminalOutput += Environment.NewLine;
            }

            TerminalOutput += "未找到匹配的命令";
            return Task.CompletedTask;
        }

        var commandText = (commandDefinition.Command ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(commandText))
        {
            if (!string.IsNullOrEmpty(TerminalOutput))
            {
                TerminalOutput += Environment.NewLine;
            }

            TerminalOutput += "命令为空";
            return Task.CompletedTask;
        }

        var terminal = _session?.Terminal ?? NormalizeTerminal(commandDefinition.Terminal);
        var workingDirectoryBeforeCommand = _currentWorkingDirectory;

        var handledLocally = TryHandleDirectoryCommand(commandText, terminal);

        EnsureSession(terminal);

        var prefixBuilder = new StringBuilder();

        prefixBuilder
            .Append('[')
            .Append(workingDirectoryBeforeCommand)
            .Append("] > ")
            .Append(commandText);

        AppendToTerminal(prefixBuilder.ToString());

        _session?.SendCommand(commandText);

        return Task.CompletedTask;
    }

    private void EnsureSession(string terminal)
    {
        if (_session != null)
        {
            return;
        }

        _session = new TerminalSession(terminal, _currentWorkingDirectory);
        _session.OutputReceived += HandleSessionOutput;
        _session.ErrorReceived += HandleSessionError;

        try
        {
            _session.Start();
        }
        catch (Exception ex)
        {
            _session.Dispose();
            _session = null;
            AppendToTerminal($"终端启动失败: {ex.Message}");
        }
    }

    private void HandleSessionOutput(string value)
    {
        AppendToTerminal(value);
    }

    private void HandleSessionError(string value)
    {
        AppendToTerminal(value);
    }

    private void AppendToTerminal(string value)
    {
        void Append()
        {
            if (!string.IsNullOrEmpty(TerminalOutput))
            {
                TerminalOutput += Environment.NewLine;
            }

            TerminalOutput += value;
        }

        var application = System.Windows.Application.Current;

        if (application?.Dispatcher?.CheckAccess() == true)
        {
            Append();
            return;
        }

        application?.Dispatcher?.Invoke(Append);
    }

    private bool TryHandleDirectoryCommand(string commandText, string terminal)
    {
        var value = (commandText ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (
            terminal.Equals("cmd", StringComparison.OrdinalIgnoreCase)
            || terminal.Equals("powershell", StringComparison.OrdinalIgnoreCase)
        )
        {
            if (value.Length == 2 && char.IsLetter(value[0]) && value[1] == ':')
            {
                var driveRoot = char.ToUpperInvariant(value[0]).ToString() + ":\\";

                if (Directory.Exists(driveRoot))
                {
                    _currentWorkingDirectory = driveRoot;
                }

                return true;
            }

            if (value.StartsWith("cd", StringComparison.OrdinalIgnoreCase))
            {
                var argument = value.Length > 2 ? value.Substring(2).Trim() : string.Empty;

                if (string.IsNullOrWhiteSpace(argument) || argument == ".")
                {
                    return true;
                }

                if (argument == "..")
                {
                    try
                    {
                        var parent = Directory.GetParent(_currentWorkingDirectory);

                        if (parent != null)
                        {
                            _currentWorkingDirectory = parent.FullName;
                        }
                    }
                    catch
                    {
                    }

                    return true;
                }

                string candidatePath;

                if (Path.IsPathRooted(argument))
                {
                    candidatePath = argument;
                }
                else
                {
                    try
                    {
                        candidatePath = Path.GetFullPath(
                            Path.Combine(_currentWorkingDirectory, argument)
                        );
                    }
                    catch
                    {
                        return true;
                    }
                }

                if (Directory.Exists(candidatePath))
                {
                    _currentWorkingDirectory = candidatePath;
                }

                return true;
            }
        }

        return false;
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

    private sealed class TerminalSession : IDisposable
    {
        private readonly Process _process;
        private readonly string _terminal;

        public string Terminal => _terminal;

        public event Action<string>? OutputReceived;

        public event Action<string>? ErrorReceived;

        public TerminalSession(string terminal, string workingDirectory)
        {
            _terminal = terminal;
            _process = new Process();

            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.CreateNoWindow = true;

            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                _process.StartInfo.WorkingDirectory = workingDirectory;
            }

            if (string.Equals(terminal, "powershell", StringComparison.OrdinalIgnoreCase))
            {
                _process.StartInfo.FileName = "powershell.exe";
                _process.StartInfo.Arguments = "-NoLogo -NoProfile";
            }
            else if (string.Equals(terminal, "bash", StringComparison.OrdinalIgnoreCase))
            {
                _process.StartInfo.FileName = "bash";
            }
            else
            {
                _process.StartInfo.FileName = "cmd.exe";
            }

            _process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    OutputReceived?.Invoke(e.Data);
                }
            };

            _process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    ErrorReceived?.Invoke(e.Data);
                }
            };
        }

        public void Start()
        {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void SendCommand(string command)
        {
            if (_process.HasExited)
            {
                return;
            }

            _process.StandardInput.WriteLine(command);
        }

        public void Dispose()
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }
            }
            catch
            {
            }

            _process.Dispose();
        }
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
