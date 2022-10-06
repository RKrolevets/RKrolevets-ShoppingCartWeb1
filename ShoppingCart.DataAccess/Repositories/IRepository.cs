using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingCart.DataAccess.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task <IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null,string? includeProperties=null);
        Task<T> GetTAsync(Expression<Func<T, bool>> predicate, string? includeProperties = null);
        Task AddAsync(T entity);
        void Delete(T entity);
        void DeleteRange (IEnumerable<T> entity);
    }
}
