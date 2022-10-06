using Microsoft.EntityFrameworkCore;
using ShoppingCart.DataAccess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingCart.DataAccess.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public void DeleteRange(IEnumerable<T> entity)
        {
            _dbSet.RemoveRange(entity);
        }

        public async Task <IEnumerable<T>> GetAllAsync(System.Linq.Expressions.Expression<Func<T, bool>>? predicate = null
            , string? includePropertirs = null)
        {
            IQueryable<T> query = _dbSet;
            if (predicate != null)
                query = query.Where(predicate);
            if (includePropertirs != null)
                foreach (var item in includePropertirs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    query = query.Include(item);
            var result = await query.ToListAsync();
            return result;
        }

        public async Task<T> GetTAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate
            , string? includePropertirs = null)
        {
            IQueryable<T> query = _dbSet;
            query = query.Where(predicate);
            if (includePropertirs != null)
                foreach (var item in includePropertirs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    query = query.Include(item);
            return await query.FirstOrDefaultAsync();
        }
    }
}
