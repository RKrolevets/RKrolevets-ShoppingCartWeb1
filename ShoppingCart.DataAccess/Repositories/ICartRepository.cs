using ShoppingCart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingCart.DataAccess.Repositories
{
    public interface ICartRepository : IRepository<Cart>
    {
        Task UpdateAsync (Cart cart);
        Task IncrementCartItemAsync (Cart cart, int amount);
        Task DecrementCartItemAsync (Cart cart, int amount);
    }
}
