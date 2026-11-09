using System.Windows;
using System.Windows.Threading;
using Xunit;

namespace Dragonmark.UiTests;

[CollectionDefinition("WPF", DisableParallelization = true)]
public sealed class WpfCollection : ICollectionFixture<WpfHost>;

public sealed class WpfHost : IDisposable
{
    private readonly Thread _thread;
    private readonly TaskCompletionSource<Dragonmark.App.App> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public WpfHost()
    {
        _thread = new(() =>
        {
            try
            {
                var app = new Dragonmark.App.App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                app.InitializeComponent();
                _ready.SetResult(app);
                Dispatcher.Run();
            }
            catch (Exception e) { _ready.TrySetException(e); }
        })
        { IsBackground = true };
        _thread.SetApartmentState(ApartmentState.STA); _thread.Start();
    }
    public async Task Run(Func<Task> test)
    {
        var app = await _ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
        await app.Dispatcher.InvokeAsync(test).Task.Unwrap().WaitAsync(TimeSpan.FromSeconds(30));
    }
    public void Dispose()
    {
        if (_ready.Task.IsCompletedSuccessfully) _ready.Task.Result.Dispatcher.Invoke(() => _ready.Task.Result.Shutdown());
        _thread.Join(TimeSpan.FromSeconds(5));
    }
}
