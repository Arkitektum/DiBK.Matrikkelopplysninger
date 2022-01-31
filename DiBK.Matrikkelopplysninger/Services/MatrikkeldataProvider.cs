﻿using no.kxml.skjema.dibk.matrikkelregistrering;

namespace DiBK.Matrikkelopplysninger.Services;

public class MatrikkeldataProvider
{
    private readonly IConfiguration _config;

    public MatrikkeldataProvider(IConfiguration config)
    {
        _config = config;
    }

    public MatrikkelregistreringType GetMatrikkelOpplysninger(int knr, int gnr, int bnr)
    {
        return new MatrikkelregistreringType
        {
            eiendomsidentifikasjon = new MatrikkelnummerType[]
            {
                new()
                {
                    kommunenummer = knr.ToString(),
                    gaardsnummer = gnr.ToString(),
                    bruksnummer = bnr.ToString()
                }
            }
        };
    }
}
