using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Description;
using EPiServer.ServiceApi.Configuration;
using Swashbuckle.Swagger;

namespace EPiServer.Reference.Commerce.Site.Infrastructure.WebApi
{
    public class AuthorizationHeaderOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            var isSecured = apiDescription.ActionDescriptor.GetCustomAttributes<AuthorizePermissionAttribute>().Any();

            if (!isSecured) return;

            if (operation.parameters == null)
                operation.parameters = new List<Parameter>();

            operation.parameters.Add(new Parameter()
            {
                name = "Authorization",
                @in = "header",
                description = "Access token",
                required = true,
                type = "string"
            });
        }
    }
}