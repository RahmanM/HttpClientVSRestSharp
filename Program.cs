using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;

namespace WebApiClient
{
    class Program
    {

        private static void Main(string[] args)
        {
            // MainAsync().Wait();

            MainAsyncRestSharp().Wait();

            Console.ReadLine();
        }

        #region Restsharp client

        private static async Task MainAsyncRestSharp()
        {
            ReadValuesRestSharp();
            await ReadValuesRestSharpAsync();
            AddValueRestSharpAsync();
            DeleteValueRestSharp();
        }

        private static void DeleteValueRestSharp()
        {
            var url = new Uri("http://localhost:13628/api/Values/1");
            var request = new RestRequest(Method.DELETE);

            var client = new RestClient(url);
            var response = client.Execute(request);
            if (response.ErrorException != null)
            {
                Console.Write(response.ErrorMessage);
            }
            else if (response.StatusCode != HttpStatusCode.OK)
            {
                dynamic msg = JsonConvert.DeserializeObject(response.Content);
                Console.Write(msg.Message);
            }
            else
            {
                ReadValuesRestSharp();
            }
        }

        private static void AddValueRestSharpAsync()
        {
            var url = new Uri("http://localhost:13628/api/Values/");
            var request = new RestRequest(Method.POST);

            ObjectToStore value = new ObjectToStore() { Value = "Value 3 to add", Id = 3 };
            string jsonToSend = request.JsonSerializer.Serialize(value);
            request.AddParameter("application/json", jsonToSend, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;

            var client = new RestClient(url);
            var response = client.Execute(request);
            if (response.ErrorException != null)
            {
                Console.Write(response.ErrorMessage);
            }
            else if (response.StatusCode != HttpStatusCode.OK)
            {
                dynamic msg = JsonConvert.DeserializeObject(response.Content);
                Console.Write(msg.Message);
            }
            else
            {
                ReadValuesRestSharp();
            }
        }

        private static async Task ReadValuesRestSharpAsync()
        {
            var url = new Uri("http://localhost:13628/api/Values/");
            var request = new RestRequest();

            var helper = new RestsharpHelper(url);
            var response = await helper.ExecuteAsync<List<ObjectToStore>>(request);
            foreach (var objectToStore in response)
            {
                Console.WriteLine(objectToStore.Value);
            }
        }

        private static void ReadValuesRestSharp()
        {
            var url = new Uri("http://localhost:13628/api/Values/");
            var client = new RestClient(url);

            var request = new RestRequest(Method.GET);
            var response = client.Execute<List<ObjectToStore>>(request);

            if (response.ErrorException != null)
            {
                Console.WriteLine(response.ErrorException);
            }
            else
            {
                var data = response.Data;
                foreach (var value in data)
                {
                    Console.WriteLine(value.Value);
                }
            }
        }

        #endregion

        #region Web Client

        static async Task MainAsync()
        {
            await ReadValues();
            await AddValue();
            await ReadValue(1);
            await DeleteValue(2);

            await DeleteValue(111); // Delete with Error

            Console.ReadLine();
        }

        private static async Task DeleteValue(int id)
        {
            var url = "http://localhost:13628/api/Values/" + id;
            using (var client = new HttpClient())
            {
                var response = await client.DeleteAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    await ReadValues();
                }
                else
                {
                    Console.WriteLine(response.ReasonPhrase);
                    Console.WriteLine(response.StatusCode);
                }
            }
        }

        private static async Task ReadValue(int id)
        {
            var url = "http://localhost:13628/api/Values/" + id;

            using (var client = new HttpClient())
            {
                var httpResponseMessage = await client.GetAsync(url);

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    var jsonString = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    var result = JsonConvert.DeserializeObject<ObjectToStore>(jsonString);
                    Console.WriteLine("Getting one value->" + result.Value);
                }
                else
                {
                    Console.WriteLine(httpResponseMessage.StatusCode);
                    Console.WriteLine(httpResponseMessage.ReasonPhrase);
                }
            }
        }

        private static async Task AddValue()
        {
            var url = "http://localhost:13628/api/Values/";

            using (var client = new HttpClient())
            {
                var value = new ObjectToStore() { Id = 3, Value = "value 3" };
                var content = JsonConvert.SerializeObject(value);
                var stringContent = new StringContent(content, Encoding.Default, "application/json");
                var result = await client.PostAsync(url, stringContent);

                if (result.IsSuccessStatusCode)
                {
                    await ReadValues();
                }
                else
                {
                    Console.WriteLine(result.StatusCode);
                    Console.WriteLine(result.ReasonPhrase);
                }
            }
        }

        private static async Task ReadValues()
        {
            var url = "http://localhost:13628/api/Values";
            var client = new HttpClient();
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = response.Content.ReadAsStringAsync().Result;
                var values = JsonConvert.DeserializeObject<List<ObjectToStore>>(jsonString);

                foreach (ObjectToStore value in values)
                {
                    Console.WriteLine(value.Value);
                }
            }
        }

        #endregion

    }

    public class ObjectToStore
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }

    public class RestsharpHelper
    {
        public RestsharpHelper(Uri baseUrl)
        {
            BaseUrl = baseUrl;
        }

        public Uri BaseUrl { get; set; }

        public async Task<T> ExecuteAsync<T>(RestRequest request) where T : new()
        {
            var client = new RestClient();
            var taskCompletionSource = new TaskCompletionSource<T>();
            client.BaseUrl = BaseUrl;
            //client.Authenticator = new HttpBasicAuthenticator(_accountSid, _secretKey);
            //request.AddParameter("AccountSid", _accountSid, ParameterType.UrlSegment);
            client.ExecuteAsync<T>(request, (response) => taskCompletionSource.SetResult(response.Data));
            return await taskCompletionSource.Task;
        }
    }
}
