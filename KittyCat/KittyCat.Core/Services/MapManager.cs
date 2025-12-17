using DotTiled;
using KittyCat.Core.Loaders;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.DI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KittyCat.Services;

[Singleton]
public class MapManager(ContentManager contentManager)
{
    private readonly TiledMapLoader _mapLoader = new(contentManager);
    private readonly Dictionary<string, Map> _maps = new(StringComparer.OrdinalIgnoreCase);
    public Map? CurrentMap { get; private set; }


}
