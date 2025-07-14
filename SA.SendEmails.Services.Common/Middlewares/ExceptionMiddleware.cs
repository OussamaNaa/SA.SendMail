using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace SA.SendEmails.Services.Common.Middlewares
{
    public class ExceptionMiddleware
    {
        #region Fields

        private readonly RequestDelegate requestDelegate;

        private readonly System.ILogger logger;

        #endregion Fields

        #region Constructors

        public ExceptionMiddleware(RequestDelegate requestDelegate, System.ILogger logger)
        {
            this.requestDelegate = requestDelegate;
            this.logger = logger;
        }

        #endregion Constructors

        #region Methods

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await this.requestDelegate(httpContext);
            }
            catch (Exception exception)
            {
                this.logger.Fatal(string.Format("An exception was raised: {0}", exception));

                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                httpContext.Response.ContentType = "text/plain";

                await httpContext.Response.WriteAsync("Internal server error.");
            }
        }

        #endregion Methods
    }

    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomException(this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}