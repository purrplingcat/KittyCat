namespace PurrplingCore.Toolkit;


/// <summary>
/// Root constants for generating deterministic identifiers (RFC 4122).
/// This architecture utilizes cascaded UUIDv5 namespaces.
/// </summary>
public static class Namespaces
{
    /// <summary>
    /// The global root namespace for the entire studio and engine ecosystem.
    /// Calculated as UUIDv5: ns:DNS ("6ba7b810-9dad-11d1-80b4-00c04fd430c8") + "purrplingcat.com".
    /// All engine subsystems derive their specific namespaces from this root.
    /// </summary>
    public static readonly Guid Root = new(
        0x0de3cb09,
        0xb740,
        0x5340,
        0xbb, 0x22, 0x93, 0x10,
        0xb6, 0x32, 0x87, 0xfb
    );

    /// <summary>
    /// The namespace root for game content identifiers (AssetId).
    /// Calculated as UUIDv5: <see cref="Root"/> + "assets".
    /// </summary>
    public static readonly Guid Assets = new(
        0xc524c0e8,
        0x181d,
        0x571e,
        0xa3, 0x60, 0x12, 0x90,
        0x5e, 0xd9, 0x09, 0xff
    );
}
