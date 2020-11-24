using BestBooks.DataAccess.Data;
using BestBooks.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Text;

namespace BestBooks.DataAccess.Repository
{
    // UoW class coordinates the work of multiple repositories by creating a single database context shared by all of them
    // Controller -> UoW (repos and context) -> EF Core and Database
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            Category = new CategoryRepository(_db);
            SP_Call = new SP_Call(_db);
        }

        public ICategoryRepository Category { get; private set; }
        public ISP_Call SP_Call { get; private set; }

        public void Dispose()
        {
            _db.Dispose();
        }

        public void Save()
        {
            _db.SaveChanges();
        }
    }
}
