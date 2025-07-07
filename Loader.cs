namespace Jamkletip_s_Mod_Manager;

public readonly struct Loader(string name, string label)
{
    public readonly string Name { get; } = name;
    public readonly string Label { get; } = label;
}
