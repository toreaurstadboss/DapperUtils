  
### ToreAurstadIT.DapperHelpers.NetStandard

This Nuget package contains assorted helper methods for Dapper.
There are already multiple helper libraries for Dapper, but this 
library will complement missing helper methods that DapperContrib and 
DapperUtils do not include yet.

To use the helper methods, add a using of the following namespace to your code first to access the 
public extension methods of the library.

```csharp
using DapperUtils.ToreAurstadIT;
```

Now you can access the methods of this lib via your IDbConnection ADO.NET DB Connection instance.

### Inner joins via typed lambda expressions

The library supports helpers for joining 2-7 tables via lambda expresisions. The joins are for now
inner joins without filtering capability (i.e. all rows are fetched), but this will be included in future versions.
If you want to fetch less columns, just make your DTOs non-wide with few columns or add the [NotMapped] attribute 
to skip them.

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

<hr />
<br />
*Reminder
 Build new versions of this lib*

Edit the .nuspec file and bump versions and run:

```bash 
nuget pack
``` 
Then upload nuget package to nuget.org again with new version.

