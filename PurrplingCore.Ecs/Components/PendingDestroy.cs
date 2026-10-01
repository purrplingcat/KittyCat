using Friflo.Engine.ECS;

namespace PurrplingCore.Ecs.Components;

/// <summary>
/// Marks a disabled entity for deletion by <see cref="Systems.EntityCleanupSystem"/>.
/// </summary>
public readonly struct PendingDestroy : ITag;