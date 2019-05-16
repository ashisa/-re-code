#r "Newtonsoft.Json"

using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Rest;
using System.Threading;
using System.Threading.Tasks;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    string textAnalyticsAPIKey = Environment.GetEnvironmentVariable("text_analytics_api_key");
    string textAnalyticsEndpoint = Environment.GetEnvironmentVariable("text_analytics_endpoint");

    log.LogInformation($"found key: {textAnalyticsAPIKey}");

    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    dynamic data = JsonConvert.DeserializeObject(requestBody);
    string text = data.text;

    var credentials = new ApiKeyServiceClientCredentials(textAnalyticsAPIKey);
    var client = new TextAnalyticsClient(credentials)
    {
        Endpoint = textAnalyticsEndpoint
    };

    string language = await DetectLanguage(client, text);
    log.LogInformation($"Detected: {language}");

    var inputDocuments = new LanguageBatchInput(
            new List<LanguageInput>
                {
                    new LanguageInput(id: "1", text: text)
                });

    var langResults = await client.DetectLanguageAsync(false, inputDocuments);
    foreach (var document in langResults.Documents)
    {
        log.LogInformation($"Document ID: {document.Id} , Language: {document.DetectedLanguages[0].Iso6391Name}");
    }

    return text != null
        ? (ActionResult)new OkObjectResult($"Hello, {text}")
        : new BadRequestObjectResult("Please pass the text input for the text analytics operations");
}

public static async Task<string> DetectLanguage(TextAnalyticsClient client, string text)
{
    var inputDocuments = new LanguageBatchInput(
        new List<LanguageInput>
            {
                    new LanguageInput(id: "1", text: text)
            });

    var langResults = await client.DetectLanguageAsync(false, inputDocuments);
    foreach (var document in langResults.Documents)
    {
        log.LogInformation($"Document ID: {document.Id} , Language: {document.DetectedLanguages[0].Iso6391Name}");
    }

    return document.DetectedLanguages[0].Iso6391Name;
}

class ApiKeyServiceClientCredentials : ServiceClientCredentials
{
    private readonly string subscriptionKey;

    /// <summary>
    /// Creates a new instance of the ApiKeyServiceClientCredentails class
    /// </summary>
    /// <param name="subscriptionKey">The subscription key to authenticate and authorize as</param>
    public ApiKeyServiceClientCredentials(string subscriptionKey)
    {
        this.subscriptionKey = subscriptionKey;
    }

    /// <summary>
    /// Add the Basic Authentication Header to each outgoing request
    /// </summary>
    /// <param name="request">The outgoing request</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException("request");
        }

        request.Headers.Add("Ocp-Apim-Subscription-Key", this.subscriptionKey);
        return base.ProcessHttpRequestAsync(request, cancellationToken);
    }
}
