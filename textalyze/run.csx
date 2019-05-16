#r "Newtonsoft.Json"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
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
