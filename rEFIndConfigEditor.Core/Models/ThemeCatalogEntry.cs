namespace rEFIndConfigEditor.Models;

public sealed class ThemeCatalogEntry
{
    public string Id { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string Github { get; set; } = "";
    public string Preview { get; set; } = "";
    public int GithubStars { get; set; }
    public DateTimeOffset Created { get; set; }
    public bool RecentlyAdded { get; set; }
    public bool IsDark { get; set; }
    public bool IsMinimal { get; set; }
    public bool IsLight { get; set; }
    public List<string> Tags { get; set; } = [];
}
