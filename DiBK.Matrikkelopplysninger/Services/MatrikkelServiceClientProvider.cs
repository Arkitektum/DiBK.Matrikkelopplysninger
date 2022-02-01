using System.ServiceModel;
using System.ServiceModel.Description;

namespace DiBK.Matrikkelopplysninger.Services
{
    public class MatrikkelServiceClientProvider
    {
        private readonly IConfigurationSection _webServiceConfig;

        public MatrikkelServiceClientProvider(IConfiguration config)
        {
            _webServiceConfig = config.GetSection("WebServiceConfig");
        }

        public MatrikkelContext GetMatrikkelContextObject()
        {
            return new MatrikkelContext
            {
                locale = _webServiceConfig.GetValue<string>("Locale"),
                brukOriginaleKoordinater = false,
                koordinatsystemKodeId = new KoordinatsystemKodeId(),
                klientIdentifikasjon = _webServiceConfig.GetValue<string>("KlientIdentifikasjon"),
                systemVersion = _webServiceConfig.GetValue<string>("SystemVersion"),
                snapshotVersion = new Timestamp {timestamp = new DateTime(9999, 1, 1, 0, 0, 0)}
            };
        }

        private void SetCredentialsFromConfig(ClientCredentials clientCredentials)
        {
            clientCredentials.UserName.UserName = _webServiceConfig.GetValue<string>("Username");
            clientCredentials.UserName.Password = _webServiceConfig.GetValue<string>("Password");
        }
        
        private BasicHttpBinding GetBasicHttpBinding()
        {
            return new BasicHttpBinding
            {
                Security =
                {
                    Mode = BasicHttpSecurityMode.Transport,
                    Transport = {ClientCredentialType = HttpClientCredentialType.Basic}
                },
                MaxReceivedMessageSize = _webServiceConfig.GetValue<int>("MaxMessageSize")
            };
        }
    }
}
