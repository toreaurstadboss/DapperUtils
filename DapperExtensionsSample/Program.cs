using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace DapperExtensionsSample
{
    static class Program
    {
        static void Main(string[] args)
        {
            var config = SetupConfigurationFile();
            using var connection = new SqlConnection(config.GetConnectionString("Northwind"));
            //var products = connection.Query("select * from products");
            //foreach (var product in products)
            //{
            //    //var productSerialized = JsonConvert.SerializeObject(product);
            //    //Console.WriteLine($"{product.ProductID}: {product.ProductName}");
            //}

            //string sql = "select * from products where ProductId = @ProductID";
            //var cheeses = connection.ParameterizedQuery(sql, new Dictionary<string, object> { { "@ProductID", 75 } });

            //foreach (var cheese in cheeses)
            //{
            //    Console.WriteLine($"{cheese.ProductName}");
            //}

            //var sql = $"select * from products where ProductName like @ProdName";
            //var herringsInNorthwindDb = connection.ParameterizedQuery(sql, new Dictionary<string, object> { { "@ProdName", Like("sild") } });

            //foreach (var herring in herringsInNorthwindDb)
            //{
            //    Console.WriteLine($"{herring.ProductName}");
            //}

            //var sql = $"select * from products";
            //var productPage = connection.GetPage<Product>(m => m.ProductID, sql, 0, 5, sortAscending: true);

            var count = connection.GetAggregate<Product>(p => p.UnitPrice, AggregateFunction.Count,
                new Expression<Func<Product, object>>[] { p => p.CategoryID }, tableName: "products");


            Console.WriteLine("Hit the any key to continue..");
            Console.Read();
        }

        /// <summary>
        /// Fetches page with page number <paramref name="pageNumber"/> with a page size set to <paramref name="pageSize"/>.
        /// Last page may contains 0 - <paramref name="pageSize"/> items. The page number <paramref name="pageNumber"/> is 0-based,
        /// i.e starts with 0. The method relies on the 'FETCH NEXT' and 'OFFSET' methods
        /// of the database engine provider.
        /// Note: When sorting with <paramref name="sortAscending"/> set to false, you will at the first page get the last items.
        /// The parameter <paramref name="orderByMember"/> specified which property member to sort the collection by. Use a lambda.
        /// </summary>
        /// <typeparam name="T">The type of ienumerable to return and strong type to return upon</typeparam>
        /// <param name="connection">IDbConnection instance (e.g. SqlConnection)</param>
        /// <param name="orderByMember">The property to order with</param>
        /// <param name="sql">The select clause sql to use as basis for the complete paging</param>
        /// <param name="pageNumber">The page index to fetch. 0-based (Starts with 0)</param>
        /// <param name="pageSize">The page size. Must be a positive number</param>
        /// <param name="sortAscending">Which direction to sort. True means ascending, false means descending</param>
        /// <returns></returns>
        public static IEnumerable<T> GetPage<T>(this IDbConnection connection, Expression<Func<T, object>> orderByMember,
            string sql, int pageNumber, int pageSize, bool sortAscending = true)
        {
            if (string.IsNullOrEmpty(sql) || pageNumber < 0 || pageSize <= 0)
            {
                return null;
            }
            int skip = Math.Max(0, (pageNumber)) * pageSize;
            if (!sql.Contains("order by", StringComparison.CurrentCultureIgnoreCase))
            {
                string orderByMemberName = GetMemberName(orderByMember);
                sql += $" ORDER BY [{orderByMemberName}] {(sortAscending ? "ASC": " DESC")} OFFSET @Skip ROWS FETCH NEXT @Next ROWS ONLY";
                return connection.ParameterizedQuery<T>(sql, new Dictionary<string, object> { { "@Skip", skip }, { "@Next", pageSize } });
            }
            else
            {
                sql += $" OFFSET @Skip ROWS FETCH NEXT @Next ROWS ONLY";
                return connection.ParameterizedQuery<T>(sql, new Dictionary<string, object> { { "@Skip", skip }, { "@Next", pageSize } });
            }

        }

        public enum AggregateFunction
        {
            Count 
        }

        public static string GetAggregateFunctionExpression(AggregateFunction function, string aggregateColumnName,
            string groupingColumnsJoined)
        {
            string aggregateColumnSuffix = !string.IsNullOrEmpty(groupingColumnsJoined) ? $",{groupingColumnsJoined}" : string.Empty;
            string aggregateFunctionExpression = string.Empty;
            switch (function)
            {
                case AggregateFunction.Count:
                    aggregateFunctionExpression = $"count({aggregateColumnName ?? "*"}) as Value";
                    break;
                default:
                    break;
            }
            return $"{aggregateFunctionExpression}{aggregateColumnSuffix}";
        }

        public static dynamic GetAggregate<T>(this IDbConnection connection, Expression<Func<T, object>> aggregateColumn, 
            AggregateFunction calculation, Expression<Func<T, object>>[] groupingColumns = null,
            string tableName = null)
        {
            if (tableName == null)
            {
                tableName = typeof(T).Name;
            }
            string aggregateColumnName = GetMemberName<T>(aggregateColumn);
            string groupingColumnsJoined = string.Empty;
            if (groupingColumns != null && groupingColumns.Any())
            {
                groupingColumnsJoined = string.Join(",", groupingColumns.Select(c => GetMemberName<T>(c)));
            }
            string aggregateFunctionExpression = GetAggregateFunctionExpression(calculation, aggregateColumnName, groupingColumnsJoined);

          
            var sql = $"select {aggregateFunctionExpression} from {tableName}";           
            sql += $"{Environment.NewLine}group by {groupingColumnsJoined}";

            var results = connection.Query(sql);
            return results;
        }

        private static string GetMemberName<T>(Expression<Func<T, object>> expression)
        {
            switch (expression.Body)
            {
                case MemberExpression m:
                    return m.Member.Name;
                case UnaryExpression u when u.Operand is MemberExpression m:
                    return m.Member.Name;
                default:
                    throw new NotImplementedException(expression.GetType().ToString());
            }
        }

        public static string Like(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return null;
            }
            Func<string, string> encodeForLike = searchTerm => searchTerm.Replace("[", "[[]").Replace("%", "[%]");
            return $"%{encodeForLike(searchTerm)}%";
        }

        public static dynamic ParameterizedQuery(this IDbConnection connection, string sql,
           Dictionary<string, object> parametersDictionary)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return null;
            }
            string missingParameters = string.Empty;
            foreach (var item in parametersDictionary)
            {
                if (!sql.Contains(item.Key))
                {
                    missingParameters += $"Missing parameter: {item.Key}";
                }
            }
            if (!string.IsNullOrEmpty(missingParameters))
            {
                throw new ArgumentException($"Parameterized query failed. {missingParameters}");
            }
            var parameters = new DynamicParameters(parametersDictionary);
            return connection.Query(sql, parameters);
        }

        public static IEnumerable<T> ParameterizedQuery<T>(this IDbConnection connection, string sql,
            Dictionary<string, object> parametersDictionary)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return null;
            }
            string missingParameters = string.Empty;
            foreach (var item in parametersDictionary)
            {
                if (!sql.Contains(item.Key))
                {
                    missingParameters += $"Missing parameter: {item.Key}";
                }
            }
            if (!string.IsNullOrEmpty(missingParameters))
            {
                throw new ArgumentException($"Parameterized query failed. {missingParameters}");
            }
            var parameters = new DynamicParameters(parametersDictionary);
            return connection.Query<T>(sql, parameters);
        }

        private static IConfigurationRoot SetupConfigurationFile()
        {
            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();
            return configuration;
        }

    }
}
