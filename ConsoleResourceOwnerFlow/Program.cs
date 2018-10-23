using IdentityModel;
using IdentityModel.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleResourceOwnerFlow
{
    internal class Program
    {
        private static async Task Main()
        {
            Console.Title = "Console ResourceOwner Flow";
            var response = await RequestTokenAsync();
            Show(response);
            Console.ReadLine();
            await CallServiceAsync(response.AccessToken);
        }

        private static async Task<TokenResponse> RequestTokenAsync()
        {
            var disco = await DiscoveryClient.GetAsync(Constants.Authority);
            if (disco.IsError) throw new Exception(disco.Error);

            var client = new TokenClient(disco.TokenEndpoint, Constants.Client, Constants.Secret);

            // idsrv supports additional non-standard parameters 
            // that get passed through to the user service
            //var optional = new
            //{
            //    acr_values = "tenant:custom_account_store1 foo bar quux"
            //};

            //return await client.RequestResourceOwnerPasswordAsync("bob", "bob", "api1 api2.read_only", optional);
            Console.WriteLine("Enter username:");
            var username = Console.ReadLine();
            Console.WriteLine("Enter password:");
            var password = Console.ReadLine();
            return await client.RequestResourceOwnerPasswordAsync(username, password, Constants.Scope);
        }

        private static async Task CallServiceAsync(string token)
        {
            var baseAddress = Constants.SampleApi;
            var client = new HttpClient
            {
                BaseAddress = new Uri(baseAddress)
            };
            client.SetBearerToken(token);
            var response = await client.GetAsync("/api/v1/whoami/");
            var result  = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Whoami:");
            Console.WriteLine(JObject.Parse(result));
        }


        private static void Show(TokenResponse response)
        {
            if (!response.IsError)
            {
                Console.WriteLine("Token response:");
                Console.WriteLine(response.Json);
                if (response.AccessToken.Contains("."))
                {
                    Console.WriteLine("\nAccess Token (decoded):");
                    var parts = response.AccessToken.Split('.');
                    var header = parts[0];
                    var claims = parts[1];
                    Console.WriteLine(JObject.Parse(Encoding.UTF8.GetString(Base64Url.Decode(header))));
                    Console.WriteLine(JObject.Parse(Encoding.UTF8.GetString(Base64Url.Decode(claims))));
                }
            }
            else
            {
                if (response.ErrorType == ResponseErrorType.Http)
                {
                    Console.WriteLine("HTTP error: ");
                    Console.WriteLine(response.Error);
                    Console.WriteLine("HTTP status code: ");
                    Console.WriteLine(response.HttpStatusCode);
                }
                else
                {
                    Console.WriteLine("Protocol error response:");
                    Console.WriteLine(response.Raw);
                }
            }
        }
    }
}