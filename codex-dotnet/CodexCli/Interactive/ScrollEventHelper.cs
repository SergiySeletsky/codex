// Mirrors codex-rs/tui/src/scroll_event_helper.rs (done)
using System;
using System.Threading;
using System.Threading.Tasks;
using CodexCli.Protocol;

namespace CodexCli.Interactive;

/// <summary>
/// Debounces scroll wheel events so we can report the cumulative delta
/// after a short delay. Integration with TuiApp pending.
/// </summary>
public sealed class ScrollEventHelper
{
    private readonly AppEventSender _sender;
    private int _scrollDelta;
    private int _timerScheduled;
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromMilliseconds(100);

    public ScrollEventHelper(AppEventSender sender)
    {
        _sender = sender;
    }

    public void ScrollUp()
    {
        Interlocked.Decrement(ref _scrollDelta);
        ScheduleNotification();
    }

    public void ScrollDown()
    {
        Interlocked.Increment(ref _scrollDelta);
        ScheduleNotification();
    }

    private void ScheduleNotification()
    {
        if (Interlocked.CompareExchange(ref _timerScheduled, 1, 0) != 0)
            return;

        Task.Run(async () =>
        {
            await Task.Delay(DebounceWindow);
            var delta = Interlocked.Exchange(ref _scrollDelta, 0);
            if (delta != 0)
            {
                _sender.Send(new ScrollEvent(Guid.NewGuid().ToString(), delta));
            }
            Volatile.Write(ref _timerScheduled, 0);
        });
    }
}
