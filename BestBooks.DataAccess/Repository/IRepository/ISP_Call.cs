using Dapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace BestBooks.DataAccess.Repository.IRepository
{
    // stored procedure call
    public interface ISP_Call : IDisposable
    {
        // If we have to return a single value like count or any first value it will be single of type T
        // The parameters will be Procedure names, And then we will be using dapper to pass all the parameters 
        T Single<T>(string procedureName, DynamicParameters param = null);

        // For operations where you just want to execute something to the database and you do not want to retrieve anything
        void Execute(string procedureName, DynamicParameters param = null);

        // Retrieve one complete row or one complete record
        T OneRecord<T>(string procedureName, DynamicParameters param = null);

        // Retrieve all rows
        IEnumerable<T> List<T>(string procedureName, DynamicParameters param = null);

        // Returns two tables, we would use tuples
        Tuple<IEnumerable<T1>, IEnumerable<T2>> List<T1, T2>(string procedureName, DynamicParameters param = null);
    }
}

// Now the difference between single and oneRecord is in single we will be using execute scalar that returns
// an integer value or a boolean value. 
// An example could be first column of first row in the reserve set 
// but inside one record, we will retrieve the complete row