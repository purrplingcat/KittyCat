# Content Path Resolution

`ContentPathResolver` maps a package name, optionally restricted to an object-path
prefix, to a root in the content namespace. It does not know about asset catalogs,
mount filesystems, or load assets. A root can point to loose files or content
already mounted from a PAK or ZIP; mapping a path does not mount it.

## Mapping API

- `Map(packageName, path)` sets or replaces the package's fallback root.
- `Map(packageName, path, objectPathPrefix)` registers a prefix-specific root.
  The first registration for an identical prefix wins, even if a later path differs.
- `Resolve(packageName, objectPath)` returns the root for the longest matching
  prefix, or the package's fallback root when no prefix matches.
- `Resolve(packageName)` and `TryResolve(packageName, out path)` consult only the
  fallback root, not prefix mappings.
- `Unmap(packageName)` removes both the fallback root and all prefix mappings.

Package names are case-insensitive. Prefixes are case-sensitive and use ordinal
comparison. Supply normalized object paths and prefixes with forward slashes;
the resolver does not normalize them.

## Matching Rules

A prefix matches an exact object path or a complete leading path segment.
`Textures` matches `Textures` and `Textures/Fur`, but not `TexturesExtra/Fur`.
When multiple prefixes match, the longest wins regardless of registration order.

An empty prefix matches every object path, including when a fallback root also
exists. An empty root is valid and means that no root directory is prepended.
Only `null` means that no mapping was found.

The resolver returns a root, not the complete asset path. It does not strip the
matching prefix from `ObjectPath`.

```csharp
resolver.Map("Winter", "Content/Winter");
resolver.Map("Winter", "DLC/Winter", "Textures");
resolver.Map("Winter", "DLC/WinterUI", "Textures/UI");

resolver.Resolve("winter", "Textures/Fur");        // DLC/Winter
resolver.Resolve("Winter", "Textures/UI/Panel");   // DLC/WinterUI
resolver.Resolve("Winter", "Sounds/Click");        // Content/Winter
resolver.Resolve("Winter", "TexturesExtra/Fur");   // Content/Winter
```

`AssetManager` appends the complete `ObjectPath` and optional raw `FileExtension`
to that root. The first example therefore loads
`DLC/Winter/Textures/Fur`, not `DLC/Winter/Fur`. The loader handles the compiled
XNB extension; raw content keeps its explicit extension.

If no mapping exists, `AssetManager` falls back to
`<default content root>/<PackageName>/<ObjectPath><FileExtension>`.
The engine currently registers `game -> Content` and `engine -> Content/Engine`
as fallback mappings during `ContentPackageManager.Startup`.

## Catalog Integration

Catalog redirects are declarative data for content-only DLC and mods; no
registration DLL is required. `AssetCatalogIndexer` translates each redirect into
normalized strings and calls the generic resolver API. The resolver never accepts
or retains `AssetCatalog` or `AssetCatalogRedirect` instances.

`AssetCatalog.Import(registry, sourceId)` only registers asset metadata. It does
not apply redirects. Automatic catalog indexing handles mappings separately.
`AssetMetadata` and `AssetCatalogEntry` contain no content-root field or root index.

The builder groups entries under shared package/path-prefix redirects. A bundle
with one common output root produces one package-wide redirect. Multiple roots
may require separate prefixes. Identical prefixes are deduplicated; assets from
the same directory split between different roots can require redirects as
specific as individual object paths.

## Content Managers

`DefaultContentManagerProvider.CreateContentManager(root)` resolves fallback
package aliases only. It has no asset object path with which to select a prefix.
When `root` is null, it uses `ContentManagerOptions.ContentRoot` instead.