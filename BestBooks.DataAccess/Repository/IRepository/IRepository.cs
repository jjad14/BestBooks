using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace BestBooks.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        // get record by id
        T Get(int id);

        // get all records with specifications
        IEnumerable<T> GetAll(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = null
            );

        // get a record with specifications
        T GetFirstOrDefault(
            Expression<Func<T, bool>> filter = null,
            string includeProperties = null
            );

        // add entity to db
        void Add(T entity);
        // remove entity from db by id
        void Remove(int id);
        // remove entity from db
        void Remove(T entity);
        // remove a range of entities from db
        void RemoveRange(IEnumerable<T> entity);

    }
}
