using BestBooks.DataAccess.Data;
using BestBooks.DataAccess.Repository.IRepository;
using BestBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BestBooks.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _db;
        public OrderHeaderRepository(ApplicationDbContext db)
        : base(db)
        {
            _db = db;
        }

        // update a single category
        public void Update(OrderHeader order)
        {
            _db.Update(order);
        }

    }
}
