using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace WebApp_OpenIDConnect_DotNetCore21.Controllers
{
    public class HelloController : Controller
    {
        private readonly HttpClient _client;
        private readonly AzureADB2CWithApiOptions _options;

        public HelloController(IOptions<AzureADB2CWithApiOptions> optionsAccessor, HttpClient client)
        {
            _options = optionsAccessor.Value;
            _client = client;
        }

        public async Task<IActionResult> Index()
        {
            string content = "";

            var clientCredential = new ClientCredential(_options.ClientSecret);
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var tokenCache = new SessionTokenCache(HttpContext, userId);

            var confidentialClientApplication = new ConfidentialClientApplication(
                _options.ClientId,
                _options.Authority,
                _options.RedirectUri,
                clientCredential,
                tokenCache.GetInstance(),
                null);

            var authenticationResult = await confidentialClientApplication.AcquireTokenSilentAsync(
                _options.ApiScopes.Split(' '),
                confidentialClientApplication.Users.FirstOrDefault(),
                _options.Authority,
                false);

            using (var request = new HttpRequestMessage(HttpMethod.Get, _options.ApiUrl))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);

                using (var response = await _client.SendAsync(request))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        content = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        content = "Error";
                    }
                }
            }

            ViewData["Content"] = $"{content}";
            return View();
        }
    }
}
