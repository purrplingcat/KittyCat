using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Toolkit.DI;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Defines a contract for a render pipeline provider.
/// </summary>
public interface IRenderPipelineFactory
{
    event Action<RenderPipeline> RenderPipelineCreated;

    /// <summary>
    /// Creates and returns a NEW, independent instance of the render pipeline on each call.
    /// </summary>
    /// <returns>A new instance of <see cref="RenderPipeline"/>.</returns>
    RenderPipeline Create();
}

/// <summary>
/// A concrete implementation of <see cref="IRenderPipelineFactory"/> that uses an <see cref="IServiceProvider"/>
/// to construct a <see cref="RenderPipeline"/> from registered <see cref="IRenderer"/> services.
/// </summary>
public class RenderPipelineFactory : IRenderPipelineFactory
{
    private readonly PipelineBuilder _pipelineBuilder;

    public event Action<RenderPipeline>? RenderPipelineCreated;


    /// <summary>
    /// Initializes a new instance of the <see cref="RenderPipelineFactory"/> class
    /// that creates pipelines using ALL non-keyed <see cref="IRenderer"/> services.
    /// </summary>
    /// <param name="provider">The service provider from which the renderers are retrieved.</param>
    public RenderPipelineFactory(IServiceProvider provider, ISetup<IRenderPipelineBuilder> setup)
    {
        _pipelineBuilder = new PipelineBuilder(provider);
        setup.Configure(_pipelineBuilder);
    }

    /// <inheritdoc />
    public RenderPipeline Create()
    {
        var pipeline = _pipelineBuilder.Build();
        RenderPipelineCreated?.Invoke(pipeline);
        return pipeline;
    }

    private class PipelineBuilder(IServiceProvider provider) : IRenderPipelineBuilder
    {
        private readonly Dictionary<Type, Func<IServiceProvider, IRenderer>> _factories = [];
        private readonly IServiceProvider _provider = provider;

        IRenderPipelineBuilder IRenderPipelineBuilder.AddRenderer<TRenderer>()
        {
            var key = typeof(TRenderer);
            _factories[key] = provider => ActivatorUtilities.CreateInstance<TRenderer>(provider);
            return this;
        }

        IRenderPipelineBuilder IRenderPipelineBuilder.AddRenderer<TRenderer>(Func<IServiceProvider, TRenderer> factory)
        {
            var key = typeof(TRenderer);
            _factories[key] = factory;
            return this;
        }

        public RenderPipeline Build()
        {
            var renderers = new List<IRenderer>(_factories.Count);

            foreach (var factory in _factories.Values)
            {
                renderers.Add(factory.Invoke(_provider));
            }

            return new RenderPipeline(renderers);
        }
    }
}
