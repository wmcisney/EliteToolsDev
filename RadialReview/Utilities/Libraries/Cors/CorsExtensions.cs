using Owin;
using System;
using RadialReview.Utilities.Libraries.Cors;

namespace RadialReview.Utilities.Libraries.Cors {
	/// <summary>
	/// Extension methods for adding CorsMiddleware to an application pipeline.
	/// </summary>
	public static class CorsExtensions {
		/// <summary>
		/// Adds a CORS middleware to your web application pipeline to allow cross domain requests.
		/// </summary>
		/// <param name="app">The IAppBuilder passed to your configuration method</param>
		/// <param name="options">An options class that controls the middleware behavior</param>
		/// <returns>The original app parameter</returns>
		public static IAppBuilder UseTTCors(this IAppBuilder app, CorsOptions options) {
			if (app == null) {
				throw new ArgumentNullException("app");
			}

			if (options == null) {
				throw new ArgumentNullException("options");
			}

			return app.Use(typeof(CorsMiddleware), options);
		}

	}
}