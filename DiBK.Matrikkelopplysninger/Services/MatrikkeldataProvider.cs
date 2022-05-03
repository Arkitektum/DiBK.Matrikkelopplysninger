using no.kxml.skjema.dibk.matrikkelregistrering;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.adresse;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.bygning;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.matrikkelenhet;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.store;
using no.statkart.matrikkel.matrikkelapi.wsapi.v1.service.kodeliste;
using System.Linq;

namespace DiBK.Matrikkelopplysninger.Services;

public class MatrikkeldataProvider
{
    private readonly MatrikkelContext _matrikkelContextObject;
    private readonly StoreServiceClient _storeServiceClient;
    private readonly BygningServiceClient _bygningServiceClient;
    private readonly AdresseServiceClient _adresseServiceClient;
    private readonly MatrikkelenhetServiceClient _matrikkelenhetServiceClient;
    private readonly KodelisteServiceClient _kodelisteServiceClient;


    public MatrikkeldataProvider(IConfiguration config)
    {
        var matrikkelClientProvider = new MatrikkelServiceClientProvider(config);

        _matrikkelContextObject = matrikkelClientProvider.GetMatrikkelContextObject();
        _matrikkelenhetServiceClient = matrikkelClientProvider.GetMatrikkelenhetServiceClient();
        _adresseServiceClient = matrikkelClientProvider.GetAdresseServiceClient();
        _bygningServiceClient = matrikkelClientProvider.GetBygningServiceClient();
        _storeServiceClient = matrikkelClientProvider.GetStoreServiceClient();
        _kodelisteServiceClient = matrikkelClientProvider.GetKodelisteServiceClient();
    }


    public MatrikkelregistreringType GetMatrikkelOpplysninger(int knr, int gnr, int bnr, int fnr, int snr)
    {
        var matrikkelenhetId = GetMatrikkelenhetId(knr, gnr, bnr, fnr, snr);
        var matrikkelenhet = _storeServiceClient.getObject(matrikkelenhetId, _matrikkelContextObject) as Matrikkelenhet;      
        var kommune = _storeServiceClient.getObject(matrikkelenhet.matrikkelnummer.kommuneId, _matrikkelContextObject) as Kommune;
        
        
        /******KLADD**********/
        ////samling av bobleobjekter
        //var matrikkelTransfer = _matrikkelenhetServiceClient.findObjekterForMatrikkelenhet(matrikkelenhetId, _matrikkelContextObject);
        //var bobleobjekter = matrikkelTransfer.bubbleObjects;
        ////matrikkelenhet med alle teiggrenser og kvalitet
        //var matrikkelmedteig = _matrikkelenhetServiceClient.findMatrikkelenhetMedTeiger(new MatrikkelenhetIdent { kommuneIdent = new() { kommunenummer = knr.ToString() }, gardsnummer = gnr, gardsnummerSpecified = true, bruksnummer = bnr, bruksnummerSpecified = true, festenummer = fnr, festenummerSpecified = true, seksjonsnummer = snr, seksjonsnummerSpecified = true }, _matrikkelContextObject);
        //var bobleteig = matrikkelmedteig.bubbleObjects;
        //var teigid = bobleteig.First().id;
        //var teig = (Teiggrense)_storeServiceClient.getObject(teigid, _matrikkelContextObject);
        //var kvalitet = teig.kvalitet;
        ///********************/

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
            adresse = GetAdresser(matrikkelenhet),
            bygning = GetBygninger(matrikkelenhetId),
            prosjektnavn = "",
            kommunenavn = kommune?.kommunenavn,
        };
    }


    public AdresseType[] GetAdresser(Matrikkelenhet matrikkelenhet)
    {
        var adresseList = new List<AdresseType>();
        AdresseId[] adresseIds = _adresseServiceClient.findAdresserForMatrikkelenhet((MatrikkelenhetId)matrikkelenhet.id, _matrikkelContextObject);
       
        foreach (var adresseId in adresseIds)
        {                     
            var vegadresse = GetVegadresse(adresseId); //TODO: sjekk på matrikkeladresse? - spør Tine
            
            if (vegadresse != null)
            {
                var veg = GetVeg(vegadresse?.vegId);
                var adresser = new AdresseType
                {
                    adressekode = veg?.adressekode.ToString(),
                    adressenavn = veg?.adressenavn,
                    adressenummer = vegadresse?.nummer.ToString(),
                    adressebokstav = vegadresse?.bokstav,
                    seksjonsnummer = matrikkelenhet.matrikkelnummer.seksjonsnummer.ToString()
                };

            adresseList.Add(adresser);
            }
        }

        return adresseList.ToArray();
    }


    public BygningType[] GetBygninger(MatrikkelenhetId matrikkelenhetId)
    {
        var bygninger = new List<BygningType>();

        ByggId[] byggIds = _bygningServiceClient.findByggForMatrikkelenhet(matrikkelenhetId, _matrikkelContextObject);
        MatrikkelBubbleObject[] matrikkelBubbleObjectsBygg = _storeServiceClient.getObjects(byggIds, _matrikkelContextObject);

        foreach (Bygg bygg in matrikkelBubbleObjectsBygg.Where(p => p is Bygning))
        {            
            var bygning = new BygningType();
            AvlopsKode avlopsKode = null;
            BygningstypeKode bygningstypeKode = null;
            VannforsyningsKode vannforsyningsKode = null;  

            //variabler til kodeliste
            if(bygg.naringsgruppeKodeId != null && _storeServiceClient.getObject(bygg.naringsgruppeKodeId, _matrikkelContextObject) is NaringsgruppeKode naeringsgruppeKode)
                bygning.naeringsgruppe = GetKodeType(naeringsgruppeKode);
           
            if (bygg.avlopsKodeId != null)
            {
                avlopsKode = _storeServiceClient.getObject(bygg.avlopsKodeId, _matrikkelContextObject) as AvlopsKode;
                bygning.avlop = GetKodeType(avlopsKode);
            }
            if (((Bygning)bygg).bygningstypeKodeId != null)
            {
                bygningstypeKode = _storeServiceClient.getObject(((Bygning)bygg).bygningstypeKodeId, _matrikkelContextObject) as BygningstypeKode;
                bygning.bygningstype = GetKodeType(bygningstypeKode);
            }
            if (bygg.vannforsyningsKodeId != null)
            {
                vannforsyningsKode = _storeServiceClient.getObject(bygg.vannforsyningsKodeId, _matrikkelContextObject) as VannforsyningsKode;
                bygning.vannforsyning = GetKodeType(vannforsyningsKode);
            }
         
            var snr = GetSeksjonsnummer(bygg);

            bygning.bygningsnummer = bygg.bygningsnummer.ToString();            
            bygning.bebygdAreal = bygg.bebygdAreal;
            bygning.bebygdArealSpecified = bygg.bebygdArealSpecified;
            bygning.etasjer = GetEtasjer(bygg);            
            bygning.bruksenheter = GetBruksenheter(bygg, snr); //TODO: Må jeg kanskje sende med seksjonsnummer her?
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


    private static KodeType GetKodeType(MatrikkelEnumKode kode)
    {
        var kodeverdi = kode?.kodeverdi;
        var kodebeskrivelse = kode?.navn[0]?.value?.ToString();

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
                varmefordelinger.Add(GetKodeType(oppvarmingsKodeId));
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
                energiforsyninger.Add(GetKodeType(energikildeKodeId));
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
                etasjeplan = etasjeplanKode != null ? GetKodeType(etasjeplanKode) : null, //TODO: hør med Jørgen om vi skal sjekke på null her eller før storeservice-kallet ?
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


    private BruksenhetType[] GetBruksenheter(Bygg bygg, int snr)
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
                etasjeplan = GetKodeType(etasjeplanKode);
            }            
            if (bruksenhet.kjokkentilgangId != null)
            {
                kjokkenkode = _storeServiceClient.getObject(bruksenhet.kjokkentilgangId, _matrikkelContextObject) as KjokkentilgangKode;
                kjokkenTilgang = GetKodeType(kjokkenkode);
            }
            if (bruksenhet.bruksenhetstypeKodeId != null)
            {
                bruksenhetTypeKode = _storeServiceClient.getObject(bruksenhet.bruksenhetstypeKodeId, _matrikkelContextObject) as BruksenhetstypeKode;
                bruksenhetsType = GetKodeType(bruksenhetTypeKode);
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
                    var matrikkelenhet = _storeServiceClient.getObject(bruksenhet.matrikkelenhetId, _matrikkelContextObject) as Matrikkelenhet;
                    adresse.seksjonsnummer = matrikkelenhet.matrikkelnummer.seksjonsnummer.ToString(); //TODO: hent fra bruksobjekt                
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
                //boligOpplysning = ,   //TODO:             
                bruksenhetstype = bruksenhetsType,
                adresse = adresse
            };

            bruksenheter.Add(bruksenhetType);
        }
        return bruksenheter.ToArray();
    }

    private int GetSeksjonsnummer(Bygg bygg)
    {
        MatrikkelenhetId[] matrikkelenhetIds = _matrikkelenhetServiceClient.findMatrikkelenheterForBygg((ByggId)bygg.id, _matrikkelContextObject);
        var matrikkelenhet = _storeServiceClient.getObject(matrikkelenhetIds.First(), _matrikkelContextObject) as Matrikkelenhet;

        var snr = matrikkelenhet.matrikkelnummer.seksjonsnummer;
        return snr;
    }
}
