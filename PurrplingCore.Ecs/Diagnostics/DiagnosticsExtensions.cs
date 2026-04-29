using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using System.Text;

namespace PurrplingCore.Ecs.Diagnostics;

public static class DiagnosticExtensions
{
    public static void LogWorldTopology(this ILogger logger, ManagedWorld world)
    {
        if (!logger.IsEnabled(LogLevel.Debug)) return;

        logger.LogDebug("=== System tree for '{WorldName}' [{WorldType}] ===", world.Name, world.WorldType.Name);
        logger.LogDebug("{RootType}", world.SystemRoot.Name);

        var children = world.SystemRoot.ChildSystems;
        for (int i = 0; i < children.Count; i++)
        {
            LogTreeLine(logger, children[i], "", i == children.Count - 1);
        }

        logger.LogDebug("======================================================");
    }

    private static void LogTreeLine(ILogger logger, BaseSystem system, string indent, bool isLast)
    {
        bool isEnabled = system.Enabled;
        string marker = isLast ? "└──" : "├──";

        // Textový fallback pro FileLogger (do textového souboru se barvy nepropíšou)
        string statusText = isEnabled ? "" : " (Disabled)";

        // Žádné [Group] prefixy, jen čistý formátovaný název
        logger.LogDebug("{Indent}{Marker} {SystemName} {Status}",
            indent, marker, system.Name, statusText);

        if (system is SystemGroup group)
        {
            var children = group.ChildSystems;
            for (int i = 0; i < children.Count; i++)
            {
                string nextIndent = indent + (isLast ? "    " : "│   ");
                LogTreeLine(logger, children[i], nextIndent, i == children.Count - 1);
            }
        }
    }
}
