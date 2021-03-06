﻿using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebLocation.Startup))]
namespace WebLocation
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            app.Map("/realtime", map =>
            {
                map.UseCors(CorsOptions.AllowAll);
                var config = new HubConfiguration()
                {
                    EnableJSONP = true,
                    EnableJavaScriptProxies = false
                };
#if DEBUG
                config.EnableDetailedErrors = true;
#endif
                map.RunSignalR(config);
            });
        }
    }
}
