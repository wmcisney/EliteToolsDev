using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using log4net;
using MathNet.Numerics.Statistics;
using Microsoft.AspNet.Identity.EntityFramework;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Envers.Configuration;
using NHibernate.Event;
using NHibernate.Impl;
using NHibernate.SqlCommand;
using NHibernate.Tool.hbm2ddl;
using RadialReview.App_Start;
//using RadialReview.Areas.CoreProcess.Models.MapModel;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Payments;
using RadialReview.Models.Periods;
using RadialReview.Models.Reviews;
using RadialReview.Models.Rocks;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Tasks;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Models.VideoConference;
using RadialReview.Models.VTO;
using RadialReview.Utilities.Constants;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.Productivity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using static RadialReview.Models.Issues.IssueModel;
using FluentConfiguration = NHibernate.Envers.Configuration.Fluent.FluentConfiguration;
using Mapping = NHibernate.Mapping;

//using Microsoft.VisualStudio.Profiler;

namespace RadialReview.Utilities {
	public static class NHSQL {
		public static string NHibernateSQL { get; set; }
		public static bool SaveCommands { get; set; }
	}
	public class NHSQLInterceptor : EmptyInterceptor, IInterceptor {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		SqlString IInterceptor.OnPrepareStatement(SqlString sql) {
			NHSQL.NHibernateSQL = sql.ToString();
			if (NHSQL.SaveCommands) {
				//log.Info(NHSQL.NHibernateSQL);
			}

			return sql;
		}
	}

	public class HibernateSession {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);



		public class RuntimeNames {
			private Configuration cfg;

			public RuntimeNames(Configuration cfg) {
				this.cfg = cfg;
			}

			public string ColumnName<T>(Expression<Func<T, object>> property)
				where T : class, new() {
				var accessor = FluentNHibernate.Utils.Reflection
					.ReflectionHelper.GetAccessor(property);

				var names = accessor.Name.Split('.');

				var classMapping = cfg.GetClassMapping(typeof(T));

				return WalkPropertyChain(classMapping.GetProperty(names.First()), 0, names);
			}

			private string WalkPropertyChain(Mapping.Property property, int index, string[] names) {
				if (property.IsComposite) {
					return WalkPropertyChain(((Mapping.Component)property.Value).GetProperty(names[++index]), index, names);
				}

				return property.ColumnIterator.First().Text;
			}

			public string TableName<T>() where T : class, new() {
				return cfg.GetClassMapping(typeof(T)).Table.Name;
			}
		}


		private static Dictionary<Env, ISessionFactory> factories;
		private static Env? CurrentEnv;
		private static String DbFile = null;

		public static void MockFactory(Env env, ISessionFactory factory) {
			if (factories.ContainsKey(env)) {
				throw new Exception("Already set factory for Env=" + env);
			}

			factories[env] = factory;
		}

		/*public static void SetDbFile(string file)
		{
			DbFile = file;
		}*/
		private static object lck = new object();
		public static ISession Session { get; set; }

		public class TestClearDispose : IDisposable {
			public Action OnDispose { get; set; }
			public Env? OldEnv { get; set; }
			public void Dispose() {
				OnDispose?.Invoke();
				//ClearSessionFactory_TestOnly(Config.GetEnv(),null);
				GetDatabaseSessionFactory(OldEnv);
				//CurrentEnv = OldEnv;
			}
		}
		static HibernateSession() {
			factories = new Dictionary<Env, ISessionFactory>();
		}
		public static RuntimeNames Names { get; private set; }

		[Obsolete("Run in a using(). Use only in synchronous environments. Built for test purposes.")]
		public static IDisposable SetDatabaseEnv_TestOnly(Env environmentOverride, Action onDispose = null) {
			lock (lck) {
				//factory = null;
				var old = CurrentEnv;
				GetDatabaseSessionFactory(environmentOverride);

				return new TestClearDispose() {
					OldEnv = old,
					OnDispose = onDispose,
				};
			}
		}

		public static ISessionFactory GetDatabaseSessionFactory(Env? environmentOverride_testOnly = null) {
			Configuration c;
			var env = environmentOverride_testOnly ?? CurrentEnv ?? Config.GetEnv();
			CurrentEnv = env;
			//if (factories == null)
			//	factories = new Dictionary<Env, ISessionFactory>();
			if (!factories.ContainsKey(env)) {
				lock (lck) {
					ChromeExtensionComms.SendCommand("dbStart");
					var config = System.Configuration.ConfigurationManager.AppSettings;
					var connectionStrings = System.Configuration.ConfigurationManager.ConnectionStrings;

					switch (environmentOverride_testOnly ?? Config.GetEnv()) {
						case Env.local_sqlite: {

								var connectionString = connectionStrings["DefaultConnectionLocalSqlite"].ConnectionString;
								var file = connectionString.Split(new String[] { "Data Source=" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(';')[0];
								DbFile = file;
								try {
									c = new Configuration();
									c.SetInterceptor(new NHSQLInterceptor());
									//SetupAudit(c);
									factories[env] = Fluently.Configure(c).Database(SQLiteConfiguration.Standard.ConnectionString(connectionString))
									.Mappings(m => {
										//m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
										//   .Conventions.Add<StringColumnLengthConvention>();
										// m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\sqlite\");
										//m.AutoMappings.Add(CreateAutomappings);
										//m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");

									})
								   .CurrentSessionContext("web")
								   .ExposeConfiguration(SetupAudit)
								   .ExposeConfiguration(x => BuildSqliteSchema(x))
								   .BuildSessionFactory();
								} catch (Exception e) {
									throw e;
								}
								break;
							}
						case Env.local_mysql: {
								try {
									c = new Configuration();
									c.SetInterceptor(new NHSQLInterceptor());
									//SetupAudit(c);
									factories[env] = Fluently.Configure(c).Database(
												MySQLConfiguration.Standard.Dialect<MySQL5Dialect>().ConnectionString(connectionStrings["DefaultConnectionLocalMysql"].ConnectionString)/*.ShowSql()*/)
									   .Mappings(m => {
										   m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
											   .Conventions.Add<StringColumnLengthConvention>();
										   //  m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\mysql\");
										   ////m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\mysql\");
										   ////m.AutoMappings.Add(CreateAutomappings);
										   ////m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");
									   })
									   .CurrentSessionContext("web")
									   .ExposeConfiguration(SetupAudit)
									   .ExposeConfiguration(BuildProductionMySqlSchema)
									   .BuildSessionFactory();
								} catch (Exception e) {
									var mbox = e.Message;
									if (e.InnerException != null && e.InnerException.Message != null) {
										mbox = e.InnerException.Message;
									}

									ChromeExtensionComms.SendCommand("dbError", mbox);
									if (e.InnerException != null && e.InnerException.Message == "Unable to connect to any of the specified MySQL hosts.") {
										throw new Exception("Could not connect to the specified database. Is your database running?", e);
									}

									throw e;
								}
								break;
							}
						case Env.production: {
								//var connectionString = connectionStrings["DefaultConnectionProduction"].ConnectionString;
								var dbCred = KeyManager.ProductionDatabaseCredentials;
								var connectionString = string.Format("Server={2};Port={3};Database={4};Uid={0};Pwd={1};", dbCred.Username, dbCred.Password, dbCred.Host, dbCred.Port, dbCred.Database);

								c = new Configuration();
								//SetupAudit(c);
								factories[env] = Fluently.Configure(c).Database(
											MySQLConfiguration.Standard.Dialect<MySQL5Dialect>().ConnectionString(connectionString).ShowSql())
								   .Mappings(m => {
									   m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
										   .Conventions.Add<StringColumnLengthConvention>();
									   //m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\mysql\");
									   //m.AutoMappings.Add(CreateAutomappings);
									   //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");
								   })
								   .CurrentSessionContext("web")
								   .ExposeConfiguration(SetupAudit)
								   .ExposeConfiguration(BuildProductionMySqlSchema)
								   .BuildSessionFactory();
								break;
							}
						case Env.local_test_sqlite: {
								//var connectionString = connectionStrings["DefaultConnectionLocalSqlite"].ConnectionString;
								//var file = connectionString.Split(new String[] { "Data Source=" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(';')[0];
								//DbFile = file;
								//var connectionString = connectionStrings["DefaultConnectionLocalSqlite"].ConnectionString;
								// var file = connectionString.Split(new String[] { "Data Source=" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(';')[0];


								string Path = "C:\\UITests";//Config.GetAppSetting("DBPATH");//System.Environment.CurrentDirectory;
								if (!Directory.Exists(Path)) {
									Directory.CreateDirectory(Path);
								}

								DbFile = Path + "\\_testdb.db";
								// string[] appPath = Path.Split(new string[] { "bin" }, StringSplitOptions.None);
								AppDomain.CurrentDomain.SetData("DataDirectory", Path);
								var connectionString = "Data Source=|DataDirectory|\\_testdb.db";
								var forceDbCreate = false;
								var useSqliteInMemory = Config.GetAppSetting("local_test_sqlite_memory", "false").ToBooleanJS();
								var useMysqlTest = Config.GetAppSetting("use_local_test_mysql", "false").ToBooleanJS();


								IPersistenceConfigurer dbConfig;
								if (useMysqlTest && useSqliteInMemory) {
									throw new Exception("Multiple database types selected. Choose either mysql test or sqliteInMemory");
								} else if (useSqliteInMemory) {
									connectionString = "FullUri=file:memorydb.db?mode=memory&cache=shared;PRAGMA journal_mode=WAL;";
									forceDbCreate = true;
									dbConfig = SQLiteConfiguration.Standard.ConnectionString(connectionString).IsolationLevel(System.Data.IsolationLevel.ReadCommitted);
								} else if (useMysqlTest) {
									connectionString = "Server=localhost; Port=3306; Database=radial-test; Uid=root; Pwd=; SslMode=none;";
									forceDbCreate = false;
									dbConfig = MySQLConfiguration.Standard.Dialect<MySQL5Dialect>().ConnectionString(connectionString);
								} else {
									dbConfig = SQLiteConfiguration.Standard.ConnectionString(connectionString).IsolationLevel(System.Data.IsolationLevel.ReadCommitted);
								}

								try {
									c = new Configuration();
									c.SetInterceptor(new NHSQLInterceptor());
									factories[env] = Fluently.Configure(c).Database(dbConfig)
									.Mappings(m => {
										m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
										   .Conventions.Add<StringColumnLengthConvention>();
									})
									.CurrentSessionContext("web")
								   .ExposeConfiguration(SetupAudit)
								   .ExposeConfiguration(x => BuildSqliteSchema(x, forceDbCreate))
								   .BuildSessionFactory();
								} catch (Exception e) {
									throw e;
								}
								break;
							}
						case Env.dev_testing: {
								//var connectionString = connectionStrings["DefaultConnectionProduction"].ConnectionString;
								var dbCred = Config.GetEnvironmentRDS();
								var connectionString = string.Format("Server={2};Port={3};Database={4};Uid={0};Pwd={1};", dbCred.Username, dbCred.Password, dbCred.Host, dbCred.Port, dbCred.Database);

								c = new Configuration();
								//SetupAudit(c);
								factories[env] = Fluently.Configure(c).Database(
											MySQLConfiguration.Standard.Dialect<MySQL5Dialect>().ConnectionString(connectionString).ShowSql())
								   .Mappings(m => {
									   m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>().Conventions.Add<StringColumnLengthConvention>();
								   })
								   .CurrentSessionContext("web")
								   .ExposeConfiguration(SetupAudit)
								   .ExposeConfiguration(BuildProductionMySqlSchema)
								   .BuildSessionFactory();
								break;
							}

						default:
							throw new Exception("No database type");
					}
					Names = new RuntimeNames(c);
					ChromeExtensionComms.SendCommand("dbComplete");
				}
			}
			return factories[env];

		}


		public static bool CloseCurrentSession() {
			var session = (SingleRequestSession)HttpContext.Current.NotNull(x => x.Items["NHibernateSession"]);
			if (session != null) {
				if (session.IsOpen) {
					session.Close();
				}

				if (session.WasDisposed) {
					session.GetBackingSession().Dispose();
				}
				HttpContext.Current.Items.Remove("NHibernateSession");
				return true;
			}
			return false;
		}

		private static SingleRequestSession GetExistingSingleRequestSession() {
			if (!(HttpContext.Current == null || HttpContext.Current.Items == null) && HttpContext.Current.Items["IsTest"] == null) {
				try {
					var session = (SingleRequestSession)HttpContext.Current.Items["NHibernateSession"];
					return session;
				} catch (Exception) {
					//Something went wrong.. revert
					//var a = "Error";
				}
			}
			return null;
		}

		public static async Task RunAfterSuccessfulDisposeOrNow(ISession waitUntilFinished, Func<ISession, ITransaction, Task> method) {
			var s = (waitUntilFinished as SingleRequestSession) ?? GetExistingSingleRequestSession();
			if (s is SingleRequestSession) {
				s.RunAfterDispose(new SingleRequestSession.OnDisposedModel(method, true));
			} else {
				using (var ss = HibernateSession.GetCurrentSession()) {
					using (var tx = ss.BeginTransaction()) {
						await method(ss, tx);
					}
				}
			}
		}

		public static ISession GetCurrentSession(bool singleSession = true, Env? environmentOverride_TestOnly = null) {

			if (singleSession && !(HttpContext.Current == null || HttpContext.Current.Items == null) && HttpContext.Current.Items["IsTest"] == null) {
				try {
					var session = GetExistingSingleRequestSession();
					if (session == null) {
						session = new SingleRequestSession(GetDatabaseSessionFactory(environmentOverride_TestOnly).OpenSession()); // Create session, like SessionFactory.createSession()...
						HttpContext.Current.Items.Add("NHibernateSession", session);
					} else {
						session.AddContext();
					}
					return session;
				} catch (Exception) {
					//Something went wrong.. revert
					//var a = "Error";
				}
			}
			if (!(HttpContext.Current == null || HttpContext.Current.Items == null) && HttpContext.Current.Items["IsTest"] != null) {
				return GetDatabaseSessionFactory(environmentOverride_TestOnly).OpenSession();
			}

			if (singleSession == false) {
				return GetDatabaseSessionFactory(environmentOverride_TestOnly).OpenSession();
			}

			return new SingleRequestSession(GetDatabaseSessionFactory(environmentOverride_TestOnly).OpenSession(), true);
			//GetDatabaseSessionFactory().OpenSession();
			/*while(true)
			{
				lock (lck)
				{
					if ( Session == null || !Session.IsOpen )
					{
						Session = GetDatabaseSessionFactory().OpenSession();
						return Session;
					}
				}
				Thread.Sleep(10);
			}*/
		}
		/*
		private static AutoPersistenceModel CreateAutomappings()
		{
			// This is the actual automapping - use AutoMap to start automapping,
			// then pick one of the static methods to specify what to map (in this case
			// all the classes in the assembly that contains Employee), and then either
			// use the Setup and Where methods to restrict that behaviour, or (preferably)
			// supply a configuration instance of your definition to control the automapper.
			return AutoMap
				.AssemblyOf<UserOrganizationModel>(new AutomappingConfiguration())
				.Conventions.Add<CascadeConvention>();
		}*/

		private static void BuildSqliteSchema(Configuration config, bool forceCreate = false) {
			// delete the existing db on each run
			if (Config.ShouldUpdateDB("BuildSqliteSchema") || forceCreate) {
				if (!File.Exists(DbFile) || forceCreate) {
					new SchemaExport(config).Create(false, true);
				} else {
					new SchemaUpdate(config).Execute(false, true);
				}
				Config.DbUpdateSuccessful();
			}

			var auditEvents = new AuditEventListener();
			config.EventListeners.PreInsertEventListeners = new IPreInsertEventListener[] { auditEvents };
			config.EventListeners.PreUpdateEventListeners = new IPreUpdateEventListener[] { auditEvents };

			// this NHibernate tool takes a configuration (with mapping info in)
			// and exports a database schema from it
		}



		private static void SetupAudit(Configuration nhConf) {

			var enversConf = new FluentConfiguration();
			nhConf.SetEnversProperty(ConfigurationKey.StoreDataAtDelete, true);
			nhConf.SetEnversProperty(ConfigurationKey.AuditStrategyValidityStoreRevendTimestamp, true);
			nhConf.SetEnversProperty(ConfigurationKey.AuditStrategy, typeof(CustomValidityAuditStrategy));


			enversConf.Audit<VtoItem>().ExcludeRelationData(x => x.Vto);
			enversConf.Audit<VtoItem_Bool>().ExcludeRelationData(x => x.Vto);
			enversConf.Audit<VtoItem_String>().ExcludeRelationData(x => x.Vto);
			enversConf.Audit<VtoItem_DateTime>().ExcludeRelationData(x => x.Vto);
			enversConf.Audit<VtoItem_Decimal>().ExcludeRelationData(x => x.Vto);
			//enversConf.Audit<Vto_Rocks>().ExcludeRelationData(x => x.Vto);

			enversConf.Audit<CoreFocusModel>();//.ExcludeRelationData(x => x.Vto);
			enversConf.Audit<MarketingStrategyModel>();//.ExcludeRelationData(x => x.Vto);
			enversConf.Audit<OneYearPlanModel>();//.ExcludeRelationData(x => x.Vto);
			enversConf.Audit<QuarterlyRocksModel>();//.ExcludeRelationData(x => x.Vto);
			enversConf.Audit<ThreeYearPictureModel>();//.ExcludeRelationData(x => x.Vto);
			enversConf.Audit<VtoModel>()
				.ExcludeRelationData(x => x.CoreFocus)
				.ExcludeRelationData(x => x.MarketingStrategy)
				.ExcludeRelationData(x => x.OneYearPlan)
				.ExcludeRelationData(x => x.QuarterlyRocks)
				.ExcludeRelationData(x => x.ThreeYearPicture);

			enversConf.Audit<TodoModel>();
			enversConf.Audit<IssueModel>();
			enversConf.Audit<ScoreModel>();
			enversConf.Audit<MeasurableModel>();
			enversConf.Audit<L10Meeting>();
			enversConf.Audit<L10Recurrence>();
			enversConf.Audit<IssueModel_Recurrence>()
				.ExcludeRelationData(x => x.CopiedFrom)
				.ExcludeRelationData(x => x.ParentRecurrenceIssue);

			enversConf.Audit<ClientReviewModel>();
			enversConf.Audit<LongModel>();
			enversConf.Audit<LongTuple>();
			enversConf.Audit<PaymentModel>();
			enversConf.Audit<PaymentPlanModel>();
			enversConf.Audit<InvoiceModel>();
			enversConf.Audit<InvoiceItemModel>();
			enversConf.Audit<QuestionCategoryModel>();
			enversConf.Audit<LocalizedStringModel>();
			enversConf.Audit<LocalizedStringPairModel>();
			enversConf.Audit<ImageModel>();

			enversConf.Audit<SurveyResponse>().Exclude(x=>x.Item).Exclude(x=>x.ItemFormat).Exclude(x=>x.About_SUN).Exclude(x=>x.By_SUN);//.ExcludeRelationData(x=>x.Item);

			enversConf.Audit<PeriodModel>();
			enversConf.Audit<ReviewModel>();
			enversConf.Audit<ReviewsModel>();
			enversConf.Audit<RockModel>();
			enversConf.Audit<Milestone>();

			enversConf.Audit<L10Recurrence.L10Recurrence_Page>()
				.ExcludeRelationData(x => x.L10Recurrence);

			enversConf.Audit<RoleModel>();
			enversConf.Audit<UserOrganizationModel>()
				.ExcludeRelationData(x => x.Groups)
				.ExcludeRelationData(x => x.ManagingGroups)
				.Exclude(x => x.Cache);
			//.ExcludeRelationData(x => x.CustomQuestions);
			enversConf.Audit<PositionDurationModel>();
			enversConf.Audit<QuestionModel>();
			enversConf.Audit<TeamDurationModel>();
			enversConf.Audit<ManagerDuration>();
			enversConf.Audit<OrganizationTeamModel>();
			enversConf.Audit<OrganizationPositionModel>();
			enversConf.Audit<PositionModel>();

			enversConf.Audit<OrganizationModel>();
			enversConf.Audit<ResponsibilityGroupModel>();
			enversConf.Audit<ResponsibilityModel>();
			enversConf.Audit<TempUserModel>();
			//enversConf.Audit<UserLookup>()
			//	.Exclude(x => x.LastLogin);
			enversConf.Audit<UserModel>();
			enversConf.Audit<UserLogin>();
			enversConf.Audit<UserRoleModel>();
			enversConf.Audit<IdentityUserClaim>();

			enversConf.Audit<PaymentSpringsToken>();
			enversConf.Audit<ScheduledTask>();


			enversConf.Audit<Dashboard>();
			enversConf.Audit<TileModel>();

			enversConf.Audit<AbstractVCProvider>();
			enversConf.Audit<ZoomUserLink>();
			enversConf.Audit<WebhookDetails>();
			enversConf.Audit<WebhookEventsSubscription>();
			/*enversConf.Audit<Task_Camunda>();
			enversConf.Audit<ProcessDef_Camunda>();
			enversConf.Audit<ProcessDef_CamundaFile>();
			enversConf.Audit<ProcessInstance_Camunda>();
			enversConf.Audit<Task_Camunda>();*/

			//enversConf.Audit<TokenIdentifier>();
			nhConf.IntegrateWithEnvers(enversConf);
		}


		private static void AddItem(List<string> list, string item, Stopwatch sw, RunningStatistics stats) {
			list.Add(item);
			stats.Push(sw.ElapsedMilliseconds);
			sw.Restart();
		}

		private static void BuildProductionMySqlSchema(Configuration config) {
			var swFull = Stopwatch.StartNew();
			//UPDATE DATABASE:
			var updates = new List<string>();
			var stats = new RunningStatistics();
			//Microsoft.VisualStudio.Profiler.DataCollection.MarkProfile(1);

			if (Config.ShouldUpdateDB("BuildProductionMySqlSchema")) {
				AuditForeignKeyInterceptor.Intercept(config, true);
				var su = new SchemaUpdate(config);
				var sw = Stopwatch.StartNew();
				su.Execute(x => AddItem(updates, x, sw, stats), true);
				Config.DbUpdateSuccessful();
				log.Info("[DatabaseUpdate] Done: Updated:" + stats.Count + "; Duration: " + swFull.ElapsedMilliseconds + "ms  Mean:" + stats.Mean + "ms  Std:" + stats.StandardDeviation + "ms");

			} else {
				//Microsoft.VisualStudio.Profiler.DataCollection.MarkProfile(3);
				log.Info("[DatabaseUpdate] Skipped. ");
			}

			/*config.DataBaseIntegration(prop => {
				prop.BatchSize = 50;
				prop.Batcher<MySqlClientBatchingBatcherFactory>();
			});*/

			var end = swFull.Elapsed;

			var auditEvents = new AuditEventListener();
			config.EventListeners.PreInsertEventListeners = new IPreInsertEventListener[] { auditEvents };
			config.EventListeners.PreUpdateEventListeners = new IPreUpdateEventListener[] { auditEvents };


			config.SetProperty("command_timeout", "600");
			//KILL/CREATE DATABASE:
			//new SchemaExport(config).Execute(true, true, false);
			// DELETE THE EXISTING DB ON EACH RUN
			/*if (!File.Exists(DbFile))
			{
				new SchemaExport(config).Create(false, true);
			}
			else
			{
				new SchemaUpdate(config).Execute(false, true);
			}*/

		}

		public static DateTime GetDbTime(ISession s) {
			switch (Config.GetDatabaseType()) {
				case Config.DbType.MySql:
					return ((DateTime)s.CreateSQLQuery("select now();").List()[0]);
				case Config.DbType.Sqlite:
					var db = s.CreateSQLQuery("select CURRENT_TIMESTAMP;").List()[0];
					if (db is DateTime) {
						return (DateTime)db;
					}

					return DateTime.ParseExact((string)db, "yyyy-MM-dd HH:mm:ss", new System.Globalization.CultureInfo("en-us"));
				default:
					throw new NotImplementedException("Db type unknown");
			}
		}
	}
}
namespace NHibernate.Criterion {
	public static class ModHelper {
		public static Int64 Mod(this Int64 numericProperty, Int64 divisor) {
			throw new Exception("Not to be used directly - use inside QueryOver expression");
		}

		internal static IProjection ProcessMod(MethodCallExpression methodCallExpression) {
			IProjection property = ExpressionProcessor.FindMemberProjection(methodCallExpression.Arguments[0]).AsProjection();
			object divisor = ExpressionProcessor.FindValue(methodCallExpression.Arguments[1]);
			return Projections.SqlFunction("mod", NHibernateUtil.Int64, property, Projections.Constant(divisor));
		}
	}
}
