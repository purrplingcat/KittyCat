/// <summary>
/// Compiles configured assets, generates catalogs, and publishes PAKs or loose XNB files.
/// </summary>
/// <remarks>
/// DI binds options from the host's ContentBuilder section (appsettings.json and command-line overrides).
/// PAK is the default; use --ContentBuilder:CreatePak=false for loose files under Content.
/// </remarks>

using Microsoft.Extensions.Logging;
using PurrplingCore.ContentBuilder;
using PurrplingCore.Toolkit.Hosting;

var builder = ApplicationHost.CreateBuilder(args);
builder.UseContentBuilder();
builder.AddBundle<GameBundle>();
builder.Logging.AddSimpleConsole();

using var host = builder.Build();
return host.Run();
