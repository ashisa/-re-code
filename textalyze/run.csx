#r "Newtonsoft.Json"

using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Rest;
using System.Threading;
using System.Threading.Tasks;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    string textAnalyticsAPIKey = Environment.GetEnvironmentVariable("text_analytics_api_key");
    string textAnalyticsEndpoint = Environment.GetEnvironmentVariable("text_analytics_endpoint");

    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    dynamic data = JsonConvert.DeserializeObject(requestBody);
    string inputText = data.text;

    var credentials = new ApiKeyServiceClientCredentials(textAnalyticsAPIKey);
    var client = new TextAnalyticsClient(credentials)
    {
        Endpoint = textAnalyticsEndpoint
    };

    dynamic result = new JObject();

    //Detecting language first
    var inputDocuments = new LanguageBatchInput(
            new List<LanguageInput>
                {
                    new LanguageInput(id: "1", text: inputText)
                });

    var langResults = await client.DetectLanguageAsync(false, inputDocuments);
    string inputLanguage = null;
    foreach (var document in langResults.Documents)
    {
        inputLanguage = document.DetectedLanguages[0].Iso6391Name;
    }

    result.language = inputLanguage;
    log.LogInformation($"{result.ToString()}");

    //Detecting sentiment of the input text
    var inputDocuments2 = new MultiLanguageBatchInput(
    new List<MultiLanguageInput>
    {
            new MultiLanguageInput(inputLanguage, "1", inputText)
    });

    var sentimentResult = await client.SentimentAsync(false, inputDocuments2);
    double? sentimentScore = 0;
    foreach (var document in sentimentResult.Documents)
    {
        sentimentScore = document.Score;
    }

    result.sentimentScore = sentimentScore;
    log.LogInformation($"{result.ToString()}");

    //Detecting entities in the text
    var entitiesResult = await client.EntitiesAsync(false, inputDocuments2);
    JArray entities = new JArray();
    foreach (var document in entitiesResult.Documents)
    {
        dynamic entityObject = new JObject();
        foreach (var entity in document.Entities)
        {
            entityObject.name = entity.Name;
            entityObject.type = entity.Type;
            entityObject.subtype = entity.SubType;
            foreach (var match in entity.Matches)
            {
                entityObject.offset = match.Offset;
                entityObject.length = match.Length;
                entityObject.score = match.EntityTypeScore;
                //log.LogInformation($"\t\t\tOffset: {match.Offset},\tLength: {match.Length},\tScore: {match.EntityTypeScore:F3}");
            }
            entities.Add(entityObject);
        }
    }
    result.entities = entities;
    log.LogInformation($"{result.ToString()}");

    //Detecting keyphrases
    var kpResults = await client.KeyPhrasesAsync(false, inputDocuments2);
    JArray keyPhrases = new JArray();

    // Printing keyphrases
    foreach (var document in kpResults.Documents)
    {
        dynamic phraseObject = new JObject();
        foreach (string keyphrase in document.KeyPhrases)
        {
            phraseObject.keyPhrase = keyphrase;
        }
        keyPhrases.Add(phraseObject);
    }
    result.keyphrases = keyPhrases;
    log.LogInformation($"{result.ToString()}");


    return inputText != null
        ? (ActionResult)new OkObjectResult($"{result.ToString()}")
        : new BadRequestObjectResult("{ \"error\": \"Please pass the text input for the text analytics operations\"");
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
