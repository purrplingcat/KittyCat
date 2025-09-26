namespace PurrplingCore.Toolkit.DI;

public interface ISetup<in T>
{
    void Configure(T service);
}

internal sealed class Setup<T>(IEnumerable<Setup<T>.SetupAction> actions) : ISetup<T>
{
    internal sealed class SetupAction(Action<T> configure)
    {
        public void Execute(T service) => configure(service);
    }

    public void Configure(T service)
    {
        foreach (var action in actions)
        {
            action.Execute(service);
        }
    }
}
