using KittyCat;
using KittyCat.DesktopGL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Modding;
using PurrplingCore.Toolkit.Vfs;
using System;
using System.IO;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var builder = GameHost.CreateBuilder(args);

builder.AddGame<KittyCatGame>();
builder.AddMods(Path.Combine(AppContext.BaseDirectory, "Mods"));
builder.Logging.AddDefaultLogging();

using var host = builder.Build();

var vfs = host.Services.GetRequiredService<IVirtualFileSystemManager>();

foreach(var path in vfs.GetFileSystem().EnumerateItems("/", SearchOption.AllDirectories))
{
    host.Logger.LogTrace("{Path}", path.FullName);
}

//vfs.Open("test", FileMode.Open, FileAccess.ReadWrite);



host.Run();
