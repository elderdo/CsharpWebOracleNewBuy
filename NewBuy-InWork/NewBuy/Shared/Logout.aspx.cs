using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.OracleClient;
using System.Web.SessionState;
using NewBuy.Helpers;
using NewBuy.DbInterface;
using System.Web.Script.Serialization;
using System.Web.Services;

namespace NewBuy.Shared
{
    public partial class Logout : System.Web.UI.Page
    {
        protected void Page_Init(object sender, EventArgs e)
        { 
            //Expire current session
            Session.Clear();
            Session.Abandon();

            //Redirect user
            Response.Redirect("~/Default.aspx");
        }
        
        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}