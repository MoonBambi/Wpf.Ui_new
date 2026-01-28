// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wpf.Ui.Controls;
using Wpf.Ui.Gallery.Models;

namespace Wpf.Ui.Gallery.ViewModels.Pages.Text;

public partial class TextViewModel : ViewModel
{
    [ObservableProperty]
    private ICollection<NavigationCard> _navigationCards = LoadNavigationCards();

    private static ICollection<NavigationCard> LoadNavigationCards()
    {
        var commands = LoadCommandsFromJson();

        return new ObservableCollection<NavigationCard>(
            commands.Select(
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

    private sealed class CommandDefinition
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("terminal")]
        public string? Terminal { get; set; }
    }
}
