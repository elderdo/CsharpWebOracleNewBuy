using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewBuy.Helpers;
using NewBuy.DbInterface;
using NewBuy.Properties;

namespace NewBuy
{
    public partial class NewBuy : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {            
            
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            //If logged in, display username
            if (HttpContext.Current.Session["Connection"] != null)
            {
                User.Text = HttpUtility.HtmlEncode(Helper.getSessionConnectionInfoById(1)) + " @ " + HttpUtility.HtmlEncode(Helper.getSessionConnectionInfoById(0).ToUpper());
            }
        }

        /// <summary>
        /// Gets Application last update date using supplied style code
        /// </summary>
        /// <param name="style">false - Returns MM/DD/YYYY; true - Returns meta tag style YYYY-MM-DD</param>
        /// <returns>formatted string date</returns>
        protected string GetLastUpdateDate(bool metaStyle)
        {
            string result = "";
            
            switch (metaStyle)
            {
                case false:
                    result = Settings.Default.LastUpdated.ToString("MM/dd/yyyy");
                    break;
                case true:
                    result = Settings.Default.LastUpdated.ToString("yyyy-MM-dd");
                    break;
                default:
                    break;
            }
            
            return result;
        }
    }
}