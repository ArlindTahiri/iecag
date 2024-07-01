using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HealthCheckFunction
{
    public class HealthCheck
    {
        private readonly ILogger<HealthCheck> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public HealthCheck(ILogger<HealthCheck> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [Function("HealthCheck")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var endpoints = _configuration["RESOURCE_ENDPOINTS"].Split(';');
            var tasks = new List<Task<ResourceStatus>>();

            foreach (var endpoint in endpoints)
            {
                tasks.Add(CheckResourceAsync(endpoint));
            }

            var results = await Task.WhenAll(tasks);

            var jsonResult = JsonConvert.SerializeObject(results);
            return new OkObjectResult(jsonResult);
        }

        private async Task<ResourceStatus> CheckResourceAsync(string endpoint)
        {
            try
            {
                var isOnline = await IsResourceOnlineAsync(endpoint);
                return new ResourceStatus
                {
                    Resource = endpoint,
                    Online = isOnline
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking status of resource: {endpoint}");
                return new ResourceStatus
                {
                    Resource = endpoint,
                    Online = false
                };
            }
        }

        private async Task<bool> IsResourceOnlineAsync(string url)
        {
            try
            {
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }

                var response = await _httpClient.GetAsync(new Uri(url));
                return response != null; // true zurückgeben, wenn eine Antwort empfangen wurde
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking status of resource: {url}");
                return false;
            }
        }



        private class ResourceStatus
        {
            public string Resource { get; set; }
            public bool Online { get; set; }
        }
    }
}
