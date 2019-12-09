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

namespace NewBuy
{
    public partial class Default : System.Web.UI.Page
    {       
        
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Login button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Submit_Click(object sender, EventArgs e)
        {
            //Create connection class instance
            using (Interface db = new Interface())
            {
                //Clear last error message
                LoginFeedback.Text = "";

                //Get db environment
                string dbEnvironment = "";
                switch (Database.SelectedValue)
                {
                    case "Dev":
                        {
                            dbEnvironment = "descm";
                            break;
                        }
                    case "Test":
                        {
                            dbEnvironment = "tescm";
                            break;
                        }
                    case "Prod":
                        {
                            dbEnvironment = "escm";
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

                //Set session info
                try
                {
                    db.setSessionConnectionInfo(dbEnvironment, Username.Text, Password.Text);
                    db.setSessionPermissionInfo();
                }
                catch (OracleException oe)
                {
                    switch (oe.Code)
                    {
                        case 1017:
                        case 1005:
                            {
                                LoginFeedback.Text = "Invalid Username or Passwordy.";
                                break;
                            }
                        case 12154:
                            {
                                LoginFeedback.Text = "TNS was unable to find the database.";
                                break;
                            }
                        case 12526:
                            {
                                LoginFeedback.Text = "Database is currently restricted for maintenance. Please check back later.";
                                break;
                            }
                        case 28001:
                            {
                                LoginFeedback.Text = "Password has expired.  Please change.";
                                break;
                            }
                        default:
                            {
                                //Return error to user
                                LoginFeedback.Text = "Error logging in. Please try again or notify application administrator.   Error code is: " + oe.Code +
                                                      oe.ToString(); //For Debugging";;
                                break;
                            }
                    }
                }
                catch (Exception)
                {
                    //Return error
                    LoginFeedback.Text = e.ToString(); //For Debugging
                    LoginFeedback.Text = "Error logging in. Please try again or notify application administrator.";
                }

                //Add styling if error
                if (!String.IsNullOrEmpty(LoginFeedback.Text))
                {
                    LoginFeedback.CssClass = "ui-state-error";
                }
            }
        }

        protected void Database_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}