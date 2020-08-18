using System;
using System.Configuration;
using System.Threading.Tasks;
using DxSync.Common;
using DxSync.Entity;
using DxSync.Log;
using DxSyncClient.RequestAPIModule;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DxSyncClient.Service
{
    public abstract class AbstractClientSyncService
    {
        private string _username = ConfigurationManager.AppSettings["username"];
        private string _password = ConfigurationManager.AppSettings["password"];

        protected string Token { get; private set; }

        private readonly ILogger _logger;

        public AbstractClientSyncService()
        {
            _logger = LoggerFactory.GetLogger("WindowsEventViewer");
        }

        private Credential Credential
        {
            get => new Credential { Username = _username, Password = _password };
        }
        public bool Connect()
        {
            Task<ResponseData> responseData = Task.Run(async () =>
            {
                var httpExtensions = new HttpExtensions(APISyncEndpoint.CheckConnection);
                return await httpExtensions.GetRaw();
            });
            var result = responseData.GetAwaiter().GetResult();

            if(result != null)
                if (result.StatusCode == HttpResponseCode.OK)
                    return true;

            return false;
        }

        public void Authenticate()
        {

            const string endpoint = APISyncEndpoint.GetToken;

            Task<ResponseData> responseData = Task.Run(async () =>
            {
                var httpExtensions = new HttpExtensions(endpoint);
                httpExtensions.Body(Credential);
                return await httpExtensions.PostRaw();
            });

            var result = responseData.GetAwaiter().GetResult();

            if (result != null)
            {
                string token = (string)((JObject)result.Data).SelectToken("Token");
                SetToken(token);
            }
            WriteLog(endpoint, result, Credential);
        }

        private void SetToken(string token)
        {
            Token = token;
        }

        private void WriteLog(string endpoint, ResponseData responseData, Credential credential)
        {
            string applicationName = ConfigurationManager.AppSettings["ApplicationName"];
            string body = JsonConvert.SerializeObject(credential);
            string response = JsonConvert.SerializeObject(responseData);
            string logMessage = @"" + applicationName +" "
                                + DateTime.Now + "\n " + endpoint + "\n " + body  + "\n" + response ;
            _logger.Write(logMessage);
        }
    }
}
