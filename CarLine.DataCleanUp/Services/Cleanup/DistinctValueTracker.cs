namespace CarLine.DataCleanUp.Services.Cleanup;

internal sealed class DistinctValueTracker
{
    private readonly Dictionary<string, HashSet<string>> _distinctValues;

    public DistinctValueTracker(IEnumerable<string> header)
    {
        _distinctValues = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in header)
            _distinctValues[h] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public void TrackRow(IReadOnlyDictionary<string, string> record)
    {
        foreach (var kvp in record)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Value))
                _distinctValues[kvp.Key].Add(kvp.Value);
        }
    }

    public IEnumerable<(string Column, int Count)> GetCountsOrderedByColumn()
    {
        foreach (var kvp in _distinctValues.OrderBy(k => k.Key))
            yield return (kvp.Key, kvp.Value.Count);
    }
}

