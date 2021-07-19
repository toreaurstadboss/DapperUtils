  
# ToreAurstadIT.DapperHelpers.NetStandard

This Nuget package contains assorted helper methods for Dapper.
To use the helper methods, add a using of the following namespace to your code first to access the 
public extension methods of the library.
```csharp
using ToreAurstadIT.DapperUtils;
```
Now you can access the methods of this lib via your IDbConnection ADO.NET DB Connection instance.
Extended readme here (or check out Fuget explorer):
<a href='https://github.com/toreaurstadboss/DapperUtils/blob/main/ExtendedReadme.md'>ExtendedReadme.md</a>

## Library methods

### Inner joins with lambda expressions 
Inner joins in Dapper has never been easier for those of us who wants to use Lambda 
expression and not write tedious manual sql. This gives us Intellisense / autocomplete
and a way to express join relations. Supported are inner joins among 2-7 tables with 
overloads. The example below shows also how filters can be applied! 

```csharp
        [Test]
        public void InnerJoinSixTablesWithFilterWithoutManualSqlReturnsExpected()
        {
            var joinedproductsandcategory = Connection.InnerJoin(
                (Order o, OrderDetail od) => o.OrderID == od.OrderID,
                (Order o, Employee e) => o.EmployeeID == e.EmployeeID,
                (OrderDetail od, Product p) => od.ProductID == p.ProductID,
                (Product p, Category c) => p.CategoryID == c.CategoryID,
                (Product p, Supplier s) => p.SupplierID == s.SupplierID,
                new Tuple<string, Type>[] { new Tuple<string, Type>("CategoryID = 1", typeof(Product)) });
            dynamic firstRow = joinedproductsandcategory.ElementAt(0);
            Assert.AreEqual(firstRow.EmployeeID + firstRow.Title + firstRow.OrderID + firstRow.ShipName + firstRow.ProductID.ToString() + firstRow.ProductName + firstRow.CategoryID + firstRow.CategoryName + firstRow.SupplierID + firstRow.CompanyName, "3Sales Representative10253Hanari Carnes39Chartreuse verte1Beverages18Aux joyeux ecclésiastiques");
        }
```

### Create Update and Delete helpers 
The lib includes generic helpers for modifying data:
* Insert
* InsertMany
* UpdateMany
* Delete
* DeleteMany

All methods are used by providing an IEnumerable of type TTable.

UpdateMany needs a property bag of type IDictionary<string, object> to specify 
which properties to set. Insert and InsertMany will also set the keyed columns values computed from the DB if the 
column is IDENTITY or COMPUTED.

```csharp
    [Test]
        public async Task InsertManyAndUpdateManyRemoveAgainPerformsExpected()
        {
            var product = new Product
            {
                ProductName = "Misvaerost",
                SupplierID = 15,
                CategoryID = 4,
                QuantityPerUnit = "300 g",
                UnitPrice = 2.70M,
                UnitsInStock = 130,
                UnitsOnOrder = 0,
                ReorderLevel = 20,
                Discontinued = false
            };
            var anotherProduct = new Product
            {
                ProductName = "Jarslbergost",
                SupplierID = 15,
                CategoryID = 4,
                QuantityPerUnit = "170 g",
                UnitPrice = 2.80M,
                UnitsInStock = 70,
                UnitsOnOrder = 0,
                ReorderLevel = 10,
                Discontinued = false
            };

            var products = new List<Product> { product, anotherProduct };
            var productIds = await Connection.InsertMany(products);
            productIds.Cast<int>().Count().Should().Be(2, "Expected to insert two rows into the DB.");
            productIds.Cast<int>().All(p => p > 0).Should().Be(true, "Expected to insert two rows into the DB with non-zero ids");

            var updatePropertyBag = new Dictionary<string, object>
            {
                { "UnitPrice", 133 },
                { "UnitsInStock", 192 }
            };

            products[0].ProductID = productIds.Cast<int>().ElementAt(0);
            products[1].ProductID = productIds.Cast<int>().ElementAt(1);

            var updatedProductsIds = await Connection.UpdateMany(products, updatePropertyBag);

            foreach (var productId in productIds.Cast<int>())
            {
                var productAfterUpdateToDelete = Connection.Query<Product>($"select * from Products where ProductID = {productId}").First();
                productAfterUpdateToDelete.UnitPrice.Should().Be(133);
                productAfterUpdateToDelete.UnitsInStock.Should().Be(192);
                await Connection.Delete(productAfterUpdateToDelete);
            }
        }
```
### Additional methods in the lib

The lib also includes methods for:
* Paginated access
* Parameterized Query
* Like operator support
* ToExpandoObject method for converting for example DapperRow to a dynamic object via ExpandoObject
* Aggregate methods on a column that supports all major aggregate functions in T-SQL such as Avg, Min, Max, Stdev, Count, Sum and so on, with lambda syntax support.

<hr />

Read the extended readme for details about usage and also check out the Fuget explorer link on Nuget.org for API details.

Last update 2021-07-19,
Tore Aurstad