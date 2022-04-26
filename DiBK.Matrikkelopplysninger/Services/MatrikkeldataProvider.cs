﻿using no.kxml.skjema.dibk.matrikkelregistrering;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.adresse;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.bygning;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.matrikkelenhet;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.store;
using System.Linq;



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
        var matrikkelenhet = _storeServiceClient.getObject(GetMatrikkelenhetId(knr, gnr, bnr, fnr, snr), _matrikkelContextObject) as Matrikkelenhet;
        var kommune = _storeServiceClient.getObject(matrikkelenhet.matrikkelnummer.kommuneId, _matrikkelContextObject) as Kommune;

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
        AdresseId[] adresseIds = _adresseServiceClient.findAdresserForMatrikkelenhet(GetMatrikkelenhetId(knr, gnr, bnr, fnr, snr), _matrikkelContextObject);

        foreach (var obj in adresseIds)
        {            
            var vegadresse = GetVegadresse(obj); //TODO:
            
            if (vegadresse != null)
            {
                var veg = GetVeg(vegadresse?.vegId);
                var adresser = new AdresseType();
                adresser.adressekode = veg?.adressekode.ToString();
                adresser.adressenavn = veg?.adressenavn;
                adresser.adressenummer = vegadresse?.nummer.ToString();
                adresser.adressebokstav = vegadresse?.bokstav;
                //adresser.seksjonsnummer = GetMatrikkelenhet(knr, gnr, bnr, fnr, snr).matrikkelnummer.seksjonsnummer.ToString(); //TODO: er seksjonsnummer en del av adresser?
                adresseList.Add(adresser);
            }
        }

        return adresseList.ToArray();
    }


    private BygningType[] GetBygninger(int knr, int gnr, int bnr, int fnr, int snr)
    {
        var bygninger = new List<BygningType>();

        ByggId[] byggIds = _bygningServiceClient.findByggForMatrikkelenhet(GetMatrikkelenhetId(knr, gnr, bnr, fnr, snr), _matrikkelContextObject);
        MatrikkelBubbleObject[] matrikkelBubbleObjectsBygg = _storeServiceClient.getObjects(byggIds, _matrikkelContextObject);

        foreach (Bygg bygg in matrikkelBubbleObjectsBygg.Where(p => p is Bygning))
        {
            var bygning = new BygningType();
            NaringsgruppeKode naeringsgruppeKode = null;
            AvlopsKode avlopsKode = null;
            BygningstypeKode bygningstypeKode = null;
            VannforsyningsKode vannforsyningsKode = null;  

            //variabler til kodeliste
            if (bygg.naringsgruppeKodeId != null)
            {
                naeringsgruppeKode = _storeServiceClient.getObject(bygg.naringsgruppeKodeId, _matrikkelContextObject) as NaringsgruppeKode;
                bygning.naeringsgruppe = GetKodeType(naeringsgruppeKode?.kodeverdi, naeringsgruppeKode?.navn[0]?.value?.ToString());
            }
            if (bygg.avlopsKodeId != null)
            {
                avlopsKode = _storeServiceClient.getObject(bygg.avlopsKodeId, _matrikkelContextObject) as AvlopsKode;
                bygning.avlop = GetKodeType(avlopsKode?.kodeverdi, avlopsKode?.navn[0]?.value?.ToString());
            }
            if (((Bygning)bygg).bygningstypeKodeId != null)
            {
                bygningstypeKode = _storeServiceClient.getObject(((Bygning)bygg).bygningstypeKodeId, _matrikkelContextObject) as BygningstypeKode;
                bygning.bygningstype = GetKodeType(bygningstypeKode?.kodeverdi, bygningstypeKode?.navn[0]?.value?.ToString());
            }
            if (bygg.vannforsyningsKodeId != null)
            {
                vannforsyningsKode = _storeServiceClient.getObject(bygg.vannforsyningsKodeId, _matrikkelContextObject) as VannforsyningsKode;
                bygning.vannforsyning = GetKodeType(vannforsyningsKode?.kodeverdi, vannforsyningsKode?.navn[0]?.value?.ToString());
            }                       
         
            //var bygningstatusKode = (BygningsstatusKode)_storeServiceClient.getObject(bygg.bygningsstatusKodeId, _matrikkelContextObject);                 

            bygning.bygningsnummer = bygg.bygningsnummer.ToString();            
            bygning.bebygdAreal = bygg.bebygdAreal;
            bygning.bebygdArealSpecified = bygg.bebygdArealSpecified;
            bygning.etasjer = GetEtasjer(bygg);            
            bygning.bruksenheter = GetBruksenheter(bygg);
            bygning.energiforsyning = new EnergiforsyningType
            {
                varmefordeling = GetVarmefordelinger(bygg),
                energiforsyning = GetEnergiforsyninger(bygg)
                //relevant = //TODO: ser ut som denne allerede ligger i kodelisten
                //relevantSpecified = 
            };
            
            bygning.harHeis = bygg.harHeis;
            bygning.harHeisSpecified = bygg.harHeisSpecified;

            bygninger.Add(bygning);
        }

        return bygninger.ToArray();
    }


    //***---Hjelpemetoder---***
    private MatrikkelenhetId _matrikkelenhetId;   

    private MatrikkelenhetId GetMatrikkelenhetId(int knr, int gnr, int bnr, int fnr, int snr)
    {
        if (_matrikkelenhetId == null)
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
            _matrikkelenhetId = matrikkelenhetIds.First();
        }
        return _matrikkelenhetId;   
    }

    private Dictionary<long, Vegadresse?> _vegadresseCache = new();
    private Vegadresse? GetVegadresse(AdresseId adresseId)
    {
        if(_vegadresseCache.TryGetValue(adresseId.value, out var vegadresse))
            return vegadresse;

        try
        {
            var vegadresseFromStoreService = _storeServiceClient.getObject(adresseId, _matrikkelContextObject) as Vegadresse;
            _vegadresseCache.Add(adresseId.value, vegadresseFromStoreService);
            return vegadresseFromStoreService;
        }
        catch (Exception e)
        {
            var message = e.Message.ToString();
            return null;
        }

    }

    private Dictionary<long, Veg?> _vegCache = new();
    private Veg? GetVeg(VegId vegId)
    {
        if (_vegCache.TryGetValue(vegId.value, out var veg))
            return veg;
        try
        {
            var veg2 = _storeServiceClient.getObject(vegId, _matrikkelContextObject) as Veg;
            _vegCache.Add(vegId.value, veg2);
            return veg2;
        }
        catch (Exception e)
        {
            var message = e.Message.ToString();
            return null;
        }
    }


    private static KodeType GetKodeType(string kodeverdi, string kodebeskrivelse)
    {
        if (string.IsNullOrEmpty(kodeverdi) && string.IsNullOrEmpty(kodebeskrivelse))
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
            if (varmeObjekt != null)
            {
                var oppvarmingsKodeId = _storeServiceClient.getObject(varmeObjekt, _matrikkelContextObject) as OppvarmingsKode;
                varmefordelinger.Add(GetKodeType(oppvarmingsKodeId?.kodeverdi, oppvarmingsKodeId?.navn[0]?.value?.ToString()));
            }            
        }
        return varmefordelinger.ToArray();
    }


    private KodeType[] GetEnergiforsyninger(Bygg bygg)
    {
        var energiforsyninger = new List<KodeType>();
        foreach (var energiObjekt in bygg.energikildeKodeIds)
        {
            if (energiObjekt != null)
            {
                var energikildeKodeId = _storeServiceClient.getObject(energiObjekt, _matrikkelContextObject) as EnergikildeKode;
                energiforsyninger.Add(GetKodeType(energikildeKodeId?.kodeverdi, energikildeKodeId?.navn[0]?.value?.ToString()));
            }            
        }
        return energiforsyninger.ToArray();
    }


    private EtasjeType[] GetEtasjer(Bygg bygg)
    {
        var etasjer = new List<EtasjeType>();
        foreach (var etasje in bygg.etasjer)
        {
            var etasjeplanKode = _storeServiceClient.getObject(etasje.etasjeplanKodeId, _matrikkelContextObject) as EtasjeplanKode;
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
            EtasjeplanKode etasjeplanKode = null;
            KodeType etasjeplan = null;
            KjokkentilgangKode kjokkenkode = null;
            KodeType kjokkenTilgang = null;
            BruksenhetstypeKode bruksenhetTypeKode = null;
            KodeType bruksenhetsType = null;

            if (bruksenhet.etasjeplanKodeId != null)
            {
                etasjeplanKode = _storeServiceClient.getObject(bruksenhet.etasjeplanKodeId, _matrikkelContextObject) as EtasjeplanKode;
                etasjeplan = GetKodeType(etasjeplanKode?.kodeverdi, etasjeplanKode?.navn[0]?.value?.ToString());
            }            
            if (bruksenhet.kjokkentilgangId != null)
            {
                kjokkenkode = _storeServiceClient.getObject(bruksenhet.kjokkentilgangId, _matrikkelContextObject) as KjokkentilgangKode;
                kjokkenTilgang = GetKodeType(kjokkenkode?.kodeverdi, kjokkenkode?.navn[0]?.value?.ToString());
            }
            if (bruksenhet.bruksenhetstypeKodeId != null)
            {
                bruksenhetTypeKode = _storeServiceClient.getObject(bruksenhet.bruksenhetstypeKodeId, _matrikkelContextObject) as BruksenhetstypeKode;
                bruksenhetsType = GetKodeType(bruksenhetTypeKode?.kodeverdi, bruksenhetTypeKode?.navn[0]?.value?.ToString());
            }                 

            BoligadresseType adresse = null;

            if (bruksenhet.adresseId != null)
            {
                var vegadresse = GetVegadresse(bruksenhet.adresseId);
                adresse = new BoligadresseType();
                if (vegadresse != null)
                {
                    var veg = GetVeg(vegadresse.vegId);
                    adresse.adressekode = veg?.adressekode!.ToString();             //TODO: kan dette være det samme som i AdresseType[]?
                    adresse.adressenavn = veg?.adressenavn;
                    adresse.adressenummer = vegadresse.nummer.ToString();
                    adresse.adressebokstav = vegadresse.bokstav;
                    //seksjonsnummer = GetMatrikkelenhet(knr, gnr, bnr, fnr, snr).matrikkelnummer.seksjonsnummer.ToString() //TODO: hent fra bruksobjekt                
                }
            }
            var bruksenhetType = new BruksenhetType()
            {
                bruksenhetsnummer = new BruksenhetsnummerType
                {
                    etasjeplan = etasjeplan,
                    etasjenummer = bruksenhet.etasjenummer.ToString(),
                    loepenummer = bruksenhet.lopenummer.ToString()
                },
                bruksareal = bruksenhet.bruksareal,
                bruksarealSpecified = bruksenhet.bruksarealSpecified,
                kjoekkentilgang = kjokkenTilgang,
                antallRom = bruksenhet.antallRom.ToString(),
                antallBad = bruksenhet.antallBad.ToString(),
                antallWC = bruksenhet.antallWC.ToString(),
                bruksenhetstype = bruksenhetsType,
                adresse = adresse
            };

            bruksenheter.Add(bruksenhetType);
        }
        return bruksenheter.ToArray();
    }

}
