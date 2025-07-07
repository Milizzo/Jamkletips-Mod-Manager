using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Modrinth;
using MsBox.Avalonia;

namespace Jamkletip_s_Mod_Manager;

public partial class ProfileView : UserControl
{
    private bool _addModsMenuLoaded;
    private bool _searching;

    public readonly string ProfilePath;
    public readonly Profile Profile;

    public ProfileView(string profilePath)
    {
        ProfilePath = profilePath;

        InitializeComponent();

        BackButton.Click += (_, _) => MainWindow.Instance.ShowProfilesList();

        UpdateUseButton();
        UseButton.Click += async (_, _) =>
        {
            try
            {
                var minecraftMods = Path.Combine(MainWindow.GetMinecraftDirectory(), "mods");
                if (!Directory.Exists(minecraftMods)) Directory.CreateDirectory(minecraftMods);

                foreach (var file in Directory.GetFiles(minecraftMods, "*.jar"))
                {
                    File.Delete(file);
                }

                foreach (var file in Directory.GetFiles(ProfilePath, "*.jar"))
                {
                    File.Copy(file, Path.Combine(minecraftMods, Path.GetFileName(file)));
                }

                UpdateUseButton();
            }
            catch (Exception e)
            {
                UseButton.Content = "Failed";
            }
        };

        try
        {
            var jsonPath = Path.Combine(ProfilePath, MainWindow.ProfileJsonFilePath);
            var jsonFile = File.ReadAllText(jsonPath);

            Profile = JsonSerializer.Deserialize<Profile>(jsonFile);

            // Overview text

            OverviewText.Text =
                $"Name: {Path.GetFileName(ProfilePath)}{Environment.NewLine}Minecraft: {Profile.MinecraftVersion}{Environment.NewLine}Loader: {Profile.GetLoader().Label}";

            // Installed mods panel

            LoadInstalledMods();

            // Add mods panel

            Tabs.SelectionChanged += SelectionChanged;

            SearchButton.Click += (sender, e) => { _ = LoadMods(); };

            SearchBox.KeyDown += (sender, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Enter)
                {
                    _ = LoadMods();
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            MainWindow.Instance.ShowProfilesList();
        }
    }

    private async void SelectionChanged(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_addModsMenuLoaded) return;
            if (!ReferenceEquals(Tabs.SelectedItem, AddModsTab)) return;

            await LoadMods();
        }
        catch
        {
            // ignored
        }
    }

    public void UpdateUseButton()
    {
        try
        {
            var filesInProfile = Directory.GetFiles(ProfilePath, "*.jar");
            var filesInMinecraft =
                Directory.GetFiles(Path.Combine(MainWindow.GetMinecraftDirectory(), "mods"), "*.jar");

            UseButton.Content = MainWindow.HaveSameFiles(filesInProfile, filesInMinecraft)
                ? "Current Profile ✓"
                : "Activate";
        }
        catch
        {
            // ignored
        }
    }

    public void LoadInstalledMods()
    {
        var mods = Directory.GetFiles(ProfilePath, "*.jar");

        InstalledModsPanel.Children.Clear();
        foreach (var mod in mods)
        {
            Button button = new()
            {
                Content = Path.GetFileNameWithoutExtension(mod),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            };

            InstalledModsPanel.Children.Add(button);
        }
    }

    private async Task LoadMods()
    {
        if (_searching) return;

        try
        {
            _addModsMenuLoaded = true;
            _searching = true;

            AddModsPanel.Children.Clear();

            var version = Profile.MinecraftVersion;
            var loader = MainWindow.Loaders[Profile.Loader].Name;

            var modrinthClient = new ModrinthClient();

            var facets = new FacetCollection
            {
                Facet.ProjectType(Modrinth.Models.Enums.Project.ProjectType.Mod),
                Facet.Category(loader),
                Facet.Version(version),
            };

            var searchResults = await modrinthClient.Project.SearchAsync(
                query: SearchBox.Text ?? string.Empty,
                limit: 100,
                facets: facets
            );

            foreach (var hit in searchResults.Hits)
            {
                if (hit.Slug == null) continue;

                Button button = new()
                {
                    Content = hit.Title,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                };

                button.Click += (_, _) => { MainWindow.Instance.ShowModView(this, hit.Slug); };

                AddModsPanel.Children.Add(button);
            }
        }
        catch (Exception ex)
        {
            AddModsPanel.Children.Clear();
            AddModsPanel.Children.Add(new TextBlock
            {
                Text = $"Failed to load: {ex}",
            });
        }
        finally
        {
            _searching = false;
        }
    }
}