using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BestBooks.DataAccess.Repository.IRepository
{
    public interface IRepositoryAsync<T> where T : class
    {
        // get record by id
        Task<T> GetAsync(int id);

        // get all records with specifications
        Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = null
            );

        // get a record with specifications
        Task<T> GetFirstOrDefaultAsync(
            Expression<Func<T, bool>> filter = null,
            string includeProperties = null
            );

        // add entity to db
        Task AddAsync(T entity);
        // remove entity from db by id
        Task RemoveAsync(int id);
        // remove entity from db
        Task RemoveAsync(T entity);
        // remove a range of entities from db
        Task RemoveRangeAsync(IEnumerable<T> entity);

    }
}
