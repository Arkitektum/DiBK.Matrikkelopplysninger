using System.Collections;
using System.Collections.ObjectModel;
using System.Data;
using Microsoft.AspNetCore.Mvc.Localization;
using no.kxml.skjema.dibk.matrikkelregistrering;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.matrikkelenhet;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.adresse;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.bygning;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.store;


namespace DiBK.Matrikkelopplysninger.Services;

public class MatrikkeldataProvider
{
    private readonly MatrikkelContext _matrikkelContextObject;
    private readonly StoreServiceClient _storeServiceClient;
    private readonly BygningServiceClient _bygningServiceClient;
    private readonly AdresseServiceClient _adresseServiceClient;
    private readonly MatrikkelenhetServiceClient _matrikkelenhetServiceClient;


    public MatrikkeldataProvider(IConfiguration config)
    {
        var matrikkelClientProvider = new MatrikkelServiceClientProvider(config);

        _matrikkelContextObject = matrikkelClientProvider.GetMatrikkelContextObject();
        _matrikkelenhetServiceClient = matrikkelClientProvider.GetMatrikkelenhetServiceClient();
        _adresseServiceClient = matrikkelClientProvider.GetAdresseServiceClient();
        _bygningServiceClient = matrikkelClientProvider.GetBygningServiceClient();
        _storeServiceClient = matrikkelClientProvider.GetStoreServiceClient();
    }


    public MatrikkelregistreringType GetMatrikkelOpplysninger(int knr, int gnr, int bnr, int fnr, int snr)
    {
        var matrikkelenhet = (Matrikkelenhet)_storeServiceClient.getObject(GetMatrikkelenhetId(knr, gnr, bnr, fnr, snr), _matrikkelContextObject);
        var kommune = (Kommune)_storeServiceClient.getObject(matrikkelenhet.matrikkelnummer.kommuneId, _matrikkelContextObject);

        return new MatrikkelregistreringType
        {
            eiendomsidentifikasjon = new MatrikkelnummerType[]
            {
                new()
                {
                    kommunenummer = matrikkelenhet.matrikkelnummer.kommuneId.value.ToString(),
                    gaardsnummer = matrikkelenhet.matrikkelnummer.gardsnummer.ToString(),
                    bruksnummer = matrikkelenhet.matrikkelnummer.bruksnummer.ToString(),
                    festenummer = matrikkelenhet.matrikkelnummer.festenummer.ToString(),
                    seksjonsnummer = matrikkelenhet.matrikkelnummer.seksjonsnummer.ToString()                    
                }
            },
            adresse = GetAdresser(knr, gnr, bnr, fnr, snr),
            bygning = GetBygninger(knr, gnr, bnr, fnr, snr),
            prosjektnavn = "",            
            kommunenavn = kommune.kommunenavn,
        };
    }    
      

    private AdresseType[] GetAdresser(int knr, int gnr, int bnr, int fnr, int snr)
    {
        var adresseList = new List<AdresseType>();
        var adresseIds = _adresseServiceClient.findAdresserForMatrikkelenhet(GetMatrikkelenhetId(knr, gnr, bnr, fnr, snr), _matrikkelContextObject);

        foreach (var obj in adresseIds)
        {
            var adresser = new AdresseType();
            var vegadresse = GetVegadresse(obj);
            var veg = GetVeg(vegadresse.vegId);
            adresser.adressekode = veg.adressekode.ToString();
            adresser.adressenavn = veg.adressenavn;
            adresser.adressenummer = vegadresse.nummer.ToString();
            adresser.adressebokstav = vegadresse.bokstav;
            //adresser.seksjonsnummer = GetMatrikkelenhet(knr, gnr, bnr, fnr, snr).matrikkelnummer.seksjonsnummer.ToString(); //TODO: er seksjonsnummer en del av adresser?
            adresseList.Add(adresser);
        }

        return adresseList.ToArray();
    }
    
    
    private BygningType[] GetBygninger(int knr, int gnr, int bnr, int fnr, int snr)
    {
        var bygninger = new List<BygningType>();

        ByggId[] byggIds = _bygningServiceClient.findByggForMatrikkelenhet(GetMatrikkelenhetId(knr, gnr, bnr, fnr, snr), _matrikkelContextObject);
        var matrikkelBubbleObjectsBygg = _storeServiceClient.getObjects(byggIds, _matrikkelContextObject);
        
        foreach (Bygg bygg in matrikkelBubbleObjectsBygg)
        {
            var bygning = new BygningType();

            //variabler til kodeliste
            var naeringsgruppeKode = (NaringsgruppeKode)_storeServiceClient.getObject(bygg.naringsgruppeKodeId, _matrikkelContextObject);
            var bygningstypeKode = (BygningstypeKode)_storeServiceClient.getObject(((Bygning)bygg).bygningstypeKodeId, _matrikkelContextObject);
            //var bygningstatusKode = (BygningsstatusKode)_storeServiceClient.getObject(bygg.bygningsstatusKodeId, _matrikkelContextObject);
            var avlopsKode = (AvlopsKode)_storeServiceClient.getObject(bygg.avlopsKodeId, _matrikkelContextObject);
            var vannforsyningKode = (VannforsyningsKode)_storeServiceClient.getObject(bygg.vannforsyningsKodeId, _matrikkelContextObject);
            
            bygning.bygningsnummer = bygg.bygningsnummer.ToString();
            bygning.naeringsgruppe = GetKodeType(naeringsgruppeKode?.kodeverdi, naeringsgruppeKode?.navn[0]?.value?.ToString());
            bygning.bygningstype = GetKodeType(bygningstypeKode?.kodeverdi, bygningstypeKode?.navn[0]?.value?.ToString());
            bygning.bebygdAreal = bygg.bebygdAreal;
            bygning.bebygdArealSpecified = bygg.bebygdArealSpecified;                                  
            bygning.etasjer = GetEtasjer(bygg);
            bygning.avlop = GetKodeType(avlopsKode?.kodeverdi, avlopsKode?.navn[0]?.value?.ToString());
            bygning.bruksenheter = GetBruksenheter(bygg);
            
            bygning.energiforsyning = new EnergiforsyningType
            {
                varmefordeling = GetVarmefordelinger(bygg),
                energiforsyning = GetEnergiforsyninger(bygg)
            //relevant = //TODO: ser ut som denne allerede ligger i kodelisten
            //relevantSpecified = 
            };
            
            bygning.vannforsyning = GetKodeType(vannforsyningKode?.kodeverdi, vannforsyningKode?.navn[0]?.value?.ToString());
            bygning.harHeis = bygg.harHeis;
            bygning.harHeisSpecified = bygg.harHeisSpecified;
            
            bygninger.Add(bygning);
        }

        return bygninger.ToArray();
    }


    //***---Hjelpemetoder---***
    private MatrikkelenhetId GetMatrikkelenhetId(int knr, int gnr, int bnr, int fnr, int snr)
    {
        var matrikkelenhetsokModel = new MatrikkelenhetsokModel()
        {
            kommunenummer = knr.ToString(),
            gardsnummer = gnr.ToString(),
            bruksnummer = bnr.ToString(),
            festenummer = fnr,
            seksjonsnummer = snr

        };
        MatrikkelenhetId[] matrikkelenhetIds = _matrikkelenhetServiceClient.findMatrikkelenheter(matrikkelenhetsokModel, _matrikkelContextObject);
        return matrikkelenhetIds.First();
    }


    private Vegadresse GetVegadresse(AdresseId adresseId)
    {
        try
        {
            return (Vegadresse)_storeServiceClient.getObject(adresseId, _matrikkelContextObject);
        } catch (Exception e)
        {
            var message = e.Message.ToString();
            return null;
        }
            
    }


    private Veg GetVeg(VegId vegId)
    {
        return (Veg)_storeServiceClient.getObject(vegId, _matrikkelContextObject);       
    }


    private KodeType GetKodeType(string kodeverdi, string kodebeskrivelse)
    {
        if(string.IsNullOrEmpty(kodeverdi)&& string.IsNullOrEmpty(kodebeskrivelse))
            return null;

        return new KodeType()
        {
            kodeverdi = kodeverdi,
            kodebeskrivelse = kodebeskrivelse
        };        
    }


    private KodeType[] GetVarmefordelinger(Bygg bygg)
    {
        var varmefordelinger = new List<KodeType>();
        foreach (var varmeObjekt in bygg.oppvarmingsKodeIds)
        {
            var oppvarmingsKodeId = (OppvarmingsKode)_storeServiceClient.getObject(varmeObjekt, _matrikkelContextObject);
            varmefordelinger.Add(GetKodeType(oppvarmingsKodeId?.kodeverdi, oppvarmingsKodeId?.navn[0]?.value?.ToString()));           
        }
        return varmefordelinger.ToArray();
    }


    private KodeType[] GetEnergiforsyninger(Bygg bygg)
    {       
        var energiforsyninger = new List<KodeType>();
        foreach (var energiObjekt in bygg.energikildeKodeIds)
        {
            var energikildeKodeId = (EnergikildeKode)_storeServiceClient.getObject(energiObjekt, _matrikkelContextObject);
            energiforsyninger.Add(GetKodeType(energikildeKodeId?.kodeverdi, energikildeKodeId?.navn[0]?.value?.ToString()));
        }
        return energiforsyninger.ToArray();
    }


    private EtasjeType[] GetEtasjer(Bygg bygg)
    {
        var etasjer = new List<EtasjeType>();
        foreach (var etasje in bygg.etasjer)
        {
            var etasjeplanKode = (EtasjeplanKode)_storeServiceClient.getObject(etasje.etasjeplanKodeId, _matrikkelContextObject);
            var etasjeType = new EtasjeType()
            {
                antallBoenheter = etasje.antallBoenheter.ToString(),
                bruksarealTilAnnet = etasje.bruksarealTilAnnet,
                bruksarealTilAnnetSpecified = etasje.bruksarealTilAnnetSpecified,
                bruksarealTilBolig = etasje.bruksarealTilBolig,
                bruksarealTilBoligSpecified = etasje.bruksarealTilBoligSpecified,
                bruksarealTotalt = etasje.bruksarealTotalt,
                bruksarealTotaltSpecified = etasje.bruksarealTotaltSpecified,
                etasjenummer = etasje.etasjenummer.ToString(),
                etasjeplan = GetKodeType(etasjeplanKode?.kodeverdi, etasjeplanKode?.navn[0]?.value?.ToString()),
                bruttoarealTilBolig = etasje.bruttoarealTilBolig,
                bruttoarealTilBoligSpecified = etasje.bruttoarealTilBoligSpecified,
                bruttoarealTilAnnet = etasje.bruttoarealTilAnnet,
                bruttoarealTilAnnetSpecified = etasje.bruttoarealTilAnnetSpecified,
                //etasjeopplysning = new KodeType
                //{
                //    kodeverdi = bygningstatusKode.kodeverdi,
                //    kodebeskrivelse = bygningstatusKode.navn[0].value.ToString()
                //}, //TODO: finn ut hvor denne er i apiet
                bruttoarealTotalt = etasje.bruttoarealTotalt,
                bruttoarealTotaltSpecified = etasje.bruttoarealTotaltSpecified
            };
            etasjer.Add(etasjeType);
        }
        return etasjer.ToArray();
    }


    private BruksenhetType[] GetBruksenheter(Bygg bygg)
    {
        var bruksenheter = new List<BruksenhetType>();
        var matrikkelBubbleObjectsBruksenhet = _storeServiceClient.getObjects(bygg.bruksenhetIds, _matrikkelContextObject);
        foreach (Bruksenhet bruksenhet in matrikkelBubbleObjectsBruksenhet)
        {
            var etasjeplanKode = (EtasjeplanKode)_storeServiceClient.getObject(bruksenhet.etasjeplanKodeId, _matrikkelContextObject);
            var kjokkenkode = (KjokkentilgangKode)_storeServiceClient.getObject(bruksenhet.kjokkentilgangId, _matrikkelContextObject);
            var bruksenhetTypeKode = (BruksenhetstypeKode)_storeServiceClient.getObject(bruksenhet.bruksenhetstypeKodeId, _matrikkelContextObject);

            var vegadresse = GetVegadresse(bruksenhet.adresseId);         
            var adresse = new BoligadresseType();

            if (vegadresse != null)
            {
                var veg = GetVeg(vegadresse.vegId);
                adresse.adressekode = veg?.adressekode!.ToString();             //TODO: kan dette være det samme som i AdresseType[]?
                adresse.adressenavn = veg?.adressenavn;
                adresse.adressenummer = vegadresse.nummer.ToString();
                adresse.adressebokstav = vegadresse.bokstav;
                //seksjonsnummer = GetMatrikkelenhet(knr, gnr, bnr, fnr, snr).matrikkelnummer.seksjonsnummer.ToString() //TODO: hent fra bruksobjekt                
            }

            var bruksenhetType = new BruksenhetType()
            {
                bruksenhetsnummer = new BruksenhetsnummerType
                {
                    etasjeplan = GetKodeType(etasjeplanKode?.kodeverdi, etasjeplanKode?.navn[0]?.value?.ToString()),
                    etasjenummer = bruksenhet.etasjenummer.ToString(),
                    loepenummer = bruksenhet.lopenummer.ToString()
                },
                bruksareal = bruksenhet.bruksareal,
                bruksarealSpecified = bruksenhet.bruksarealSpecified,
                kjoekkentilgang = GetKodeType(kjokkenkode?.kodeverdi, kjokkenkode?.navn[0]?.value?.ToString()),
                antallRom = bruksenhet.antallRom.ToString(),
                antallBad = bruksenhet.antallBad.ToString(),
                antallWC = bruksenhet.antallWC.ToString(),
                bruksenhetstype = GetKodeType(bruksenhetTypeKode?.kodeverdi, bruksenhetTypeKode?.navn[0]?.value?.ToString()),
                adresse = adresse
            };

            bruksenheter.Add(bruksenhetType);
        }
        return bruksenheter.ToArray();        
    }
        
}
