namespace Ditto;

public sealed record PageCollection(
    string Name,
    IReadOnlyList<Page> Pages);

public static class PageCollectionFactory {
    public static IEnumerable<PageCollection> Create(IEnumerable<Page> pages) {
        var pageDict = new Dictionary<string, List<Page>>();

        // collections are derived from the first segment of the url
        // only pages with n > 1 subpages are included in collections
        foreach (var page in pages) {
            if (page.Path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is string[] segments
                && segments.Length > 1) {
                var collectionName = segments[0];

                if (pageDict.TryGetValue(collectionName, out var value)) {
                    pageDict[collectionName].Add(page);
                }
                else {
                    pageDict[collectionName] = [page];
                }
            }
        }

        foreach (var kvp in pageDict) {
            yield return new(kvp.Key, kvp.Value.AsReadOnly());
        }
    }
}
