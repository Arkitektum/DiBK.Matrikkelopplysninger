using System.Linq;
using DiBK.Matrikkelopplysninger.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DiBK.MatrikkelopplysningerTests.Services;

public class MatrikkeldataProviderTests
{
    private readonly ConfigurationManager _config;

    public MatrikkeldataProviderTests()
    {
        _config = new ConfigurationManager();
        _config.AddJsonFile("appsettings.json");
    }

    [Fact]
    public void GetMatrikkelOpplysningerTest()
    {
        var matrikkelregistreringType = new MatrikkeldataProvider(_config).GetMatrikkelOpplysninger(3817, 56, 16, 0, 0);

        var matrikkelnummerType = matrikkelregistreringType.eiendomsidentifikasjon.First();       

        matrikkelnummerType.kommunenummer.Should().Be("3817");
        matrikkelnummerType.gaardsnummer.Should().Be("56");
        matrikkelnummerType.bruksnummer.Should().Be("16");
        matrikkelnummerType.festenummer.Should().Be("0");
        matrikkelnummerType.seksjonsnummer.Should().Be("0");
    }

    [Fact]
    public void GetAdresserTest()
    {
        var adresseType = new MatrikkeldataProvider(_config).GetAdresser(new MatrikkelenhetId { value = 68576855 }).First();
               
        adresseType.adressekode.Should().Be("1092");
        adresseType.adressenavn.Should().Be("Skyttarvegen");
        adresseType.adressenummer.Should().Be("91");
        adresseType.adressebokstav.Should().Be(null);
    }

    [Fact]
    public void GetBygningerTest()
    {
        var bygningType = new MatrikkeldataProvider(_config).GetBygninger(new MatrikkelenhetId { value = 68576855 }).First();

        bygningType.bygningsnummer.Should().Be("165680677");
        
    }

}
