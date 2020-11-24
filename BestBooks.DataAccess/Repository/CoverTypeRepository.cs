using BestBooks.DataAccess.Data;
using BestBooks.DataAccess.Repository.IRepository;
using BestBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BestBooks.DataAccess.Repository
{
    public class CoverTypeRepository : Repository<CoverType>, ICoverTypeRepository
    {
        private readonly ApplicationDbContext _db;
        public CoverTypeRepository(ApplicationDbContext db)
        : base(db)
        {
            _db = db;
        }

        // update a single cover type
        public void Update(CoverType coverType)
        {
            // find category by id
            var objFromDb = _db.CoverTypes.FirstOrDefault(s => s.Id == coverType.Id);

            // if result is not null
            if (objFromDb != null)
            {
                // map the new cover type to existing cover type
                objFromDb.Name = coverType.Name;
            }
        }




    }
}
