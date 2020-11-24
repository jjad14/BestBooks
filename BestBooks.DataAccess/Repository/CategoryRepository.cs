using BestBooks.DataAccess.Data;
using BestBooks.DataAccess.Repository.IRepository;
using BestBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BestBooks.DataAccess.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly ApplicationDbContext _db;
        public CategoryRepository(ApplicationDbContext db)
        : base(db)
        {
            _db = db;
        }

        // update a single category
        public void Update(Category category)
        {
            // find category by id
            var objFromDb = _db.Categories.FirstOrDefault(s => s.Id == category.Id);

            // if result is not null
            if (objFromDb != null)
            {
                // map the new category to existing category
                objFromDb.Name = category.Name;
            }
        }




    }
}
