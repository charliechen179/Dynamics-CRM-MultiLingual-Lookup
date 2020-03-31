
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.ServiceModel;

namespace MLConfiguration
{
    public class Plugin : IPlugin
    {
        private Collection<Tuple<int, string, string, Action<Plugin.LocalPluginContext>>> registeredEvents;

        protected Collection<Tuple<int, string, string, Action<Plugin.LocalPluginContext>>> RegisteredEvents
        {
            get
            {
                if (this.registeredEvents == null)
                    this.registeredEvents = new Collection<Tuple<int, string, string, Action<Plugin.LocalPluginContext>>>();
                return this.registeredEvents;
            }
        }

        protected string ChildClassName { get; private set; }

        internal Plugin(Type childClassName)
        {
            this.ChildClassName = childClassName.ToString();
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));
            Plugin.LocalPluginContext localcontext = new Plugin.LocalPluginContext(serviceProvider);
            localcontext.Trace(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Entered {0}.Execute()", (object)this.ChildClassName));
            try
            {
                Action<Plugin.LocalPluginContext> action = this.RegisteredEvents.Where<Tuple<int, string, string, Action<Plugin.LocalPluginContext>>>((Func<Tuple<int, string, string, Action<Plugin.LocalPluginContext>>, bool>)(a =>
                {
                    if (a.Item1 != localcontext.PluginExecutionContext.Stage || !(a.Item2 == localcontext.PluginExecutionContext.MessageName))
                        return false;
                    if (!string.IsNullOrWhiteSpace(a.Item3))
                        return a.Item3 == localcontext.PluginExecutionContext.PrimaryEntityName;
                    return true;
                })).Select<Tuple<int, string, string, Action<Plugin.LocalPluginContext>>, Action<Plugin.LocalPluginContext>>((Func<Tuple<int, string, string, Action<Plugin.LocalPluginContext>>, Action<Plugin.LocalPluginContext>>)(a => a.Item4)).FirstOrDefault<Action<Plugin.LocalPluginContext>>();
                if (action == null)
                    return;
                localcontext.Trace(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "{0} is firing for Entity: {1}, Message: {2}", (object)this.ChildClassName, (object)localcontext.PluginExecutionContext.PrimaryEntityName, (object)localcontext.PluginExecutionContext.MessageName));
                action(localcontext);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                localcontext.Trace(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Exception: {0}", (object)ex.ToString()));
                throw;
            }
            finally
            {
                localcontext.Trace(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Exiting {0}.Execute()", (object)this.ChildClassName));
            }
        }

        protected class LocalPluginContext
        {
            internal IServiceProvider ServiceProvider { get; private set; }

            internal IOrganizationService OrganizationService { get; private set; }

            internal IPluginExecutionContext PluginExecutionContext { get; private set; }

            internal ITracingService TracingService { get; private set; }

            private LocalPluginContext()
            {
            }

            internal LocalPluginContext(IServiceProvider serviceProvider)
            {
                if (serviceProvider == null)
                    throw new ArgumentNullException(nameof(serviceProvider));
                this.PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                this.TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
                this.OrganizationService = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(new Guid?(this.PluginExecutionContext.UserId));
            }

            internal void Trace(string message)
            {
                if (string.IsNullOrWhiteSpace(message) || this.TracingService == null)
                    return;
                if (this.PluginExecutionContext == null)
                    this.TracingService.Trace(message);
                else
                    this.TracingService.Trace("{0}, Correlation Id: {1}, Initiating User: {2}", (object)message, (object)this.PluginExecutionContext.CorrelationId, (object)this.PluginExecutionContext.InitiatingUserId);
            }
        }
    }
}
