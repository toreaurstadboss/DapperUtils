﻿## Additional README

Since Nuget only supports 8 kB length README, this Readme
is the entire Readme documentation.

  
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
 
## Inserting a row into a table with generic helper method
The following example shows how a row can be inserted to a table with generic helper method.
The columns that goes into the insert statement are the public properties. It is possible to 
exclude columns whose values are computed by specifying the [Key] attribute and further specifying 
the [DatabaseGenerated] attribute correctly. For example:

Given this POCO - Products for Northwind DB: 
```csharp 
[Table("Products")]
	public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int? SupplierID { get; set; }
        public int? CategoryID { get; set; }
        public string QuantityPerUnit { get; set; }
        public decimal? UnitPrice { get; set; }
        public short? UnitsInStock { get; set; }
        public short? UnitsOnOrder { get; set; }
        public short? ReorderLevel { get; set; }
        public bool? Discontinued { get; set; }
        [NotMapped]
        public Category Category { get; set; }
    }
```

We see that the Category properties is [NotMapped] and is to be skipped for the insert statement.
Also, the column ProductID is both a Key and the [DatabaseGenerated] attribute is set to Identity here and 
not None, so the DB will calculate the value when insert is done. You can also use the Computed enum value here for same attribute.
All public properties that is not be skipped either via [NotMapped] or that they are "keyed" as mentioned above,
will be included into an insert statement. One Insert row is done with this method.

The insert method is easy to use:

```csharp
   [Test]
   public async Task InsertReturnsExpected()
   {
            var product = new Product
            {
                ProductName = "Misvaerost", SupplierID = 15, CategoryID = 4, QuantityPerUnit = "300 g", UnitPrice = 2.70M,
                UnitsInStock = 130, UnitsOnOrder = 0, ReorderLevel = 20, Discontinued = false
            };
            int productId = await Connection.Insert(product);
            productId.Should().BeGreaterThan(0, "Expected that the product is inserted into Products table and got a calculated product id from DB to signal a successful insert into the DB table");
   }
```

The insert method is passed an instance of the TTAble table type which will be the new row to be added.
Note that this method is asynchronous and returns the ID value of this insert operation.

Note this Special case: This method will not return any value in case all your columns are non-calculated, i.e. the Primary key has DatabaseGenerated Options equal to None.
Other than that, await this insert method and get the ID of the inserted row to look up the row later if desired.


## Retrieving paginated data from DB 

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
nuget pack -Prop Configuration=Release
``` 
Then upload nuget package to nuget.org again with new version.

