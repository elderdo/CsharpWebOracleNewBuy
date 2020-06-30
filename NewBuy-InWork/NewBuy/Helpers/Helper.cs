using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using System.Text;
using NewBuy.DbInterface;

namespace NewBuy.Helpers
{
    /// <summary>
    /// Custom Helpers
    /// </summary>
    /// <author>ESCM Mesa</author>
    /// <copyright>Copyright © 2012 The Boeing Company</copyright>
    public static class Helper
    {
        /// <summary>
        /// List of valid application host names
        /// </summary>
        public static readonly List<string> ValidHosts = new List<string>(new string[] {"escm-mesa-dev.web.boeing.com",
                    "escm-mesa-test.web.boeing.com",
                    "escm-mesa.web.boeing.com"});
        
        public static int ValidHostIndex(string hostname)
        {
            var host = hostname;
            
            if (!ValidHosts.Contains(host))
            {
                host = "escm-mesa.web.boeing.com";
            }

            return ValidHosts.IndexOf(host);
        }

        /// <summary>
        /// Validate Raw Url
        /// </summary>
        /// <param name="rawUrl"></param>
        /// <returns></returns>
        public static bool ValidateUrl(string rawUrl)
        {
            var url = rawUrl;
            
            return ((url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))) || // "/" or "/foo" but not "//" or "/\"
                (url.Length > 1 && url[0] == '~' && url[1] == '/') // "~/" or "~/foo"
            );   
        }
        
        /// <summary>
        /// Parse single value from session connection information by id.
        /// id: 0 = Data Source
        /// id: 1 = Username
        /// </summary>
        /// <returns>
        /// String containing session info by id,
        /// returns "error" if invalid.
        /// </returns>
        public static string getSessionConnectionInfoById(int id)
        {
            //Initialize variables
            int valueId = id;
            string returnValue = "error";

            //Check session key for null
            if (HttpContext.Current.Session["Connection"] != null)
            {
                //Check param
                if ((valueId == 0) ||
                    (valueId == 1))
                {
                    //Get session info by key
                    string sessionConnString = HttpContext.Current.Session["Connection"].ToString();

                    //Check for correct number of values
                    string[] sessionTest = sessionConnString.Split(';');
                    if (sessionTest.Length == 3)
                    {
                        returnValue = sessionTest[valueId];
                    }
                }
            }

            //Return connection string
            return returnValue;
        }

        /// <summary>
        /// Converts an array to a delimited string
        /// </summary>
        /// <param name="delimiter">Character to delimit list</param>
        /// <param name="enclosure">Optional Enclosure</param>
        /// <returns>array with specified formating</returns>
        public static string ToDelimitedString<T>(this T[] array, string delimiter, string enclosure = "")
        {
            if (array != null)
            {
                if (array.Length > 0)
                {
                    //create string builder for performance of large arrays
                    StringBuilder builder = new StringBuilder();

                    //Add first element
                    builder.Append(enclosure);
                    builder.Append(array[0].ToString());
                    builder.Append(enclosure);

                    //Add remaining elements
                    for (int i = 1; i < array.Length; i++)
                    {
                        builder.Append(delimiter);
                        builder.Append(enclosure);
                        builder.Append(array[i].ToString());
                        builder.Append(enclosure);
                    }

                    return builder.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Converts an array into string in value list format
        /// </summary>
        /// <returns>string thats value list formatted</returns>
        public static string ToValueList(string[] array)
        {
            string valueList = "";
            string value = "";

            int count = 0;

            if (array != null)
            {
                count = array.Length;
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        value = array[i];

                        if (i != (count - 1))
                        {
                            valueList += value + ":" + value + ";";
                        }
                        else
                        {
                            valueList += value + ":" + value;
                        }
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return null;
            }

            return valueList;
        }

    }
}