using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace Jamkletip_s_Mod_Manager;

public partial class MainWindow : Window
{
    public const string ProfilesPath = "profiles";

    public const string ProfileJsonFilePath = "profile.json";

    public static IReadOnlyList<Loader> Loaders =>
    [
        new("fabric", "Fabric"),
        new("forge", "Forge"),
        new("neoforge", "NeoForge"),
        new("quilt", "Quilt"),
    ];

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static MainWindow Instance { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public MainWindow()
    {
        Instance = this;

        InitializeComponent();

        ShowProfilesList();
    }

    [Obsolete("Use the static ShowMenu method instead.")]
    public void ShowProfilesList()
    {
        MenuHost.Content = new ProfilesList();
    }

    [Obsolete("Use the static ShowMenu method instead.")]
    public void ShowNewProfile()
    {
        MenuHost.Content = new NewProfile();
    }

    [Obsolete("Use the static ShowMenu method instead.")]
    public void ShowProfileView(string path)
    {
        MenuHost.Content = new ProfileView(path);
    }

    [Obsolete("Use the static ShowMenu method instead.")]
    public void ShowModView(ProfileView profileView, string slug)
    {
        MenuHost.Content = new ModView(profileView, slug);
    }

    [Obsolete("Use the static ShowMenu method instead.")]
    public void ShowCustom(UserControl userControl)
    {
        MenuHost.Content = userControl;
    }

    public static void ShowMenu(UserControl menu)
    {
        Instance.MenuHost.Content = menu;
    }

    public static string GetMinecraftDirectory()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(homeDir, "AppData", "Roaming", ".minecraft");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(homeDir, "Library", "Application Support", "minecraft");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return Path.Combine(homeDir, ".minecraft");
        }

        throw new PlatformNotSupportedException("Unsupported OS for locating Minecraft directory.");
    }
    
    public static bool HaveSameFiles(string?[] files1, string?[] files2)
    {
        files1 = files1.Select(Path.GetFileName).ToArray();
        files2 = files2.Select(Path.GetFileName).ToArray();
        
        return files1.Length == files2.Length &&
               files1.OrderBy(x => x).SequenceEqual(files2.OrderBy(x => x));
    }
    
    public static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);
        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), overwrite: true);

        foreach (var dir in Directory.GetDirectories(sourceDir))
            CopyDirectory(dir, Path.Combine(destinationDir, Path.GetFileName(dir)));
    }
}