using BestBooks.DataAccess.Data;
using BestBooks.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BestBooks.DataAccess.Repository
{
    public class RepositoryAsync<T> : IRepositoryAsync<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<T> dbSet;

        public RepositoryAsync(ApplicationDbContext db)
        {
            _db = db;
            this.dbSet = _db.Set<T>();
        }

        // add entity to dbset
        public async Task AddAsync(T entity)
        {
            await dbSet.AddAsync(entity);
        }

        // find record in dbset
        public async Task<T> GetAsync(int id)
        {
            return await dbSet.FindAsync(id);
        }

        // get all records from dbset with specifications
        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string includeProperties = null)
        {
            IQueryable<T> query = dbSet;

            // if there are filters
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // if there are includes
            if (includeProperties != null)
            {
                foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }

            // if orderby is specified
            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync();
            }

            // return list of records
            return await query.ToListAsync();
        }

        // get a record from dbset with specifications
        public async Task<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter = null, string includeProperties = null)
        {
            IQueryable<T> query = dbSet;

            // if there are filters
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // if there are includes
            if (includeProperties != null)
            {
                // add each include property individually
                foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }

            // return a single record
            return await query.FirstOrDefaultAsync();
        }

        // find entity by id then call remove to remove the entity from the dbset
        public async Task RemoveAsync(int id)
        {
            T entity = await dbSet.FindAsync(id);
            await RemoveAsync(entity);
        }

        // remove the entity from the dbset
        public async Task RemoveAsync(T entity)
        {
            dbSet.Remove(entity);
        }

        // remove a range of entities from the dbset
        public async Task RemoveRangeAsync(IEnumerable<T> entity)
        {
            dbSet.RemoveRange(entity);
        }

    }
}
