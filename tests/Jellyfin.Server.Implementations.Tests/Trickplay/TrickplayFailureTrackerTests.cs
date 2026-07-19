using System;
using Jellyfin.Server.Implementations.Trickplay;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Trickplay;

public class TrickplayFailureTrackerTests
{
    private const int MaxFailures = 3;
    private const long Size = 1_000_000L;
    private const long OtherSize = 2_000_000L;

    [Fact]
    public void ShouldSkip_NoFailures_ReturnsFalse()
    {
        var tracker = new TrickplayFailureTracker();

        Assert.False(tracker.ShouldSkip(Guid.NewGuid(), Size, MaxFailures));
    }

    [Fact]
    public void ShouldSkip_BelowThreshold_ReturnsFalse()
    {
        var tracker = new TrickplayFailureTracker();
        var id = Guid.NewGuid();
        for (var i = 0; i < MaxFailures - 1; i++)
        {
            tracker.RecordFailure(id, Size);
        }

        Assert.False(tracker.ShouldSkip(id, Size, MaxFailures));
    }

    [Fact]
    public void ShouldSkip_AtThreshold_ReturnsTrue()
    {
        var tracker = new TrickplayFailureTracker();
        var id = Guid.NewGuid();
        for (var i = 0; i < MaxFailures; i++)
        {
            tracker.RecordFailure(id, Size);
        }

        Assert.True(tracker.ShouldSkip(id, Size, MaxFailures));
    }

    [Fact]
    public void ShouldSkip_MediaSizeChanged_ClearsBlockAndReturnsFalse()
    {
        var tracker = new TrickplayFailureTracker();
        var id = Guid.NewGuid();
        for (var i = 0; i < MaxFailures; i++)
        {
            tracker.RecordFailure(id, Size);
        }

        Assert.False(tracker.ShouldSkip(id, OtherSize, MaxFailures));

        // The block was cleared, so the original media size is no longer skipped either.
        Assert.False(tracker.ShouldSkip(id, Size, MaxFailures));
    }

    [Fact]
    public void ShouldSkip_ThresholdDisabled_ReturnsFalse()
    {
        var tracker = new TrickplayFailureTracker();
        var id = Guid.NewGuid();
        for (var i = 0; i < MaxFailures + 2; i++)
        {
            tracker.RecordFailure(id, Size);
        }

        Assert.False(tracker.ShouldSkip(id, Size, 0));
    }

    [Fact]
    public void Clear_RemovesFailures()
    {
        var tracker = new TrickplayFailureTracker();
        var id = Guid.NewGuid();
        for (var i = 0; i < MaxFailures; i++)
        {
            tracker.RecordFailure(id, Size);
        }

        Assert.True(tracker.ShouldSkip(id, Size, MaxFailures));

        tracker.Clear(id);

        Assert.False(tracker.ShouldSkip(id, Size, MaxFailures));
    }

    [Fact]
    public void RecordFailure_SameMedia_IncrementsCount()
    {
        var tracker = new TrickplayFailureTracker();
        var id = Guid.NewGuid();

        Assert.Equal(1, tracker.RecordFailure(id, Size));
        Assert.Equal(2, tracker.RecordFailure(id, Size));
        Assert.Equal(3, tracker.RecordFailure(id, Size));
    }

    [Fact]
    public void RecordFailure_MediaSizeChanged_ResetsCount()
    {
        var tracker = new TrickplayFailureTracker();
        var id = Guid.NewGuid();
        tracker.RecordFailure(id, Size);
        tracker.RecordFailure(id, Size);

        Assert.Equal(1, tracker.RecordFailure(id, OtherSize));
    }
}
