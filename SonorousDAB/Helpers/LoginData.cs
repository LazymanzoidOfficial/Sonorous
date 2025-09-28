using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SonorousDAB.Helpers
{
    class LoginData
    {
        // These are static to persist cookies across instances
        public static readonly CookieContainer cookieJar = new CookieContainer();
        public static readonly HttpClientHandler handler = new HttpClientHandler
        {
            CookieContainer = cookieJar,
            UseCookies = true
        };
        public static readonly HttpClient client = new HttpClient(handler);
    }
}
