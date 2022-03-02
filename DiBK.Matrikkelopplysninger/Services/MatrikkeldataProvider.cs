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


    public MatrikkelregistreringType GetMatrikkelOpplysninger(int knr, int gnr, int bnr)
    {
        return new MatrikkelregistreringType
        {
            eiendomsidentifikasjon = new MatrikkelnummerType[]
            {
                new()
                {
                    kommunenummer = GetMatrikkelenhet(knr, gnr, bnr).matrikkelnummer.kommuneId.value.ToString(),
                    gaardsnummer = GetMatrikkelenhet(knr, gnr, bnr).matrikkelnummer.gardsnummer.ToString(),
                    bruksnummer = GetMatrikkelenhet(knr, gnr, bnr).matrikkelnummer.bruksnummer.ToString(),
                    seksjonsnummer = GetMatrikkelenhet(knr, gnr, bnr).matrikkelnummer.seksjonsnummer.ToString(),
                    festenummer = GetMatrikkelenhet(knr, gnr, bnr).matrikkelnummer.festenummer.ToString()
                }
            },

            adresse = new AdresseType[]
            {
                new()
                {
                    adressebokstav = GetVegadresse(knr, gnr, bnr).bokstav,
                    adressekode = GetVeg(knr, gnr, bnr).adressekode.ToString(),
                    adressenavn = GetVeg(knr, gnr, bnr).adressenavn,
                    adressenummer = GetVegadresse(knr, gnr, bnr).nummer.ToString(),
                    seksjonsnummer = GetMatrikkelenhet(knr, gnr, bnr).matrikkelnummer.seksjonsnummer.ToString() //TODO: hva menes egentlig her?
                }
            },
            
            bygning = GetBygninger(knr, gnr, bnr),

            signatur = GetSignatur(knr, gnr, bnr), //TODO: Er dette noe brukeren selv skal fylle ut, og ikke hentes fra matrikkelAPI?

            prosjektnavn = "", 
           
            kommunenavn = GetKommune(knr, gnr, bnr).kommunenavn,
        };
    }


    private AdresseId GetAdresseId(int knr, int gnr, int bnr)
    {
        var adressesokModel = new AdressesokModel()
        {
            kommunenummer = knr.ToString(),
            gardsnummer = gnr.ToString(),
            bruksnummer = bnr.ToString()
        };

        AdresseId[] adresseIds = _adresseServiceClient.findAdresser(adressesokModel, _matrikkelContextObject);
        return adresseIds.First();
    }

    private MatrikkelenhetId GetMatrikkelenhetId(int knr, int gnr, int bnr)
    {
        var matrikkelenhetsokModel = new MatrikkelenhetsokModel()
        {
            kommunenummer = knr.ToString(),
            gardsnummer = gnr.ToString(),
            bruksnummer = bnr.ToString()
        };

        MatrikkelenhetId[] matrikkelenhetIds = _matrikkelenhetServiceClient.findMatrikkelenheter(matrikkelenhetsokModel, _matrikkelContextObject);

        return matrikkelenhetIds.First();
    }

    private MatrikkelBubbleObject[] GetByggIds(int knr, int gnr, int bnr)
    {
        ByggId[] byggIds = _bygningServiceClient.findByggForMatrikkelenhet(GetMatrikkelenhetId(knr, gnr, bnr), _matrikkelContextObject);
        MatrikkelBubbleObject[] matrikkelBubbleObjectByggIds = _storeServiceClient.getObjects(byggIds, _matrikkelContextObject);

        return matrikkelBubbleObjectByggIds;
    }



    private Vegadresse GetVegadresse(int knr, int gnr, int bnr)
    {
        return (Vegadresse)_storeServiceClient.getObject(GetAdresseId(knr, gnr, bnr), _matrikkelContextObject);
    }

    private Veg GetVeg(int knr, int gnr, int bnr)
    {
        return (Veg)_storeServiceClient.getObject(GetVegadresse(knr, gnr, bnr).vegId, _matrikkelContextObject);
    }

        private Kommune GetKommune(int knr, int gnr, int bnr)
    {
        return (Kommune)_storeServiceClient.getObject(GetVeg(knr, gnr, bnr).kommuneId, _matrikkelContextObject);
    }

    private Matrikkelenhet GetMatrikkelenhet(int knr, int gnr, int bnr)
    {
        return (Matrikkelenhet)_storeServiceClient.getObject(GetMatrikkelenhetId(knr, gnr, bnr), _matrikkelContextObject);
    }
    
    

    public BygningType[] GetBygninger(int knr, int gnr, int bnr)
    {
        var bygninger = new List<BygningType>();

        foreach (Bygg bygg in GetByggIds(knr, gnr, bnr))
        {
            var bygning = new BygningType();

            if (bygg is Bygning) //TODO: dersom vi skal skille på bygg og bygningsendring
            {
                var matrikkelBubbleObjectBruksenheter = _storeServiceClient.getObjects(bygg.bruksenhetIds, _matrikkelContextObject);
                foreach (Bruksenhet bruksenhetObject in matrikkelBubbleObjectBruksenheter)
                {
                    //variabler til kodeliste
                    var naeringsgruppe = (NaringsgruppeKode)_storeServiceClient.getObject(((Bygning)bygg).naringsgruppeKodeId, _matrikkelContextObject);
                    var bygningstypeKode = (BygningstypeKode)_storeServiceClient.getObject(((Bygning)bygg).bygningstypeKodeId, _matrikkelContextObject);
                    var etasjeplanKode = (EtasjeplanKode)_storeServiceClient.getObject(bruksenhetObject.etasjeplanKodeId, _matrikkelContextObject);
                    var avlopskode = (AvlopsKode) _storeServiceClient.getObject(((Bygning) bygg).avlopsKodeId, _matrikkelContextObject);
                    var kjokkenkode = (KjokkentilgangKode) _storeServiceClient.getObject(bruksenhetObject.kjokkentilgangId, _matrikkelContextObject);
                    var bruksenhetType = (BruksenhetstypeKode) _storeServiceClient.getObject(bruksenhetObject.bruksenhetstypeKodeId, _matrikkelContextObject);
                    //var varmefordeling = _storeServiceClient.getObjects(((Bygning)bygg).oppvarmingsKodeIds, _matrikkelContextObject);
                    //var varme = (OppvarmingsKode)_storeServiceClient.getObject(varmefordeling[0].id, _matrikkelContextObject);
                    //var energiforsyning = _storeServiceClient.getObjects(((Bygning) bygg).energikildeKodeIds, _matrikkelContextObject);
                    //var energi = (EnergikildeKode) _storeServiceClient.getObject(energiforsyning[0].id, _matrikkelContextObject);
                    var vannforsyning = (VannforsyningsKode) _storeServiceClient.getObject(((Bygning) bygg).vannforsyningsKodeId, _matrikkelContextObject);
                   

                    bygning.bygningsnummer = bygg.bygningsnummer.ToString();

                    bygning.naeringsgruppe = new KodeType
                    {
                        kodeverdi = naeringsgruppe.kodeverdi,
                        kodebeskrivelse = naeringsgruppe.navn.ToString()
                    };

                    bygning.bygningstype = new KodeType
                    {
                        kodeverdi = bygningstypeKode.kodeverdi,
                        kodebeskrivelse = bygningstypeKode.navn.ToString()
                    };

                    bygning.bebygdAreal = bygg.bebygdAreal;
                    bygning.bebygdArealSpecified = bygg.bebygdArealSpecified;

                    var etasjer = new List<EtasjeType>();

                    foreach (var etasje in bygg.etasjer)
                        etasjer.Add(new EtasjeType
                        {
                            antallBoenheter = etasje.antallBoenheter.ToString(),
                            bruksarealTilAnnet = etasje.bruksarealTilAnnet,
                            bruksarealTilAnnetSpecified = etasje.bruksarealTilAnnetSpecified,
                            bruksarealTilBolig = etasje.bruksarealTilBolig,
                            bruksarealTilBoligSpecified = etasje.bruksarealTilBoligSpecified,
                            bruksarealTotalt = etasje.bruksarealTotalt,
                            bruksarealTotaltSpecified = etasje.bruksarealTotaltSpecified,
                            etasjenummer = etasje.etasjenummer.ToString(),//TODO: skal denne hentes fra et annet sted?
                            etasjeplan = new KodeType
                            {
                                kodeverdi = etasjeplanKode.kodeverdi,
                                kodebeskrivelse = etasjeplanKode.navn.ToString()
                            },
                            bruttoarealTilBolig = etasje.bruttoarealTilBolig,
                            bruttoarealTilBoligSpecified = etasje.bruttoarealTilBoligSpecified,
                            bruttoarealTilAnnet = etasje.bruttoarealTilAnnet,
                            bruttoarealTilAnnetSpecified = etasje.bruttoarealTilAnnetSpecified,
                            //etasjeopplysning = bygg.bygningsstatusKodeId. //TODO: finn ut hvor denne er i apiet
                            bruttoarealTotalt = etasje.bruttoarealTotalt,
                            bruttoarealTotaltSpecified = etasje.bruttoarealTotaltSpecified
                        });

                    bygning.etasjer = etasjer.ToArray();


                    bygning.avlop = new KodeType
                    {
                        kodeverdi = avlopskode.kodeverdi,
                        kodebeskrivelse = avlopskode.navn.ToString()
                    };


                    var bruksenheter = new List<BruksenhetType>();

                    foreach (var bruksenhet in bygg.bruksenhetIds) //er det riktig med bruksenhetIds?
                        bruksenheter.Add(new BruksenhetType
                        {                  
                            bruksenhetsnummer = new BruksenhetsnummerType
                            {
                                etasjeplan = new KodeType
                                {
                                    kodeverdi = etasjeplanKode.kodeverdi,
                                    kodebeskrivelse = etasjeplanKode.navn.ToString()
                                },
                                etasjenummer = bruksenhetObject.etasjenummer.ToString(),
                                loepenummer = bruksenhetObject.lopenummer.ToString()
                            },
                            bruksareal = bruksenhetObject.bruksareal,
                            bruksarealSpecified = bruksenhetObject.bruksarealSpecified,
                            kjoekkentilgang = new KodeType
                            {
                                kodeverdi = kjokkenkode.kodeverdi,
                                kodebeskrivelse = kjokkenkode.navn.ToString()
                            },
                            antallRom = bruksenhetObject.antallRom.ToString(),
                            antallBad = bruksenhetObject.antallBad.ToString(),
                            antallWC = bruksenhetObject.antallWC.ToString(),
                            bruksenhetstype = new KodeType
                            {
                                kodeverdi = bruksenhetType.kodeverdi,
                                kodebeskrivelse = bruksenhetType.navn.ToString()
                            },
                            adresse = new BoligadresseType                  //TODO: kan dette være det samme som i AdresseType[]?
                            {
                                adressekode = GetVeg(knr, gnr, bnr).adressekode.ToString(),
                                adressenavn = GetVeg(knr, gnr, bnr).adressenavn,
                                adressenummer = GetVegadresse(knr, gnr, bnr).nummer.ToString(),
                                adressebokstav = GetVegadresse(knr, gnr, bnr).bokstav,
                                seksjonsnummer = GetMatrikkelenhet(knr, gnr, bnr).matrikkelnummer.seksjonsnummer.ToString()
                            }
                            //boligOpplysning = TODO: finn fram                        
                        });

                    bygning.bruksenheter = bruksenheter.ToArray();

                    bygning.energiforsyning = new EnergiforsyningType
                    {
                        varmefordeling = new KodeType[]
                        {
                            new KodeType
                            {
                                //kodeverdi = varme.kodeverdi,
                                //kodebeskrivelse = varme.navn.ToString()
                            },
                        },
                        energiforsyning = new KodeType[]
                        {
                            new KodeType
                            {
                                //kodeverdi = energi.kodeverdi,
                                //kodebeskrivelse = energi.navn.ToString()
                            },
                        },
                        //relevant = //TODO: ser ut som denne allerede ligger i kodelisten
                        //relevantSpecified = 
                    };

                    bygning.vannforsyning = new KodeType
                    {
                        kodeverdi = vannforsyning.kodeverdi,
                        kodebeskrivelse = vannforsyning.navn.ToString()
                    };

                    bygning.harHeis = bygg.harHeis;
                    bygning.harHeisSpecified = bygg.harHeisSpecified;

                }//foreach bruksenhet

                bygninger.Add(bygning);
            }
        }

        return bygninger.ToArray();
    }

    public SignaturType GetSignatur(int knr, int gnr, int bnr)
    {
        var signatur = new SignaturType();
        signatur.signaturdato = DateTime.Now;
        signatur.signaturdatoSpecified = true;
        //signatur.signeringssteg = //TODO: finn ut hva dette er

        var matrikkelenhet = (Matrikkelenhet)_storeServiceClient.getObject(GetMatrikkelenhetId(knr, gnr, bnr), _matrikkelContextObject);

        foreach (Eierforhold eierforhold in matrikkelenhet.eierforhold)
        {
            if (eierforhold is TinglystEierforhold)
            {
                TinglystEierforhold tinglystEierforhold = null;

                if ((TinglystEierforhold)tinglystEierforhold is PersonTinglystEierforhold)
                {
                    PersonTinglystEierforhold personTinglyst = (PersonTinglystEierforhold)tinglystEierforhold;
                    Person person = (Person)_storeServiceClient.getObject(personTinglyst.eierId, _matrikkelContextObject);
                    if (person is FysiskPerson)
                    {
                        signatur.signertAv = person.navn;
                    }
                    else if (person is JuridiskPerson)
                    {
                        signatur.signertPaaVegneAv = person.navn;
                    }
                }
            }
        }
        return signatur;
    }
}
