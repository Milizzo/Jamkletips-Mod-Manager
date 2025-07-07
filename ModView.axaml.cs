using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Modrinth;
using Modrinth.Models.Enums.Version;
using MsBox.Avalonia;


namespace Jamkletip_s_Mod_Manager;

public partial class ModView : UserControl
{
    private readonly ProfileView _profileView;

    private bool _downloading;
    private bool _downloaded;

    private Button? _downloadButton;

    public ModView(ProfileView profileView, string slug)
    {
        InitializeComponent();

        _profileView = profileView;

        BackButton.Click += (_, _) => BackToProfileView();
        
        _ = Load(slug);
    }

    private async Task Load(string slug)
    {
        try
        {
            using var client = new ModrinthClient();

            var project = await client.Project.GetAsync(slug);
            Modrinth.Models.Version[] versions =
            [
                .. (await client.Version.GetProjectVersionListAsync(slug))
                .OrderByDescending(v => v.DatePublished)
                .Where(v => v.GameVersions.Contains(_profileView.Profile.MinecraftVersion))
                .Where(v => v.Loaders.Contains(_profileView.Profile.GetLoader().Name))
            ];

            var version = versions.FirstOrDefault() ?? throw new Exception("No compatible versions available.");

            TextBlock titleText = new()
            {
                Text = project.Title,
                FontSize = 36,
            };
            InfoPanel.Children.Add(titleText);

            TextBlock statusText = new()
            {
                Text = "↓" + project.Downloads.ToString("N0") + " - " +
                       $"Client side: {project.ClientSide} | Server side: {project.ServerSide}" + " - " +
                       string.Join(", ", project.Categories) + " - " + project.Status switch
                       {
                           Modrinth.Models.Enums.Project.ProjectStatus.Approved => "Approved by Moderator",
                           Modrinth.Models.Enums.Project.ProjectStatus.Archived => "Archived",
                           Modrinth.Models.Enums.Project.ProjectStatus.Draft => "Draft",
                           Modrinth.Models.Enums.Project.ProjectStatus.Private => "Approved by Moderator, Private",
                           Modrinth.Models.Enums.Project.ProjectStatus.Processing => "Under Review by Moderator",
                           Modrinth.Models.Enums.Project.ProjectStatus.Rejected => "Rejected by Moderator",
                           Modrinth.Models.Enums.Project.ProjectStatus.Scheduled =>
                               "Approved by Moderator, Scheduled for Release",
                           Modrinth.Models.Enums.Project.ProjectStatus.Unknown => "Status Unknown",
                           Modrinth.Models.Enums.Project.ProjectStatus.Unlisted => "Approved by Moderator, Unlisted",
                           Modrinth.Models.Enums.Project.ProjectStatus.Withheld => "Hidden by Moderator",
                           _ => "Status Unknown",
                       },
            };
            InfoPanel.Children.Add(statusText);

            TextBlock descriptionText = new()
            {
                Text = Environment.NewLine + project.Description + Environment.NewLine,
            };
            InfoPanel.Children.Add(descriptionText);

            _downloadButton = new()
            {
                // Content = $"Download{((version.Dependencies?.Where(d => d.DependencyType == DependencyType.Required).ToArray().Length > 0) ? " with Dependencies" : "")} ↓",

                Content = "Download ↓",
                Width = 300,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };

            _downloadButton.Click += (sender, e) =>
            {
                if (!_downloading && !_downloaded)
                {
                    _ = DownloadModVersionWithDependenciesAsync(version);
                }
            };

            InfoPanel.Children.Add(_downloadButton);
        }
        catch (Exception ex)
        {
            InfoPanel.Children.Clear();
            InfoPanel.Children.Add(new TextBlock()
            {
                Text = "Failed to load: " + ex.Message,
            });
        }
    }

    private void BackToProfileView()
    {
        if (_downloading) return;

        MainWindow.Instance.ShowCustom(_profileView);
    }

    private async Task DownloadModVersionWithDependenciesAsync(Modrinth.Models.Version version)
    {
        try
        {
            if (_downloadButton != null)
            {
                _downloadButton.Content = "Downloading...";
            }

            _downloading = true;
            _downloaded = true;

            using var client = new ModrinthClient();

            List<Modrinth.Models.Version> versions = [];
            await PopulateVersionsListWithDependenciesAsync(client, version, versions);

            using var httpClient = new HttpClient();

            List<Task> downloads = [];

            foreach (var ver in versions)
            {
                var file = ver.Files.FirstOrDefault(f => f.Primary);
                if (file == null) continue;

                var output = Path.Combine(_profileView.ProfilePath, file.FileName ?? Path.GetFileName(file.Url));

                var downloadTask = Task.Run(async () =>
                {
                    var data = await httpClient.GetByteArrayAsync(file.Url);
                    await File.WriteAllBytesAsync(output, data);
                });

                downloads.Add(downloadTask);
            }

            await Task.WhenAll(downloads);
            _downloading = false;

            _profileView.LoadInstalledMods();
            _profileView.UpdateUseButton();

            if (_downloadButton != null)
            {
                _downloadButton.Content = "Downloaded ✓";
            }
        }
        catch (Exception ex)
        {
            _downloaded = false;
            _downloading = false;

            if (_downloadButton != null)
            {
                _downloadButton.Content = "Download Failed";
            }

            await MessageBoxManager.GetMessageBoxStandard("Failed to download mods", ex.Message)
                .ShowWindowDialogAsync(MainWindow.Instance);
        }
    }

    private async Task PopulateVersionsListWithDependenciesAsync(ModrinthClient client, Modrinth.Models.Version version,
        List<Modrinth.Models.Version> versions)
    {
        if (versions.Contains(version)) return;

        versions.Add(version);

        foreach (var dependency in version.Dependencies?.Where(d => d.DependencyType == DependencyType.Required) ?? [])
        {
            if (dependency.DependencyType != DependencyType.Required) continue;

            Modrinth.Models.Version depVersion;

            if (dependency.VersionId != null)
            {
                depVersion = await client.Version.GetAsync(dependency.VersionId);
            }
            else
            {
                depVersion = (await client.Version.GetProjectVersionListAsync(dependency.ProjectId!))
                    .Where(v => v.GameVersions.Contains(_profileView.Profile.MinecraftVersion))
                    .Where(v => v.Loaders.Contains(_profileView.Profile.GetLoader().Name))
                    .OrderByDescending(v => v.DatePublished)
                    .FirstOrDefault() ?? throw new("No versions found for mod: " + dependency.ProjectId + ".");
            }

            await PopulateVersionsListWithDependenciesAsync(client, depVersion, versions);
        }
    }
}