using System.Linq;
using System.Text.Json;

namespace Jamkletip_s_Mod_Manager;

public struct Profile
{
    public int Loader { get; set; }
    public string MinecraftVersion { get; set; }

    public Profile(int loader, string minecraftVersion)
    {
        Loader = loader;
        MinecraftVersion = minecraftVersion;
    }

    public Profile(Loader loader, string minecraftVersion)
    {
        Loader = MainWindow.Loaders.ToList().IndexOf(loader);
        MinecraftVersion = minecraftVersion;
    }

    public readonly Loader GetLoader() => MainWindow.Loaders[Loader];

    public static Profile FromJson(string json) => JsonSerializer.Deserialize<Profile>(json);
    
    public string ToJson() => JsonSerializer.Serialize(this);
}