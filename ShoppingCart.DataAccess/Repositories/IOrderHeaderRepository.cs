using ShoppingCart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingCart.DataAccess.Repositories
{
    public interface IOrderHeaderRepository: IRepository<OrderHeader>
    {
        void Update (OrderHeader orderHeader);
        Task UpdateStatusAsync (int Id, string orderStatus, string? paymentStatus = null);
        Task PaymentStatusAsync(int Id, string SessionId, string PaymentIntentId);
    }
}
