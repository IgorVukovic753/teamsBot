using Microsoft.Graph.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TeamsAuth.Config;

namespace TeamsAuth.APIHandlers
{
    public class APIHandler : APIBase
    {
        public APIHandler(string authtoken) : base(authtoken)
        {

        }

        public async virtual Task<APIResult> ExecuteAPI(Intent obj)
        {
            return await Task.FromResult(new APIResult() { Code = APIResultCode.Ok });
        }
    }

    public class APIBase
    {
        protected string _AuthToken = string.Empty;

        public APIBase(string authtoken)
        {
            _AuthToken = authtoken;
        }

        protected async Task<string> Post(object obj, string url)
        {
            HttpClient client = new HttpClient();
            //HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _AuthToken);

            string payload = JsonConvert.SerializeObject(obj);
            HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            //request.Content = content;
            try
            {
                HttpResponseMessage response = await client.SendAsync(request);
                string responseString = await response.Content.ReadAsStringAsync();

                return responseString;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        protected async Task<string> Delete(string url)
        {

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _AuthToken);

            try
            {
                HttpResponseMessage response = await client.SendAsync(request);
                string responseString = await response.Content.ReadAsStringAsync();

                return responseString;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        protected async Task<string> Get(string url)
        {

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _AuthToken);

            try
            {
                HttpResponseMessage response = await client.SendAsync(request);
                string responseString = await response.Content.ReadAsStringAsync();

                return responseString;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
    }

    public class APIResult
    {
        public string IntentName { get; set; }
        public string ResultText { get; set; }
        public string Response { get; set; }
        public string ErrorText { get; set; }
        public APIResultCode Code { get; set; }

        public APIResult()
        {

        }
    }

    public class APIResults
    {
        public APIResults()
        {

        }
        public List<APIResult> APIResultList { get; set; }

        public void Add(APIResult result)
        {
            if (APIResultList == null)
                this.APIResultList = new List<APIResult>();

            if (APIResultList.Count() > 10)
                this.APIResultList.RemoveAt(0);

            this.APIResultList.Add(result);

        }
    }
    public enum APIResultCode { Ok, Error }
}
