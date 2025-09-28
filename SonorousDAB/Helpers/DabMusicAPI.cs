using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace SonorousDAB
{
    public class DabMusicAPI
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private const string BaseUrl = "https://dabmusic.xyz/api"; // Replace with actual base
        public static readonly CookieContainer Cookies = new CookieContainer();
        public static readonly HttpClientHandler Handler = new HttpClientHandler
        {
            CookieContainer = Cookies,
            UseCookies = true
        };

        public static readonly HttpClient Client = new HttpClient(Handler);
        public async Task<List<Track>> SearchTracksAsync(string query)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/search?q={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var searchResult = JsonConvert.DeserializeObject<SearchResponse>(json);
            return searchResult.Tracks;
        }

    }
}
