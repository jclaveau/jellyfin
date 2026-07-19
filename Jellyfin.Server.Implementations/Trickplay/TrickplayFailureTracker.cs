using System;
using System.Collections.Concurrent;

namespace Jellyfin.Server.Implementations.Trickplay;

/// <summary>
/// Tracks consecutive trickplay-generation failures per item to skip a permanently-failing item.
/// Failures are keyed to media size, so re-encoding the media clears the block but a metadata
/// refresh does not. In-memory; resets on restart.
/// </summary>
internal sealed class TrickplayFailureTracker
{
    private readonly ConcurrentDictionary<Guid, (long MediaSize, int Count)> _failures = new();

    /// <summary>
    /// Determines whether an item should be skipped due to repeated failures.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="mediaSize">The current media size in bytes; a change clears the record.</param>
    /// <param name="maxFailures">The failure threshold; 0 or less disables skipping.</param>
    /// <returns><c>true</c> if the item should be skipped; otherwise <c>false</c>.</returns>
    public bool ShouldSkip(Guid itemId, long mediaSize, int maxFailures)
    {
        if (maxFailures <= 0 || !_failures.TryGetValue(itemId, out var failure))
        {
            return false;
        }

        if (failure.MediaSize != mediaSize)
        {
            _failures.TryRemove(itemId, out _);
            return false;
        }

        return failure.Count >= maxFailures;
    }

    /// <summary>
    /// Records a generation failure for an item. A change in media size resets the count.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="mediaSize">The current media size in bytes.</param>
    /// <returns>The consecutive failure count after recording.</returns>
    public int RecordFailure(Guid itemId, long mediaSize)
    {
        return _failures.AddOrUpdate(
            itemId,
            _ => (mediaSize, 1),
            (_, existing) => existing.MediaSize == mediaSize
                ? (existing.MediaSize, existing.Count + 1)
                : (mediaSize, 1)).Count;
    }

    /// <summary>
    /// Clears any recorded failures for an item, e.g. after a success or when the item is removed.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    public void Clear(Guid itemId)
    {
        _failures.TryRemove(itemId, out _);
    }
}
