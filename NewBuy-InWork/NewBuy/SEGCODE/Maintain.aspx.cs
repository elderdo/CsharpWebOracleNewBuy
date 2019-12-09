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
using System.Text.RegularExpressions;

namespace NewBuy.SEGCODE
{
    public partial class Maintain : System.Web.UI.Page
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            //Session Login Check
            if (HttpContext.Current.Session["Connection"] == null)
            {
                Response.Redirect("~/Default.aspx");
            }

            //Session Permission Check
            if (!Convert.ToBoolean(HttpContext.Current.Session["cls"]) &&
                !Convert.ToBoolean(HttpContext.Current.Session["ccad"]))
            {
                Response.Redirect("~/Shared/Denied.aspx");
            }
        }
        
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Gets segcode data for jqGrid as JSON
        /// </summary>
        /// <param name="RequestDTStamp">Timestamp of request as integer</param>
        /// <param name="Page">Current page if multiple pages</param>
        /// <param name="RowLimit">Number of rows on page</param>
        /// <param name="SortIndex">Field to sort by</param>
        /// <param name="SortOrder">Asc or Desc sort order</param>
        /// <returns>requested grid page data formatted as JSON</returns>
        /// <remarks>
        /// Parameter: "RequestDTStamp" is a timestamp number used to create a unique http request.
        /// jqGrid creates this number to prevent a browser from returning cache data.
        /// It is not used in the method.
        /// </remarks>
        [WebMethod]
        public static SegcodeGrid getSegcodeGrid(double RequestDTStamp, int Page, int RowLimit,
            string SortIndex, string SortOrder, string FilterValue, string FilterField)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }  

            //Create paging request
            SegcodeViewRequest gridRequest = new SegcodeViewRequest();
            gridRequest.page = Page;
            gridRequest.rows = RowLimit;
            gridRequest.index = SortIndex.ToLower().Trim();
            gridRequest.order = SortOrder.ToLower().Trim();
            gridRequest.filterValue = FilterValue.Trim();
            gridRequest.filterField = FilterField.Trim().ToLower();

            //uppercase if seg_code
            if (gridRequest.filterField == "seg_code")
            {
                gridRequest.filterValue = gridRequest.filterValue.ToUpper();
            }

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                SegcodeGrid emptyGrid = new SegcodeGrid();
                emptyGrid.totalPages = 1;
                emptyGrid.currentPage = 1;
                emptyGrid.totalRows = 0;
                emptyGrid.rows = new List<SegcodeGrid.Segcode>();

                return emptyGrid;
            }

            //Query database using request
            SegcodeGrid results;
            using (Interface db = new Interface())
            {
                results = db.getSegcodeGrid(gridRequest);
            }

            return results;
        }

        /// <summary>
        /// Updates or creates a segcode record in the database
        /// </summary>
        /// <param name="oper"></param>
        /// <param name="id"></param>
        /// <param name="seg_code"></param>
        /// <param name="program_code"></param>
        /// <param name="buy_method"></param>
        /// <param name="include_in_bolt"></param>
        /// <param name="include_in_spo"></param>
        /// <param name="include_in_tav_reporting"></param>
        /// <param name="site_location"></param>
        /// <returns></returns>
        [WebMethod]
        public static ResponseMsg editSegcodeGrid(string oper, string id, string seg_code, string program_code,
            string buy_method, string include_in_bolt, string include_in_spo, string include_in_tav_reporting,
            string site_location)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }  

            //Variables
            ResponseMsg result = new ResponseMsg();

            //Create request
            SegcodeEditRequest editRequest = new SegcodeEditRequest();
            editRequest.oper = oper.Trim().ToLower();
            editRequest.id = id.Trim().ToUpper();
            editRequest.seg_code = seg_code.Trim().ToUpper();
            editRequest.program_code = program_code.Trim().ToUpper();
            editRequest.buy_method = buy_method.Trim();
            editRequest.include_in_bolt = include_in_bolt.Trim().ToUpper();
            editRequest.include_in_spo = include_in_spo.Trim().ToUpper();
            editRequest.include_in_tav_reporting = include_in_tav_reporting.Trim().ToUpper();
            editRequest.site_location = site_location.Trim();

            //Validate request input
            if (!editRequest._isValid())
            {
                //set error message
                result.addError("Your request to edit this segcode contains invalid input and was not saved.");

                //Return invalid request
                return result;
            }

            //Edit database using request
            using (Interface db = new Interface())
            {
                try
                {
                    //Check if user has permissions to edit
                    if (!db.checkSegcodeEditPerm())
                    {
                        //set error message
                        result.addMsg("You do not have permission to edit segcodes.");

                        //Return request
                        return result;
                    }
                }
                catch (Exception)
                {
                    //set error message
                    result.addError("Unable to verify permissions. Please try again or contact application support.");

                    //Return request
                    return result;
                }

                //Perform action
                if (editRequest.oper == "edit")
                {
                    result = db.editSegcode(editRequest);
                }
                else if (editRequest.oper == "delete")
                {
                    result = db.deleteSegcode(editRequest);
                }
                else if (editRequest.oper == "new")
                {
                    result = db.newSegcode(editRequest);
                }
            }

            return result;
        }

        /// <summary>
        /// get list of autocomplete suggestions
        /// </summary>
        /// <param name="term">filter value to search for</param>
        /// <param name="field">field to search on</param>
        /// <returns></returns>
        [WebMethod]
        public static SearchList getSearchList(string term, string field)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }  

            SegcodeSearchListRequest request = new SegcodeSearchListRequest();
            request.filterValue = term.Trim();
            request.filterField = field.Trim().ToLower();
            SearchList results = new SearchList();
            
            //Validate request input
            if (!request._isValid())
            {
                //Return an empty results
                return results;
            }

            //Query database using request
            using (Interface db = new Interface())
            {
                results = db.getSegAutocomplete(request);
            }

            return results;
        }

    }
}