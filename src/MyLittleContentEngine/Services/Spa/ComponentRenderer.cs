using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace MyLittleContentEngine.Services.Spa;

/// <summary>
/// Renders Blazor components to HTML strings using the static <see cref="HtmlRenderer"/>.
/// Registered as <b>Scoped</b> so a single renderer is shared across all slot renders
/// within a request, then disposed at scope end.
/// </summary>
/// <remarks>
/// Components rendered this way support <c>@inject</c> for DI services but
/// cannot use JavaScript interop, <c>NavigationManager</c>, or other browser-dependent APIs.
/// </remarks>
public sealed class ComponentRenderer(
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory) : IAsyncDisposable
{
    private readonly HtmlRenderer _renderer = new(serviceProvider, loggerFactory);

    /// <summary>
    /// Renders a Blazor component to an HTML string.
    /// </summary>
    /// <typeparam name="TComponent">The component type to render.</typeparam>
    /// <param name="parameters">Optional parameter dictionary matching the component's <c>[Parameter]</c> properties.</param>
    /// <returns>The rendered HTML string.</returns>
    public Task<string> RenderComponentAsync<TComponent>(
        IDictionary<string, object?>? parameters = null)
        where TComponent : IComponent
    {
        return _renderer.Dispatcher.InvokeAsync(async () =>
        {
            var pv = parameters is not null
                ? ParameterView.FromDictionary(parameters)
                : ParameterView.Empty;

            var output = await _renderer.RenderComponentAsync<TComponent>(pv);
            return output.ToHtmlString();
        });
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _renderer.DisposeAsync();
    }
}
