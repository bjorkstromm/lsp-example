using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Server
{
    internal class NuGetAutoCompleteService : IDisposable
    {
        private HttpClient _client = new HttpClient();

        public async Task<IReadOnlyCollection<string>> GetPackages(string query)
        {
            var response = await _client.GetStringAsync($"https://api-v2v3search-0.nuget.org/autocomplete?q={query}");
            return JObject.Parse(response)["data"].ToObject<List<string>>();
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task<IReadOnlyCollection<string>> GetPackageVersions(string package, string version)
        {
            var response = await _client.GetStringAsync($"https://api-v2v3search-0.nuget.org/autocomplete?id={package}");
            return JObject.Parse(response)["data"].ToObject<List<string>>();
        }
    }
}
