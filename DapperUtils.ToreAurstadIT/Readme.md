  
### ToreAurstadIT.DapperHelpers.NetStandard

This Nuget package contains assorted helper methods for Dapper.
There are already multiple helper libraries for Dapper, but this 
library will complement missing helper methods that DapperContrib and 
DapperUtils do not include yet.

To use the helper methods, add a using of the following namespace to your code first to access the 
public extension methods of the library.

```csharp
using ToreAurstadIT.DapperUtils;
```

Now you can access the methods of this lib via your IDbConnection ADO.NET DB Connection instance.

### Inner joins via typed lambda expressions

The library supports helpers for joining 2-7 tables via lambda expresisions. The joins are for now
inner joins without filtering capability (i.e. all rows are fetched), but this will be included in future versions.
If you want to fetch less columns, just make your DTOs non-wide with few columns or add the [NotMapped] attribute 
to skip them.

The columns added to the result sets of this inner join following this rule:
<ul>
<li>Columns in the tables the left and right part of first join included in the DTO are added always.</li>
<li>Columns in the right table of the second join is included (specified as public properties in the DTO) if you join three tables with 2 joins.</li>
<li>Columns in the right table of the third join is included (specified as public properties in the DTO) if you join four tables with 3 joins.</li>
<li>Columns in the right table of the fourth join is included (specified as public properties in the DTO) if you join five tables with 4 joins.</li>
<li>Columns in the right table of the fifth join is included (specified as public properties in the DTO) if you join sixth tables with 5 joins.</li>
<li>Columns in the right table of the sixth join is included (specified as public properties in the DTO) if you join seven tables with 6 joins.</li>
</ul>

An example of inner joins via typed Lambda expression is one key feature of the library.
The following integration test against Northwind DB shows how you can inner join 
against six tables via five join expressions. The join expressions must have this 
format and make note that we select ALL columns from the tables via all the public properties
on your DTO. If you want to specify another tablename for the DTO, use the [Table] attribute
from System.ComponentModel.DataAnnotations.Schema namespace. If you want to ignore a property, add the
[NotMapped] attribute to the property (column) to skip from the result set.

I will add a filtering capability on this join method and other join types such as left outer joins in the future.

The following example shows how you can join six tables via typed lambda expressions with helper method *InnerJoin*. 

```csharp

        [Test]
        public void InnerJoinSixTablesWithoutManualSqlReturnsExpected()
        {
            var joinedproductsandcategory = Connection.InnerJoin(
                (Order o, OrderDetail od) => o.OrderID == od.OrderID,
                (Order o, Employee e) => o.EmployeeID == e.EmployeeID,
                (OrderDetail od, Product p) => od.ProductID == p.ProductID,
                (Product p, Category c) => p.CategoryID == c.CategoryID,
                (Product p, Supplier s) => p.SupplierID == s.SupplierID);
            dynamic firstRow = joinedproductsandcategory.ElementAt(0);
            Assert.AreEqual(firstRow.EmployeeID + firstRow.Title + firstRow.OrderID + firstRow.ShipName + firstRow.ProductID.ToString() + firstRow.ProductName + firstRow.CategoryID + firstRow.CategoryName + firstRow.SupplierID + firstRow.CompanyName, "5Sales Manager10248Vins et alcools Chevalier11Queso Cabrales4Dairy Products5Cooperativa de Quesos 'Las Cabras'");
        }

```

### Adding a filter to the inner join
The following example shows how a filter can be added. Do not add a table alias, but specify the 
type of the filter (i.e the table the filter is targeting) and the sql itself. Here, Lambda expressions are not supported, so you have to add the 
filter manually as shown in the example. 

```csharp
        [Test]
        public void InnerJoinTwoTablesWithFilterWithoutManualSqlReturnsExpected()
        {
            var joinedproductsandcategory = Connection.InnerJoin((Product p, Category c) => p.CategoryID == c.CategoryID,
                   new Tuple<string, Type>[] { new Tuple<string, Type>("CategoryID = 4", typeof(Product)) });
            dynamic firstRow = joinedproductsandcategory.ElementAt(0);
            Assert.AreEqual(firstRow.ProductID + firstRow.ProductName + firstRow.CategoryID + firstRow.CategoryName, "11Queso Cabrales4Dairy Products");
        }
```

# Retrieving paginated data from DB 

This example shows how you can retrieve a page from a result set via the helper method *GetPage*

```csharp
        [Test]
        public void GetPageReturnsExpected()
        {
            var sql = $"select * from products";
            var productPage = Connection.GetPage<Product>(m => m.ProductID, sql, 0, 5, sortAscending: true).ToList();
            Assert.IsNotNull(productPage);
            productPage?.Should().NotBeEmpty();
            productPage?.Count().Should().Be(5);
            string productIds = string.Join(",", productPage?.Select(x => x.ProductID));
            productIds.Should().Be("1,2,3,4,5");
        }

```

# Calculating aggregating values for columns 

The following example shows how you can calculate aggregate values for columns with lambda syntax.


```csharp
        [Test]
        public void GetStDevpReturnsExpected()
        {
            var stdevps = Connection.GetAggregate<Product>(p => p.UnitsInStock, AggregateFunction.Stdevp,
                tableName: "products",
                aliasForAggregate: "Stdevp").ToList();
            stdevps.Count.Should().Be(1);
            dynamic totalSum = stdevps.First();
            Assert.IsTrue(Math.Abs(totalSum.Stdevp - 35.92f) < 0.01);
        }
```

The following aggregate methods are supported (taken for source code extract of the lib). 

```csharp
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
```

<hr />
<br />
*Reminder
 Build new versions of this lib*

Edit the .nuspec file and bump versions and run:

```bash 
nuget pack
``` 
Then upload nuget package to nuget.org again with new version.

