using System;
using System.Collections.Generic;
using System.Text;

namespace BestBooks.DataAccess.Repository.IRepository
{
    // UoW class coordinates the work of multiple repositories by creating a single database context shared by all of them
    // Controller -> UoW (repos and context) -> EF Core and Database
    public interface IUnitOfWork : IDisposable
    {
        ICategoryRepository Category { get;  }
        ICoverTypeRepository CoverType { get;  }
        IProductRepository Product { get;  }
        ICompanyRepository Company { get;  }
        IApplicationUserRepository ApplicationUser { get;  }
        ISP_Call SP_Call { get; }

        void Save();
    }
}
