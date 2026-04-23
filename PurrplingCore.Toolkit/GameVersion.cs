using System.Reflection;
using System.Text;

namespace PurrplingCore.Toolkit;

public sealed class GameVersion
{
    private readonly string _name;
    private readonly Version _version;
    private readonly Type? _type;
    private readonly string? _informationalVersion;

    public string Name => _name;
    public Version Version => _version;
    public Type? Type => _type;
    public string? InformationalVersion => _informationalVersion;

    public static GameVersion Empty => new();

    public GameVersion()
    {
        _name = string.Empty;
        _version = new Version();
    }

    public GameVersion(Type gameType)
    {
        ArgumentNullException.ThrowIfNull(gameType, nameof(gameType));

        var asmName = gameType.Assembly.GetName();
        var info = gameType.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        _name = asmName.Name ?? asmName.FullName;
        _version = asmName.Version ?? new Version();
        _informationalVersion = info?.InformationalVersion;
        _type = gameType;
    }

    public GameVersion(string name, Version version, Type? gameType = null, string? informationalVersion = null)
    {
        _name = name;
        _version = version;
        _type = gameType;
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

    public static GameVersion Of<TGame>() where TGame : class, IGame
    {
        return new GameVersion(typeof(TGame));
    }
}
