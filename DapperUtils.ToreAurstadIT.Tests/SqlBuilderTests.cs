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

namespace ToreAurstadIT.DapperUtils.Tests
{
    [TestFixture]
    public class SqlBuilderTests
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
        public void WhereClauseReturnsExpected()
        {
            var builder = new SqlBuilder();
            var selector = builder.AddTemplate("select * from products /**where**/");
            builder.Where("UnitPrice > @UnitPrice", new { UnitPrice = 50 }).Where("CategoryID = @CategoryID", new { CategoryID = 6 });
            var priceyItems = Connection.Query<Product>(selector.RawSql, selector.Parameters);
        }

        [Test]
        public void InnerJoinWithManualSqlReturnsExpected()
        {
            var builder = new SqlBuilder();
            var selector = builder.AddTemplate("select p.ProductID, p.ProductName, p.CategoryID, c.CategoryName, s.SupplierID, s.City from products p /**innerjoin**/");
            builder.InnerJoin("categories c on c.CategoryID = p.CategoryID");
            builder.InnerJoin("suppliers s on p.SupplierID = s.SupplierID");
            dynamic joinedproductsandcategoryandsuppliers = Connection.Query(selector.RawSql, selector.Parameters).Select(x => (ExpandoObject)DapperUtilsExtensions.ToExpandoObject(x)).ToList();
            var firstRow = joinedproductsandcategoryandsuppliers[0];
            Assert.AreEqual(firstRow.ProductID + firstRow.ProductName + firstRow.CategoryID + firstRow.CategoryName + firstRow.SupplierID + firstRow.City, "1Chai1Beverages1London");
        }

        [Test]
        public void InnerJoinTwoTablesWithoutManualSqlReturnsExpected()
        {
            var joinedproductsandcategory = Connection.InnerJoin((Product p, Category c) => p.CategoryID == c.CategoryID);
            dynamic firstRow = joinedproductsandcategory.ElementAt(0);
            Assert.AreEqual(firstRow.ProductID + firstRow.ProductName + firstRow.CategoryID + firstRow.CategoryName, "1Chai1Beverages");
        }

        [Test]
        public void InnerJoinTwoTablesWithFilterWithoutManualSqlReturnsExpected()
        {
            var joinedproductsandcategory = Connection.InnerJoin((Product p, Category c) => p.CategoryID == c.CategoryID,
                   new Tuple<string, Type>[] { new Tuple<string, Type>("CategoryID = 4", typeof(Product)) });
            dynamic firstRow = joinedproductsandcategory.ElementAt(0);
            Assert.AreEqual(firstRow.ProductID + firstRow.ProductName + firstRow.CategoryID + firstRow.CategoryName, "11Queso Cabrales4Dairy Products");
        }

        [Test]
        public void InnerJoinThreeTablesWithoutManualSqlReturnsExpected()
        {
            var joinedproductsandcategory = Connection.InnerJoin((Product p, Category c) => p.CategoryID == c.CategoryID,
                (Product p, Supplier s) => p.SupplierID == s.SupplierID);
            dynamic firstRow = joinedproductsandcategory.ElementAt(0);
            Assert.AreEqual(firstRow.ProductID + firstRow.ProductName + firstRow.CategoryID + firstRow.CategoryName + firstRow.SupplierID + firstRow.CompanyName, "1Chai1Beverages1Exotic Liquids");
        }

        [Test]
        public void InnerJoinFourTablesWithoutManualSqlReturnsExpected()
        {
            var joinedproductsandcategory = Connection.InnerJoin((OrderDetail od, Product p) => od.ProductID == p.ProductID,
                (Product p, Category c) => p.CategoryID == c.CategoryID,
                (Product p, Supplier s) => p.SupplierID == s.SupplierID);
            dynamic firstRow = joinedproductsandcategory.ElementAt(0);
            Assert.AreEqual(firstRow.ProductID.ToString() + firstRow.ProductName + firstRow.CategoryID + firstRow.CategoryName + firstRow.SupplierID + firstRow.CompanyName, "11Queso Cabrales4Dairy Products5Cooperativa de Quesos 'Las Cabras'");
        }

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
