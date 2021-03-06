﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SQLite;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using System.Reflection;
using NHibernate.Tool.hbm2ddl;
using RadialReview.Models;
using RadialReview.Accessors;
using RadialReview.Utilities.Productivity;
using System.Threading;
using Hangfire;
using RadialReview.Utilities;
using Hangfire.Pro.Redis;

namespace TractionTools.Tests.Startup {
	[TestClass]
	public static class SetupAssemblyInitializer {
        private const string ConnectionString = "FullUri=file:memorydb.db?mode=memory&cache=shared";//"file::memory:?cache=shared";//;

        private static SQLiteConnection _connection;

		[AssemblyInitialize]
		public static void AssemblyInit(TestContext context) {
			//ReconfigureSqlite();

			GlobalConfiguration.Configuration.UseRedisStorage(Config.GetHangfireConnectionString(), new RedisStorageOptions() {
				InvisibilityTimeout = TimeSpan.FromHours(3)
			});
		}

        public static void ReconfigureSqlite() {

           //if (_connection != null) {
           //     //_connection.Cancel();
           //     _connection.Close();
           //     GC.Collect();
           //     GC.WaitForPendingFinalizers();
           //     _connection.Dispose();
           //     _connection = null;
           // }
            var configuration = Fluently.Configure()
                                           .Database(SQLiteConfiguration.Standard.ConnectionString(ConnectionString))
                                           .Mappings(m => m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>())
                                           .ExposeConfiguration(x => x.SetProperty("current_session_context_class", "call"))
                                           .ExposeConfiguration(x => x.SetProperty("release_mode", "on_close"))
                                           .BuildConfiguration();

            // Create the schema in the database
            // Because it's an in-memory database, we hold this connection open until all the tests are finished
            var schemaExport = new SchemaExport(configuration);
            _connection = new SQLiteConnection(ConnectionString);
            _connection.Open();
            schemaExport.Execute(false, true, false, _connection, null);

            ApplicationAccessor.EnsureApplicationExists();
        }

		[AssemblyCleanup]
		public static void AssemblyTearDown() {
			ChromeExtensionComms.SendCommand("testDone");

			Thread.Sleep(200);

			if (_connection != null) {
				_connection.Dispose();
				_connection = null;
			}
		}
	}
}
