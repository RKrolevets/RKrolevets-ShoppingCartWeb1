﻿using Microsoft.EntityFrameworkCore;
using ShoppingCart.DataAccess.Data;
using ShoppingCart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingCart.DataAccess.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context) :base(context)
        {
            _context = context;
        }
        public async Task UpdateAsync (Product product)
        {
            var productDb = await _context.Products.FirstOrDefaultAsync(x =>x.Id==product.Id);
            if (productDb != null)
            {
                productDb.Name = product.Name;
                productDb.Description = product.Description;
                productDb.Price = product.Price;
                if (product.ImageUrl != null)
                    productDb.ImageUrl = product.ImageUrl;
            }
        }
    }
}
