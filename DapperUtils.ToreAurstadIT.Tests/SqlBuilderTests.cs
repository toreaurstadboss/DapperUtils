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
