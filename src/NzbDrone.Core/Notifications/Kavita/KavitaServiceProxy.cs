using System.Net.Http;
using System.Text.Json;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Notifications.Kavita;

public interface IKavitaServiceProxy
{
    string GetBaseUrl(KavitaSettings settings, string relativePath = null);
    void Notify(KavitaSettings settings, string message);
    string GetToken(KavitaSettings settings);
}

public class KavitaServiceProxy : IKavitaServiceProxy
{
    private readonly IHttpClient _httpClient;
    private readonly Logger _logger;

    public KavitaServiceProxy(IHttpClient httpClient, Logger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public string GetBaseUrl(KavitaSettings settings, string relativePath = null)
    {
        var baseUrl = HttpRequestBuilder.BuildBaseUrl(settings.UseSsl, settings.Host, settings.Port, string.Empty);
        baseUrl = HttpUri.CombinePath(baseUrl, relativePath);

        return baseUrl;
    }

    public void Notify(KavitaSettings settings, string folderPath)
    {
        var request = GetKavitaServerRequest("library/scan-folder", HttpMethod.Post, settings);
        request.Headers.ContentType = "application/json";
        var postRequest = request.Build();
        postRequest.SetContent(new
        {
            ApiKey = settings.ApiKey,
            FolderPath = folderPath.Replace("/", "//")
        }.ToJson());

        var response = _httpClient.Post(postRequest);
        _logger.Trace("Update response: {0}", string.IsNullOrEmpty(response.Content) ? "Success" : response.Content);
    }

    public string GetToken(KavitaSettings settings)
    {
        var request = GetKavitaServerRequest("plugin/authenticate", HttpMethod.Post, settings);
        request.AddQueryParam("apiKey", settings.ApiKey)
            .AddQueryParam("pluginName", BuildInfo.AppName);
        var response = _httpClient.Execute(request.Build());

        _logger.Trace("Authenticate response: {0}", response.Content);

        var authResult = JsonSerializer.Deserialize<KavitaAuthenticationResult>(response.Content);

        if (authResult == null)
        {
            throw new KavitaException("Could not authenticate with Kavita");
        }

        return authResult.Token;
    }

    private HttpRequestBuilder GetKavitaServerRequest(string resource, HttpMethod method, KavitaSettings settings)
    {
        var client = new HttpRequestBuilder(GetBaseUrl(settings, "api"));

        client.Resource(resource);

        if (settings.ApiKey.IsNotNullOrWhiteSpace())
        {
            client.Headers["x-kavita-apikey"] = settings.ApiKey;
            client.Headers["x-kavita-plugin"] = BuildInfo.AppName;
        }

        client.Method = method;

        return client;
    }
}
