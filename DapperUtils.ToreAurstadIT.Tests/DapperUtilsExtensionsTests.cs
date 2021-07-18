using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Dapper;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using FluentAssertions;
using ToreAurstadIT.DapperUtils;
using System.Threading.Tasks;

namespace ToreAurstadIT.DapperUtils.Tests
{
    /// <summary>
    /// Note - these tests required a Northwind DB running on .\\sqlexpress or adjusts the tests to go against another data source.
    /// </summary>
    [TestFixture]
    public class DapperUtilsExtensionsTests
    {
       private IConfigurationRoot Config { get; set; }
       
       private IDbConnection Connection { get; set; }

        [OneTimeSetUp]
        public void TestFixtureInitialize()
        {
            Config = SetupConfigurationFile();
            Connection = new SqlConnection(Config.GetConnectionString("Northwind"));
            Connection.Open();
        }
        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            if (Connection != null)
            {
                Connection.Close();
                Connection.Dispose();
                Connection = null;
            }
        }

        [Test]
        public void ParameterizedLikeReturnsExpected()
        {
            var sql = "select * from products where ProductName like @ProdName";
            var herrings = Connection.ParameterizedLike<Product>(sql, "sild",
                    new Dictionary<string, object> {{"@ProdName", "sild"}}).ToList();
            herrings.Count().Should().Be(2);
            string herringsSorts = string.Join(",", herrings?.Select(x => x.ProductName));
            herringsSorts.Should().Be("Rogede sild,Spegesild");
        }

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

        [Test]
        public void GetCountWithGroupingColumnReturnsExpected()
        {
            var counts = Connection.GetAggregate<Product>(p => p.UnitPrice, AggregateFunction.Count,
                new Expression<Func<Product, object>>[] { p => p.CategoryID }, tableName: "products", aliasForAggregate: "Count").ToList();
            counts.Count.Should().Be(8);
            dynamic firstCount = counts.First();
            Assert.AreEqual(firstCount.Count, 12);
            Assert.AreEqual(firstCount.CategoryID, 1);
        }

        [Test]
        public void GetSumReturnsExpected()
        {
            var sums = Connection.GetAggregate<Product>(p => p.UnitsInStock, AggregateFunction.Sum,
                tableName: "products",
                aliasForAggregate: "Sum").ToList();
            sums.Count.Should().Be(1);
            dynamic totalSum = sums.First();
            Assert.AreEqual(totalSum.Sum, 3119);
        }

        [Test]
        public void GetMinReturnsExpected()
        {
            var mins = Connection.GetAggregate<Product>(p => p.UnitsInStock, AggregateFunction.Min,
                tableName: "products",
                aliasForAggregate: "Min").ToList();
            mins.Count.Should().Be(1);
            dynamic totalSum = mins.First();
            Assert.AreEqual(totalSum.Min, 0);
        }


        [Test]
        public void GetMaxReturnsExpected()
        {
            var maxes = Connection.GetAggregate<Product>(p => p.UnitsInStock, AggregateFunction.Max,
                tableName: "products",
                aliasForAggregate: "Max").ToList();
            maxes.Count.Should().Be(1);
            dynamic totalSum = maxes.First();
            Assert.AreEqual(totalSum.Max, 125);
        }

        [Test]
        public void GetAvgReturnsExpected()
        {
            var avgs = Connection.GetAggregate<Product>(p => p.UnitsInStock, AggregateFunction.Avg,
                tableName: "products",
                aliasForAggregate: "Avg").ToList();
            avgs.Count.Should().Be(1);
            dynamic totalSum = avgs.First();
            Assert.AreEqual(totalSum.Avg, 40);
        }

        [Test]
        public void GetStDevReturnsExpected()
        {
            var stdevs = Connection.GetAggregate<Product>(p => p.UnitsInStock, AggregateFunction.Stdev,
                tableName: "products",
                aliasForAggregate: "Stdev").ToList();
            stdevs.Count.Should().Be(1);
            dynamic totalSum = stdevs.First();
            Assert.IsTrue(Math.Abs(totalSum.Stdev - 36.15f) < 0.01);
        }

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

        [Test]
        public void GetVarpReturnsExpected()
        {
            var varps = Connection.GetAggregate<Product>(p => p.UnitsInStock, AggregateFunction.Varp,
                tableName: "products",
                aliasForAggregate: "Varp").ToList();
            varps.Count.Should().Be(1);
            dynamic totalSum = varps.First();
            Assert.IsTrue(Math.Abs(totalSum.Varp - 1289.65f) < 0.01);
        }

        //Test also Dapper's ExecuteScalar built-in .. 
        [Test]
        public void GetStDevViaBuiltInReturnsExpected()
        {
            var stdev = Connection.ExecuteScalar<double>("select stdev(UnitsInStock) from products");
            Assert.IsTrue(Math.Abs(stdev - 36.15f) < 0.01);
        }

        [Test]
        public void GetVarReturnsExpected()
        {
            var vars = Connection.GetAggregate<Product>(p => p.UnitsInStock, AggregateFunction.Var,
                tableName: "products",
                aliasForAggregate: "Var").ToList();
            vars.Count.Should().Be(1);
            dynamic totalSum = vars.First();
            Assert.IsTrue(Math.Abs(totalSum.Var - 1306.62f) < 0.01);
        }

        [Test]
        public void ParameterizedQueryReturnsExpected()
        {
            string sql = "select * from products where ProductId = @ProductID";
            List<ExpandoObject> products = Connection.ParameterizedQuery(sql, new Dictionary<string, object> { { "@ProductID", 75 } }).ToList();
            Assert.IsNotNull(products);
            Assert.AreEqual(((IDictionary<string, object>)products.First())["ProductID"], 75);
            dynamic firstProduct = products.First(); 
            Assert.AreEqual(firstProduct.ProductID, 75);
        }

        [Test]
        public async Task InsertManyPerformsExpected()
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

        }

        [Test]
        public async Task InsertAndUpdateAndDeletePerformsExpected()
        {
            var product = new Product
            {
                ProductName = "Misvaerost", SupplierID = 15, CategoryID = 4, QuantityPerUnit = "300 g", UnitPrice = 2.70M,
                UnitsInStock = 130, UnitsOnOrder = 0, ReorderLevel = 20, Discontinued = false
            };
            int productId = (int) await Connection.Insert(product);
            productId.Should().BeGreaterThan(0, "Expected that the product is inserted into Products table and got a calculated product id from DB to signal a successful insert into the DB table");

            product.UnitPrice = 3.70M;
            product.UnitsInStock = 120;

            await Connection.Update(product);
            var productUpdated = Connection.Query<Product>($"select * from Products where ProductID = {product.ProductID}").Single();
            productUpdated.UnitPrice.Should().Be(3.70M);
            productUpdated.UnitsInStock.Should().Be(120);
            await Connection.Delete(product);
            var productDeleted = Connection.Query<Product>($"select * from Products where ProductID = {product.ProductID}");
            productDeleted.Should().BeEmpty("Expected that the newly created Product which was inserted and then updated is now deleted from DB.");
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
