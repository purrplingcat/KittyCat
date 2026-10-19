# Packages and Asset Catalogs

The following responsibilities are kept separate:

| Layer | Responsibility |
| --- | --- |
| VFS and `PurrplingCore.Content.ContentPackageManager` | Mount directories, PAKs and ZIPs and select the file bytes through existing layering. |
| `PurrplingCore.Toolkit.Content.PackageManager` | Map logical package names to VFS roots with exact registrations or named route templates. No file discovery, providers, JSON, or asset catalogs. |
| `AssetRegistry`, `AssetCatalogIndexer`, `AssetManager` | Discover metadata, identify assets, resolve their package roots and load them. |

`PackageManager` replaces `ContentPathResolver`; there is no second resolver to
configure. Neither `AssetMetadata` nor the binary catalog contains mount paths,
redirects, or mount indices. Catalog schema 8 requires rebuilding older catalogs.

Runtime package names use interned `Name` values; catalogs continue to store text.
Empty or whitespace-only names become `Name.None` and are not valid package names.

## Registering Packages

The content builder writes binary asset catalogs under
`Content/AssetCatalogs/<bundle>.bin`. It does not write package descriptors.
Mount package roots directly when the host attaches content to the VFS:

```csharp
vfs.AddRootContainer(packageFileSystem);
packages.RegisterMountPoint(new Name("Winter"), "Mods/Winter/Content");
catalogs.Index(contentFiles, "Mods/Winter");
```

The host already knows the package identity and target VFS root when it mounts a
container. That mount operation is the source of truth; no additional descriptor
is embedded in or discovered from a PAK. For the base game, the engine registers
`game -> Content` and `engine -> Content/Engine` during startup. For mods, use
the existing mod loader's manifest and register the package as part of mounting
the mod VFS. `AssetManager.Get` never scans files, refreshes packages, or indexes
catalogs; an unregistered asset fails immediately with `KeyNotFoundException`.

## Relocating a Container

When the existing VFS mounts a container under `Mods/Winter`, register the
package against that actual VFS root and explicitly index the catalog. The
provider must expose the mounted VFS namespace:

```csharp
packages.RegisterMountPoint(new Name("Winter"), "Mods/Winter/Content");
catalogs.Index(fileProvider, "Mods/Winter");
```

Loading the asset whose `PackageName` is `Winter` and `ObjectPath` is `Fonts/Hud` opens
`Mods/Winter/Content/Fonts/Hud` (with the loader's compiled extension or the
metadata's raw extension). The catalog bytes remain unchanged if the container
is mounted under a different root.

The code that chooses a non-root VFS mount supplies its actual root explicitly.

## Explicit Mount Points

```csharp
packages.RegisterMountPoint(new Name("Winter"), "Mods/Winter/Content");

var root = packages.Resolve(new Name("Winter"));
packages.UnregisterMountPoint(new Name("Winter"));
```

- Each package has exactly one root, selected by one dictionary lookup. There are
  no object-path prefixes, alternative roots, or try-load searches.
- Package names are case-insensitive. An empty root is valid; only a null result
  means the package is not registered.
- Registering the same package replaces its root, allowing remounts.
  `UnregisterMountPoint` removes that logical mount, not its VFS files.
- Unload affected assets before remounting or unregistering a package. Existing
  loaded objects are not automatically migrated or invalidated.

For package names that follow a shared convention, register a route template once
instead of adding a mount registration for every mod:

```csharp
packages.RegisterMountPoint(new Name("Mod/{name}"), "Mods/{name}/Content");
packages.Resolve(new Name("Mod/Frostfall")); // Mods/Frostfall/Content
```

Templates use one named placeholder for exactly one path segment; they are not
regular expressions. More-specific templates win, while an exact package mount
always takes precedence. Resolution returns one root; asset loading does not
probe candidate paths.

The engine supplies the initial `Game -> Content` and `Engine -> Content/Engine`
mounts. Without a registered mount, `AssetManager` retains its fallback of
`<default content root>/<PackageName>/<ObjectPath><FileExtension>`.

## Catalog Independence

`AssetCatalog.Import(registry, sourceId)` and `AssetCatalogIndexer` only import
metadata. They never register packages or contain mount paths. Package mounts
are established by the host alongside VFS mounting.

The builder rejects assets of one package spread across different content roots;
use separate logical packages and direct mount registrations for those roots.
Multiple archives contributing to a single root are handled
by VFS layering, not by package lookup. Overlays that disable catalog generation
also omit the generated catalog and rely on existing metadata registration.

`DefaultContentManagerProvider.CreateContentManager(root)` uses the supplied root
literally and has no dependency on `PackageManager`. A null root selects
`ContentManagerOptions.ContentRoot`; an empty root stays empty. Logical package
resolution belongs to the asset loading layer. In no-provider
setups, register package mounts explicitly before loading assets; title-catalog
indexing does not discover JSON package descriptors.