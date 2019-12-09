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

namespace NewBuy.CLS
{
    public partial class CCB : System.Web.UI.Page
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            //Session Login Check
            if (HttpContext.Current.Session["Connection"] == null)
            {
                Response.Redirect("~/Default.aspx");
            }

            //Session Permission Check
            if (!Convert.ToBoolean(HttpContext.Current.Session["cls"]))
            {
                Response.Redirect("~/Shared/Denied.aspx");
            }
        }
        
        protected void Page_Load(object sender, EventArgs e)
        {
            //Create connection resources
            using (Interface db = new Interface())
            {
                try
                {
                    //Query database using request
                    DateTime result = db.getLtaLastUpdate();

                    //Set label value
                    LTALastUpdate.Text = result.Date.ToShortDateString();
                }
                catch (Exception)
                {
                    LTALastUpdate.Text = "error";
                }
            }
        }

        /// <summary>
        /// Gets CCB data for jqGrid as JSON
        /// </summary>
        /// <param name="RequestDTStamp">Timestamp of request as integer</param>
        /// <param name="Page">Current page if multiple pages</param>
        /// <param name="RowLimit">Number of rows on page</param>
        /// <param name="SortIndex">Field to sort by</param>
        /// <param name="SortOrder">Asc or Desc sort order</param>
        /// <param name="AssetManager">User to filter by</param>
        /// <param name="Part_no">Part to filter by</param>
        /// <param name="ProgramCode">Program to filter by</param>
        /// <returns>requested grid page data formatted as JSON</returns>
        /// <remarks>
        /// Parameter: "RequestDTStamp" is a timestamp number used to create a unique http request.
        /// jqGrid creates this number to prevent a browser from returning cache data.
        /// It is not used in the method.
        /// </remarks>
        [WebMethod]
        public static CLSccbGrid getCCBGrid(double RequestDTStamp,
            int Page, int RowLimit, string SortIndex, string SortOrder, string AssetManager,
            string Part_no, string ActivityStatus, string ProgramCode, bool ViewHistory)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            } 

            //Create paging request
            CLSccbViewRequest gridRequest = new CLSccbViewRequest();
            gridRequest.page = Page;
            gridRequest.rows = RowLimit;
            gridRequest.index = SortIndex.ToLower();
            gridRequest.order = SortOrder.ToLower();
            gridRequest.assetManager = AssetManager.ToUpper();
            gridRequest.part_no = Part_no.ToUpper();
            gridRequest.activity_status = ActivityStatus.Trim();
            gridRequest.program = ProgramCode.ToUpper();
            gridRequest.viewHistory = ViewHistory;

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                CLSccbGrid emptyGrid = new CLSccbGrid();
                emptyGrid.totalPages = 1;
                emptyGrid.currentPage = 1;
                emptyGrid.totalRows = 0;
                emptyGrid.rows = new List<CLSccbGrid.CLSccb>();

                return emptyGrid;
            }

            //Query database using request
            CLSccbGrid results;
            using (Interface db = new Interface())
            {
                results = db.getCLSccbGrid(gridRequest);
            }

            return results;
        }

        /// <summary>
        /// performs an edit on multiple orders in the database
        /// </summary>
        /// <param name="oper">type of operation to perform</param>
        /// <param name="orders">array of order numbers</param>
        /// <returns>ResponseMsg object</returns>
        [WebMethod]
        public static ResponseMsg multiEdit(string oper, string[] orders)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            } 

            //Variables
            ResponseMsg result = new ResponseMsg();

            //Create paging request
            CLSMultiOrderEditRequest editRequest = new CLSMultiOrderEditRequest();
            editRequest.oper = oper.Trim().ToLower();
            foreach (string order in orders)
            {
                editRequest.orders.Add(order.Trim().ToUpper());
            }

            //Validate request input
            if (!editRequest._isValid())
            {
                //set error message
                result.addError("Your Request to edit these orders contains invalid input and was canceled.");

                //Return invalid request
                return result;
            }

            //Create db interface instance
            using (Interface db = new Interface())
            {
                try
                {
                    //Check if user has permissions to edit
                    if (!Convert.ToBoolean(HttpContext.Current.Session["rev"]))
                    {
                        //set error message
                        result.addError("Reviewer role required to make changes.");

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

                //Edit database using request
                switch (editRequest.oper)
                {
                    case "approve":
                        {
                            result = db.approveCLSCcbOrders(editRequest);
                            break;
                        }
                    case "reject":
                        {
                            result = db.rejectCLSCcbOrders(editRequest);
                            break;
                        }
                    default:
                        {
                            result.addError("A valid operation was not specified. No changes were made.");
                            break;
                        }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets asset manager pop-up data for jqGrid as JSON
        /// </summary>
        /// <param name="RequestDTStamp">Timestamp of request as integer</param>
        /// <param name="AssetManager">Current Assest manager to filter results with</param>
        /// <returns>requested grid page data formatted as JSON</returns>
        /// <remarks>
        /// Parameter: "RequestDTStamp" is a timestamp number used to create a unique http request.
        /// jqGrid creates this number to prevent a browser from returning cache data.
        /// It is not used in the method.
        /// </remarks>
        [WebMethod]
        public static CLSManagerGrid getManGrid(double RequestDTStamp, string AssetManager, bool ViewHistory)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            } 

            //Create paging request
            CLSManagerViewRequest gridRequest = new CLSManagerViewRequest();
            gridRequest.assetManager = (AssetManager.ToUpper()).Trim();
            gridRequest.viewHistory = ViewHistory;

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                CLSManagerGrid emptyGrid = new CLSManagerGrid();
                emptyGrid.totalPages = 1;
                emptyGrid.currentPage = 1;
                emptyGrid.totalRows = 0;
                emptyGrid.rows = new List<CLSManagerGrid.CLSManager>();

                return emptyGrid;
            }

            //limit results to ccb
            gridRequest.role = "ccb";
            
            //Query database using request
            CLSManagerGrid results;
            using (Interface db = new Interface())
            {
                results = db.getCLSManagerGrid(gridRequest);
            }

            return results;
        }

        /// <summary>
        /// Gets part pop-up data for jqGrid as JSON
        /// </summary>
        /// <param name="RequestDTStamp">Timestamp of request as integer</param>
        /// <param name="Page">Current page if multiple pages</param>
        /// <param name="RowLimit">Number of rows on page</param>
        /// <param name="SortIndex">Field to sort by</param>
        /// <param name="SortOrder">Asc or Desc sort order</param>
        /// <param name="SpoUser">Current Assest manager to filter results with</param>
        /// <returns>requested grid page data formatted as JSON</returns>
        /// <remarks>
        /// Parameter: "RequestDTStamp" is a timestamp number used to create a unique http request.
        /// jqGrid creates this number to prevent a browser from returning cache data.
        /// It is not used in the method.
        /// </remarks>
        [WebMethod]
        public static PartGrid getPrtGrid(double RequestDTStamp,
            int Page, int RowLimit, string SortIndex, string SortOrder, string Part_no, bool LtaPart, bool ViewHistory)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            } 

            //Create paging request
            PartViewRequest gridRequest = new PartViewRequest();
            gridRequest.page = Page;
            gridRequest.rows = RowLimit;
            gridRequest.index = SortIndex.ToLower();
            gridRequest.order = SortOrder.ToLower();
            gridRequest.part_no = (Part_no.ToUpper()).Trim();
            gridRequest.viewHistory = ViewHistory;

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                PartGrid emptyGrid = new PartGrid();
                emptyGrid.totalPages = 1;
                emptyGrid.currentPage = 1;
                emptyGrid.totalRows = 0;
                emptyGrid.rows = new List<PartGrid.Part>();

                return emptyGrid;
            }

            //limit results to ccb
            gridRequest.role = "ccb";

            //Query database using request
            PartGrid result;
            using (Interface db = new Interface())
            {
                if (LtaPart)
                {
                    result = db.getLtaPartGrid(gridRequest);
                }
                else
                {
                    result = db.getCLSPartGrid(gridRequest);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets Program pop-up data for jqGrid as JSON
        /// </summary>
        /// <param name="RequestDTStamp">Timestamp of request as integer</param>
        /// <param name="Page">Current page if multiple pages</param>
        /// <param name="RowLimit">Number of rows on page</param>
        /// <param name="SortIndex">Field to sort by</param>
        /// <param name="SortOrder">Asc or Desc sort order</param>
        /// <param name="ProgramCode">Current program to filter results with</param>
        /// <returns>requested grid page data formatted as JSON</returns>
        /// <remarks>
        /// Parameter: "RequestDTStamp" is a timestamp number used to create a unique http request.
        /// jqGrid creates this number to prevent a browser from returning cache data.
        /// It is not used in the method.
        /// </remarks>
        [WebMethod]
        public static CLSProgramGrid getPgmGrid(double RequestDTStamp,
            int Page, int RowLimit, string SortIndex, string SortOrder, string ProgramCode)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }

            //Create paging request
            CLSProgramViewRequest gridRequest = new CLSProgramViewRequest();
            gridRequest.page = Page;
            gridRequest.rows = RowLimit;
            gridRequest.index = SortIndex.ToLower();
            gridRequest.order = SortOrder.ToLower();
            gridRequest.program = (ProgramCode.ToUpper()).Trim();

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                CLSProgramGrid emptyGrid = new CLSProgramGrid();
                emptyGrid.totalPages = 1;
                emptyGrid.currentPage = 1;
                emptyGrid.totalRows = 0;
                emptyGrid.rows = new List<CLSProgramGrid.CLSProgram>();

                return emptyGrid;
            }

            //Query database using request
            CLSProgramGrid results;
            using (Interface db = new Interface())
            {
                results = db.getCLSProgramGrid(gridRequest);
            }

            return results;
        }

        /// <summary>
        /// Gets activity status pop-up data for ccb page jqGrid as JSON
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
        public static CLSccbStatusGrid getActivityStatusGrid(double RequestDTStamp,
            int Page, int RowLimit, string SortIndex, string SortOrder)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            } 

            //Create paging request
            CLSccbStatusViewRequest gridRequest = new CLSccbStatusViewRequest();
            gridRequest.page = Page;
            gridRequest.rows = RowLimit;
            gridRequest.index = SortIndex.ToLower();
            gridRequest.order = SortOrder.ToLower();

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                CLSccbStatusGrid emptyGrid = new CLSccbStatusGrid();
                emptyGrid.totalPages = 1;
                emptyGrid.currentPage = 1;
                emptyGrid.totalRows = 0;
                emptyGrid.rows = new List<CLSccbStatusGrid.CLSccbStatus>();

                return emptyGrid;
            }

            //Query database using request
            CLSccbStatusGrid results;
            using (Interface db = new Interface())
            {
                results = db.getCLSccbStatusGrid(gridRequest);
            }

            return results;
        }

        /// <summary>
        /// Get asset manager remark grid
        /// </summary>
        /// <param name="RequestDTStamp"></param>
        /// <param name="OrderNo">order number to get remark</param>
        /// <returns></returns>
        [WebMethod]
        public static CLSAmRemarkGrid getAmRemarkGrid(double RequestDTStamp, string OrderNo)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            } 

            //Create paging request
            CLSRemarkViewRequest gridRequest = new CLSRemarkViewRequest();
            gridRequest.order_no = OrderNo;

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                CLSAmRemarkGrid emptyGrid = new CLSAmRemarkGrid();
                emptyGrid.totalPages = 1;
                emptyGrid.currentPage = 1;
                emptyGrid.totalRows = 0;
                emptyGrid.rows = new List<CLSAmRemarkGrid.CLSAmRemark>();

                return emptyGrid;
            }

            //Query database using request
            CLSAmRemarkGrid results;
            using (Interface db = new Interface())
            {
                results = db.getCLSAmRemark(gridRequest);
            }

            return results;
        }

        /// <summary>
        /// Edits a ccb/reviewer remark
        /// </summary>
        /// <param name="oper">edit operation</param>
        /// <param name="id">order number to edit</param>
        /// <param name="review_board_remark">remark to save</param>
        /// <returns></returns>
        [WebMethod]
        public static ResponseMsg editCcbRemark(string oper, string id, string review_board_remark)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            } 

            //Variables
            ResponseMsg result = new ResponseMsg();

            //Create edit request
            CLSRemarkEditRequest editRequest = new CLSRemarkEditRequest();
            editRequest.oper = oper.Trim().ToLower();
            editRequest.order_no = id.Trim().ToUpper();
            editRequest.remark = review_board_remark.Trim();

            //Validate request input
            if (!editRequest._isValid())
            {
                //set error message
                result.addError("Your request to edit the ccb remarks contains invalid input and was not saved.");

                //Return invalid request
                return result;
            }

            //Create db interface instance
            using (Interface db = new Interface())
            {
                try
                {
                    //Check if user has permissions to edit
                    if (!Convert.ToBoolean(HttpContext.Current.Session["rev"]))
                    {
                        //set error message
                        result.addError("Reviewer role required to make changes.");

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

                //Edit database using request
                if (editRequest.oper == "edit")
                {
                    result = db.editCLSccbRemark(editRequest);
                }
            }

            return result;
        }

        /// <summary>
        /// Get ccb remark grid
        /// </summary>
        /// <param name="RequestDTStamp"></param>
        /// <param name="OrderNo">order number to get remark</param>
        /// <returns></returns>
        [WebMethod]
        public static CLSCcbRemarkGrid getCcbRemarkGrid(double RequestDTStamp, string OrderNo)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            } 

            //Create paging request
            CLSRemarkViewRequest gridRequest = new CLSRemarkViewRequest();
            gridRequest.order_no = OrderNo;

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                CLSCcbRemarkGrid emptyGrid = new CLSCcbRemarkGrid();
                emptyGrid.totalPages = 1;
                emptyGrid.currentPage = 1;
                emptyGrid.totalRows = 0;
                emptyGrid.rows = new List<CLSCcbRemarkGrid.CLSCcbRemark>();

                return emptyGrid;
            }

            //Query database using request
            CLSCcbRemarkGrid results;
            using (Interface db = new Interface())
            {
                results = db.getCLSCcbRemark(gridRequest);
            }

            return results;
        }

        /// <summary>
        /// get lta price break report grid
        /// </summary>
        /// <param name="RequestDTStamp"></param>
        /// <param name="SearchValue">part to search for</param>
        /// <returns></returns>
        [WebMethod]
        public static LtaGrid getLtaGrid(double RequestDTStamp, string SearchValue)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }

            //Create paging request
            LtaViewRequest gridRequest = new LtaViewRequest();
            gridRequest.search = SearchValue.Trim().ToUpper();

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                LtaGrid emptyGrid = new LtaGrid();
                emptyGrid.totalRows = 0;

                return emptyGrid;
            }

            //Query database using request
            LtaGrid results;
            using (Interface db = new Interface())
            {
                results = db.getLtaGrid(gridRequest);
            }

            return results;
        }

        /// <summary>
        /// get part details from lta data
        /// </summary>
        /// <param name="part">part to lookup detail</param>
        /// <returns>description and order qtys as string for given part</returns>
        [WebMethod]
        public static LtaPartDetail getLtaPartInfo(string part)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }

            //Create paging request
            LtaViewRequest gridRequest = new LtaViewRequest();
            gridRequest.search = part.Trim().ToUpper();

            //Validate request input
            if (!gridRequest.search_isValid())
            {
                //Return an empty grid
                LtaPartDetail empty = new LtaPartDetail();
                return empty;
            }

            //Query database using request
            LtaPartDetail result;
            using (Interface db = new Interface())
            {
                result = db.getLtaPartDetail(gridRequest.search);
            }

            return result;
        }

        /// <summary>
        /// Gets Summary by Program for jqGrid as JSON
        /// </summary>
        /// <param name="RequestDTStamp"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        //// <returns>requested grid page data formatted as JSON</returns>
        /// <remarks>
        /// Parameter: "RequestDTStamp" is a timestamp number used to create a unique http request.
        /// jqGrid creates this number to prevent a browser from returning cache data.
        /// It is not used in the method.
        /// </remarks>
        [WebMethod]
        public static CLSProgramSummaryGrid getSumryGrid(double RequestDTStamp, string startDate, string endDate)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }

            //Create paging request
            CLSSummaryProgramViewRequest gridRequest = new CLSSummaryProgramViewRequest();
            try
            {
                gridRequest.startDate = Convert.ToDateTime(startDate);
                gridRequest.endDate = Convert.ToDateTime(endDate);
            }
            catch
            {
                //Ignore
            }

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                CLSProgramSummaryGrid emptyGrid = new CLSProgramSummaryGrid();
                emptyGrid.totalPages = 1;
                emptyGrid.currentPage = 1;
                emptyGrid.totalRows = 0;
                emptyGrid.rows = new List<CLSProgramSummaryGrid.CLSProgramSummary>();

                return emptyGrid;
            }

            //Query database using request
            CLSProgramSummaryGrid results;
            using (Interface db = new Interface())
            {
                results = db.getCLSSummaryProgramGrid(gridRequest);
            }

            return results;
        }

        /// <summary>
        /// Gets Summary by reason for jqGrid as JSON
        /// </summary>
        /// <param name="RequestDTStamp"></param>
        /// <param name="programCode"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        /// /// <remarks>
        /// Parameter: "RequestDTStamp" is a timestamp number used to create a unique http request.
        /// jqGrid creates this number to prevent a browser from returning cache data.
        /// It is not used in the method.
        /// </remarks>
        [WebMethod]
        public static CLSReasonSummaryGrid getSumryReasonGrid(double RequestDTStamp, string programCode, string startDate, string endDate)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }

            //Create paging request
            CLSSummaryReasonViewRequest gridRequest = new CLSSummaryReasonViewRequest();
            gridRequest.program_code = programCode.Trim().ToUpper();
            try
            {
                gridRequest.startDate = Convert.ToDateTime(startDate);
                gridRequest.endDate = Convert.ToDateTime(endDate);
            }
            catch
            {
                //Ignore
            }

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                CLSReasonSummaryGrid emptyGrid = new CLSReasonSummaryGrid();
                emptyGrid.totalPages = 1;
                emptyGrid.currentPage = 1;
                emptyGrid.totalRows = 0;
                emptyGrid.rows = new List<CLSReasonSummaryGrid.CLSReasonSummary>();

                return emptyGrid;
            }

            //Query database using request
            CLSReasonSummaryGrid results;
            using (Interface db = new Interface())
            {
                results = db.getCLSSummaryReasonGrid(gridRequest);
            }

            return results;
        }
    }
}