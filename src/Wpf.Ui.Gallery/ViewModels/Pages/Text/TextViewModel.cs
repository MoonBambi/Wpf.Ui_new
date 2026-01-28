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
using Wpf.Ui.Gallery.ControlsLookup;
using Wpf.Ui.Gallery.Models;
using Wpf.Ui.Gallery.Views.Pages.Text;

namespace Wpf.Ui.Gallery.ViewModels.Pages.Text;

public partial class TextViewModel : ViewModel
{
    [ObservableProperty]
    private ICollection<NavigationCard> _navigationCards = LoadNavigationCards();

    private static ICollection<NavigationCard> LoadNavigationCards()
    {
        var pages = ControlPages.FromNamespace(typeof(TextPage).Namespace!);
        var titles = LoadTitlesFromCommands();

        return new ObservableCollection<NavigationCard>(
            pages.Select(
                (x, index) =>
                {
                    var title =
                        index < titles.Count && !string.IsNullOrWhiteSpace(titles[index])
                            ? titles[index]
                            : x.Name;

                    return new NavigationCard
                    {
                        Name = title,
                        Icon = x.Icon,
                        Description = x.Description,
                        PageType = x.PageType,
                    };
                }
            )
        );
    }

    private static IReadOnlyList<string> LoadTitlesFromCommands()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (string.IsNullOrWhiteSpace(localAppData))
            {
                return Array.Empty<string>();
            }

            var directory = Path.Combine(localAppData, "WpfUiGallery");
            var path = Path.Combine(directory, "commands.json");

            if (!File.Exists(path))
            {
                return Array.Empty<string>();
            }

            using var stream = File.OpenRead(path);

            var items =
                JsonSerializer.Deserialize<List<CommandDefinition>>(stream)
                ?? new List<CommandDefinition>();

            return items
                .Select(i => (i.Title ?? string.Empty).Trim())
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private sealed class CommandDefinition
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }
}
