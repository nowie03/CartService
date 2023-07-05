using CartService.Context;
using CartService.MessageBroker;
using CartService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CartService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly ServiceContext _context;
        private readonly IMessageSender _rabbitMQClient;

        public CartsController(ServiceContext context, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _rabbitMQClient = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IMessageSender>();
        }

        // GET: api/Carts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cart>>> GetCart()
        {
            if (_context.Carts == null)
            {
                return NotFound();
            }
            return await _context.Carts.ToListAsync();
        }

      

        // GET: api/Carts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cart>> GetCart(int id)
        {
            if (_context.Carts == null)
            {
                return NotFound();
            }
            var cart = await _context.Carts.Where(cart=>cart.UserId==id).AsNoTracking().FirstAsync();

            if (cart == null)
            {
                return NotFound();
            }

            return cart;
        }

        [HttpGet]
        [Route("cartItems")]
        public async Task<ActionResult<IEnumerable<CartItem>>> GetCartItems(int cartId)
        {
            Cart? cart = await _context.Carts.FindAsync(cartId);

            if (cart == null)
                return NotFound();

            try
            {
                IEnumerable<CartItem> cartItems = _context.CartItems.Where(item => item.CartId == cartId);

                if (!cartItems.Any())
                    return NoContent();

                return Ok(cartItems);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest();
            }


        }
        // PUT: api/Carts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCart(int id, Cart cart)
        {
            if (id != cart.Id)
            {
                return BadRequest();
            }

            _context.Entry(cart).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CartExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Carts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Cart>> PostCart(Cart cart)
        {
            if (_context.Carts == null)
            {
                return Problem("Entity set 'ServiceContext.Carts'  is null.");
            }
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCart", new { id = cart.Id }, cart);
        }

        [HttpPost]
        [Route("checkout")]
        public async Task<ActionResult<bool>> CheckOutCart(int cartId)
        {
            //process all orders in cart and make payments for them 
            if (_context.Carts == null)
                return NoContent();

            var transcation = await _context.Database.BeginTransactionAsync();
            try
            {
                Cart? userCart = await _context.Carts.FindAsync(cartId);

                if (userCart == null)
                    return NotFound();

                //throws InvalidOperationException when it cannot find any cart items
                IEnumerable<CartItem> userCartItems = _context.CartItems.Where(item => item.CartId == cartId);

                if (!userCartItems.Any())
                    throw new InvalidOperationException();

                //for each cart item send a message to queue to initiate payment for them
                foreach (var cartItem in userCartItems)
                {
                    //send message to Outbox
                    string serializedCartItem = JsonConvert.SerializeObject(cartItem);
                    ulong nextSequenceNumber = _rabbitMQClient.GetNextSequenceNumber();

                    Message message = new(Constants.EventTypes.PAYMENT_INITIATED, serializedCartItem,
                        nextSequenceNumber, Constants.EventStates.EVENT_ACK_PENDING);

                    await _context.Outbox.AddAsync(message);
                    await _context.SaveChangesAsync();

                }

                await transcation.CommitAsync();

                return Ok(true);

            }
            catch (InvalidOperationException ex)
            {
                await transcation.RollbackAsync();
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                await transcation.RollbackAsync();
                return Problem(ex.Message);
            }
        }

        [HttpPost]
        [Route("cartItems")]
        public async Task<ActionResult<CartItem>> PostCartItem(CartItem cartItem)
        {
            Cart? cart = await _context.Carts.FindAsync(cartItem.CartId);

            if (cart == null)
                return BadRequest();

            try
            {
                await _context.CartItems.AddAsync(cartItem);
                await _context.SaveChangesAsync();

                return Ok(cartItem);

            }
            catch (Exception ex)
            {
                return Problem("unable to add to cart at the moment");
            }
        }

        // DELETE: api/Carts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCart(int id)
        {
            if (_context.Carts == null)
            {
                return NotFound();
            }
            var cart = await _context.Carts.FindAsync(id);
            if (cart == null)
            {
                return NotFound();
            }

            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpDelete]
        [Route("cartItems")]
        public async Task<ActionResult> DeleteCartItem(int cartItemId)
        {
            if (_context.CartItems == null)
            {
                return NotFound();
            }
            CartItem? cartItem = await _context.CartItems.FindAsync(cartItemId);

            if (cartItem == null)
            {
                return NotFound();
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return NoContent();

        }

        private bool CartExists(int id)
        {
            return (_context.Carts?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
