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

namespace DapperUtils.ToreAurstadIT
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

        public static IEnumerable<ExpandoObject> InnerJoin<
TFirstJoinLeft, TFirstJoinRight>(this IDbConnection connection,
Expression<Func<TFirstJoinLeft, TFirstJoinRight, bool>> firstJoin)
        {
            return InnerJoin<TFirstJoinLeft, TFirstJoinRight, TUnsetType, TUnsetType, TUnsetType, TUnsetType,
                TUnsetType, TUnsetType, TUnsetType, TUnsetType, TUnsetType, TUnsetType>(connection,
                firstJoin, null, null, null, null, null);
        }

        public static IEnumerable<ExpandoObject> InnerJoin<
     TFirstJoinLeft, TFirstJoinRight,
     TSecondJoinLeft, TSecondJoinRight>(this IDbConnection connection,
     Expression<Func<TFirstJoinLeft, TFirstJoinRight, bool>> firstJoin,
     Expression<Func<TSecondJoinLeft, TSecondJoinRight, bool>> secondJoin)
        {
            return InnerJoin<TFirstJoinLeft, TFirstJoinRight, TSecondJoinLeft, TSecondJoinRight, TUnsetType, TUnsetType,
                TUnsetType, TUnsetType, TUnsetType, TUnsetType, TUnsetType, TUnsetType>(connection,
                firstJoin, secondJoin, null, null, null, null);
        }

        public static IEnumerable<ExpandoObject> InnerJoin<
        TFirstJoinLeft, TFirstJoinRight,
        TSecondJoinLeft, TSecondJoinRight,
        TThirdJoinLeft, TThirdJoinRight>(this IDbConnection connection,
        Expression<Func<TFirstJoinLeft, TFirstJoinRight, bool>> firstJoin,
        Expression<Func<TSecondJoinLeft, TSecondJoinRight, bool>> secondJoin,
        Expression<Func<TThirdJoinLeft, TThirdJoinRight, bool>> thirdJoin)
        {
            return InnerJoin<TFirstJoinLeft, TFirstJoinRight, TSecondJoinLeft, TSecondJoinRight, TThirdJoinLeft, TThirdJoinRight,
                TUnsetType, TUnsetType, TUnsetType, TUnsetType, TUnsetType, TUnsetType>(connection,
                firstJoin, secondJoin, thirdJoin, null, null, null);
        }

        public static IEnumerable<ExpandoObject> InnerJoin<
           TFirstJoinLeft, TFirstJoinRight,
           TSecondJoinLeft, TSecondJoinRight,
           TThirdJoinLeft, TThirdJoinRight,
           TFourthJoinLeft, TFourthJoinRight>(this IDbConnection connection,
           Expression<Func<TFirstJoinLeft, TFirstJoinRight, bool>> firstJoin,
           Expression<Func<TSecondJoinLeft, TSecondJoinRight, bool>> secondJoin,
           Expression<Func<TThirdJoinLeft, TThirdJoinRight, bool>> thirdJoin,
           Expression<Func<TFourthJoinLeft, TFourthJoinRight, bool>> fourthJoin
   )
        {
            return InnerJoin<TFirstJoinLeft, TFirstJoinRight, TSecondJoinLeft, TSecondJoinRight, TThirdJoinLeft, TThirdJoinRight,
                TFourthJoinLeft, TFourthJoinRight, TUnsetType, TUnsetType, TUnsetType, TUnsetType>(connection,
                firstJoin, secondJoin, thirdJoin, fourthJoin, null, null);
        }

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
                Expression<Func<TFifthJoinLeft, TFifthJoinRight, bool>> fifthJoin            
        )
        {
            return InnerJoin<TFirstJoinLeft, TFirstJoinRight, TSecondJoinLeft, TSecondJoinRight, TThirdJoinLeft, TThirdJoinRight,
                TFourthJoinLeft, TFourthJoinRight, TFifthJoinLeft, TFifthJoinRight, TUnsetType, TUnsetType>(connection,
                firstJoin, secondJoin, thirdJoin, fourthJoin, fifthJoin, null);
        }

            /// <summary>
            /// Inner joins the six tables by specified six key expression lambdas.
            /// This uses a template builder and a shortcut to join two tables without having to specify any SQL manually
            /// and gives you the entire inner join result set. It is an implicit requirement that the <paramref name="firstKey"/>
            /// and <paramref name="secondKey"/> are compatible data types as they are used for the join, plus the other keys involved.
            /// This method do for now not allow specifying any filtering (where-clause) or logic around the joining besides
            /// just specifying the two columns to join.
            /// </summary>
            /// <typeparam name="TfirstTable"></typeparam>
            /// <typeparam name="TsecondTable"></typeparam>
            /// <param name="connection"></param>
            /// <param name="firstKey"></param>
            /// <param name="secondKey"></param>
            /// <returns></returns>
            public static IEnumerable<ExpandoObject> InnerJoin<
                TFirstJoinLeft, TFirstJoinRight,
                TSecondJoinLeft, TSecondJoinRight,
                TThirdJoinLeft, TThirdJoinRight,
                TFourthJoinLeft, TFourthJoinRight,
                TFifthJoinLeft, TFifthJoinRight,
                TSixthJoinLeft, TSixthJoinRight>(this IDbConnection connection,     
                Expression<Func<TFirstJoinLeft, TFirstJoinRight, bool>> firstJoin = null,
                Expression<Func<TSecondJoinLeft, TSecondJoinRight, bool>> secondJoin = null,
                Expression<Func<TThirdJoinLeft, TThirdJoinRight, bool>> thirdJoin = null,
                Expression<Func<TFourthJoinLeft, TFourthJoinRight, bool>> fourthJoin = null,
                Expression<Func<TFifthJoinLeft, TFifthJoinRight, bool>> fifthJoin = null,
                Expression<Func<TSixthJoinLeft, TSixthJoinRight, bool>> sixthJoin = null
            )
        {
            var builder = new SqlBuilder();
            string firstTableSelectClause = string.Join(",", GetPublicPropertyNames<TFirstJoinLeft>("t1"));
            string secondTableSelectClause = string.Join(",", GetPublicPropertyNames<TFirstJoinRight>("t2"));
            string thirdTableSelectClause = string.Join(",", GetPublicPropertyNames<TSecondJoinRight>("t3"));
            string fourthTableSelectClause = typeof(TThirdJoinRight) != typeof(TUnsetType) ? string.Join(",", GetPublicPropertyNames<TThirdJoinRight>("t4")) : null;
            string fifthTableSelectClause = typeof(TFourthJoinRight) != typeof(TUnsetType) ? string.Join(",", GetPublicPropertyNames<TFourthJoinRight>("t5")) : null;
            string sixthTableSelectClause = typeof(TFifthJoinRight) != typeof(TUnsetType) ? string.Join(",", GetPublicPropertyNames<TFifthJoinRight>("t6")) : null;
           
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


            string firstTableName = GetDbTableName<TFirstJoinLeft>();
            string secondTableName = GetDbTableName<TFirstJoinRight>();
            string thirdTableName = GetDbTableName<TSecondJoinRight>();
            string fourthTableName = typeof(TThirdJoinRight) != typeof(TUnsetType) ? GetDbTableName<TThirdJoinRight>() : null;
            string fifthTableName = typeof(TFourthJoinRight) != typeof(TUnsetType) ? GetDbTableName<TFourthJoinRight>() : null;
            string sixthTableName = typeof(TFifthJoinRight) != typeof(TUnsetType) ? GetDbTableName<TFifthJoinRight>() : null;

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
            joinSelectClause = joinSelectClause.TrimEnd().TrimEnd(',');

            var registeredTableAliases = new Dictionary<string, Type>();
            registeredTableAliases.Add("t1", typeof(TFirstJoinLeft));

            joinSelectClause += $" from {firstTableName} t1 /**innerjoin**/"; 
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
            var joinedResults = connection.Query(selector.RawSql, selector.Parameters)
                .Select(x => (ExpandoObject)DapperUtilsExtensions.ToExpandoObject(x)).ToList();
            return joinedResults;
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
            var results = connection.Query(sql).Select(x => (ExpandoObject) ToExpandoObject(x));
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
