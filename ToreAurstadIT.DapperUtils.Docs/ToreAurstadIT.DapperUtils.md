## `AggregateFunction`

```csharp
public enum ToreAurstadIT.DapperUtils.AggregateFunction
    : Enum, IComparable, IFormattable, IConvertible

```

Enum

| Value | Name | Summary | 
| --- | --- | --- | 
| `0` | Count |  | 
| `1` | Avg |  | 
| `2` | Max |  | 
| `3` | Min |  | 
| `4` | Stdev |  | 
| `5` | Stdevp |  | 
| `6` | Sum |  | 
| `7` | Var |  | 
| `8` | Varp |  | 
| `9` | CountBig |  | 


## `DapperUtilsExtensions`

```csharp
public static class ToreAurstadIT.DapperUtils.DapperUtilsExtensions

```

Static Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `Task` | Delete(this `IDbConnection` connection, `TTable` rowToDelete) |  | 
| `Task<IEnumerable<Object>>` | DeleteMany(this `IDbConnection` connection, `IEnumerable<TTable>` rowsToDelete) |  | 
| `IEnumerable<ExpandoObject>` | GetAggregate(this `IDbConnection` connection, `Expression<Func<T, Object>>` aggregateColumn, `AggregateFunction` calculation, `Expression`1[]` groupingColumns = null, `String` tableName = null, `String` aliasForAggregate = Value) |  | 
| `Task<IEnumerable<GroupingInfo<TTable>>>` | GetGroups(this `IDbConnection` connection, `Expression`1[]` groupingkeys, `Boolean` loadItemsInGroup = True) |  | 
| `IEnumerable<T>` | GetPage(this `IDbConnection` connection, `Expression<Func<T, Object>>` orderByMember, `String` sql, `Int32` pageNumber, `Int32` pageSize, `Boolean` sortAscending = True) |  | 
| `IEnumerable<ExpandoObject>` | InnerJoin(this `IDbConnection` connection, `Expression<Func<TFirstJoinLeft, TFirstJoinRight, Boolean>>` firstJoin, `Tuple`2[]` filters = null) |  | 
| `IEnumerable<ExpandoObject>` | InnerJoin(this `IDbConnection` connection, `Expression<Func<TFirstJoinLeft, TFirstJoinRight, Boolean>>` firstJoin, `Expression<Func<TSecondJoinLeft, TSecondJoinRight, Boolean>>` secondJoin, `Tuple`2[]` filters = null) |  | 
| `IEnumerable<ExpandoObject>` | InnerJoin(this `IDbConnection` connection, `Expression<Func<TFirstJoinLeft, TFirstJoinRight, Boolean>>` firstJoin, `Expression<Func<TSecondJoinLeft, TSecondJoinRight, Boolean>>` secondJoin, `Expression<Func<TThirdJoinLeft, TThirdJoinRight, Boolean>>` thirdJoin, `Tuple`2[]` filters = null) |  | 
| `IEnumerable<ExpandoObject>` | InnerJoin(this `IDbConnection` connection, `Expression<Func<TFirstJoinLeft, TFirstJoinRight, Boolean>>` firstJoin, `Expression<Func<TSecondJoinLeft, TSecondJoinRight, Boolean>>` secondJoin, `Expression<Func<TThirdJoinLeft, TThirdJoinRight, Boolean>>` thirdJoin, `Expression<Func<TFourthJoinLeft, TFourthJoinRight, Boolean>>` fourthJoin, `Tuple`2[]` filters = null) |  | 
| `IEnumerable<ExpandoObject>` | InnerJoin(this `IDbConnection` connection, `Expression<Func<TFirstJoinLeft, TFirstJoinRight, Boolean>>` firstJoin, `Expression<Func<TSecondJoinLeft, TSecondJoinRight, Boolean>>` secondJoin, `Expression<Func<TThirdJoinLeft, TThirdJoinRight, Boolean>>` thirdJoin, `Expression<Func<TFourthJoinLeft, TFourthJoinRight, Boolean>>` fourthJoin, `Expression<Func<TFifthJoinLeft, TFifthJoinRight, Boolean>>` fifthJoin, `Tuple`2[]` filters = null) |  | 
| `IEnumerable<ExpandoObject>` | InnerJoin(this `IDbConnection` connection, `Expression<Func<TFirstJoinLeft, TFirstJoinRight, Boolean>>` firstJoin, `Expression<Func<TSecondJoinLeft, TSecondJoinRight, Boolean>>` secondJoin, `Expression<Func<TThirdJoinLeft, TThirdJoinRight, Boolean>>` thirdJoin, `Expression<Func<TFourthJoinLeft, TFourthJoinRight, Boolean>>` fourthJoin, `Expression<Func<TFifthJoinLeft, TFifthJoinRight, Boolean>>` fifthJoin, `Expression<Func<TSixthJoinLeft, TSixthJoinRight, Boolean>>` sixthJoin, `Tuple`2[]` filters = null) |  | 
| `Task<Object>` | Insert(this `IDbConnection` connection, `TTable` rowToAdd) |  | 
| `Task<IEnumerable<Object>>` | InsertMany(this `IDbConnection` connection, `IEnumerable<TTable>` rowsToAdd) |  | 
| `String` | Like(`String` searchTerm) |  | 
| `IEnumerable<T>` | ParameterizedLike(this `IDbConnection` connection, `String` sql, `String` searchTerm, `Dictionary<String, Object>` parametersDictionary) |  | 
| `IEnumerable<T>` | ParameterizedQuery(this `IDbConnection` connection, `String` sql, `Dictionary<String, Object>` parametersDictionary) |  | 
| `IEnumerable<ExpandoObject>` | ParameterizedQuery(this `IDbConnection` connection, `String` sql, `Dictionary<String, Object>` parametersDictionary) |  | 
| `ExpandoObject` | ToExpandoObject(`Object` value) |  | 
| `Task` | Update(this `IDbConnection` connection, `TTable` rowToUpdate) |  | 
| `Task<IEnumerable<Object>>` | UpdateMany(this `IDbConnection` connection, `IEnumerable<TTable>` rowsToUpdate, `IDictionary<String, Object>` propertiesToSet) |  | 


## `GroupingInfo<TTable>`

```csharp
public class ToreAurstadIT.DapperUtils.GroupingInfo<TTable>

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `String` | Key |  | 
| `IEnumerable<TTable>` | Rows |  | 
| `Int32` | TotalCount |  | 


## `ReflectionHelper`

```csharp
public static class ToreAurstadIT.DapperUtils.ReflectionHelper

```

Static Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `String` | GetColumnName(`PropertyInfo` p) |  | 
| `String` | GetColumnNameFromMemberExpression(`Expression<Func<TTAble, Object>>` member) |  | 
| `Dictionary<String, PropertyInfo>` | GetPublicProperties(`Boolean` includePropertiesMarkedAsKeyOrNotDatabaseGenerated = True, `Boolean` includePropertiesMarkedAsNotMapped = False) |  | 
| `Dictionary<String, PropertyInfo>` | GetPublicPropertiesWithKeyAttribute() |  | 


## `SqlBuilder`

```csharp
public class ToreAurstadIT.DapperUtils.SqlBuilder

```

Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `SqlBuilder` | AddClause(`String` name, `String` sql, `Object` parameters, `String` joiner, `String` prefix = , `String` postfix = , `Boolean` isInclusive = False) |  | 
| `SqlBuilder` | AddParameters(`Object` parameters) |  | 
| `Template` | AddTemplate(`String` sql, `Object` parameters = null) |  | 
| `SqlBuilder` | GroupBy(`String` sql, `Object` parameters = null) |  | 
| `SqlBuilder` | Having(`String` sql, `Object` parameters = null) |  | 
| `SqlBuilder` | InnerJoin(`String` sql, `Object` parameters = null) |  | 
| `SqlBuilder` | Intersect(`String` sql, `Object` parameters = null) |  | 
| `SqlBuilder` | Join(`String` sql, `Object` parameters = null) |  | 
| `SqlBuilder` | LeftJoin(`String` sql, `Object` parameters = null) |  | 
| `SqlBuilder` | OrderBy(`String` sql, `Object` parameters = null) |  | 
| `SqlBuilder` | OrWhere(`String` sql, `Object` parameters = null) |  | 
| `SqlBuilder` | RightJoin(`String` sql, `Object` parameters = null) |  | 
| `SqlBuilder` | Select(`String` sql, `Object` parameters = null) |  | 
| `SqlBuilder` | Set(`String` sql, `Object` parameters = null) |  | 
| `SqlBuilder` | Where(`String` sql, `Object` parameters = null) |  | 


