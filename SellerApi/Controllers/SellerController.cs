using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SellerApi;

namespace SellerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellerController : ControllerBase
    {

        private HttpClient _httpClient;
        private JsonSerializerOptions options;
        public SellerController(SellerDBContext dBContext)
        {
            _httpClient = new HttpClient();
            options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }
        [HttpGet]
        [Route("GetOk")]
        public ActionResult Index()
        {
            return Ok();
            //r//eturn View();
        }
    }
}
