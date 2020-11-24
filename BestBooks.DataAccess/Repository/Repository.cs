using BestBooks.DataAccess.Data;
using BestBooks.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace BestBooks.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<T> dbSet;

        public Repository(ApplicationDbContext db)
        {
            _db = db;
            this.dbSet = _db.Set<T>();
        }

        // add entity to dbset
        public void Add(T entity)
        {
            dbSet.Add(entity);
        }

        // find record in dbset
        public T Get(int id)
        {
            return dbSet.Find(id);
        }

        // get all records from dbset with specifications
        public IEnumerable<T> GetAll(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string includeProperties = null)
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
                return orderBy(query).ToList();
            }

            // return list of records
            return query.ToList();
        }

        // get a record from dbset with specifications
        public T GetFirstOrDefault(Expression<Func<T, bool>> filter = null, string includeProperties = null)
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
            return query.FirstOrDefault();
        }

        // find entity by id then call remove to remove the entity from the dbset
        public void Remove(int id)
        {
            T entity = dbSet.Find(id);
            Remove(entity);
        }

        // remove the entity from the dbset
        public void Remove(T entity)
        {
            dbSet.Remove(entity);
        }

        // remove a range of entities from the dbset
        public void RemoveRange(IEnumerable<T> entity)
        {
            dbSet.RemoveRange(entity);
        }

    }
}
