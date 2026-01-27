// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;

namespace Wpf.Ui.Gallery.Controls;

[ContentProperty(nameof(ExampleContent))]
public class ControlExample : Control
{
    /// <summary>Identifies the <see cref="HeaderText"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(
        nameof(HeaderText),
        typeof(string),
        typeof(ControlExample),
        new PropertyMetadata(null)
    );

    /// <summary>Identifies the <see cref="ExampleContent"/> dependency property.</summary>
    public static readonly DependencyProperty ExampleContentProperty = DependencyProperty.Register(
        nameof(ExampleContent),
        typeof(object),
        typeof(ControlExample),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty XamlCodeProperty = DependencyProperty.Register(
        nameof(XamlCode),
        typeof(string),
        typeof(ControlExample),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty XamlCodeSourceProperty = DependencyProperty.Register(
        nameof(XamlCodeSource),
        typeof(Uri),
        typeof(ControlExample),
        new PropertyMetadata(
            null,
            static (o, args) =>
            {
                ((ControlExample)o).OnXamlCodeSourceChanged((Uri?)args.NewValue);
            }
        )
    );

    public static readonly DependencyProperty CsharpCodeProperty = DependencyProperty.Register(
        nameof(CsharpCode),
        typeof(string),
        typeof(ControlExample),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty CsharpCodeSourceProperty = DependencyProperty.Register(
        nameof(CsharpCodeSource),
        typeof(Uri),
        typeof(ControlExample),
        new PropertyMetadata(
            null,
            static (o, args) =>
            {
                ((ControlExample)o).OnCsharpCodeSourceChanged((Uri?)args.NewValue);
            }
        )
    );

    public static readonly DependencyProperty IsExpanderVisibleProperty = DependencyProperty.Register(
        nameof(IsExpanderVisible),
        typeof(bool),
        typeof(ControlExample),
        new PropertyMetadata(true)
    );

    public string? HeaderText
    {
        get => (string?)GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public object? ExampleContent
    {
        get => GetValue(ExampleContentProperty);
        set => SetValue(ExampleContentProperty, value);
    }

    public static readonly DependencyProperty ExpanderHeaderProperty = DependencyProperty.Register(
        nameof(ExpanderHeader),
        typeof(string),
        typeof(ControlExample),
        new PropertyMetadata("Source code")
    );

    public static readonly DependencyProperty ExpanderContentProperty = DependencyProperty.Register(
        nameof(ExpanderContent),
        typeof(object),
        typeof(ControlExample),
        new PropertyMetadata(null)
    );

    public bool IsExpanderVisible
    {
        get => (bool)GetValue(IsExpanderVisibleProperty);
        set => SetValue(IsExpanderVisibleProperty, value);
    }

    public string? XamlCode
    {
        get => (string?)GetValue(XamlCodeProperty);
        set => SetValue(XamlCodeProperty, value);
    }

    public Uri? XamlCodeSource
    {
        get => (Uri?)GetValue(XamlCodeSourceProperty);
        set => SetValue(XamlCodeSourceProperty, value);
    }

    public string? CsharpCode
    {
        get => (string?)GetValue(CsharpCodeProperty);
        set => SetValue(CsharpCodeProperty, value);
    }

    public Uri? CsharpCodeSource
    {
        get => (Uri?)GetValue(CsharpCodeSourceProperty);
        set => SetValue(CsharpCodeSourceProperty, value);
    }

    public string ExpanderHeader
    {
        get => (string)GetValue(ExpanderHeaderProperty);
        set => SetValue(ExpanderHeaderProperty, value);
    }

    public object? ExpanderContent
    {
        get => GetValue(ExpanderContentProperty);
        set => SetValue(ExpanderContentProperty, value);
    }

    private void OnXamlCodeSourceChanged(Uri? uri)
    {
        SetCurrentValue(XamlCodeProperty, LoadResource(uri));
    }

    private void OnCsharpCodeSourceChanged(Uri? uri)
    {
        SetCurrentValue(CsharpCodeProperty, LoadResource(uri));
    }

    private static string LoadResource(Uri? uri)
    {
        try
        {
            if (uri is null || Application.GetResourceStream(uri) is not { } steamInfo)
            {
                return string.Empty;
            }

            using StreamReader streamReader = new(steamInfo.Stream, Encoding.UTF8);

            return streamReader.ReadToEnd();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return e.ToString();
        }
    }
}
