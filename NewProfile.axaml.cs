using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;

namespace Jamkletip_s_Mod_Manager;

public partial class NewProfile : UserControl
{
    private bool _checking;

    private int _currentLoader;

    public NewProfile()
    {
        InitializeComponent();

        BackButton.Click += (_, _) =>
        {
            MainWindow.Instance.ShowProfilesList();
        };

        UpdateLoaderButtonText();
        LoaderButton.Click += (_, _) =>
        {
            _currentLoader++;
            if (_currentLoader >= MainWindow.Loaders.Count) _currentLoader = 0;

            UpdateLoaderButtonText();
        };

        NextButton.Click += Create;
    }

    private void UpdateLoaderButtonText()
    {
        LoaderButton.Content = MainWindow.Loaders[_currentLoader].Label;
    }

    private async void Create(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_checking) return;

            _checking = true;

            // Make sure the info entered is valid

            var name = NameBox.Text?.Trim();
            var version = VersionBox.Text?.Trim();
            var loader = _currentLoader;

            if (string.IsNullOrWhiteSpace(name)) throw new("No profile name provided.");
            if (string.IsNullOrWhiteSpace(version)) throw new("No profile version provided.");

            var invalidChars = Path.GetInvalidFileNameChars();

            if (name.Any(c => invalidChars.Contains(c)))
            {
                throw new("Profile name contains invalid file name characters.");
            }

            const string point0 = ".0";
            if (version.EndsWith(point0)) version = version[..^point0.Length];

            using HttpClient client = new();
            var data = await client.GetStringAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json");

            var doc = JsonDocument.Parse(data);
            var versions = doc.RootElement.GetProperty("versions")
                .EnumerateArray()
                .Select(obj => obj.GetProperty("id").GetString())
                .ToArray();

            if (!versions.Contains(version)) throw new($"Provided version is not a valid Minecraft version. Try something like \"{doc.RootElement.GetProperty("latest").GetProperty("release").GetString()}\" instead.");

            // Create profile folder

            var folder = Path.Combine(MainWindow.ProfilesPath, name);

            if (Directory.Exists(folder)) throw new($"A profile already exists by the name \"{name}\".");
            Directory.CreateDirectory(folder);

            Profile profile = new(loader, version);
            var profileJson = JsonSerializer.Serialize(profile);

            var profileJsonPath = Path.Combine(folder, MainWindow.ProfileJsonFilePath);
            await File.WriteAllTextAsync(profileJsonPath, profileJson);

            await MessageBoxManager.GetMessageBoxStandard("Profile created", $"Profile \"{name}\" has been created successfully.").ShowWindowDialogAsync(MainWindow.Instance);
            MainWindow.Instance.ShowProfilesList();
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("Failed to create", ex.Message).ShowWindowDialogAsync(MainWindow.Instance);
        }
        finally
        {
            _checking = false;
        }
    }
}