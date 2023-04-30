using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductApi.Classes;
using ProductApi.Models;
using Stripe;
using Swashbuckle.AspNetCore.Annotations;
using System;

namespace ProductApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Produces("app/json")]
    public class ProductController : ControllerBase
    {
        private ProductDBContext _dbContext;
        public ProductController(ProductDBContext context) 
        {
            _dbContext = context;
        }

        /// <summary>
        /// Obtenir un produit
        /// </summary>
        /// <param name="id">ID du produit.</param>
        /// <returns>Un produit</returns>
        /// <response code="200">Produit trouvé</response>
        /// <response code="404">Le produit n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("GetProduct/{id}")]
        [HttpGet]
        public async Task<ActionResult<Models.Product>> GetProduct(int id)
        {
            try
            {
                var product = await _dbContext.Product.FindAsync(id);

                if (product == null)
                    return NotFound();

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Obtenir plusieurs produits
        /// </summary>
        /// <param name="id">ID du produit.</param>
        /// <returns>Un produit</returns>
        /// <response code="200">Les produits ont été trouvés</response>
        /// <response code="404">Les produits n'existent pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("GetProductsById")]
        [HttpPost]
        public async Task<ActionResult<List<Models.Product>>> GetProductsById([FromBody] int[] id)
        {
            try
            {
                List<Models.Product> lstProducts = await _dbContext.Product.Where(c => id.Contains(c.ProductId)).ToListAsync();

                if (lstProducts.Count == 0)
                    return NotFound();

                return Ok(lstProducts);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Obtenir les produits du panier d'un client
        /// </summary>
        /// <param name="id">ID du client.</param>
        /// <returns>Une liste de produits ainsi que leur quantité</returns>
        /// <response code="200">Les produits ont été trouvés</response>
        /// <response code="204">Aucun produit dans le panier</response>
        /// <response code="404">Le client n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("GetCartProducts/{id}")]
        [HttpGet]
        public async Task<ActionResult<List<ProductAndQuantity>>> GetCartProducts(int id)
        {
            try
            {
                if (ClientExists(id).Result == false)
                    return NotFound("Le client n'existe pas");

                var products = await _dbContext.CartProduct
                    .Include(x => x.Product)
                    .Where(c => c.ClientId == id)
                    .ToListAsync();

                if(products.Count == 0)
                    return NoContent();

                List<ProductAndQuantity> lstCartProducts = new List<ProductAndQuantity>();

                foreach (var product in products)
                { 
                    ProductAndQuantity productAndQuantity = new ProductAndQuantity(product.Product, product.Quantity);
                    lstCartProducts.Add(productAndQuantity);
                }    

                return Ok(lstCartProducts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Gèrer un produit dans le panier d'un client
        /// </summary>
        /// <param name="clientId">ID du client.</param>
        /// <param name="productId">ID du produit.</param>
        /// <param name="quantity">Quantité d'article.</param>
        /// <returns></returns>
        /// <response code="200">Le panier a été modifié</response>
        /// <response code="404">Le client ou le produit n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("ManageCartProduct")]
        [HttpPost]
        public async Task<ActionResult> ManageCartProduct(int clientId, int productId, int quantity)
        {
            try
            {
                if (ClientExists(clientId).Result == false)
                    return NotFound("Le client n'existe pas");

                CartProduct? cartProduct = await _dbContext.CartProduct
                .Where(c => c.ClientId == clientId)
                .Where(c => c.ProductId == productId)
                .FirstOrDefaultAsync();

                
                if (cartProduct == null) //Création du CartProduct
                {
                    cartProduct = new CartProduct()
                    {
                        ClientId = clientId,
                        ProductId = productId,
                        Quantity = quantity
                    };
                    await _dbContext.CartProduct.AddAsync(cartProduct);
                }
                else
                { 
                    if(quantity == 0) //Suppression
                        _dbContext.CartProduct.Remove(cartProduct);
                    else //Modifier la quantité
                        cartProduct.Quantity = quantity;
                }

                await _dbContext.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Obtenir les produits d'un vendeur
        /// </summary>
        /// <param name="id">ID du vendeur.</param>
        /// <returns>Les produits d'un vendeur</returns>
        /// <response code="200">Les produits ont été trouvés</response>
        /// <response code="404">Le vendeur n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("GetSellerProducts/{id}")]
        [HttpGet]
        public async Task<ActionResult<List<Models.Product>>> GetSellerProducts([SwaggerParameter("ID du vendeur")] int id)
        {
            try
            {
                //Appeler l'API vendeur pour déterminer si le vendeur existe vraiment?
                //if (seller == null)
                //    return NotFound();

                List<Models.Product> products = await _dbContext.Product.Where(c => c.SellerId == id).ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Ajouter un nouveau produit
        /// </summary>
        /// <param name="product">Produit</param>
        /// <returns>Le produit</returns>
        /// <response code="201">Le produit a été créé</response>
        /// <response code="404">Le vendeur n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("AddProduct")]
        [HttpPost]
        public async Task<ActionResult> AddProduct(Models.Product product)
        {
            try 
            { 
                //Vérifier si le vendeur existe

                await _dbContext.Product.AddAsync(product);
                await _dbContext.SaveChangesAsync();

                StripeConfiguration.ApiKey = "sk_test_51N2RC7IqRYm380CMGVdsBlJd8b10SjrX7EHP9OrzEP51LNdEHHBa493d0Z8QR1GOqYPtZfJGZHNblulpxp2dWgBb000D1B2HAV";

                //Créer le produit dans Stripe
                var optionsProduct = new ProductCreateOptions
                {
                    Name = product.ProductTitle,
                    Description = product.Usage
                };
                var serviceProduct = new ProductService();
                Stripe.Product stripeProduct = await serviceProduct.CreateAsync(optionsProduct);

                //Y ajoute un prix
                var optionsPrice = new PriceCreateOptions
                {
                    UnitAmount = (long)(product.Price * 100),
                    Currency = "cad",
                    Product = stripeProduct.Id
                };
                var servicePrice = new PriceService();
                Price price = await servicePrice.CreateAsync(optionsPrice);

                return StatusCode(201, product);
            }
            catch(Exception ex) 
            { 
                return StatusCode(500, ex.Message);
            }

        }

        /// <summary>
        /// Modifier un produit
        /// </summary>
        /// <param name="product">Produit</param>
        /// <returns></returns>
        /// <response code="200">Le produit a été modifié</response>
        /// <response code="404">Le produit n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("UpdateProduct")]
        [HttpPut]
        public async Task<ActionResult> UpdateProduct(Models.Product product)
        {
            try 
            {
                Models.Product? productDb = await _dbContext.Product.FindAsync(product.ProductId);

                if (productDb == null)
                {
                    return NotFound();
                }
                //Modifier les attributs du produit
                productDb.Colour = product.Colour;
                productDb.Gender = product.Gender;
                productDb.Category = product.Category;
                productDb.SubCategory = product.SubCategory;
                productDb.Image = product.Image;
                productDb.ImageURL = product.ImageURL;
                productDb.Usage = product.Usage;
                productDb.Price = product.Price;
                productDb.ProductTitle = product.ProductTitle;

                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch(Exception ex) 
            { 
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Supprimer un produit
        /// </summary>
        /// <param name="id">ID du Produit</param>
        /// <returns></returns>
        /// <response code="200">Le produit a été supprimé</response>
        /// <response code="404">Le produit n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("DeleteProduct/{id}")]
        [HttpDelete]
        public async Task<ActionResult> DeleteProduct([SwaggerParameter("ID du produit")] int id)
        {
            try
            {
                Models.Product? product = await _dbContext.Product.FindAsync(id);

                if (product == null)
                    return NotFound();

                _dbContext.Product.Remove(product);

                await _dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Supprimer les produits au painer d'un client
        /// </summary>
        /// <param name="id">ID du client</param>
        /// <returns></returns>
        /// <response code="200">Les produits ont été supprimés du panier</response>
        /// <response code="404">Le client n'existe pas</response>
        /// <response code="500">Erreur serveur interne</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("ClearCartProducts/{id}")]
        [HttpDelete]
        public async Task<ActionResult> ClearCartProducts([SwaggerParameter("ID du client")] int id)
        {
            try
            {
                List<CartProduct> cartProducts = await _dbContext.CartProduct.Where(c => c.ClientId == id).ToListAsync();

                if (ClientExists(id).Result == false)
                    return NotFound();

                if (cartProducts.Count != 0)
                {
                    _dbContext.CartProduct.RemoveRange(cartProducts);
                    await _dbContext.SaveChangesAsync();
                }

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

            //TODO changer pour la passerelle quand fonctionnelle
            HttpResponseMessage response = await httpClient.GetAsync($"http://localhost:5001/Client/GetClient/{clientId}");

            return response.IsSuccessStatusCode;
        }
    }
}
