using Microsoft.AspNetCore.Mvc;

namespace ProductApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : ControllerBase
    {
        private ProductDBContext _dbContext;
        public ProductController(ProductDBContext context) 
        {
            _dbContext = context;
        }
        [HttpGet]
        [Route("GetProduct")]
        public ActionResult GetProduct(int id)
        {
            return Ok();
        }
    }
}
