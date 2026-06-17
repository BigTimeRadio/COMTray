using System.Text.Json;

namespace ComTray;

class PortNameStore
{
    static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ComTray");

    static string FilePath => Path.Combine(Folder, "names.json");

    Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);

    public static PortNameStore Load()
    {
        var store = new PortNameStore();
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath));
                if (loaded != null)
                    store.map = new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // A corrupt or unreadable config should not stop the app.
        }
        return store;
    }

    public string? Get(string key) => map.TryGetValue(key, out var name) ? name : null;

    public void Set(string key, string name) => map[key] = name;

    public void Remove(string key) => map.Remove(key);

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
        }
    }
}
