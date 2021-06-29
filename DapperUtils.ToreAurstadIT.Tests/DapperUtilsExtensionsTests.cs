using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using FluentAssertions;

namespace DapperUtils.ToreAurstadIT.Tests
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
        public void ParameterizedQueryReturnsExpected()
        {
            string sql = "select * from products where ProductId = @ProductID";
            List<ExpandoObject> products = Connection.ParameterizedQuery(sql, new Dictionary<string, object> { { "@ProductID", 75 } }).ToList();
            Assert.IsNotNull(products);
            Assert.AreEqual(((IDictionary<string, object>)products.First())["ProductID"], 75);
            dynamic firstProduct = products.First(); 
            Assert.AreEqual(firstProduct.ProductID, 75);
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
