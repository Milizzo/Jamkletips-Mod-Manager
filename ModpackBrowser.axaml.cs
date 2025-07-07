using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Modrinth;
using Modrinth.Models.Enums.Project;
using MsBox.Avalonia;

namespace Jamkletip_s_Mod_Manager;

public partial class ModpackBrowser : UserControl
{
    private bool _searching;
    private bool _downloading;

    public ModpackBrowser()
    {
        InitializeComponent();

        // Back button
        BackButton.Click += (_, _) =>
        {
            if (_searching || _downloading) return;
            MainWindow.ShowMenu(new ProfilesList());
        };

        // Load packs
        _ = LoadModpacks();

        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        Status.Text = (_searching, _downloading) switch
        {
            (false, false) => "Idle",
            (false, true) => "Downloading modpack...",
            (true, false) => "Searching for modpacks...",
            (true, true) => "Searching and downloading modpacks...",
        };
    }

    private async Task LoadModpacks()
    {
        if (_searching) return;
        _searching = true;
        UpdateStatusText();

        try
        {
            ModpacksPanel.Children.Clear();

            var client = new ModrinthClient();

            var facets = new FacetCollection
            {
                Facet.ProjectType(ProjectType.Modpack)
            };

            var results = await client.Project.SearchAsync(
                SearchBox.Text ?? string.Empty,
                limit: 100,
                facets: facets);

            foreach (var result in results.Hits)
            {
                Button button = new()
                {
                    Content = $"{result.Title} - {result.Description ?? "(No description)"}",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                };

                button.Click += (_, _) => _ = DownloadModpack(result.Slug ?? result.ProjectId);

                ModpacksPanel.Children.Add(button);
            }
        }
        catch (Exception e)
        {
            ModpacksPanel.Children.Clear();
            ModpacksPanel.Children.Add(new TextBlock
            {
                Text = $"Failed to load modpacks: {e.Message}",
            });
        }

        _searching = false;
        UpdateStatusText();
    }

    private async Task DownloadModpack(string slug)
    {
        if (_downloading || _searching) return;
        _downloading = true;
        UpdateStatusText();

        try
        {
            // Modpacks are still WIP; throw an exception
            throw new NotImplementedException("Modpacks are still a WIP feature.");
            
            using var client = new ModrinthClient();

            var modpack = await client.Project.GetAsync(slug);
            if (modpack.ProjectType != ProjectType.Modpack) throw new("Requested project is not a modpack.");

            var versions = await client.Version.GetProjectVersionListAsync(slug);
            var version = versions.FirstOrDefault(v => v.Files.Any(f => (f.FileName ?? f.Url).EndsWith(".mrpack"))) ??
                          throw new("No valid versions found.");

            var file = version.Files.First(f => (f.FileName ?? f.Url).EndsWith(".mrpack"));

            var url = file.Url;

            var outputPath = Path.Combine(MainWindow.ProfilesPath, slug);
            var tempOutputPath = $"{outputPath}.tmp";
            var archiveOutputPath = $"{outputPath}.archive.tmp";
            
            Directory.CreateDirectory(outputPath);

            using var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(archiveOutputPath, data);

            ZipFile.ExtractToDirectory(archiveOutputPath, tempOutputPath, true);

            var modpackModsPath = Path.Combine(tempOutputPath, "mods");

            if (Directory.Exists(modpackModsPath))
            {
                // Copy existing mods folder
                MainWindow.CopyDirectory(modpackModsPath, outputPath);
            }
            else
            {
                // Download mods manually
                var indexFilePath = Path.Combine(outputPath, "modrinth.index.json");
                var indexFile = await File.ReadAllTextAsync(indexFilePath);
                
                var doc = JsonDocument.Parse(indexFile);

                foreach (var modrinthFile in doc.RootElement.GetProperty("files").EnumerateArray())
                {
                    // Work on this
                }
            }

            var profileFilePath = Path.Combine(outputPath, MainWindow.ProfileJsonFilePath);
            var profile = new Profile(MainWindow.Loaders.First(l => modpack.Loaders.Contains(l.Name)),
                modpack.GameVersions.First());

            var jsonProfile = profile.ToJson();
            await File.WriteAllTextAsync(profileFilePath, jsonProfile);

            // Cleanup
            File.Delete(archiveOutputPath);
            Directory.Delete(tempOutputPath, true);

            Status.Text = $"Download complete: {slug}";
        }
        catch (Exception e)
        {
            Status.Text = $"Download failed: {e.Message}";
        }

        _downloading = false;
    }
}