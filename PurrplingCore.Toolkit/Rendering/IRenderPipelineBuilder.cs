using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Toolkit.Rendering;

public interface IRenderPipelineBuilder
{
    IRenderPipelineBuilder AddRenderer<TRenderer>() where TRenderer : class, IRenderer;
    IRenderPipelineBuilder AddRenderer<TRenderer>(Func<IServiceProvider, TRenderer> factory) where TRenderer : class, IRenderer;
}
