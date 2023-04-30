using ClientApi.Classes;
using ClientApi.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Cryptography;

namespace ClientApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("AddClient")]
        [SwaggerOperation(Summary = "Ajouter un nouveau client")]
        [SwaggerResponse(StatusCodes.Status201Created, "Le client a été créé", typeof(Client))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        public async Task<ActionResult<Client>> AddClient(ClientCreation client) 
        {
            try 
            {
                bool nameTaken = _dbContext.Client.Where(c => c.Username == client.Username).Count() != 0;

                if (nameTaken)
                    return BadRequest("Ce nom d'utilisateur est déjà pris.");

                if(client.Password.Length < 5)
                    return BadRequest("Le mot de passe doit contenir au moins 5 caractères.");

                //Enregistrer le client
                Client clientDb = new Client()
                {
                    FirstName = client.FirstName,
                    LastName = client.LastName,
                    Credit = 0,
                    Username = client.Username
                };

                await _dbContext.Client.AddAsync(clientDb);
                await _dbContext.SaveChangesAsync();

                ClientStats stats = new ClientStats()
                {
                    PurchasedItems = 0,
                    TotalSpent = 0,
                    ClientId = clientDb.Id //Id modifié automatiquement après la sauvegarde
                };

                //Hasher le mot de passe
                byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
                string hashedPassword = HashPassword(client.Password, salt);

                _dbContext.Password.Add(new Password() { 
                    Salt = Convert.ToBase64String(salt),
                    Hash = hashedPassword,
                    ClientId = clientDb.Id
                });

                await _dbContext.ClientStats.AddAsync(stats);

                await _dbContext.SaveChangesAsync();

                return StatusCode(201, clientDb);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("ConnectClient")]
        [SwaggerOperation(Summary = "Connecter un client")]
        [SwaggerResponse(StatusCodes.Status200OK, "La connexion a été autorisée")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "La requête est invalide", typeof(ValidationProblemDetails))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Le client n'existe pas")]
        public async Task<ActionResult> ConnectClient(string username, string password)
        {
            try
            {
                Client? clientDb = await _dbContext.Client.Where(c => c.Username == username).FirstOrDefaultAsync();

                if (clientDb == null)
                    return BadRequest("Cet usager n'existe pas.");

                if (CheckPassword(clientDb.Id, password))
                    return Ok(password); //retourner un jsonWebToken

                //var claims = new[]
                //{
                //    new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                //};
                //var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                //var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                //var token = new JwtSecurityToken(
                //    _configuration["Jwt:Issuer"],
                //    _configuration["Jwt:Issuer"],
                //    claims,
                //    expires: DateTime.Now.AddMinutes(30),
                //    signingCredentials: creds
                //);
                //return Ok(new
                //{
                //    token = new JwtSecurityTokenHandler().WriteToken(token)
                //});

                return BadRequest("Mot de passe eronné.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
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
                return BadRequest(ex.Message);
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
                return BadRequest(ex.Message);
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
                return BadRequest(ex.Message);
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
                return BadRequest(ex.Message);
            }
        }

        private bool CheckPassword(int clientId, string password)
        {
            Password? passwordDb = _dbContext.Password.Where(c => c.ClientId == clientId).FirstOrDefault();

            byte[] salt = Convert.FromBase64String(passwordDb.Salt);
            //Teste le mot de passe à l'aide du sel enregistré
            return HashPassword(password, salt) == passwordDb.Hash;
        }

        //Source : https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing?view=aspnetcore-7.0
        private string HashPassword(string password, byte[] salt)
        {
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password!,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return hashed;
        }
    }
}
