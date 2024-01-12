using System.Diagnostics;
using System.Text.Json;

namespace TextbookDownloader;

internal static class Program
{
    private static async Task<JsonDocument> GetJsonAsync(string url)
    {
        var httpClient = new HttpClient();
        var bookInformationJsonUrlsJson =
            await (await httpClient.GetAsync(url)).Content.ReadAsStringAsync();
        return JsonDocument.Parse(bookInformationJsonUrlsJson);
    }

    public static bool IsChineseCharacter(char c)
    {
        var codePoint = char.ConvertToUtf32(c.ToString(), 0);

        // BMP中的中文字符和SMP中的中文字符
        return codePoint is >= 0x4E00 and <= 0x9FFF or >= 0x20000 and <= 0x215FF;
    }

    private static async Task Main(string[] args)
    {
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
            let labelJsonList = bookInformationJson.GetProperty("label").EnumerateArray()
            let labels = from labelJson in labelJsonList
                select labelJson.GetString()
            let label = labels.First(label =>
            {
                Debugger.Log(0, null, label);
                return IsChineseCharacter(label[0]);
            })
            let tags = label.Split(' ')
            select new KeyValuePair<string, IEnumerable<string>>(id, tags);

        Console.WriteLine(string.Join(',', booksInformation.Select(pair => (pair.Key, string.Join(' ', pair.Value)))));
    }
}