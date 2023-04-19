using ClientApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ClientApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClientController : ControllerBase
    {
        private ClientDBContext _dbContext;
        public ClientController(ClientDBContext context)
        {
            _dbContext = context;
        }

        [HttpGet]
        [Route("GetClient/{id}")]
        [SwaggerOperation(Summary = "Créer un nouveau client")]
        [SwaggerResponse(StatusCodes.Status200OK, "Le client a été créé", typeof(Client))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le client n'existe pas", typeof(ValidationProblemDetails))]
        public async Task<ActionResult<Client>> GetClient(int id)
        {
            try
            {
                var client = await _dbContext.Client.FindAsync(id);

                if (client == null) 
                    return NotFound();

                return Ok(client);
            }
            catch (Exception ex)
            {
                return StatusCode(400, ex.Message);
            }
        }

        [HttpPost]
        [Route("CreateClient")]
        [SwaggerOperation(Summary = "Créer un nouveau client")]
        [SwaggerResponse(StatusCodes.Status201Created, "Le client a été créé", typeof(Client))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        public async Task<ActionResult<Client>> CreateClient(Client client) 
        {
            try 
            { 
                await _dbContext.Client.AddAsync(client);
                _dbContext.SaveChanges();
                return StatusCode(201, client);
            }
            catch (Exception ex)
            {
                return StatusCode(400, ex.Message);
            }
        }

    }
}
