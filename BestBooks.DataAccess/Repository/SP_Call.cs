using BestBooks.DataAccess.Data;
using BestBooks.DataAccess.Repository.IRepository;
using Dapper;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BestBooks.DataAccess.Repository
{
    public class SP_Call : ISP_Call
    {
        private readonly ApplicationDbContext _db;
        private static string ConnectionString = "";
        public SP_Call(ApplicationDbContext db)
        {
            _db = db;
            ConnectionString = db.Database.GetDbConnection().ConnectionString;
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        // If you have a stored procedure that should update we can use execute.
        public void Execute(string procedureName, DynamicParameters param = null)
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(ConnectionString))
            {
                mySqlConnection.Open();
                mySqlConnection.Execute(procedureName, param, commandType:System.Data.CommandType.StoredProcedure);
            }
        }

        // If you have a stored procedure that should retrieve all of the categories we can use this.
        public IEnumerable<T> List<T>(string procedureName, DynamicParameters param = null)
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(ConnectionString))
            {
                mySqlConnection.Open();
                return mySqlConnection.Query<T>(procedureName, param, commandType: System.Data.CommandType.StoredProcedure);
            }
        }

        // If you have a stored procedure that retrieves two tables you can use this.
        public Tuple<IEnumerable<T1>, IEnumerable<T2>> List<T1, T2>(string procedureName, DynamicParameters param = null)
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(ConnectionString))
            {
                mySqlConnection.Open();
                
                var result = SqlMapper.QueryMultiple(mySqlConnection, procedureName, param, commandType: System.Data.CommandType.StoredProcedure);
                
                var item1 = result.Read<T1>().ToList();
                var item2 = result.Read<T2>().ToList();
                
                if (item1 != null && item2 != null)
                {
                    return new Tuple<IEnumerable<T1>, IEnumerable<T2>>(item1, item2);
                }
            }

            return new Tuple<IEnumerable<T1>, IEnumerable<T2>>(new List<T1>(), new List<T2>());
        }

        // If you have a stored procedure that retrieves just one complete record or row
        public T OneRecord<T>(string procedureName, DynamicParameters param = null)
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(ConnectionString))
            {
                mySqlConnection.Open();
                var value = mySqlConnection.Query<T>(procedureName, param, commandType: System.Data.CommandType.StoredProcedure);

                return (T)Convert.ChangeType(value.FirstOrDefault(), typeof(T));
            }
        }

        // If you have a stored procedure that retrieves a single value like count or any first value it will be single of type T
        public T Single<T>(string procedureName, DynamicParameters param = null)
        {
            using (MySqlConnection mySqlConnection = new MySqlConnection(ConnectionString))
            {
                mySqlConnection.Open();
                return (T)Convert.ChangeType(mySqlConnection.ExecuteScalar<T>(procedureName, param, commandType: System.Data.CommandType.StoredProcedure), typeof(T));
            }
        }
    }
}
