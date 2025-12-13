using Hangfire.Dashboard;

namespace Wihngo.Configuration
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // In development, allow all access
            // TODO: In production, implement proper authorization (e.g., check for admin role)
            // Example production logic:
            // var httpContext = context.GetHttpContext();
            // return httpContext.User.Identity?.IsAuthenticated == true && 
            //        httpContext.User.IsInRole("Admin");
            
            return true;
        }
    }
}
