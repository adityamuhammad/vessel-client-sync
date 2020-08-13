using DxSync.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DxSyncClient.RequestAPIModule
{
    public class HttpExtensions
    {
        private IDictionary<string, string> _headers =null;
        private object _body;
        private string _queryParameters = string.Empty;
        private string _url;
        public HttpExtensions() { }
        public HttpExtensions(string url)
        {
            _url = url;
        }

        public void AddHeader(string key, string value)
        {
            _headers.Add(key, value);
        }

        public void AddQueryParam(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(_queryParameters))
                _queryParameters += "?" + key + "=" + value;
            else
                _queryParameters += "&" + key + "=" + value;
        }

        public void Body(object body)
        {
            _body = body;
        }

        public void Url(string url)
        {
            _url = url;
        }

        public async Task<ResponseData> PostRaw()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string body = JsonConvert.SerializeObject(_body);
                    var content = new StringContent(body, Encoding.UTF8, "application/json");

                    if(_headers != null)
                    {
                        foreach (KeyValuePair<string, string> header in _headers)
                        {
                            client.DefaultRequestHeaders.Add(header.Key, header.Value);
                        }
                    }
                    var result = await client.PostAsync(_url, content);
                    var responseData = result.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<ResponseData>(responseData);
                } catch (HttpRequestException)
                {
                    return new ResponseData
                    {
                        StatusCode = HttpResponseCode.NO_INTERNET_CONNECTION,
                        Message = Message.NO_INTERNET_CONNECTION
                    };
                }
            }
        }

        public async Task<ResponseData> GetRaw()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var result = await client.GetAsync(_url);
                    var responseData = result.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<ResponseData>(responseData);
                } catch (HttpRequestException)
                {
                    return new ResponseData
                    {
                        StatusCode = HttpResponseCode.NO_INTERNET_CONNECTION,
                        Message = Message.NO_INTERNET_CONNECTION
                    };
                }
            }
        }
    }
}
