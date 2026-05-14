using PurrplingCore.Toolkit.Metadata;
using PurrplingCore.Toolkit.Modding;
using System.Reflection;
using System.Text;

namespace PurrplingCore.Toolkit;

public sealed class GameVersion
{
    private readonly string _name;
    private readonly Version _version;
    private readonly Type? _type;
    private readonly string? _informationalVersion;
    private readonly string _fullName;
    private readonly string _author;

    public string FullName => _fullName;
    public string Name => _name;
    public string Author => _author;
    public Version Version => _version;
    public Type? Type => _type;
    public string? InformationalVersion => _informationalVersion;

    public static GameVersion Empty => new();

    public GameVersion()
    {
        _fullName = string.Empty;
        _author = string.Empty;
        _name = string.Empty;
        _version = new Version();
    }

    public GameVersion(Type gameType)
    {
        ArgumentNullException.ThrowIfNull(gameType, nameof(gameType));

        var asmName = gameType.Assembly.GetName();
        var displayName = gameType.GetCustomAttribute<NameAttribute>();
        var info = gameType.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var companyAttr = gameType.Assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
        var productAttr = gameType.Assembly.GetCustomAttribute<AssemblyProductAttribute>();
        
        _name = displayName?.Name 
            ?? productAttr?.Product 
            ?? asmName.Name 
            ?? asmName.FullName;
        _version = asmName.Version ?? new Version();
        _fullName = $"{companyAttr?.Company ?? gameType.Namespace}.{productAttr?.Product ?? asmName.Name ?? gameType.Name}";
        _informationalVersion = info?.InformationalVersion;
        _author = companyAttr?.Company ?? string.Empty;
        _type = gameType;
    }

    public GameVersion(
        string name, 
        Version version, 
        string? author = null,
        Type? gameType = null,
        string? informationalVersion = null
    )
    {
        _name = name;
        _version = version;
        _type = gameType;
        _author = author ?? string.Empty;
        _fullName = gameType?.Assembly.FullName ?? name;
        _informationalVersion = informationalVersion;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append(Name)
          .Append($" {Version.Major}.{Version.Minor}.{Version.Revision}");

        if (Version.Build > 0)
        {
            sb.Append($" build {Version.Build}");
        }

        if (!string.IsNullOrEmpty( _informationalVersion))
        {
            sb.Append($" ({_informationalVersion})");
        }

        return sb.ToString();
    }

    public ModManifest ToManifest()
    {
        return new ModManifest()
        {
            Id = FullName,
            Name = Name,
            Version = InformationalVersion ?? Version.ToString(),
            Author = Author
        };
    }

    public static GameVersion Of<TGame>() where TGame : class, IGame
    {
        return new GameVersion(typeof(TGame));
    }
}
