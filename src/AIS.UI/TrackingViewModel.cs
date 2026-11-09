using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AIS.Core;
namespace AIS.UI;
public sealed class TrackingViewModel(ITrackingClient client) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string TrackingNumber { get; set; } = "LH-2026-000003";
    public ObservableCollection<TrackingEvent> Events { get; } = new();
    private bool _isBusy;
    public bool IsBusy { get => _isBusy; private set { _isBusy = value; Changed(); } }
    private string? _error;
    public string? Error { get => _error; private set { _error = value; Changed(); } }
    public async Task LoadAsync(CancellationToken token = default)
    {
        if (IsBusy) return;
        IsBusy = true; Error = null; Events.Clear();
        try
        {
            var page = await client.GetAsync(TrackingNumber.Trim().ToUpperInvariant(), 50, token);
            foreach (var item in page.Events) Events.Add(item);
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
        { Error = "История недоступна. Проверьте подключение и права доступа."; }
        finally { IsBusy = false; }
    }
    private void Changed([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
