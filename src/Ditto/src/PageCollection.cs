// namespace Ditto;

// public sealed class PageCollection {
//     private readonly Dictionary<string, IReadOnlyList<PageInfo>> _pages = [];

//     internal PageCollection(Dictionary<string, List<PageInfo>> pages) {
//         _pages = pages.ToDictionary(
//             kvp => kvp.Key,
//             kvp => (IReadOnlyList<PageInfo>)kvp.Value.AsReadOnly(),
//             StringComparer.OrdinalIgnoreCase);
//     }

//     public static PageCollection Empty => new([]);

//     public IReadOnlyList<PageInfo> this[string collection] =>
//         _pages.TryGetValue(collection, out var pages) ? pages : [];
// }

// public interface IPageCollectionProcessor {
//     PageCollection Process(IEnumerable<PageInfo> pages);
// }

// public sealed class PageCollectionProcessor : IPageCollectionProcessor {
//     public PageCollection Process(IEnumerable<PageInfo> pages) {
//         var pageDict = new Dictionary<string, List<PageInfo>>();

//         // collections are derived from the first segment of the url
//         // only pages with n > 1 subpages are included in collections
//         foreach (var page in pages) {
//             if (page.Path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is string[] segments
//                 && segments.Length > 1) {
//                 var collectionName = segments[0];

//                 if (pageDict.TryGetValue(collectionName, out var value)) {
//                     pageDict[collectionName].Add(page);
//                 }
//                 else {
//                     pageDict[collectionName] = [page];
//                 }
//             }
//         }

//         return new(pageDict);
//     }
// }
