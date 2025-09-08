// namespace Ditto;

// public interface IPageWriter {
//     Task Write(TextWriter output, Page page, PageCollection collections);
// }

// public sealed class PageWriter(IViewEngine viewEngine, SiteConfig siteConfig) : IPageWriter {
//     public async Task Write(TextWriter output, Page page, PageCollection collections) {
//         var pageContent = await viewEngine.Render(
//             page.View,
//             new(Path: page.Path,
//                 FullUrl: page.Url,
//                 Title: page.Title,
//                 Description: page.Description,
//                 Data: page.Metadata,
//                 Site: siteConfig,
//                 Collections: collections));

//         if (pageContent is not null) {
//             await output.WriteAsync(pageContent);
//         }
//     }
// }
