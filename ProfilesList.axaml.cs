using System.IO;
using Avalonia.Controls;

namespace Jamkletip_s_Mod_Manager;

public partial class ProfilesList : UserControl
{
    private static Button GenerateButton(string label) => new()
    {
        Content = label,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
    };

    public ProfilesList()
    {
        InitializeComponent();

        if (!Directory.Exists(MainWindow.ProfilesPath)) Directory.CreateDirectory(MainWindow.ProfilesPath);

        var profiles = Directory.GetDirectories(MainWindow.ProfilesPath);

        foreach (var profile in profiles)
        {
            var profileButton = GenerateButton(Path.GetFileName(profile));
            profileButton.Click += (_, _) => MainWindow.Instance.ShowProfileView(profile);
            ProfilesPanel.Children.Add(profileButton);
        }
        
        Separator separator = new()
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
        };
        ProfilesPanel.Children.Add(separator);

        var newProfileButton = GenerateButton("+ New Profile");
        newProfileButton.Click += (_, _) => MainWindow.Instance.ShowNewProfile();
        ProfilesPanel.Children.Add(newProfileButton);
        
        var downloadModpackButton = GenerateButton("↓ Install Modpack");
        downloadModpackButton.Click += (_, _) => MainWindow.ShowMenu(new ModpackBrowser());
        ProfilesPanel.Children.Add(downloadModpackButton);
    }
}