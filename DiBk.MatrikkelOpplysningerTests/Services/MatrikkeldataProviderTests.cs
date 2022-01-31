﻿using System.Linq;
using DiBK.Matrikkelopplysninger.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DiBK.MatrikkelopplysningerTests.Services;

public class MatrikkeldataProviderTests
{
    private readonly IConfiguration _config;

    public MatrikkeldataProviderTests(IConfiguration config)
    {
        _config = config;
    }

    [Fact]
    public void GetMatrikkelOpplysningerTest()
    {
        var matrikkelregistreringType = new MatrikkeldataProvider(_config).GetMatrikkelOpplysninger(3817, 55, 13);

        var matrikkelnummerType = matrikkelregistreringType.eiendomsidentifikasjon.First();

        matrikkelnummerType.kommunenummer.Should().Be("3817");
        matrikkelnummerType.gaardsnummer.Should().Be("55");
        matrikkelnummerType.bruksnummer.Should().Be("13");
    }
}
