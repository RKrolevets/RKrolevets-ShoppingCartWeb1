using Microsoft.EntityFrameworkCore;
using ShoppingCart.DataAccess.Data;
using ShoppingCart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingCart.DataAccess.Repositories
{
    public class CartRepository : Repository<Cart>, ICartRepository
    {
        private ApplicationDbContext _context;

        public CartRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task UpdateAsync(Cart cart)
        {
            var cartDb = await _context.Carts.FirstOrDefaultAsync(x => x.Id == cart.Id);
            if (cartDb != null)
            {
                cartDb.Product = cart.Product;
                cartDb.ApplicationUserId = cart.ApplicationUserId;
                cartDb.ApplicationUser = cart.ApplicationUser;
                cartDb.Count = cart.Count;
            }
        }

        public async Task DecrementCartItemAsync(Cart cart, int amount)
        {
            var cartDb = await _context.Carts.FirstOrDefaultAsync(c => c.Id == cart.Id);
            if (cartDb != null)
                cartDb.Count -= amount;
        }

        public async Task IncrementCartItemAsync(Cart cart, int count)
        {
            var cartDb = await _context.Carts.FirstOrDefaultAsync(c => c.Id == cart.Id);
            if (cartDb != null)
                cartDb.Count += count;
        }
    }
}
