using Microsoft.UI.Dispatching;
using UniversalVideoDownloader.ViewModels.Services;

namespace UniversalVideoDownloader.App.Services;

internal sealed class DispatcherQueueUiDispatcher(DispatcherQueue queue) : IUiDispatcher
{
    // TryEnqueue fails only once the UI thread is shutting down; dropping work then is fine.
    public void Post(Action action) => queue.TryEnqueue(() => action());
}
