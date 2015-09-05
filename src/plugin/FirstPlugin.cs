using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace PROJECT.Plugins
{
    public class FirstPlugin : IPlugin
    {
        ITracingService _tracing;
        public void Execute(IServiceProvider serviceProvider)
        {
            _tracing = (ITracingService) serviceProvider.GetService(typeof (ITracingService));
            var context = (IPluginExecutionContext) serviceProvider.GetService(typeof (IPluginExecutionContext));
            var factory = (IOrganizationServiceFactory) serviceProvider.GetService(typeof (IOrganizationServiceFactory));
            var service = factory.CreateOrganizationService(context.UserId);

            if (!IsValidContext(context))
            {
                return;
            }

            var target = ((Entity) context.InputParameters["Target"]);

            //using (var crmContext = new CrmContext(service))
            //{
            //    // Your code here
            //}
        }

        /// <summary>
        /// Check the plugin registration
        /// </summary>
        /// <returns>False, if registered on wrong entity, otherwise true</returns>
        private bool IsValidContext(IPluginExecutionContext context)
        {
            _tracing.Trace("Checking for Target");
            // The InputParameters collection contains all the data passed in the message request.);
            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity))
            {
                return false;
            }

            _tracing.Trace("Checking PrimaryEntityName");
            if (context.PrimaryEntityName != "")
            {
                return false;
            }

            _tracing.Trace("Checking context MessageName");
            if (context.MessageName != "Create" && context.MessageName != "Update")
            {
                return false;
            }

            _tracing.Trace("Checking vor Offline");
            if (context.IsExecutingOffline)
            {
                return false;
            }

            _tracing.Trace("Context is valid");
            return true;
        }
    }
}