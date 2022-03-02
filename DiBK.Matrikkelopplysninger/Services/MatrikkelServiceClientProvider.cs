using System.ServiceModel;
using System.ServiceModel.Description;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.adresse;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.bygning;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.matrikkelenhet;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.store;

namespace DiBK.Matrikkelopplysninger.Services
{
    public class MatrikkelServiceClientProvider
    {
        private readonly IConfigurationSection _webServiceConfig;

        public MatrikkelServiceClientProvider(IConfiguration config)
        {
            _webServiceConfig = config.GetSection("WebServiceConfig");
        }
        
        public StoreServiceClient GetStoreServiceClient()
        {
            var storeServiceClient = new StoreServiceClient(GetBasicHttpBinding(), new EndpointAddress
                (_webServiceConfig.GetValue<string>("EndpointAddress") + "StoreServiceWS"));

            SetCredentialsFromConfig(storeServiceClient.ClientCredentials);

            return storeServiceClient;
        }

        public BygningServiceClient GetBygningServiceClient()
        {
            var bygningServiceClient = new BygningServiceClient(GetBasicHttpBinding(), new EndpointAddress
                (_webServiceConfig.GetValue<string>("EndpointAddress") + "BygningServiceWS"));

            SetCredentialsFromConfig(bygningServiceClient.ClientCredentials);

            return bygningServiceClient;

        }

        public AdresseServiceClient GetAdresseServiceClient()
        {
            var adresseServiceClient = new AdresseServiceClient(GetBasicHttpBinding(), new EndpointAddress
                (_webServiceConfig.GetValue<string>("EndpointAddress") + "AdresseServiceWS"));

            SetCredentialsFromConfig(adresseServiceClient.ClientCredentials);

            return adresseServiceClient;

        }

        public MatrikkelenhetServiceClient GetMatrikkelenhetServiceClient()
        {
            var matrikkelenhetServiceClient = new MatrikkelenhetServiceClient(GetBasicHttpBinding(), new EndpointAddress
                (_webServiceConfig.GetValue<string>("EndpointAddress") + "MatrikkelenhetServiceWS"));

            SetCredentialsFromConfig(matrikkelenhetServiceClient.ClientCredentials);

            return matrikkelenhetServiceClient;

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
