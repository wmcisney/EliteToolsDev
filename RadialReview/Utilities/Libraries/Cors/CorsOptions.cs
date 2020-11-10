using RadialReview.Utilities.Libraries.Cors;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Libraries.Cors {

	/// <summary>
	/// Contains the options used by the CorsMiddleware
	/// </summary>
	public class CorsOptions {
		/// <summary>
		/// A policy that allows all headers, all methods, any origin and supports credentials
		/// </summary>
		public static CorsOptions AllowAll {
			get {
				// Since we can't prevent this from being mutable, just create a new one everytime.
				return new CorsOptions {
					PolicyProvider = new CorsPolicyProvider {
						PolicyResolver = context => Task.FromResult(new CorsPolicy {
							AllowAnyHeader = true,
							AllowAnyMethod = true,
							AllowAnyOrigin = true,
							SupportsCredentials = true
						})
					}
				};
			}
		}

		/// <summary>
		/// The cors policy to apply
		/// </summary>
		public ICorsPolicyProvider PolicyProvider { get; set; }

		/// <summary>
		/// The cors engine
		/// </summary>
		public ICorsEngine CorsEngine { get; set; }
	}

}