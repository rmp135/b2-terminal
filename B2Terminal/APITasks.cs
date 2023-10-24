using System.Net.Http.Headers;
using B2Net;
using B2Net.Models;

namespace B2Terminal;

public interface IAPITasks
{
    Task<IEnumerable<B2Bucket>> GetBucketsAsync();

    Task<IEnumerable<B2File>> GetFilesAsync(
        string bucketId,
        string prefix = ""
    );

    Task Authorise(string keyId, string applicationKey);
    
    Task<HttpResponseMessage> DownloadFile(
        string fileId,
        CancellationToken cancellationToken,
        long beginAt,
        long endAt
    );

    Task<HttpResponseMessage> UploadFile(
        Stream fileStream,
        CancellationToken cancellationToken,
        string bucketId,
        string fileName,
        string sha1Hash
    );
}

public class APITasks : IAPITasks
{

    private B2Client? Client { get; set; }
    private B2Options? Options { get; set; }
    
    private IHttpClientFactory HttpClientFactory { get; }
    
    public APITasks(
        IHttpClientFactory httpClientFactory
    )
    {
        HttpClientFactory = httpClientFactory;
    }

    public async Task Authorise(
        string keyId,
        string applicationKey
    )
    {
        if (Client is null)
        {
            var options = new B2Options
            {
                KeyId = keyId,
                ApplicationKey = applicationKey
            };
            Options = await B2Client.AuthorizeAsync(options);
            Client = new B2Client(options);
        }
    }
    
    public async Task<IEnumerable<B2Bucket>> GetBucketsAsync()
    {
        var buckets = await Client.Buckets.GetList();
        return buckets;
    }
    
    public async Task<IEnumerable<B2File>> GetFilesAsync(
        string bucketId,
        string prefix = ""
    )
    {
        var buckets = await Client.Files.GetListWithPrefixOrDemiliter(
            bucketId: bucketId,
            delimiter:"/",
            prefix: prefix
        );
        return buckets.Files;
    }

    public async Task<HttpResponseMessage> DownloadFile(
        string fileId,
        CancellationToken cancellationToken,
        long beginAt,
        long endAt
    )
    {
        var client = new HttpClient();
        var url = $"{Options.DownloadUrl}/b2api/v2/b2_download_file_by_id?fileId={fileId}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (endAt != 0)
        {
            request.Headers.TryAddWithoutValidation("Range", $"bytes={beginAt}-");
        }
        request.Headers.TryAddWithoutValidation("Authorization", Options.AuthorizationToken);
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return response;
    }

    public async Task<HttpResponseMessage> UploadFile(
        Stream fileStream,
        CancellationToken cancellationToken,
        string bucketId,
        string fileName,
        string sha1Hash
    )
    {
        var uploadUrl = await Client.Files.GetUploadUrl(bucketId, cancellationToken);
        
        var httpClient = HttpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl.UploadUrl);
        
        using var content = new StreamContent(fileStream);
        request.Content = content;
        request.Headers.TryAddWithoutValidation("Authorization", uploadUrl.AuthorizationToken);
        request.Headers.Add("X-Bz-File-Name", fileName);
        request.Headers.Add("X-Bz-Content-Sha1", sha1Hash.ToLower());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("b2/x-auto");
        request.Content.Headers.ContentLength = fileStream.Length;
        
        return await httpClient.SendAsync(request, cancellationToken);
    }
}