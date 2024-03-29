﻿using DxSync.Common;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DxSyncClient.RequestAPIModule
{
    public class RequestAPI
    {
        private IDictionary<string, string> _headers = new Dictionary<string, string>();
        private object _body;
        private string _queryParameters = string.Empty;
        private string _url;
        public RequestAPI() { }
        public RequestAPI(string url)
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

        public async Task<ResponseData> PostAsync()
        {

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                    var url = _url + _queryParameters;
                    string body = JsonConvert.SerializeObject(_body);
                    string contentType = "application/json";
                    var content = new StringContent(body, Encoding.UTF8, contentType);

                    if(_headers != null)
                    {
                        foreach (KeyValuePair<string, string> header in _headers)
                        {
                            client.DefaultRequestHeaders.Add(header.Key, header.Value);
                        }
                    }
                    client.DefaultRequestHeaders.Accept.Clear();
                    var result = await client.PostAsync(url, content);
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

        public static async Task<ResponseData> PostAsync(string url, object objbody = null, object header = null)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                    string data = JsonConvert.SerializeObject(objbody);
                    string contentType = "application/json";
                    var content = new StringContent(data, Encoding.UTF8, contentType);

                    if(header != null)
                    {
                        foreach (var headerAttr in header.GetType().GetProperties())
                        {
                            client.DefaultRequestHeaders.Add(headerAttr.Name, headerAttr.GetValue(header, null).ToString());
                        }
                    }
                    var result = await client.PostAsync(url, content);
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

        public static async Task<ResponseData> GetAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                    client.DefaultRequestHeaders.Accept.Clear();
                    var result = await client.GetAsync(url);
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

        public async Task<ResponseData> GetAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
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
