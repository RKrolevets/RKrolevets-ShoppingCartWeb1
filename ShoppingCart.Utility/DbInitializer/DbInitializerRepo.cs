﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShoppingCart.DataAccess.Data;
using ShoppingCart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingCart.Utility.DbInitializer
{
    public class DbInitializerRepo :IDbInitializer
    {
		private readonly UserManager<IdentityUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly ApplicationDbContext _context;

		public DbInitializerRepo (UserManager<IdentityUser> userManager,
			RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
		{
			_userManager = userManager;
			_roleManager = roleManager;
			_context = context;
		}

        public void Initialize()
        {
			try
			{
				if(_context.Database.GetPendingMigrations().Count() > 0)
					_context.Database.Migrate();
			}
			catch (Exception)
			{

				throw;
			}
			if (!_roleManager.RoleExistsAsync(WebSiteRole.Role_Admin).GetAwaiter().GetResult())
			{
				_roleManager.CreateAsync(new IdentityRole(WebSiteRole.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(WebSiteRole.Role_User)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(WebSiteRole.Role_Employee)).GetAwaiter().GetResult();

                _userManager.CreateAsync(new ApplicationUser
				{
					UserName = "admin@gmail.com",
					Email = "admin@gmail.com",
					Name = "Admin",
					PhoneNumber = "1234567898",
					Address = "Khreschatik 10",
					City = "Kyiv",
					State = "Ukraine",
					PinCode = "333011"
				}, "Admin@123").GetAwaiter().GetResult();
				ApplicationUser user = _context.ApplicationUsers.First(x => x.Email == "admin@gmail.com");
				_userManager.AddToRoleAsync(user, WebSiteRole.Role_Admin).GetAwaiter().GetResult();
            }
			return;
        }
    }
}
