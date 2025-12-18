using PurrplingCore.Toolkit.Extensions;
using System.Reflection;
using static PurrplingCore.Toolkit.Messaging.IMessageBus;

namespace PurrplingCore.Toolkit.Messaging;

internal sealed class DefaultMessageBus : IMessageBus
{
    private readonly Dictionary<Type, List<WeakHandler>> _subscribers = [];
    private readonly object _lock = new();

    public void Publish<T>(in T message) where T : notnull
    {
        var messageType = typeof(T);

        lock (_lock)
        {
            if (!_subscribers.TryGetValue(messageType, out var handlers))
            {
                return;
            }

            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                var handler = handlers[i];
                if (!handler.Invoke(message))
                {
                    handlers.RemoveAt(i); // Auto clean (GC collected)
                }
            }
        }
    }

    public ISubscription Subscribe<T>(Subscriber<T> subscriber)
    {
        var messageType = typeof(T);
        WeakHandler handler;

        lock (_lock)
        {
            if (!_subscribers.TryGetValue(messageType, out var handlers))
            {
                handlers = [];
                _subscribers[messageType] = handlers;
            }

            handler = new WeakHandler(subscriber);
            handlers.Add(handler);
        }

        return new Subscription<T>(this, handler);
    }

    private void UnsubscribeInternal<T>(WeakHandler handler)
    {
        var messageType = typeof(T);
        lock (_lock)
        {
            if (_subscribers.TryGetValue(messageType, out var handlers))
            {
                handlers.Remove(handler);
            }
        }
    }

    #region Helper classes

    private class Subscription<T>(DefaultMessageBus bus, WeakHandler handler) : ISubscription
    {
        private readonly DefaultMessageBus _bus = bus;
        private readonly WeakHandler _handler = handler;
        private bool _disposed;

        public bool IsAlive => !_disposed;

        public void Unsubscribe() => Dispose();

        public void Dispose()
        {
            if (!_disposed)
            {
                _bus.UnsubscribeInternal<T>(_handler);
                _disposed = true;
            }
        }
    }

    private class WeakHandler
    {
        private readonly WeakReference? _target;
        private readonly object? _strongTarget;
        private readonly MethodInfo _method;
        

        public WeakHandler(Delegate handler)
        {
            _method = handler.Method;
            
            if (handler.Target != null)
            {
                if (handler.Target.GetType().IsGenerated())
                {
                    // Keep Auto-generated target alive (e.g., lambda expressions)
                    _strongTarget = handler.Target;
                }
                else
                {
                    // Use weak reference for normal instance methods
                    _target = new WeakReference(handler.Target);
                }
            }
        }

        public bool Invoke(object message)
        {
            // Is lambda?
            if (_strongTarget != null)
            {
                _method.Invoke(_strongTarget, [message]);
                return true;
            }

            // Is static method?
            if (_target == null)
            {
                _method.Invoke(null, [message]);
                return true;
            }

            // Is instance method?
            if (_target.IsAlive && _target.Target is object target)
            {
                _method.Invoke(target, [message]);
                return true;
            }

            return false;
        }
    }
    #endregion
}
