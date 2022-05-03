using DiBK.Matrikkelopplysninger.Services;
using Microsoft.AspNetCore.Mvc;
using no.kxml.skjema.dibk.matrikkelregistrering;

namespace DiBK.Matrikkelopplysninger.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatrikkelController : ControllerBase
    {
        private readonly IConfiguration _config;

        public MatrikkelController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("{knr:int},{gnr:int},{bnr:int},{fnr:int},{snr:int}")]
        public MatrikkelregistreringType Get(int knr, int gnr, int bnr, int fnr, int snr)
        {
            return new MatrikkeldataProvider(_config).GetMatrikkelOpplysninger(knr, gnr, bnr, fnr, snr);
        }
    }
}
