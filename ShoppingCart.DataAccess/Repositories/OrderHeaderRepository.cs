using Microsoft.EntityFrameworkCore;
using ShoppingCart.DataAccess.Data;
using ShoppingCart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingCart.DataAccess.Repositories
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private ApplicationDbContext _context;

        public OrderHeaderRepository(ApplicationDbContext context): base(context)
        {
            _context = context;
        }

        public async Task PaymentStatusAsync(int Id, string SessionId, string PaymentIntentId)
        {
            var orderHeader = await _context.OrderHeaders.FirstOrDefaultAsync(o => o.Id == Id);
            orderHeader.DateOfPayment = DateTime.Now;
            orderHeader.PaymentIntentId = PaymentIntentId;
            orderHeader.SessionId = SessionId;
        }

        public void Update(OrderHeader orderHeader)
        {
            _context.OrderHeaders.Update(orderHeader);
        }

        public async Task UpdateStatusAsync(int Id, string orderStatus, string? paymentStatus = null)
        {
            var order = await _context.OrderHeaders.FirstOrDefaultAsync(o => o.Id == Id);
            if (order != null)
            {
                order.OrderStatus = orderStatus;
                if (paymentStatus != null)
                    order.PaymentStatus = paymentStatus;
            } 
        }
    }
}
