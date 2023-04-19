using ClientApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        [SwaggerOperation(Summary = "Obtenir un client")]
        [SwaggerResponse(StatusCodes.Status200OK, "Le client a été trouvé", typeof(Client))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le client n'existe pas", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        public async Task<ActionResult<Client>> GetClient([SwaggerParameter("ID du client")] int id)
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
                await _dbContext.SaveChangesAsync();

                ClientStats stats = new ClientStats()
                {
                    PurchasedItems = 0,
                    TotalSpent = 0,
                    ClientId = client.Id //Id modifié automatiquement après la sauvegarde
                };
                await _dbContext.ClientStats.AddAsync(stats);

                await _dbContext.SaveChangesAsync();

                return StatusCode(201, client);
            }
            catch (Exception ex)
            {
                return StatusCode(400, ex.Message);
            }
        }

        [HttpPut]
        [Route("UpdateClient")]
        [SwaggerOperation(Summary = "Modifier un client")]
        [SwaggerResponse(StatusCodes.Status200OK, "Le client a été modifié", typeof(Client))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le client n'existe pas")]
        public async Task<ActionResult<Client>> UpdateClient(Client client) 
        {
            try 
            {
                Client? clientDb = await _dbContext.Client.FindAsync(client.Id);

                if (clientDb == null)
                    return NotFound();

                clientDb.LastName = client.LastName;
                clientDb.FirstName = client.FirstName;
                clientDb.Credit = client.Credit;
                await _dbContext.SaveChangesAsync();

                return Ok(client);
            }
            catch (Exception ex)
            {
                return StatusCode(400, ex.Message);
            }
        }

        [HttpGet]
        [Route("GetClientStats/{id}")]
        [SwaggerOperation(Summary = "Obtenir les statistiques d'un client")]
        [SwaggerResponse(StatusCodes.Status200OK, "Les statistiques ont été trouvées", typeof(Client))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le client n'existe pas")]
        public async Task<ActionResult<ClientStats>> GetClientStats([SwaggerParameter("ID du client")] int id) //id du client
        {
            try 
            {
                ClientStats? stats = await _dbContext.ClientStats.Where(c => c.ClientId == id).FirstOrDefaultAsync();

                if (stats == null)
                    return NotFound();

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(400, ex.Message);
            }
        }

        [HttpPut]
        [Route("UpdateClientStats")]
        [SwaggerOperation(Summary = "Modifier les statistiques d'un client")]
        [SwaggerResponse(StatusCodes.Status200OK, "Les statistiques ont été modifiées", typeof(ClientStats))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le client n'existe pas")]
        public async Task<ActionResult<ClientStats>> UpdateClientStats(ClientStats clientStats)
        {
            try 
            {
                ClientStats? stats = await _dbContext.ClientStats.Where(c => c.ClientId == clientStats.ClientId).FirstOrDefaultAsync();

                if (stats == null)
                    return NotFound();

                stats.TotalSpent = clientStats.TotalSpent;
                stats.PurchasedItems = clientStats.PurchasedItems;
                await _dbContext.SaveChangesAsync();

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(400, ex.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteClient/{id}")]
        [SwaggerOperation(Summary = "Supprimer un client")]
        [SwaggerResponse(StatusCodes.Status200OK, "Le client a été supprimé", typeof(ClientStats))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le client n'existe pas")]
        public async Task<ActionResult<ClientStats>> DeleteClient(int id)
        {
            try
            {
                Client? client = await _dbContext.Client.FindAsync(id);
                ClientStats? stats = await _dbContext.ClientStats.Where(c => c.ClientId == id).FirstOrDefaultAsync();

                if (client == null)
                    return NotFound();

                _dbContext.Client.Remove(client);
                _dbContext.ClientStats.Remove(stats);
                await _dbContext.SaveChangesAsync();

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(400, ex.Message);
            }
        }



    }
}
