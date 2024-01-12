using System.Text.Json;

namespace TextbookDownloader;

internal readonly struct Book(string id, IEnumerable<string> tags, string title)
{
    public readonly string Id = id;
    public readonly IEnumerable<string> Tags = tags;
    public readonly string Title = title;
}

internal static class Program
{
    private static async Task<JsonDocument> GetJsonAsync(string url)
    {
        var httpClient = new HttpClient();
        var bookInformationJsonUrlsJson =
            await (await httpClient.GetAsync(url)).Content.ReadAsStringAsync();
        return JsonDocument.Parse(bookInformationJsonUrlsJson);
    }

    private static IEnumerable<string> GetTags(JsonElement.ArrayEnumerator tagJsonList, JsonElement tagsTreeNode)
    {
        var result = new List<string>();

        var queue = new Queue<JsonElement>();
        queue.Enqueue(tagsTreeNode);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.TryGetProperty("tag_id", out var tagTreeNodeIdJson) && tagJsonList.Any(tagJson =>
                    tagJson.GetProperty("tag_id").GetString() == tagTreeNodeIdJson.GetString()))
            {
                result.Add(current.GetProperty("tag_name").GetString()!);
            }

            var hierarchiesJson = current.GetProperty("hierarchies");
            if (hierarchiesJson.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            var children = hierarchiesJson.EnumerateArray().First().GetProperty("children").EnumerateArray();
            foreach (var child in children)
            {
                queue.Enqueue(child);
            }
        }

        return result;
    }

    private static async Task Main(string[] args)
    {
        var tagsJson = await GetJsonAsync("https://s-file-1.ykt.cbern.com.cn/zxx/ndrs/tags/tch_material_tag.json");

        var bookInformationJsonListUrlsJson =
            await GetJsonAsync(
                "https://s-file-2.ykt.cbern.com.cn/zxx/ndrs/resources/tch_material/version/data_version.json");
        var bookInformationJsonListUrls =
            bookInformationJsonListUrlsJson.RootElement.GetProperty("urls").GetString()!.Split(',');
        var booksInformation =
            from bookInformationListJsonUrl in bookInformationJsonListUrls
            let bookInformationListJson = GetJsonAsync(bookInformationListJsonUrl).Result
            from bookInformationJson in bookInformationListJson.RootElement.EnumerateArray()
            let id = bookInformationJson.GetProperty("id").GetString()
            let tagJsonList = bookInformationJson.GetProperty("tag_list").EnumerateArray()
            let tags = GetTags(tagJsonList, tagsJson.RootElement)
            let title = bookInformationJson.GetProperty("title").GetString()
            select new Book(id, tags, title);

        Console.WriteLine(string.Join(',',
            booksInformation.Select(book => $"id:{book.Id};tags:{string.Join(' ', book.Tags)};title:{book.Title}\n")));
    }
}