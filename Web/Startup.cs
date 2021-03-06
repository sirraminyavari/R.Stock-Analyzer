using System;
using Microsoft.Owin;
using Owin;
using RaaiVan.Web.Ajax;
using RaaiVan.Modules.GlobalUtilities;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using RaaiVan.Web;

[assembly: OwinStartup(typeof(Startup))]

namespace RaaiVan.Web
{
    /// <summary>
    /// The server needs to know which URL to intercept and direct to SignalR. To do that we add an OWIN startup class.
    /// </summary>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            RVScheduler.run_jobs();

            //Ignore SSL certificate check for web requests
            ServicePointManager.ServerCertificateValidationCallback = delegate (object s,
                X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };
            //end of Ignore SSL certificate check for web requests

            ConfigureAuth(app);
        }

        public void ConfigureAuth(IAppBuilder app)
        {
            app.UseTenantCore(new HostNameTenantResolver());
        }
    }
}
