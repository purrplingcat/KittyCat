using KittyCat;
using KittyCat.DesktopGL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Modding;
using System;
using System.IO;
using System.Text;
using Zio;

Console.OutputEncoding = Encoding.UTF8;

var builder = GameHost.CreateBuilder(args);

builder.Services.AddPhysicalVfs(Path.Combine(AppContext.BaseDirectory, "Content"));
builder.AddGame<KittyCatGame>();
builder.AddMods(Path.Combine(AppContext.BaseDirectory, "Mods"));
builder.Logging.AddDefaultLogging();

using var host = builder.Build();

var vfs = host.Services.GetRequiredService<IFileSystem>();
host.Logger.LogVfsStructure(vfs);

host.Run();
