using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScreenR.Web.Server.Data;

namespace ScreenR.Web.Server.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDb _appDb;

        public UsersController(AppDb appDb)
        {
            _appDb = appDb;
        }

        [HttpGet("any")]
        public async Task<bool> Any()
        {
            return await _appDb.Users.AnyAsync();
        }
    }
}
