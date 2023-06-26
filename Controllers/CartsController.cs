using CartService.Context;
using CartService.MessageBroker;
using CartService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CartService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly ServiceContext _context;
        private readonly IMessageBrokerClient _rabbitMQClient;

        public CartsController(ServiceContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _rabbitMQClient = serviceProvider.GetRequiredService<IMessageBrokerClient>();
        }

        // GET: api/Carts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cart>>> GetCart()
        {
            if (_context.Cart == null)
            {
                return NotFound();
            }
            return await _context.Cart.AsNoTracking().ToListAsync();
        }

        // GET: api/Carts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cart>> GetCart(int id)
        {
            if (_context.Cart == null)
            {
                return NotFound();
            }
            var cart = await _context.Cart.FindAsync(id);

            if (cart == null)
            {
                return NotFound();
            }

            return cart;
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
        //cart checkout method
        [HttpPost]
        [Route("checkout")]
        public async Task<ActionResult<bool>> CheckOutCart(int userId)
        {
            //process all orders in cart and make payments for them 
            if (_context.Cart == null)
                return NoContent();

            try
            {
                //throws InvalidOperationException when it cannot find any cart items
                IEnumerable<Cart> userCartItems = _context.Cart.Where(cart => cart.UserId == userId);

                if (userCartItems.Count() == 0)
                    throw new InvalidOperationException();

                //for each cart item send a message to queue to initiate payment for them
                foreach (var cartItem in userCartItems)
                {
                    Message<Cart> message = new(Constants.EventTypes.PAYMENT_INITIATED, cartItem);

                    //send message to queue
                    _rabbitMQClient.SendMessage(message, Constants.EventTypes.PAYMENT_INITIATED);

                }

                return Ok(true);

            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        // POST: api/Carts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Cart>> PostCart(Cart cart)
        {
            if (_context.Cart == null)
            {
                return Problem("Entity set 'ServiceContext.Cart'  is null.");
            }

            try
            {
                _context.Cart.Add(cart);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetCart", new { id = cart.Id }, cart);

            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        // DELETE: api/Carts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCart(int id)
        {
            if (_context.Cart == null)
            {
                return NotFound();
            }
            var cart = await _context.Cart.FindAsync(id);
            if (cart == null)
            {
                return NotFound();
            }

            try
            {
                _context.Cart.Remove(cart);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }

        private bool CartExists(int id)
        {
            return (_context.Cart?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
