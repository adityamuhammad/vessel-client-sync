using DxSync.Common;
using DxSync.Entity;
using DxSync.Log;
using DxSyncClient.RequestAPIModule;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace DxSyncClient.Service
{
    public abstract class AbstractClientSyncService
    {
        private readonly ILogger _logger;
        public AbstractClientSyncService()
        {
            _logger = LoggerFactory.GetLogger("WindowsEventViewer");
        }
        public string GetAuthenticationToken()
        {

            const string endpoint = APISyncEndpoint.GetToken;

            var username = ConfigurationManager.AppSettings["username"];
            var password = ConfigurationManager.AppSettings["password"];
            Credential credential = new Credential
            {
                Username = username,
                Password = password
            };

            Task<ResponseData> responseData = Task.Run(async () =>
            {
                var httpExtensions = new HttpExtensions(endpoint);
                httpExtensions.Body(credential);
                return await httpExtensions.PostRaw();
            });

            var result = responseData.GetAwaiter().GetResult();

            if (result is null) return null;

            JObject data = (JObject)result.Data;
            string token = (string)data.SelectToken("Token");

            WriteLog(endpoint, result, credential);

            return token;
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
