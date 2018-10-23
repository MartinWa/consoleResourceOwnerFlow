using IdentityModel;
using IdentityModel.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleResourceOwnerFlow
{
    internal class Program
    {
        private static Dictionary<string, (TokenResponse, DateTime)> _tokens = new Dictionary<string, (TokenResponse, DateTime)>();
        private static async Task Main()
        {
            Console.Title = "Console ResourceOwner Flow";
            Console.WriteLine("Enter username:");
            var username = Console.ReadLine();
            Console.WriteLine("Enter password:");
            var password = Console.ReadLine();
            var response = await GetTokenAsync(username, password);
            //response = await GetTokenAsync(username, password);
            //ShowToken(response);
            //Console.ReadLine();
            await CallServiceAsync(response.AccessToken);
            Console.ReadLine();
        }

        private static bool IsExpired(DateTime date)
        {
            return date < DateTime.UtcNow;
        }

        private static async Task<TokenResponse> GetTokenAsync(string username, string password)
        {
            var key = $"{username}-{password}";
            if (_tokens.TryGetValue(key, out var tokenAndDate))
            {
                if (!IsExpired(tokenAndDate.Item2))
                {
                    return tokenAndDate.Item1;
                }
            }
            var tokenEndpoint = $"{Constants.Authority}/connect/token";
            var client = new TokenClient(tokenEndpoint, Constants.Client, Constants.Secret);
            var token = await client.RequestResourceOwnerPasswordAsync(username, password, Constants.Scope);
            if (token.IsError)
            {
                throw new Exception($"Token error from {tokenEndpoint}: {token.Error}");
            }
            var tokenExpiry = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 60); // Subtract 60s to allow for call time}
            _tokens.Add(key, (token, tokenExpiry));
            return token;
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
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Whoami:");
            Console.WriteLine(JObject.Parse(result));
        }


        private static void ShowToken(TokenResponse response)
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