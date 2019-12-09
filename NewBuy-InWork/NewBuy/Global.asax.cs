using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using NewBuy.DbInterface;
using NewBuy.Helpers;

namespace NewBuy
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            
        }

        protected void Session_Start(object sender, EventArgs e)
        {            
            //Sets Default Session timeout length (in minutes)
            Session.Timeout = 60;
#if TESTING
            HttpContext.Current.Session["cls"] = true;
            HttpContext.Current.Session["ccad"] = true;
            HttpContext.Current.Session["uk"] = true;
            HttpContext.Current.Session["rev"] = true;
#endif

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {            
            //Redirect to https
            if (!HttpContext.Current.Request.IsSecureConnection && !HttpContext.Current.Request.IsLocal)
            {
                var environmentHost = Helper.ValidHostIndex(HttpContext.Current.Request.Url.DnsSafeHost);
                var newUrl = "https://" + Helper.ValidHosts[environmentHost] + VirtualPathUtility.ToAbsolute("~/Default.aspx");
                
                //Redirect to secure home page
                Response.Redirect(newUrl, true);
            }
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {
            // Code that runs when a session ends. 
            // Note: The Session_End event is raised only when the sessionstate mode
            // is set to InProc in the Web.config file. If session mode is set to StateServer 
            // or SQLServer, the event is not raised.

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}