using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Dapper;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Transactions;

namespace ToreAurstadIT.DapperUtils
{
    public static class DapperUtilsExtensions
    {
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
        /// <param name="sql">The select clause sql to use as basis for the complete paging e.g. 'select * from mytable' and so on.</param>
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
            if (!sql.Contains("order by"))
            {
                string orderByMemberName = GetMemberName(orderByMember);
                sql += $" ORDER BY [{orderByMemberName}] {(sortAscending ? "ASC" : " DESC")} OFFSET @Skip ROWS FETCH NEXT @Next ROWS ONLY";
                return connection.ParameterizedQuery<T>(sql, new Dictionary<string, object> { { "@Skip", skip }, { "@Next", pageSize } });
            }
            else
            {
                sql += $" OFFSET @Skip ROWS FETCH NEXT @Next ROWS ONLY";
                return connection.ParameterizedQuery<T>(sql, new Dictionary<string, object> { { "@Skip", skip }, { "@Next", pageSize } });
            }
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

        #region OldImplementationInnerJoin
        ///// <summary>
        ///// Inner joins the left and right tables by specified left and right key expression lambdas.
        ///// This uses a template builder and a shortcut to join two tables without having to specify any SQL manually
        ///// and gives you the entire inner join result set. It is an implicit requirement that the <paramref name="leftKey"/>
        ///// and <paramref name="rightKey"/> are compatible data types as they are used for the join.
        ///// This method do for now not allow specifying any filtering (where-clause) or logic around the joining besides
        ///// just specifying the two columns to join.
        ///// </summary>
        ///// <typeparam name="TLeftTable"></typeparam>
        ///// <typeparam name="TRightTable"></typeparam>
        ///// <param name="connection"></param>
        ///// <param name="leftKey"></param>
        ///// <param name="rightKey"></param>
        ///// <returns></returns>
        //public static IEnumerable<ExpandoObject> InnerJoin<TLeftTable, TRightTable>(this IDbConnection connection, 
        //    Expression<Func<TLeftTable, object>> leftKey, Expression<Func<TRightTable, object>> rightKey)
        //{
        //    var builder = new SqlBuilder();
        //    string leftTableSelectClause = string.Join(",", GetPublicPropertyNames<TLeftTable>("l"));
        //    string rightTableSelectClause = string.Join(",", GetPublicPropertyNames<TRightTable>("r"));
        //    string leftKeyName = GetMemberName(leftKey);
        //    string rightKeyName = GetMemberName(rightKey); 
        //    string leftTableName = GetDbTableName<TLeftTable>();
        //    string rightTableName = GetDbTableName<TRightTable>(); 
        //    string joinSelectClause = $"select {leftTableSelectClause}, {rightTableSelectClause} from {leftTableName} l /**innerjoin**/";
        //    var selector = builder.AddTemplate(joinSelectClause);
        //    builder.InnerJoin($"{rightTableName} r on l.{leftKeyName} = r.{rightKeyName}");
        //    var joinedResults = connection.Query(selector.RawSql, selector.Parameters)
        //        .Select(x => (ExpandoObject)DapperUtilsExtensions.ToExpandoObject(x)).ToList();
        //    return joinedResults;
        //}
        #endregion

        /// <summary>
        /// Inner joins two tables by specified up to join expressions
        /// This uses internally a template builder and a shortcut to join two tables without having to specify any SQL manually
        /// and gives you the entire inner join result set. 
        /// The one join expresions must conform to a specific predicate expression type.
        /// <paramref name="connection">IDbConnection object</paramref>
        /// <paramref name="firstJoin">First join expression</paramref>
        /// </summary>
        /// <example>This example shows how you can join three tables from the Northwind DB as an example. The joins involved must 
        /// be expressions that return bool (predicates) for the two tables involved in each join operation. This method supports 3 tables
        /// via inner joins, via overloads to this method.
        /// <code>
        ///    [Test]
        /// public void InnerJoinTwoTablesWithoutManualSqlReturnsExpected()
        ///{
        ///    var joinedproductsandcategory = Connection.InnerJoin(
        ///        (Order o, OrderDetail od) => o.OrderID == od.OrderID,
        ///       );
        ///    dynamic firstRow = joinedproductsandcategory.ElementAt(0);
        ///    Assert.AreEqual(firstRow.EmployeeID + firstRow.Title + firstRow.OrderID + firstRow.ShipName, "5Sales Manager10248Vins et alcools Chevalier'");
        ///}
        /// </code>
        /// </example>
        public static IEnumerable<ExpandoObject> InnerJoin<
          TFirstJoinLeft, TFirstJoinRight>(this IDbConnection connection,
          Expression<Func<TFirstJoinLeft, TFirstJoinRight, bool>> firstJoin,
          Tuple<string, Type>[] filters = null)
        {
            return InnerJoin<TFirstJoinLeft, TFirstJoinRight, TUnsetType, TUnsetType, TUnsetType, TUnsetType,
                TUnsetType, TUnsetType, TUnsetType, TUnsetType, TUnsetType, TUnsetType>(connection,
                firstJoin, null, null, null, null, null, filters);
        }

        /// <summary>
        /// Inner joins three tables by specified up to two join expressions
        /// This uses internally a template builder and a shortcut to join two tables without having to specify any SQL manually
        /// and gives you the entire inner join result set. 
        /// The 3 join expresions must conform to a specific predicate expression type.
        /// <paramref name="connection">IDbConnection object</paramref>
        /// <paramref name="firstJoin">First join expression</paramref>
        /// <paramref name="secondJoin">Second join expression</paramref>
        /// </summary>
        /// <example>This example shows how you can join three tables from the Northwind DB as an example. The joins involved must 
        /// be expressions that return bool (predicates) for the two tables involved in each join operation. This method supports 3 tables
        /// via inner joins, via overloads to this method.
        /// <code>
        ///    [Test]
        /// public void InnerJoinFiveTablesWithoutManualSqlReturnsExpected()
        ///{
        ///    var joinedproductsandcategory = Connection.InnerJoin(
        ///        (Order o, OrderDetail od) => o.OrderID == od.OrderID,
        ///        (Order o, Employee e) => o.EmployeeID == e.EmployeeID    
        ///       );
        ///    dynamic firstRow = joinedproductsandcategory.ElementAt(0);
        ///    Assert.AreEqual(firstRow.EmployeeID + firstRow.Title + firstRow.OrderID + firstRow.ShipName + firstRow.ProductID.ToString() + firstRow.ProductName, "5Sales Manager10248Vins et alcools Chevalier11Queso Cabrales'");
        ///}
        /// </code>
        /// </example>
        public static IEnumerable<ExpandoObject> InnerJoin<
     TFirstJoinLeft, TFirstJoinRight,
     TSecondJoinLeft, TSecondJoinRight>(this IDbConnection connection,
     Expression<Func<TFirstJoinLeft, TFirstJoinRight, bool>> firstJoin,
     Expression<Func<TSecondJoinLeft, TSecondJoinRight, bool>> secondJoin,
     Tuple<string, Type>[] filters = null)
        {
            return InnerJoin<TFirstJoinLeft, TFirstJoinRight, TSecondJoinLeft, TSecondJoinRight, TUnsetType, TUnsetType,
                TUnsetType, TUnsetType, TUnsetType, TUnsetType, TUnsetType, TUnsetType>(connection,
                firstJoin, secondJoin, null, null, null, null, filters);
        }

        /// <summary>
        /// Inner joins four tables by specified up to three join expressions
        /// This uses internally a template builder and a shortcut to join two tables without having to specify any SQL manually
        /// and gives you the entire inner join result set. 
        /// The 3 join expresions must conform to a specific predicate expression type.
        /// <paramref name="connection">IDbConnection object</paramref>
        /// <paramref name="firstJoin">First join expression</paramref>
        /// <paramref name="secondJoin">Second join expression</paramref>
        /// <paramref name="thirdJoin">Third join expression</paramref>
        /// </summary>
        /// <example>This example shows how you can join five tables from the Northwind DB as an example. The joins involved must 
        /// be expressions that return bool (predicates) for the two tables involved in each join operation. This method supports 4 tables
        /// via inner joins, via overloads to this method.
        /// <code>
        ///    [Test]
        /// public void InnerJoinFiveTablesWithoutManualSqlReturnsExpected()
        ///{
        ///    var joinedproductsandcategory = Connection.InnerJoin(
        ///        (Order o, OrderDetail od) => o.OrderID == od.OrderID,
        ///        (Order o, Employee e) => o.EmployeeID == e.EmployeeID,
        ///       (OrderDetail od, Product p) => od.ProductID == p.ProductID        ///      
        ///       );
        ///    dynamic firstRow = joinedproductsandcategory.ElementAt(0);
        ///    Assert.AreEqual(firstRow.EmployeeID + firstRow.Title + firstRow.OrderID + firstRow.ShipName + firstRow.ProductID.ToString() + firstRow.ProductName + firstRow.CategoryID + firstRow.CategoryName, "5Sales Manager10248Vins et alcools Chevalier11Queso Cabrales4Dairy Products'");
        ///}
        /// </code>
        /// </example>
        public static IEnumerable<ExpandoObject> InnerJoin<
        TFirstJoinLeft, TFirstJoinRight,
        TSecondJoinLeft, TSecondJoinRight,
        TThirdJoinLeft, TThirdJoinRight>(this IDbConnection connection,
        Expression<Func<TFirstJoinLeft, TFirstJoinRight, bool>> firstJoin,
        Expression<Func<TSecondJoinLeft, TSecondJoinRight, bool>> secondJoin,
        Expression<Func<TThirdJoinLeft, TThirdJoinRight, bool>> thirdJoin,
        Tuple<string, Type>[] filters = null)
        {
            return InnerJoin<TFirstJoinLeft, TFirstJoinRight, TSecondJoinLeft, TSecondJoinRight, TThirdJoinLeft, TThirdJoinRight,
                TUnsetType, TUnsetType, TUnsetType, TUnsetType, TUnsetType, TUnsetType>(connection,
                firstJoin, secondJoin, thirdJoin, null, null, null, filters);
        }

        /// <summary>
        /// Inner joins five tables by specified up to four join expressions
        /// This uses internally a template builder and a shortcut to join two tables without having to specify any SQL manually
        /// and gives you the entire inner join result set. 
        /// The 4 join expresions must conform to a specific predicate expression type.
        /// <paramref name="connection">IDbConnection object</paramref>
        /// <paramref name="firstJoin">First join expression</paramref>
        /// <paramref name="secondJoin">Second join expression</paramref>
        /// <paramref name="thirdJoin">Third join expression</paramref>
        /// <paramref name="fourthJoin">Fourth join expression</paramref>
        /// </summary>
        /// <example>This example shows how you can join five tables from the Northwind DB as an example. The joins involved must 
        /// be expressions that return bool (predicates) for the two tables involved in each join operation. This method supports 6 tables
        /// via inner joins, via overloads to this method.
        /// <code>
        ///    [Test]
        /// public void InnerJoinFiveTablesWithoutManualSqlReturnsExpected()
        ///{
        ///    var joinedproductsandcategory = Connection.InnerJoin(
        ///        (Order o, OrderDetail od) => o.OrderID == od.OrderID,
        ///        (Order o, Employee e) => o.EmployeeID == e.EmployeeID,
        ///       (OrderDetail od, Product p) => od.ProductID == p.ProductID,
        ///        (Product p, Category c) => p.CategoryID == c.CategoryID
        ///       );
        ///    dynamic firstRow = joinedproductsandcategory.ElementAt(0);
        ///    Assert.AreEqual(firstRow.EmployeeID + firstRow.Title + firstRow.OrderID + firstRow.ShipName + firstRow.ProductID.ToString() + firstRow.ProductName + firstRow.CategoryID + firstRow.CategoryName, "5Sales Manager10248Vins et alcools Chevalier11Queso Cabrales4Dairy Products'");
        ///}
        /// </code>
        /// </example>
        public static IEnumerable<ExpandoObject> InnerJoin<
           TFirstJoinLeft, TFirstJoinRight,
           TSecondJoinLeft, TSecondJoinRight,
           TThirdJoinLeft, TThirdJoinRight,
           TFourthJoinLeft, TFourthJoinRight>(this IDbConnection connection,
           Expression<Func<TFirstJoinLeft, TFirstJoinRight, bool>> firstJoin,
           Expression<Func<TSecondJoinLeft, TSecondJoinRight, bool>> secondJoin,
           Expression<Func<TThirdJoinLeft, TThirdJoinRight, bool>> thirdJoin,
           Expression<Func<TFourthJoinLeft, TFourthJoinRight, bool>> fourthJoin,
           Tuple<string, Type>[] filters = null
   )
        {
            return InnerJoin<TFirstJoinLeft, TFirstJoinRight, TSecondJoinLeft, TSecondJoinRight, TThirdJoinLeft, TThirdJoinRight,
                TFourthJoinLeft, TFourthJoinRight, TUnsetType, TUnsetType, TUnsetType, TUnsetType>(connection,
                firstJoin, secondJoin, thirdJoin, fourthJoin, null, null, filters);
        }

        /// <summary>
        /// Inner joins six tables by specified up to five join expressions
        /// This uses internally a template builder and a shortcut to join two tables without having to specify any SQL manually
        /// and gives you the entire inner join result set. 
        /// The 5 join expresions must conform to a specific predicate expression type.
        /// <paramref name="connection">IDbConnection object</paramref>
        /// <paramref name="firstJoin">First join expression</paramref>
        /// <paramref name="secondJoin">Second join expression</paramref>
        /// <paramref name="thirdJoin">Third join expression</paramref>
        /// <paramref name="fourthJoin">Fourth join expression</paramref>
        /// <paramref name="fifthJoin">Fifth join expression</paramref>
        /// </summary>
        /// <example>This example shows how you can join five tables from the Northwind DB as an example. The joins involved must 
        /// be expressions that return bool (predicates) for the two tables involved in each join operation. This method supports 6 tables
        /// via inner joins, via overloads to this method.
        /// <code>
        ///    [Test]
        /// public void InnerJoinFiveTablesWithoutManualSqlReturnsExpected()
        ///{
        ///    var joinedproductsandcategory = Connection.InnerJoin(
        ///        (Order o, OrderDetail od) => o.OrderID == od.OrderID,
        ///        (Order o, Employee e) => o.EmployeeID == e.EmployeeID,
        ///       (OrderDetail od, Product p) => od.ProductID == p.ProductID,
        ///        (Product p, Category c) => p.CategoryID == c.CategoryID,
        ///       );
        ///    dynamic firstRow = joinedproductsandcategory.ElementAt(0);
        ///    Assert.AreEqual(firstRow.EmployeeID + firstRow.Title + firstRow.OrderID + firstRow.ShipName + firstRow.ProductID.ToString() + firstRow.ProductName + firstRow.CategoryID + firstRow.CategoryName, "5Sales Manager10248Vins et alcools Chevalier11Queso Cabrales4Dairy Products'");
        ///}
        /// </code>
        /// </example>
        public static IEnumerable<ExpandoObject> InnerJoin<
                TFirstJoinLeft, TFirstJoinRight,
                TSecondJoinLeft, TSecondJoinRight,
                TThirdJoinLeft, TThirdJoinRight,
                TFourthJoinLeft, TFourthJoinRight,
                TFifthJoinLeft, TFifthJoinRight>(this IDbConnection connection,
                Expression<Func<TFirstJoinLeft, TFirstJoinRight, bool>> firstJoin,
                Expression<Func<TSecondJoinLeft, TSecondJoinRight, bool>> secondJoin,
                Expression<Func<TThirdJoinLeft, TThirdJoinRight, bool>> thirdJoin,
                Expression<Func<TFourthJoinLeft, TFourthJoinRight, bool>> fourthJoin,
                Expression<Func<TFifthJoinLeft, TFifthJoinRight, bool>> fifthJoin,
                Tuple<string, Type>[] filters = null
        )
        {
            return InnerJoin<TFirstJoinLeft, TFirstJoinRight, TSecondJoinLeft, TSecondJoinRight, TThirdJoinLeft, TThirdJoinRight,
                TFourthJoinLeft, TFourthJoinRight, TFifthJoinLeft, TFifthJoinRight, TUnsetType, TUnsetType>(connection,
                firstJoin, secondJoin, thirdJoin, fourthJoin, fifthJoin, null, filters);
        }

        /// <summary>
        /// Inner joins seven tables by specified up to six join expressions
        /// This uses internally a template builder and a shortcut to join two tables without having to specify any SQL manually
        /// and gives you the entire inner join result set. 
        /// The 6 join expresions must conform to a specific predicate expression type.
        /// <paramref name="connection">IDbConnection object</paramref>
        /// <paramref name="firstJoin">First join expression</paramref>
        /// <paramref name="secondJoin">Second join expression</paramref>
        /// <paramref name="thirdJoin">Third join expression</paramref>
        /// <paramref name="fourthJoin">Fourth join expression</paramref>
        /// <paramref name="fifthJoin">Fifth join expression</paramref>
        /// <paramref name="sixthJoin">Sixth join expression</paramref>
        /// </summary>
        /// <example>This example shows how you can join six tables from the Northwind DB as an example. The joins involved must 
        /// be expressions that return bool (predicates) for the two tables involved in each join operation. This method supports 7 tables
        /// via inner joins, via overloads to this method.
        /// <code>
        ///    [Test]
        /// public void InnerJoinSixTablesWithoutManualSqlReturnsExpected()
        ///{
        ///    var joinedproductsandcategory = Connection.InnerJoin(
        ///        (Order o, OrderDetail od) => o.OrderID == od.OrderID,
        ///        (Order o, Employee e) => o.EmployeeID == e.EmployeeID,
        ///       (OrderDetail od, Product p) => od.ProductID == p.ProductID,
        ///        (Product p, Category c) => p.CategoryID == c.CategoryID,
        ///        (Product p, Supplier s) => p.SupplierID == s.SupplierID);
        ///    dynamic firstRow = joinedproductsandcategory.ElementAt(0);
        ///    Assert.AreEqual(firstRow.EmployeeID + firstRow.Title + firstRow.OrderID + firstRow.ShipName + firstRow.ProductID.ToString() + firstRow.ProductName + firstRow.CategoryID + firstRow.CategoryName + firstRow.SupplierID + firstRow.CompanyName, "5Sales Manager10248Vins et alcools Chevalier11Queso Cabrales4Dairy Products5Cooperativa de Quesos 'Las Cabras'");
        ///}
        /// </code>
        /// </example>
        public static IEnumerable<ExpandoObject> InnerJoin<
                TFirstJoinLeft, TFirstJoinRight,
                TSecondJoinLeft, TSecondJoinRight,
                TThirdJoinLeft, TThirdJoinRight,
                TFourthJoinLeft, TFourthJoinRight,
                TFifthJoinLeft, TFifthJoinRight,
                TSixthJoinLeft, TSixthJoinRight>(this IDbConnection connection,
                Expression<Func<TFirstJoinLeft, TFirstJoinRight, bool>> firstJoin,
                Expression<Func<TSecondJoinLeft, TSecondJoinRight, bool>> secondJoin,
                Expression<Func<TThirdJoinLeft, TThirdJoinRight, bool>> thirdJoin,
                Expression<Func<TFourthJoinLeft, TFourthJoinRight, bool>> fourthJoin,
                Expression<Func<TFifthJoinLeft, TFifthJoinRight, bool>> fifthJoin,
                Expression<Func<TSixthJoinLeft, TSixthJoinRight, bool>> sixthJoin,
                Tuple<string, Type>[] filters = null
            )
        {
            var builder = new SqlBuilder();
            string firstTableSelectClause = string.Join(",", GetPublicPropertyNames<TFirstJoinLeft>("t1"));
            string secondTableSelectClause = string.Join(",", GetPublicPropertyNames<TFirstJoinRight>("t2"));
            string thirdTableSelectClause = string.Join(",", GetPublicPropertyNames<TSecondJoinRight>("t3"));
            string fourthTableSelectClause = typeof(TThirdJoinRight) != typeof(TUnsetType) ? string.Join(",", GetPublicPropertyNames<TThirdJoinRight>("t4")) : null;
            string fifthTableSelectClause = typeof(TFourthJoinRight) != typeof(TUnsetType) ? string.Join(",", GetPublicPropertyNames<TFourthJoinRight>("t5")) : null;
            string sixthTableSelectClause = typeof(TFifthJoinRight) != typeof(TUnsetType) ? string.Join(",", GetPublicPropertyNames<TFifthJoinRight>("t6")) : null;
            string seventhTableSelectClause = typeof(TSixthJoinRight) != typeof(TUnsetType) ? string.Join(",", GetPublicPropertyNames<TSixthJoinRight>("t7")) : null;

            string firstLeftKeyName = GetJoinKey(firstJoin, true);
            string firstRightKeyName = GetJoinKey(firstJoin, false);
            string secondLeftKeyName = GetJoinKey(secondJoin, true);
            string secondRightKeyName = GetJoinKey(secondJoin, false);
            string thirdLeftKeyName = GetJoinKey(thirdJoin, true);
            string thirdRightKeyName = GetJoinKey(thirdJoin, false);
            string fourthLeftKeyName = typeof(TFourthJoinLeft) != typeof(TUnsetType) ? GetJoinKey(fourthJoin, true) : null;
            string fourthRightKeyName = typeof(TFourthJoinRight) != typeof(TUnsetType) ? GetJoinKey(fourthJoin, false) : null;
            string fifthLeftKeyName = typeof(TFifthJoinLeft) != typeof(TUnsetType) ? GetJoinKey(fifthJoin, true) : null;
            string fifthRightKeyName = typeof(TFifthJoinRight) != typeof(TUnsetType) ? GetJoinKey(fifthJoin, false) : null;
            string sixthLeftKeyName = typeof(TFifthJoinLeft) != typeof(TUnsetType) ? GetJoinKey(fifthJoin, true) : null;
            string sixthRightKeyName = typeof(TFifthJoinRight) != typeof(TUnsetType) ? GetJoinKey(fifthJoin, false) : null;
            string seventhLeftKeyName = typeof(TSixthJoinLeft) != typeof(TUnsetType) ? GetJoinKey(sixthJoin, true) : null;
            string seventhRightKeyName = typeof(TSixthJoinRight) != typeof(TUnsetType) ? GetJoinKey(sixthJoin, false) : null;

            string firstTableName = GetDbTableName<TFirstJoinLeft>();
            string secondTableName = GetDbTableName<TFirstJoinRight>();
            string thirdTableName = GetDbTableName<TSecondJoinRight>();
            string fourthTableName = typeof(TThirdJoinRight) != typeof(TUnsetType) ? GetDbTableName<TThirdJoinRight>() : null;
            string fifthTableName = typeof(TFourthJoinRight) != typeof(TUnsetType) ? GetDbTableName<TFourthJoinRight>() : null;
            string sixthTableName = typeof(TFifthJoinRight) != typeof(TUnsetType) ? GetDbTableName<TFifthJoinRight>() : null;
            string seventhTableName = typeof(TSixthJoinRight) != typeof(TUnsetType) ? GetDbTableName<TSixthJoinRight>() : null;

            string joinSelectClause = $"select {firstTableSelectClause}, {secondTableSelectClause}";
            if (!string.IsNullOrEmpty(thirdTableSelectClause))
            {
                joinSelectClause += $", {thirdTableSelectClause}";
            }
            if (!string.IsNullOrEmpty(fourthTableSelectClause))
            {
                joinSelectClause += $", {fourthTableSelectClause}";
            }
            if (!string.IsNullOrEmpty(fifthTableSelectClause))
            {
                joinSelectClause += $", {fifthTableSelectClause}";
            }
            if (!string.IsNullOrEmpty(sixthTableSelectClause))
            {
                joinSelectClause += $", {sixthTableSelectClause}";
            }
            if (!string.IsNullOrEmpty(seventhTableSelectClause))
            {
                joinSelectClause += $", {seventhTableSelectClause}";
            }
            joinSelectClause = joinSelectClause.TrimEnd().TrimEnd(',');

            var registeredTableAliases = new Dictionary<string, Type>();
            registeredTableAliases.Add("t1", typeof(TFirstJoinLeft));

            joinSelectClause += $" from {firstTableName} t1 /**innerjoin**/ /**where**/";
            var selector = builder.AddTemplate(joinSelectClause);

            registeredTableAliases.Add("t2", typeof(TFirstJoinRight));
            builder.InnerJoin($"{secondTableName} t2 on t1.{firstLeftKeyName} = t2.{firstRightKeyName}");

            string unsetTypeName = typeof(TUnsetType).Name;
            if (thirdTableName != null && thirdTableName != unsetTypeName)
            {
                registeredTableAliases.Add("t3", typeof(TSecondJoinRight));
                string tableAliasToMatchForSecondJoin = GetTableAliasForJoin(secondJoin, registeredTableAliases);
                builder.InnerJoin($"{thirdTableName} t3 on {tableAliasToMatchForSecondJoin}.{secondLeftKeyName} = t3.{secondRightKeyName}");
            }
            if (fourthTableName != null && fourthTableName != unsetTypeName)
            {
                registeredTableAliases.Add("t4", typeof(TThirdJoinRight));
                string tableAliasToMatchForThirdJoin = GetTableAliasForJoin(thirdJoin, registeredTableAliases);
                builder.InnerJoin($"{fourthTableName} t4 on {tableAliasToMatchForThirdJoin}.{thirdLeftKeyName} = t4.{thirdRightKeyName}");
            }
            if (fifthTableName != null && fifthTableName != unsetTypeName)
            {
                registeredTableAliases.Add("t5", typeof(TFourthJoinRight));
                string tableAliasToMatchForFourthJoin = GetTableAliasForJoin(fourthJoin, registeredTableAliases);
                builder.InnerJoin($"{fifthTableName} t5 on {tableAliasToMatchForFourthJoin}.{fourthLeftKeyName} = t5.{fourthRightKeyName}");
            }
            if (sixthTableName != null && sixthTableName != unsetTypeName)
            {
                registeredTableAliases.Add("t6", typeof(TFifthJoinRight));
                string tableAliasToMatchForFifthJoin = GetTableAliasForJoin(fifthJoin, registeredTableAliases);
                builder.InnerJoin($"{sixthTableName} t6 on {tableAliasToMatchForFifthJoin}.{fifthLeftKeyName} = t6.{fifthRightKeyName}");
            }
            if (seventhTableName != null && seventhTableName != unsetTypeName)
            {
                registeredTableAliases.Add("t7", typeof(TFifthJoinRight));
                string tableAliasToMatchForSixthJoin = GetTableAliasForJoin(sixthJoin, registeredTableAliases);
                builder.InnerJoin($"{sixthTableName} t6 on {tableAliasToMatchForSixthJoin}.{sixthLeftKeyName} = t6.{sixthRightKeyName}");
            }

            if (filters != null && filters.Any())
            {
                foreach (var filter in filters)
                {
                    if (string.IsNullOrEmpty(filter.Item1))
                    {
                        throw new ArgumentNullException("Provide sql for filter for join.");
                    }
                    string tableAliasForFilter = GetTableAliasForFilter(filter.Item2, registeredTableAliases);
                    string whereClauseForFilter = $"{tableAliasForFilter}.{filter.Item1}";
                    builder.Where(whereClauseForFilter);
                }
            }

            var joinedResults = connection.Query(selector.RawSql, selector.Parameters)
                .Select(x => (ExpandoObject)DapperUtilsExtensions.ToExpandoObject(x)).ToList();
            return joinedResults;
        }

        private static string GetTableAliasForFilter(Type typeForFilter, Dictionary<string, Type> registeredTableAliases)
        {
            if (!registeredTableAliases.ContainsValue(typeForFilter))
            {
                throw new ArgumentException($"Could not resolve table alias for filter. The type for filter with missing table alias is: {typeForFilter.Name}");
            }
            string tableAliasForFilter = registeredTableAliases.First(x => x.Value == typeForFilter).Key;
            return tableAliasForFilter;
        }

        private static string GetTableAliasForJoin<TJoinLeft, TJoinRight>
            (Expression<Func<TJoinLeft, TJoinRight, bool>> join,
            Dictionary<string, Type> registeredTableAliases)
        {
            var joinKeyType = GetJoinKeyType(join, true);
            if (!registeredTableAliases.ContainsValue(joinKeyType))
            {
                throw new ArgumentException($"Could not resolve table alias for given join: {join?.ToString()}. Check that the sequence of joins specified builds a valid chain among the table joins. Consider reordering joins or reduce the amount of join operations.");
            }
            var tableAliasKvp = registeredTableAliases.Single(x => x.Value == joinKeyType);
            return tableAliasKvp.Key;
        }

        private static string GetJoinKey<TLeftKey, TRightKey>(Expression<Func<TLeftKey, TRightKey, bool>> joinCondition, bool chooseLeftKey)
        {
            if (joinCondition == null)
            {
                return null;
            }
            try
            {
                var binaryFunc = (BinaryExpression)joinCondition.Body;
                var joinPart = chooseLeftKey ? binaryFunc.Left : binaryFunc.Right;
                if (joinPart.NodeType == ExpressionType.Convert)
                {
                    var ue = (MemberExpression)((UnaryExpression)joinPart).Operand;
                    return ue.Member.Name;
                }
                var memberExpression = (MemberExpression)joinPart;
                return memberExpression.Member.Name;
            }
            catch (Exception err)
            {
                throw new ArgumentException($"The {nameof(joinCondition)} must be a LogicalBinaryExpression, for example: (Car c, Driver d) => c.Id == d.Id. The passed in lambda does not follow this format. Error: {err}");
            }
        }

        private static Type GetJoinKeyType<TLeftKey, TRightKey>(Expression<Func<TLeftKey, TRightKey, bool>> joinCondition, bool chooseLeftKey)
        {
            if (joinCondition == null)
            {
                return null;
            }
            try
            {
                var binaryFunc = (BinaryExpression)joinCondition.Body;
                var joinPart = chooseLeftKey ? binaryFunc.Left : binaryFunc.Right;
                if (joinPart.NodeType == ExpressionType.Convert)
                {
                    var ue = (MemberExpression)((UnaryExpression)joinPart).Operand;
                    return ue.Member.ReflectedType;
                }
                var memberExpression = (MemberExpression)joinPart;
                return memberExpression.Member.ReflectedType;
            }
            catch (Exception err)
            {
                throw new ArgumentException($"The {nameof(joinCondition)} must be a LogicalBinaryExpression, for example: (Car c, Driver d) => c.Id == d.Id. The passed in lambda does not follow this format. Error: {err}");
            }
        }

        /// <summary>
        /// Returns database table name, either via the System.ComponentModel.DataAnnotations.Schema.Table attribute
        /// if it exists, or just the name of the <typeparamref name="TClass"/> type parameter. 
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <returns></returns>
        private static string GetDbTableName<TClass>()
        {
            return GetDbTableFromType(typeof(TClass));
        }

        private static string GetDbTableFromType(Type table)
        {
            var tableAttribute = table.GetCustomAttributes(typeof(TableAttribute), false)?.FirstOrDefault() as TableAttribute;
            if (tableAttribute != null)
            {
                if (!string.IsNullOrEmpty(tableAttribute.Schema))
                {
                    return $"[{tableAttribute.Schema}].[{tableAttribute.Name}]";
                }
                return tableAttribute.Name;
            }
            return table.Name;
        }

        private static string[] GetPublicPropertyNames<T>(string tableQualifierPrefix = null)
        {
            return GetPublicPropertyNamesFromType(tableQualifierPrefix, typeof(T));
        }

        private static string[] GetPublicPropertyNamesFromType(string tableQualifierPrefix, Type table)
        {
            return table.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                 .Where(x => !IsNotMapped(x))
                 .Select(x => !string.IsNullOrEmpty(tableQualifierPrefix) ? tableQualifierPrefix + "." + x.Name : x.Name).ToArray();
        }

        private static bool IsNotMapped(PropertyInfo x)
        {
            var notmappedAttr = x.GetCustomAttributes<NotMappedAttribute>()?.OfType<NotMappedAttribute>().FirstOrDefault();
            return notmappedAttr != null;
        }

        /// <summary>
        /// Searches for a searchterm with 'LIKE' operator against the parameter and column provided in your sql provided
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="sql">Example: 'select * from products where ProductName like @ProdName. @ProdName must exist in the <paramref name="parametersDictionary"/></param>
        /// <param name="searchTerm"></param>
        /// <param name="parametersDictionary"></param>
        /// <returns></returns>
        public static IEnumerable<T> ParameterizedLike<T>(this IDbConnection connection, string sql, string searchTerm,
            Dictionary<string, object> parametersDictionary)
        {
            return connection.ParameterizedQuery<T>(sql, new Dictionary<string, object> { { "@ProdName", Like($"{searchTerm}") } });
        }

        public static string Like(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return null;
            }

            string searchTermLocal = searchTerm;
            Func<string, string> encodeForLike = x => searchTerm.Replace("[", "[[]").Replace("%", "[%]");
            return $"%{encodeForLike(searchTerm)}%";
        }

        /// <summary>
        /// Returns aggregate function calculations. To use '*' row-wise calculations pass in null for the aggregateColumn.
        /// The resulting expanded object can be iterated over and stored into a dynamic variable to retrieve the properties inside, or
        /// casted to an IDictionary&lt;string,object&gt; for example.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="aggregateColumn">If null, the '*' will be used, e.g. sum(*), count(*) and so on.</param>
        /// <param name="calculation">Type of aggregate function to use.</param>
        /// <param name="groupingColumns">If grouping is desired, list up the columns to group on</param>
        /// <param name="tableName">Provided name of DB table to retrieve. If null, the name of the POCO type attribute <typeparam name="T"></typeparam></param> will be used.
        /// <param name="aliasForAggregate"></param>
        /// <returns></returns>
        public static IEnumerable<ExpandoObject> GetAggregate<T>(this IDbConnection connection, Expression<Func<T, object>> aggregateColumn,
            AggregateFunction calculation, Expression<Func<T, object>>[] groupingColumns = null,
            string tableName = null, string aliasForAggregate = "Value")
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
            string aggregateFunctionExpression = GetAggregateFunctionExpression(calculation, aggregateColumnName, groupingColumnsJoined, aliasForAggregate);
            var sql = $"select {aggregateFunctionExpression} from {tableName}";
            if (!string.IsNullOrEmpty(groupingColumnsJoined))
            {
                sql += $"{Environment.NewLine}group by {groupingColumnsJoined}";
            }
            var results = connection.Query(sql).Select(x => (ExpandoObject)ToExpandoObject(x));
            return results;
        }

        private static string GetAggregateFunctionExpression(AggregateFunction function, string aggregateColumnName,
            string groupingColumnsJoined, string aliasForAggregate)
        {
            string aggregateColumnSuffix = !string.IsNullOrEmpty(groupingColumnsJoined) ? $",{groupingColumnsJoined}" : string.Empty;
            string aggregateFunctionExpression = string.Empty;
            switch (function)
            {
                case AggregateFunction.Count:
                    aggregateFunctionExpression = $"count({aggregateColumnName ?? "*"}) as {aliasForAggregate}";
                    break;
                case AggregateFunction.Sum:
                    aggregateFunctionExpression = $"sum({aggregateColumnName ?? "*"}) as {aliasForAggregate}";
                    break;
                case AggregateFunction.Min:
                    aggregateFunctionExpression = $"min({aggregateColumnName ?? "*"}) as {aliasForAggregate}";
                    break;
                case AggregateFunction.Max:
                    aggregateFunctionExpression = $"max({aggregateColumnName ?? "*"}) as {aliasForAggregate}";
                    break;
                case AggregateFunction.Avg:
                    aggregateFunctionExpression = $"avg({aggregateColumnName ?? "*"}) as {aliasForAggregate}";
                    break;
                case AggregateFunction.Var:
                    aggregateFunctionExpression = $"var({aggregateColumnName ?? "*"}) as {aliasForAggregate}";
                    break;
                case AggregateFunction.Varp:
                    aggregateFunctionExpression = $"varp({aggregateColumnName ?? "*"}) as {aliasForAggregate}";
                    break;
                case AggregateFunction.Stdevp:
                    aggregateFunctionExpression = $"stdevp({aggregateColumnName ?? "*"}) as {aliasForAggregate}";
                    break;
                case AggregateFunction.CountBig:
                    aggregateFunctionExpression = $"count_big({aggregateColumnName ?? "*"}) as {aliasForAggregate}";
                    break;
                case AggregateFunction.Stdev:
                    aggregateFunctionExpression = $"stdev({aggregateColumnName ?? "*"}) as {aliasForAggregate}";
                    break;
                default:
                    break;
            }
            return $"{aggregateFunctionExpression}{aggregateColumnSuffix}";
        }

        public static async Task Delete<TTable>(this IDbConnection connection, TTable rowToDelete)
        {
            var columns = ReflectionHelper.GetPublicProperties<TTable>(includePropertiesMarkedAsKeyOrNotDatabaseGenerated: false);
            var columnsWithKeys = ReflectionHelper.GetPublicProperties<TTable>(includePropertiesMarkedAsKeyOrNotDatabaseGenerated: true)
                .Where(c => !(columns.Select(x => x.Key).Contains(c.Key)));

            var sb = new StringBuilder();
            string tableName = GetDbTableName<TTable>();
            sb.AppendLine($"DELETE {tableName}");
            int columnIndex = 0;
            int columnCount = columns.Count;

            if (columnCount < 1 || columnsWithKeys.Count() < 1)
            {
                throw new ArgumentException($"The table of type {typeof(TTable)} does not have any public properties / columns or keyed columns which are detected for the delete operation. Adjust your columns and table POCO class first. Aborting delete and throwing error");
            }

            columnIndex = 0;
            foreach (var column in columnsWithKeys)
            {
                if (columnIndex == 0)
                {
                    sb.AppendLine(" WHERE ");
                }
                sb.AppendLine($"{column.Key} = @{column.Key}");
                if (columnIndex < columnCount - 1 && columnIndex > 0 && columnsWithKeys.Count() > 1)
                {
                    sb.Append(" AND ");
                }

                columnIndex++;
            }
            string sql = sb.ToString();
            await connection.ExecuteScalarAsync(sql, rowToDelete);
        }

        public static async Task Update<TTable>(this IDbConnection connection, TTable rowToUpdate)
        {
            var columns = ReflectionHelper.GetPublicProperties<TTable>(includePropertiesMarkedAsKeyOrNotDatabaseGenerated: false);
            var columnsWithKeys = ReflectionHelper.GetPublicProperties<TTable>(includePropertiesMarkedAsKeyOrNotDatabaseGenerated: true)
                .Where(c => !(columns.Select(x => x.Key).Contains(c.Key)));

            var sb = new StringBuilder();
            string tableName = GetDbTableName<TTable>();
            sb.AppendLine($"UPDATE {tableName}");
            int columnIndex = 0;
            int columnCount = columns.Count;

            if (columnCount < 1 || columnsWithKeys.Count() < 1)
            {
                throw new ArgumentException($"The table of type {typeof(TTable)} does not have any public properties / columns or keyed columns which are detected for the update operation. Adjust your columns and table POCO class first. Aborting update and throwing error");
            }

            columnIndex = 0;
            foreach (var column in columns)
            {
                if (columnIndex == 0)
                {
                    sb.AppendLine(" SET ");
                }
                sb.AppendLine($"{column.Key} = @{column.Key}");
                if (columnIndex < columnCount - 1)
                {
                    sb.Append(",");
                }

                if (columnIndex == columnCount - 1)
                {
                    sb.Append($"{Environment.NewLine}WHERE ");
                    foreach (var columnkey in columnsWithKeys) {
                        var columnValue = columnkey.Value.GetValue(rowToUpdate, null);
                        if (columnValue != null)
                        {
                            sb.Append($"{columnkey.Key} = @{columnkey.Key}");
                        }
                    }
                }
                columnIndex++;
            }
            string sql = sb.ToString();
            await connection.ExecuteScalarAsync(sql, rowToUpdate);
        }

        /// <summary>
        /// Updates multiple rows into a type of type <typeparamref name="TTable"/>. 
        /// Note ! This only works for tables
        /// with a key of type int or uniqueIdentifier (i.e. IDENTITY columns usually). Note ! Only max 1000 rows can be added at a time. Chunk your data when calling this method!
        /// The return result may contain a collection of ints or guids for the newly inserted rows.
        /// Compound keyed items will only return the first key of each new row as this not an edge-case, but not properly supported.
        /// Only items with one column as the key will be supported generally. 
        /// Make sure you declare the POCO of type <typeparamref name="TTable"/> with a column with the [Key] attribute.
        /// Also specify [DatabaseGeneratedOption] set to Identity or Computed to work properly, or else you must
        /// prepare the new key in forehand before the insert (DatabaseGeneratedOption set to None case).
        /// The method will try to set the newly generated id also on the first keyed column found. 
        /// </summary>
        /// <typeparam name="TTable"></typeparam>
        /// <param name="connection"></param>
        /// <param name="rowsToUpdate"></param>
        /// <param name="propertiesToSet">Properties to set</param>
        /// <returns>The updated key of type object, which can either be an int or a Guid of the types of keys supported by this method. Check the type via reflection before casting it at the receiving end.</returns>
        public static async Task<IEnumerable<object>> UpdateMany<TTable>(this IDbConnection connection, IEnumerable<TTable> rowsToUpdate,
            IDictionary<string, object> propertiesToSet)
        {

            if (rowsToUpdate.Count() > 1000)
            {
                throw new ArgumentException("Max 1000 rows may be added at a time due to DB limitations on INSERT batch. Instead partition your data before calling this method as chunking by the method is not implemented yet.");
            }

            if (rowsToUpdate.Count() < 1)
            {
                throw new ArgumentException("At least one row must be passed to this method.");
            }

            var rowsToUpdateList = rowsToUpdate.ToList();

            var dynamicParameters = new DynamicParameters();

            var columns = ReflectionHelper.GetPublicProperties<TTable>(includePropertiesMarkedAsKeyOrNotDatabaseGenerated: false);
            var columnKeys = ReflectionHelper.GetPublicPropertiesWithKeyAttribute<TTable>();

            var sb = new StringBuilder();
            string tableName = GetDbTableName<TTable>();
            sb.AppendLine($"UPDATE {tableName}{Environment.NewLine} SET ");
            int columnIndex = 0;
            int columnCount = columns.Count;

            if (columnCount < 1)
            {
                throw new ArgumentException($"The table of type {typeof(TTable)} does not have any public properties / columns which are detected for the insert operation. Adjust your columns and table POCO class first. Aborting insert and throwing error");
            }

            foreach (var property in propertiesToSet)
            {
                if (new Type[] { typeof(DateTime), typeof(Guid), typeof(string) }.Contains(property.GetType()))
                {
                    propertiesToSet[property.Key] = $"'{property.Value}'"; //quoted properties needs to have quotes around them in T-SQL..
                }
            }

            foreach (var column in columns)
            {
                if (!propertiesToSet.ContainsKey(column.Key))
                {
                    continue;
                }

                if (columnIndex == 0)
                {
                    //sb.Append("(");
                }
                else
                {
                    sb.Append(",");
                }
                string columnName = ReflectionHelper.GetColumnName(column.Value);
                sb.AppendLine($"{columnName} = @{column.Key}");

                if (columnIndex == columnCount - 1)
                {
                    // sb.AppendLine(")");
                }
                columnIndex++;
            }

            sb.Append($"OUTPUT ");

            int columnKeyIndex = 0;

            foreach (var columnKey in columnKeys)
            {
                sb.Append($"INSERTED.{ReflectionHelper.GetColumnName(columnKey.Value)}");
                if (columnKeyIndex > 0)
                {
                    sb.Append(",");
                }
            }
            sb.Append(Environment.NewLine);

            columnKeyIndex = 0;
            foreach (var columnKey in columnKeys)
            {
                if (columnKeyIndex == 0)
                {
                    sb.Append($"WHERE {Environment.NewLine}");
                }

                int itemIndexUpdate = 0;
                foreach (var item in rowsToUpdate)
                {
                    var itemValue = columnKey.Value.GetValue(item, null);

                    if (new Type[] { typeof(DateTime), typeof(Guid), typeof(string) }.Contains(columnKey.Value.PropertyType))
                    {
                        itemValue = $"'{itemValue}'"; //quoted properties needs to have quotes around them in T-SQL..
                    }
                    if (itemIndexUpdate > 0)
                    {
                        sb.Append($"{Environment.NewLine} OR ");
                    }
                    sb.Append($"{columnKey.Key} = {itemValue}");

                    itemIndexUpdate++;
                }

                columnKeyIndex++;
            }

            columnIndex = 0;
            int itemIndex = 0;


            columnIndex = 0;

            foreach (var column in columns)
            {
                if (!propertiesToSet.ContainsKey(column.Key))
                {
                    continue;
                }
                dynamicParameters.Add($"@{column.Key}", propertiesToSet[column.Key]);
                columnIndex++;
            }

            itemIndex++;

            string sql = sb.ToString();

            List<object> idsAfterUpdatesList = new List<object>();

            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {

                var idsAfterUpdates = (await connection.QueryAsync<object>(sql, dynamicParameters)).ToList();
                if (idsAfterUpdates != null && idsAfterUpdates.Any())
                {
                    foreach (var idsAfterUpdate in idsAfterUpdates)
                    {
                        var idAfterUpdatesDict = (IDictionary<string, object>) ToExpandoObject(idsAfterUpdate);
                        string firstColumnKey = columnKeys.Select(c => c.Key).First();
                        object idAfterInsertionValue = idAfterUpdatesDict[firstColumnKey];
                        idsAfterUpdatesList.Add(idAfterInsertionValue); //we do not support compound keys, only items with one key column. Perhaps later versions will return multiple ids per inserted row for compound keys, this must be tested.
                    } //foreach 
                }
            }
            return idsAfterUpdatesList;
        }    

        /// <summary>
        /// Inserts multiple rows into a type of type <typeparamref name="TTable"/>. Note ! This only works for tables
        /// with a key of type int or uniqueIdentifier (i.e. IDENTITY columns usually). Note ! Only max 1000 rows can be added at a time. Chunk your data when calling this method!
        /// The return result may contain a collection of ints or guids for the newly inserted rows.
        /// Compound keyed items will only return the first key of each new row as this not an edge-case, but not properly supported.
        /// Only items with one column as the key will be supported generally. 
        /// Make sure you declare the POCO of type <typeparamref name="TTable"/> with a column with the [Key] attribute.
        /// Also specify [DatabaseGeneratedOption] set to Identity or Computed to work properly, or else you must
        /// prepare the new key in forehand before the insert (DatabaseGeneratedOption set to None case).
        /// The method will try to set the newly generated id also on the first keyed column found. 
        /// </summary>
        /// <typeparam name="TTable"></typeparam>
        /// <param name="connection"></param>
        /// <param name="rowsToAdd"></param>
        /// <returns>The updated key of type object, which can either be an int or a Guid of the types of keys supported by this method. Check the type via reflection before casting it at the receiving end.</returns>
        public static async Task<IEnumerable<object>> InsertMany<TTable>(this IDbConnection connection, IEnumerable<TTable> rowsToAdd)
        {
            if (rowsToAdd.Count() > 1000)
            {
                throw new ArgumentException("Max 1000 rows may be added at a time due to DB limitations on INSERT batch. Instead partition your data before calling this method as chunking by the method is not implemented yet.");
            }

            var rowsToAddList = rowsToAdd.ToList();

            var dynamicParameters = new DynamicParameters();

            var columns = ReflectionHelper.GetPublicProperties<TTable>(includePropertiesMarkedAsKeyOrNotDatabaseGenerated: false);
            var columnKeys = ReflectionHelper.GetPublicPropertiesWithKeyAttribute<TTable>();

            var sb = new StringBuilder();
            string tableName = GetDbTableName<TTable>();
            sb.AppendLine($"INSERT INTO {tableName}");
            int columnIndex = 0;
            int columnCount = columns.Count;

            if (columnCount < 1)
            {
                throw new ArgumentException($"The table of type {typeof(TTable)} does not have any public properties / columns which are detected for the insert operation. Adjust your columns and table POCO class first. Aborting insert and throwing error");
            }

            foreach (var column in columns)
            {
                if (columnIndex == 0)
                {
                    sb.Append("(");
                }
                else
                {
                    sb.Append(",");
                }
                sb.AppendLine($"{column.Key}");

                if (columnIndex == columnCount - 1)
                {
                    sb.AppendLine(")");
                }
                columnIndex++;
            }
            sb.Append($"OUTPUT ");
            int columnKeyIndex = 0;
            foreach (var columnKey in columnKeys)
            {
                if (columnKeyIndex > 0)
                {
                    sb.Append(",INSERTED");
                }
                else
                {
                    sb.Append("INSERTED");
                }
                sb.Append($".{columnKey.Key}");
                columnKeyIndex++;
            }

            columnIndex = 0;
            int itemIndex = 0;


            var dynamicParametersForItems = rowsToAdd.Select(item =>
            {
                {
                    columnIndex = 0;
                    var tempParams = new DynamicParameters();
                    foreach (var column in columns)
                    {
                        tempParams.Add($"@{column.Key}", column.Value.GetValue(item, null));
                        columnIndex++;
                    }

                    itemIndex++;

                    return tempParams;

                }
            }); 
            
 
            sb.AppendLine($"{Environment.NewLine}VALUES ({(string.Join(",", columns.Select(c => $"@{c.Key}").ToArray()))})");
            
            string sql = sb.ToString();

            List<object> idsAfterInsertionList = new List<object>();

            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (var dynamicParametersForItem in dynamicParametersForItems)
                {
                    var idsAfterInsertion = (await connection.QueryAsync<object>(sql, dynamicParametersForItem)).ToList();
                    if (idsAfterInsertion != null && idsAfterInsertion.Any())
                    {
                        var idAfterInsertionDict = (IDictionary<string, object>) ToExpandoObject(idsAfterInsertion.First());
                        string firstColumnKey = columnKeys.Select(c => c.Key).First();
                        object idAfterInsertionValue = idAfterInsertionDict[firstColumnKey];
                        idsAfterInsertionList.Add(idAfterInsertionValue); //we do not support compound keys, only items with one key column. Perhaps later versions will return multiple ids per inserted row for compound keys, this must be tested.
                    }
                }
            }
           
            return idsAfterInsertionList;
        }

        /// <summary>
        /// Inserts a row into a type of type <typeparamref name="TTable"/>. Note ! This only works for tables
        /// with a key of type int (i.e. IDENTITY columns usually). If you want to support tables with key of type
        /// uniqueidentifier (Guid), use the parameter <paramref name="isKeyOfTypeGuid"/> set to true (defaults to false).
        /// The method will try to set the newly generated id also on the first keyed column found. 
        /// </summary>
        /// <typeparam name="TTable"></typeparam>
        /// <param name="connection"></param>
        /// <param name="rowToAdd"></param>
        /// <returns>The updated key of type object, which can either be an int or a Guid of the types of keys supported by this method. Check the type via reflection before casting it at the receiving end.</returns>
        public static async Task<object> Insert<TTable>(this IDbConnection connection, TTable rowToAdd,
            bool isKeyOfTypeGuid = false)
        {
            var columns = ReflectionHelper.GetPublicProperties<TTable>(includePropertiesMarkedAsKeyOrNotDatabaseGenerated: false);
            var sb = new StringBuilder();
            string tableName = GetDbTableName<TTable>();
            sb.AppendLine($"INSERT INTO {tableName}");
            int columnIndex = 0;
            int columnCount = columns.Count;

            if (columnCount < 1)
            {
                throw new ArgumentException($"The table of type {typeof(TTable)} does not have any public properties / columns which are detected for the insert operation. Adjust your columns and table POCO class first. Aborting insert and throwing error");
            }

            foreach (var column in columns)
            {
                if (columnIndex == 0)
                {
                    sb.Append("(");
                }
                else {
                    sb.Append(",");
                }
                sb.AppendLine($"{column.Key}");

                if (columnIndex == columnCount-1)
                {
                    sb.AppendLine(")");
                }
                columnIndex++;
            }
            columnIndex = 0;
            foreach (var column in columns)
            {
                if (columnIndex == 0)
                {
                    sb.AppendLine("VALUES (");
                }
                else
                {
                    sb.Append(",");
                }
                sb.AppendLine($"@{column.Key}");

                if (columnIndex == columnCount-1)
                {
                    sb.AppendLine(");");
                }
                columnIndex++;
            }
            if (isKeyOfTypeGuid)
            {
                sb.AppendLine($"SELECT CAST(SCOPE_IDENTITY() AS UNIQUEIDENTIFIER)");
            }
            else
            {
                sb.AppendLine($"SELECT CAST(SCOPE_IDENTITY() AS INT)");
            }        
            string sql = sb.ToString();
            var keyedColumns = ReflectionHelper.GetPublicPropertiesWithKeyAttribute<TTable>();

            object updatedId = null; 
            try
            {
                if (!isKeyOfTypeGuid)
                {
                    updatedId = await connection.ExecuteScalarAsync<int>(sql, rowToAdd);
                }
                else
                {
                    updatedId = await connection.ExecuteScalarAsync<Guid>(sql, rowToAdd);
                }
                if (keyedColumns.Count() == 1)
                {
                    keyedColumns.First().Value.SetValue(rowToAdd, updatedId);
                }
            }
            catch (Exception)
            {

            }
            return updatedId;
        }

        public static IEnumerable<ExpandoObject> ParameterizedQuery(this IDbConnection connection, string sql,
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
            return connection.Query(sql, parameters).Select(x => (ExpandoObject) ToExpandoObject(x));
        }

        public static ExpandoObject ToExpandoObject(object value)
        {
            IDictionary<string, object> dapperRowProperties = value as IDictionary<string, object>;
            IDictionary<string, object> expando = new ExpandoObject();
            if (dapperRowProperties == null)
            {
                return expando as ExpandoObject;
            }
            foreach (KeyValuePair<string, object> property in dapperRowProperties)
            {
                if (!expando.ContainsKey(property.Key))
                {
                    expando.Add(property.Key, property.Value);
                }
                else
                {
                    //prefix the colliding key with a random guid suffixed 
                    expando.Add(property.Key + Guid.NewGuid().ToString("N"), property.Value);
                } 
            }
            return expando as ExpandoObject;
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

        private static Type GetTypeWrappingMember(Expression<Func<object, object>> expression)
        {
            switch (expression.Body)
            {
                case MemberExpression m:
                    return m.Member.DeclaringType;
                case UnaryExpression u when u.Operand is MemberExpression m:
                    return m.Member.DeclaringType;
                default:
                    throw new NotImplementedException(expression.GetType().ToString());
            }
        }


    }
}
