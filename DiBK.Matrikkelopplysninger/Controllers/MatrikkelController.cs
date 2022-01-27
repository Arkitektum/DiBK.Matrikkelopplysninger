using DiBK.Matrikkelopplysninger.Services;
using Microsoft.AspNetCore.Mvc;
using no.kxml.skjema.dibk.matrikkelregistrering;

namespace DiBK.Matrikkelopplysninger.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatrikkelController : ControllerBase
    {
        [HttpGet("{knr:int},{gnr:int},{bnr:int}")]
        public MatrikkelregistreringType Get(int knr, int gnr, int bnr)
        {
            return new MatrikkeldataProvider().GetMatrikkelOpplysninger(knr, gnr, bnr);
        }
    }
}
