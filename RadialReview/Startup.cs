using log4net;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;
using RadialReview.Utilities;
using RadialReview.Utilities.Libraries.Cors;
using RadialReview.Utilities.Security;
using System;
using System.Threading.Tasks;

[assembly: OwinStartupAttribute(typeof(RadialReview.Startup))]
namespace RadialReview {

	public partial class Startup {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public void Configuration(IAppBuilder app) {
			ConfigureAuth(app);
			var redis = Config.RedisSignalR("EliteTools-SignalR");
			var redisConfig = new RedisScaleoutConfiguration(redis.Server, redis.Port, redis.Password, redis.ChannelName) {
				MaxQueueLength = 50000
			};
			GlobalHost.DependencyResolver.UseRedis(redisConfig/*redis.Server, redis.Port, redis.Password, redis.ChannelName,*/);
			//app.MapSignalR();


			app.Map("/signalr", map => {
				// Setup the CORS middleware to run before SignalR.
				// By default this will allow all origins. You can 
				// configure the set of origins and/or http verbs by
				// providing a cors options with a different policy.
				//throw new NotImplementedException();
				map.UseTTCors(CorsOptions.AllowAll);// SignalrCorsOptions.Value);
				var hubConfiguration = new HubConfiguration {
					// You can enable JSONP by uncommenting line below.
					// JSONP requests are insecure but some older browsers (and some
					// versions of IE) require JSONP to work cross domain
					EnableDetailedErrors = Config.IsLocal(),
					EnableJSONP = true
				};

				// Run the SignalR pipeline. We're not using MapSignalR
				// since this branch already runs under the "/signalr"
				// path.
				map.RunSignalR(hubConfiguration);
				
			});

		}

		private static Lazy<CorsOptions> SignalrCorsOptions = new Lazy<CorsOptions>(() => {
			return new CorsOptions {
				PolicyProvider = new CorsPolicyProvider {
					PolicyResolver = new Func<IOwinRequest, Task<CorsPolicy>>(context => {
						var policy = new CorsPolicy();
						policy.AllowAnyOrigin = false;
						try {
							string allowedOrigin = null;
							if (CorsUtility.TryGetAllowedOrigin(context.Uri, out allowedOrigin)) {
								policy.Origins.Add(allowedOrigin);
								policy.AllowAnyMethod = true;
								policy.AllowAnyHeader = true;
								policy.SupportsCredentials = false;
							} else {
								log.Info("Cors Denied:" + context.Uri);
							}
						} catch (Exception e) {
							log.Error(e);
						}

						return Task.FromResult(policy);
					})
				}
			};
		});
	}
}
