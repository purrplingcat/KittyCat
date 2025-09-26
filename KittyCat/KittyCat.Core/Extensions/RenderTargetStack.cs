using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace KittyCat.Extensions;

public static class RenderTargetStack
{
    private static readonly ConditionalWeakTable<GraphicsDevice, Stack<RenderTargetBinding[]>> _targets = [];
    
    private static Stack<RenderTargetBinding[]> CreateStack(GraphicsDevice _) => new();

    private static Stack<RenderTargetBinding[]> GetTargetStack(this GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        return _targets.GetValue(device, CreateStack);
    }

    public static void PushRenderTarget(this GraphicsDevice device, RenderTarget2D target)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(target);
        var stack = device.GetTargetStack();

        stack.Push(device.GetRenderTargets());
        device.SetRenderTarget(target);
    }

    public static void PushRenderTarget(this GraphicsDevice device, params RenderTargetBinding[] targets)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(targets);
        var stack = device.GetTargetStack();

        stack.Push(device.GetRenderTargets());
        device.SetRenderTargets(targets);
    }

    public static void PopRenderTarget(this GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        var stack = device.GetTargetStack();

        if (stack.Count == 0)
        {
            device.SetRenderTarget(null);
            return;
        }

        device.SetRenderTargets(stack.Pop());
    }

    public static void ClearRenderTargets(this GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        var stack = device.GetTargetStack();
        
        stack.Clear();
        device.SetRenderTarget(null);
    }

    public static RenderTargetSession UseCanvas(this GraphicsDevice device, Canvas canvas)
    {
        return RenderTargetSession.Open(device, canvas.RenderTarget);
    }

    public static RenderTargetSession Open(this Canvas canvas)
    {
        return RenderTargetSession.Open(canvas.GraphicsDevice, canvas.RenderTarget);
    }

    public static void UseTarget(this GraphicsDevice device, RenderTarget2D target, Action drawAction)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(drawAction);

        using var session = RenderTargetSession.Open(device, target);
        drawAction();
    }
}

public struct RenderTargetSession : IDisposable
{
    private readonly RenderTargetBinding[] _restoreTargets;
    private readonly GraphicsDevice _graphics;
    private bool closed;

    private RenderTargetSession(GraphicsDevice graphics, RenderTarget2D target)
    {
        _graphics = graphics;
        _restoreTargets = graphics.GetRenderTargets();
        _graphics.SetRenderTarget(target);
    }

    public void Close()
    {
        if (!closed)
        {
            closed = true;
            _graphics.SetRenderTargets(_restoreTargets);
        }
    }

    public void Dispose() => Close();

    public static RenderTargetSession Open(GraphicsDevice graphics, RenderTarget2D target)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(target);

        return new RenderTargetSession(graphics, target);
    }
}
