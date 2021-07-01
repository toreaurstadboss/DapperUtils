﻿using System;
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

namespace DapperUtils.ToreAurstadIT.Tests
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
        public void JoinReturnsExpected()
        {
            var builder = new SqlBuilder();
            var selector = builder.AddTemplate("select p.ProductID, p.ProductName, p.CategoryID, c.CategoryName, s.SupplierID, s.City from products p /**innerjoin**/");
            builder.InnerJoin("categories c on c.CategoryID = p.CategoryID");
            builder.InnerJoin("suppliers s on p.SupplierID = s.SupplierID");
            dynamic joinedproductsandcategory = Connection.Query(selector.RawSql, selector.Parameters).Select(x => (ExpandoObject)DapperUtilsExtensions.ToExpandoObject(x)).ToList();
            var firstRow = joinedproductsandcategory[0];
            Assert.AreEqual(firstRow.ProductID + firstRow.ProductName + firstRow.CategoryID + firstRow.CategoryName + firstRow.SupplierID + firstRow.City, "1Chai1Beverages1London");
            Assert.IsNotNull(joinedproductsandcategory);
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
