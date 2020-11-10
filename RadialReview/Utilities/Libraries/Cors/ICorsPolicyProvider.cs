using System.Threading.Tasks;
using Microsoft.Owin;

namespace RadialReview.Utilities.Libraries.Cors {
	/// <summary>
	/// Defines how to select a CORS policy for a given request.
	/// </summary>
	public interface ICorsPolicyProvider {
		/// <summary>
		/// Selects a CORS policy to apply for the given request.
		/// </summary>
		/// <param name="request"></param>
		/// <returns>The CORS policy to apply to the request, or null if no policy applies and
		/// the request should be passed through to the next middleware.</returns>
		Task<CorsPolicy> GetCorsPolicyAsync(IOwinRequest request);
	}
}