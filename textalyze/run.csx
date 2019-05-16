#r "Newtonsoft.Json"

using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Rest;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    string TextAnalyticsAPIKey = Environment.GetEnvironmentVariable("text_analytics_api_key");
    log.LogInformation($"found key: {TextAnalyticsAPIKey}");


    log.LogInformation("C# HTTP trigger function processed a request.");

    string text = req.Query["text"];
    string result = "";

    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    dynamic data = JsonConvert.DeserializeObject(requestBody);




    text = text ?? data?.text;

    return text != null
        ? (ActionResult)new OkObjectResult($"Hello, {text}")
        : new BadRequestObjectResult("Please pass the text input for the text analytics operations");
}
