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
    private MatrikkelContext _matrikkelContextObject;
    private StoreServiceClient _storeServiceClient;
    private BygningServiceClient _bygningServiceClient;
    private AdresseServiceClient _adresseServiceClient;
    private MatrikkelenhetServiceClient _matrikkelenhetServiceClient;



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
    
    
    public BygningType[] GetBygninger(int knr, int gnr, int bnr, int fnr, int snr)
    {
        var bygninger = new List<BygningType>();

        ByggId[] byggIds = _bygningServiceClient.findByggForMatrikkelenhet(GetMatrikkelenhetId(knr, gnr, bnr, fnr, snr), _matrikkelContextObject);
        var byggBubbles = _storeServiceClient.getObjects(byggIds, _matrikkelContextObject);
        
        foreach (Bygg bygg in byggBubbles)
        {
            var bygning = new BygningType();

            //variabler til kodeliste
            var naeringsgruppeKode = (NaringsgruppeKode)_storeServiceClient.getObject(bygg.naringsgruppeKodeId, _matrikkelContextObject);
            var bygningstypeKode = (BygningstypeKode)_storeServiceClient.getObject(((Bygning)bygg).bygningstypeKodeId, _matrikkelContextObject);
            var bygningstatusKode = (BygningsstatusKode)_storeServiceClient.getObject(bygg.bygningsstatusKodeId, _matrikkelContextObject);
            var avlopskode = (AvlopsKode)_storeServiceClient.getObject(bygg.avlopsKodeId, _matrikkelContextObject);
            var vannforsyningKode = (VannforsyningsKode)_storeServiceClient.getObject(bygg.vannforsyningsKodeId, _matrikkelContextObject);

            bygning.bygningsnummer = bygg.bygningsnummer.ToString();

            var naeringsgruppe = GetKodeType(naeringsgruppeKode?.kodeverdi, naeringsgruppeKode?.navn[0]?.value?.ToString());
            bygning.naeringsgruppe = naeringsgruppe;

            var bygningstypeKodeType = GetKodeType(bygningstypeKode?.kodeverdi, bygningstypeKode?.navn[0]?.value?.ToString());
            bygning.bygningstype = bygningstypeKodeType;

            bygning.bebygdAreal = bygg.bebygdAreal;
            bygning.bebygdArealSpecified = bygg.bebygdArealSpecified;
                                  
            bygning.etasjer = GetEtasjer(bygg);

            var avlop = GetKodeType(avlopskode?.kodeverdi, avlopskode?.navn[0]?.value?.ToString());
            bygning.avlop = avlop;
            
            bygning.bruksenheter = GetBruksenheter(bygg);

            var varmefordelinger = GetVarmefordelinger(bygg);
            var energiforsyninger = GetEnergiforsyninger(bygg);
            bygning.energiforsyning = new EnergiforsyningType
            {
                varmefordeling = varmefordelinger,
                energiforsyning = energiforsyninger
                //relevant = //TODO: ser ut som denne allerede ligger i kodelisten
                //relevantSpecified = 
            };

            var vannforsyning = GetKodeType(vannforsyningKode?.kodeverdi, vannforsyningKode?.navn[0]?.value?.ToString());
            bygning.vannforsyning = vannforsyning;

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
            var vegadresse = (Vegadresse)_storeServiceClient.getObject(adresseId, _matrikkelContextObject);
            return vegadresse;
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            return null;
        }
    }


    private Veg GetVeg(VegId vegId)
    {
        try
        {
            return (Veg)_storeServiceClient.getObject(vegId, _matrikkelContextObject);
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            return null;
        }
    }


    private KodeType GetKodeType(string kodeverdi, string kodebeskrivelse)
    {
        if(string.IsNullOrEmpty(kodeverdi)&& string.IsNullOrEmpty(kodebeskrivelse))
            return null;

        var kodeType = new KodeType()
        {
            kodeverdi = kodeverdi,
            kodebeskrivelse = kodebeskrivelse
        };
        return kodeType;
    }


    private KodeType[] GetVarmefordelinger(Bygg bygg)
    {
        var oppvarmingsKodeIds = _storeServiceClient.getObjects(((Bygning)bygg).oppvarmingsKodeIds, _matrikkelContextObject);
        var varmefordelinger = new List<KodeType>();
        foreach (var varmeObjekt in bygg.oppvarmingsKodeIds)
        {
            var oppvarmingsKodeId = (OppvarmingsKode)_storeServiceClient.getObject(varmeObjekt, _matrikkelContextObject);
            varmefordelinger.Add(new KodeType
            {
                kodeverdi = oppvarmingsKodeId.kodeverdi,
                kodebeskrivelse = oppvarmingsKodeId.navn[0].value.ToString()
            });
        }

        return varmefordelinger.ToArray();
    }


    private KodeType[] GetEnergiforsyninger(Bygg bygg)
    {
        var energikildeKodeIds = _storeServiceClient.getObjects(((Bygning)bygg).energikildeKodeIds, _matrikkelContextObject);
        var energiforsyninger = new List<KodeType>();
        foreach (var energiObjekt in energikildeKodeIds)
        {
            var energikildeKodeId = (EnergikildeKode)_storeServiceClient.getObject(energiObjekt.id, _matrikkelContextObject);
            energiforsyninger.Add(new KodeType
            {
                kodeverdi = energikildeKodeId.kodeverdi,
                kodebeskrivelse = energikildeKodeId.navn[0].value.ToString()
            });
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
                etasjeplan = new KodeType
                {
                    kodeverdi = etasjeplanKode.kodeverdi,
                    kodebeskrivelse = etasjeplanKode.navn[0].value.ToString()
                },
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
        var bruksenhetBubbles = _storeServiceClient.getObjects(bygg.bruksenhetIds, _matrikkelContextObject);
        foreach (Bruksenhet bruksenhet in bruksenhetBubbles)
        {
            var etasjeplanKode = (EtasjeplanKode)_storeServiceClient.getObject(bruksenhet.etasjeplanKodeId, _matrikkelContextObject);
            var kjokkenkode = (KjokkentilgangKode)_storeServiceClient.getObject(bruksenhet.kjokkentilgangId, _matrikkelContextObject);
            var bruksenhetTypeKode = (BruksenhetstypeKode)_storeServiceClient.getObject(bruksenhet.bruksenhetstypeKodeId, _matrikkelContextObject);
            var etasje = GetKodeType(etasjeplanKode?.kodeverdi, etasjeplanKode?.navn[0]?.value?.ToString());
            var kjokken = GetKodeType(kjokkenkode?.kodeverdi, kjokkenkode?.navn[0]?.value?.ToString());
            var bruksenhetKodeType = GetKodeType(bruksenhetTypeKode?.kodeverdi, bruksenhetTypeKode?.navn[0]?.value?.ToString());
            var bruksenhetType = new BruksenhetType()
            {
                bruksenhetsnummer = new BruksenhetsnummerType
                {
                    etasjeplan = etasje,
                    etasjenummer = bruksenhet.etasjenummer.ToString(),
                    loepenummer = bruksenhet.lopenummer.ToString()
                },
                bruksareal = bruksenhet.bruksareal,
                bruksarealSpecified = bruksenhet.bruksarealSpecified,
                kjoekkentilgang = kjokken,
                antallRom = bruksenhet.antallRom.ToString(),
                antallBad = bruksenhet.antallBad.ToString(),
                antallWC = bruksenhet.antallWC.ToString(),
                bruksenhetstype = bruksenhetKodeType
            };
            var adresseId = bruksenhet.adresseId;
            var vegadresse = GetVegadresse(adresseId);
            if (vegadresse != null)
            {
                var veg = GetVeg(vegadresse.vegId);
                var adresse = new BoligadresseType                  //TODO: kan dette være det samme som i AdresseType[]?
                {
                    adressekode = veg?.adressekode!.ToString(),
                    adressenavn = veg?.adressenavn,
                    adressenummer = vegadresse.nummer.ToString(),
                    adressebokstav = vegadresse.bokstav,
                    //seksjonsnummer = GetMatrikkelenhet(knr, gnr, bnr, fnr, snr).matrikkelnummer.seksjonsnummer.ToString() //TODO: hent fra bruksobjekt
                };
                bruksenhetType.adresse = adresse;
            }
            bruksenheter.Add(bruksenhetType);
        }
        return bruksenheter.ToArray();
    }
        
}
