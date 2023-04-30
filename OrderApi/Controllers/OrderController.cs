using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderApi.Classes;
using OrderApi.Models;
using Stripe;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;

namespace OrderApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class OrderController : ControllerBase
    {
        private OrderDBContext _dbContext;
        private HttpClient _httpClient = new HttpClient();
        public OrderController(OrderDBContext dbContext) 
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Obtenir les commandes d'un client
        /// </summary>
        /// <param name="clientId">ID du client.</param>
        /// <returns>Une liste des commandes du client.</returns>
        /// <response code="200">Retourne les commandes du client</response>
        /// <response code="204">Le client n'a aucune commande</response>
        /// <response code="404">Le client n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("GetClientReceipts/{clientId}")]
        [HttpGet]
        public async Task<ActionResult<List<Receipt>>> GetClientReceipts (int clientId) 
        {
            try
            {
                //Vérifier que le client existe
                bool clientExists = await ClientExists(clientId);
                if (!clientExists)
                    return NotFound("Le client n'existe pas.");

                List<Receipt> receipts = await _dbContext.Receipt.Where(c => c.ClientId == clientId).ToListAsync();
                if(receipts.Count == 0)
                    return NoContent();

                return Ok(receipts);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Obtenir le détail d'une commande
        /// </summary>
        /// <param name="receiptId">ID de la commande.</param>
        /// <returns>Une liste des produits de la commande.</returns>
        /// <response code="200">Retourne le détail de la commande</response>
        /// <response code="404">La commande n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("GetReceiptItems/{receiptId}")]
        [HttpGet]
        public async Task<ActionResult<ProductAndQuantity>> GetReceiptItems (int receiptId) 
        {
            try
            {
                Receipt? receipt = await _dbContext.Receipt.FindAsync(receiptId);

                if (receipt == null)
                    return NotFound("La commande n'existe pas.");

                List<ReceiptItem>? lstItems = await _dbContext.ReceiptItem.Where(c => c.ReceiptId == receiptId).ToListAsync();

                int[] itemsId = lstItems.Select(c => c.Id).ToArray();

                //Appelle le service product pour trouver les produits recherchés
                var responseProduct = await _httpClient.PostAsJsonAsync("http://localhost:5000/api/product/GetProductsById", itemsId);

                if (responseProduct.IsSuccessStatusCode == false)
                    return StatusCode(StatusCodes.Status500InternalServerError, "Erreur lors de l'appel de ProductApi.");

                List<Classes.Product>? lstProducts = await responseProduct.Content.ReadFromJsonAsync<List<Classes.Product>>();

                //Crée une liste contenant les produits et leur quantité
                List<ProductAndQuantity> lstReceiptProducts = new List<ProductAndQuantity>();
                foreach (Classes.Product product in lstProducts)
                {
                    int quantity = lstItems.Where(c => c.ProductId == product.ProductId).Select(c => c.Quantity).FirstOrDefault();
                    lstReceiptProducts.Add(new ProductAndQuantity(product, quantity));
                }

                return Ok(lstReceiptProducts);
            }
            catch (Exception ex) 
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            
        }

        /// <summary>
        /// Créer une commande
        /// </summary>
        /// <param name="clientId">ID du client.</param>
        /// <returns></returns>
        /// <response code="200">La commande a été créée</response>
        /// <response code="400">Aucun produit dans le panier</response>
        /// <response code="404">Le client n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("CreateOrder/{clientId}")]
        [HttpPost]
        public async Task<ActionResult> CreateOrder (int clientId) 
        {
            try
            {
                bool clientExists = await ClientExists(clientId);

                if (!clientExists) {
                    return NotFound();
                }

                //Appelle le service ProductApi pour trouver les produits dans le panier du client
                var responseCart = await _httpClient.GetAsync($"http://localhost:5000/api/product/GetCartProducts/{clientId}");

                if (responseCart.IsSuccessStatusCode == false)
                    return StatusCode(StatusCodes.Status500InternalServerError, "Erreur lors de l'appel de ProductApi.");

                List<ProductAndQuantity>? lstCartProducts = await responseCart.Content.ReadFromJsonAsync<List<ProductAndQuantity>>();

                if (lstCartProducts.Count == 0)
                    return BadRequest("Impossible de créer une commande vide!");

                //Crée la facture
                const double TPS = 0.05;
                const double TVQ = 0.09975;
                const double TAXES = 1.014975;

                double subTotal = 0;
                List<ReceiptItem> lstReceiptItems = new List<ReceiptItem>();
                foreach (var item in lstCartProducts)
                {
                    subTotal += item.Product.Price;
                    ReceiptItem receiptItem = new ReceiptItem()
                    { 
                        ProductId = item.Product.ProductId,
                        Quantity = item.Quantity,
                        ReceiptId = 0 //Id de la commande ajusté après sa création
                    };
                    lstReceiptItems.Add(receiptItem);
                }

                Receipt receipt = new Receipt()
                {
                    PurchaseDate = DateTime.Now,
                    SubTotal = subTotal,
                    TPS = subTotal * TPS,
                    TVQ = subTotal * TVQ,
                    TotalCost = subTotal * TAXES,
                    ClientId = clientId
                };

                await _dbContext.Receipt.AddAsync(receipt);
                await _dbContext.SaveChangesAsync();

                //Modifie l'id de la commande pour chaque item
                lstReceiptItems.ForEach(c => c.ReceiptId = receipt.Id);

                await _dbContext.AddRangeAsync(lstReceiptItems);
                await _dbContext.SaveChangesAsync();

                //Supprime les éléments du panier du client
                var response = await _httpClient.DeleteAsync($"http://localhost:5000/api/product/ClearCartProducts/{clientId}");

                if(!response.IsSuccessStatusCode)
                    return StatusCode(StatusCodes.Status500InternalServerError, "Erreur lors du ménage du panier");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            return Ok(); 
        }

        [Route("Payment")]
        [HttpPost]
        public async Task<ActionResult> Payment()
        {
            try
            {
                // Set your secret key. Remember to switch to your live secret key in production.
                // See your keys here: https://dashboard.stripe.com/apikeys
                

                //Payment intent
                var options = new PaymentIntentCreateOptions
                {
                    Amount = 1000,
                    Currency = "usd",
                    PaymentMethodTypes = new List<string> { "card" },
                
                };

                //var requestOptions = new RequestOptions();
                //requestOptions.StripeAccount = "{{CONNECTED_ACCOUNT_ID}}";

                //var service = new PaymentIntentService();
                //await service.CreateAsync(options, requestOptions);

                return Ok();

            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }



        private async Task<bool> ClientExists(int clientId)
        {
            HttpClient httpClient = new HttpClient();

            HttpResponseMessage response = await httpClient.GetAsync($"http://localhost:5000/api/client/GetClient/{clientId}");

            return response.IsSuccessStatusCode;
        }
    }
}
