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

namespace NewBuy.UK
{
    public partial class Activity : System.Web.UI.Page
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            //Session Login Check
            if (HttpContext.Current.Session["Connection"] == null)
            {
                Response.Redirect("~/Default.aspx");
            }

            //Session Permission Check
            if (!Convert.ToBoolean(HttpContext.Current.Session["uk"]))
            {
                Response.Redirect("~/Shared/Denied.aspx");
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            //Create connection resources
            using (Interface db = new Interface())
            {
                //Set last update date
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

                //Set budget numbers
                try
                {
                    //Query database using request
                    BudgetInfo budget = db.getUKBudgetInfo("newbuy");

                    //Set label values
                    BudgetTotal.Text = string.Format("{0:C}", budget.totalBudget);
                    BudgetedSpent.Text = string.Format("{0:C}", budget.budgetedSpent);
                    NonBudgetedSpent.Text = string.Format("{0:C}", budget.nonBudgetedSpent);
                    BudgetRemain.Text = string.Format("{0:C}", (budget.totalBudget - budget.totalSpent));
                    BudgetSpentPercent.Text = string.Format("{0:P}", Math.Round((budget.totalSpent / budget.totalBudget), 3));
                }
                catch (Exception)
                {
                    BudgetTotal.Text = "error";
                }
            }
        }

        /// <summary>
        /// Gets requirement data for jqGrid as JSON
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
        public static CLSRequirementGrid getReqGrid(double RequestDTStamp,
            int Page, int RowLimit, string SortIndex, string SortOrder, string AssetManager,
            string Part_no, bool ViewHistory)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }

            //Create paging request
            CLSRequirementViewRequest gridRequest = new CLSRequirementViewRequest();
            gridRequest.page = Page;
            gridRequest.rows = RowLimit;
            gridRequest.index = SortIndex.ToLower();
            gridRequest.order = SortOrder.ToLower();
            gridRequest.assetManager = AssetManager.ToUpper();
            gridRequest.part_no = Part_no.ToUpper();
            gridRequest.program = "UKC"; //Limit to UK only
            gridRequest.viewHistory = ViewHistory;

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                CLSRequirementGrid emptyGrid = new CLSRequirementGrid();
                emptyGrid.totalPages = 1;
                emptyGrid.currentPage = 1;
                emptyGrid.totalRows = 0;
                emptyGrid.rows = new List<CLSRequirementGrid.CLSRequirement>();

                return emptyGrid;
            }

            //Query database using request
            CLSRequirementGrid results;
            using (Interface db = new Interface())
            {
                results = db.getCLSRequirementGrid(gridRequest);
            }

            return results;
        }

        /// <summary>
        /// Gets order data for jqGrid as JSON
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
        public static CLSOrderGrid getOrdGrid(double RequestDTStamp,
            int Page, int RowLimit, string SortIndex, string SortOrder, int[] OrderNumKey)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }

            //Breakout SortIndex param group and sort field values
            GroupSortParam sortParams = new GroupSortParam(SortIndex);

            //Create paging request
            CLSOrderViewRequest gridRequest = new CLSOrderViewRequest();
            gridRequest.page = Page;
            gridRequest.rows = RowLimit;
            gridRequest.index = sortParams.SortField.field.ToLower();
            gridRequest.order = SortOrder.ToLower();
            gridRequest.orderNumKey = OrderNumKey;

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                CLSOrderGrid emptyGrid = new CLSOrderGrid();
                emptyGrid.totalPages = 1;
                emptyGrid.currentPage = 1;
                emptyGrid.totalRows = 0;
                emptyGrid.rows = new List<CLSOrderGrid.CLSOrder>();

                return emptyGrid;
            }

            //Query database using request
            CLSOrderGrid results;
            using (Interface db = new Interface())
            {
                results = db.getCLSOrderGrid(gridRequest);
            }

            return results;
        }

        /// <summary>
        /// Updates or creates an order record in the database
        /// </summary>
        /// <param name="cost_charge_number"></param>
        /// <param name="due_date"></param>
        /// <param name="id">unique row id, needed for grid</param>
        /// <param name="internal_order_no"></param>
        /// <param name="oper"></param>
        /// <param name="order_no"></param>
        /// <param name="order_quantity"></param>
        /// <param name="priority"></param>
        /// <param name="requirement_schedule_no"></param>
        /// <returns></returns>
        [WebMethod]
        public static ResponseMsg editOrdGrid(string cost_charge_number, string due_date, int id,
            int internal_order_no, string oper, int order_quantity, int priority,
            int requirement_schedule_no, int activity_status, string change_reason)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }

            //Variables
            ResponseMsg result = new ResponseMsg();

            //Create paging request
            CLSOrderEditRequest editRequest = new CLSOrderEditRequest();
            editRequest.cost_charge_number = (cost_charge_number.Trim()).ToUpper();
            editRequest.due_date = due_date.Trim();
            editRequest.internal_order_no = internal_order_no;
            editRequest.oper = oper.Trim();
            editRequest.order_quantity = order_quantity;
            editRequest.priority = priority;
            editRequest.requirement_schedule_no = requirement_schedule_no;
            editRequest.activity_status = activity_status;
            editRequest.change_reason = change_reason;

            //Validate request input
            if (!editRequest._isValid())
            {
                //set error message
                result.addError("Your request to edit this order contains invalid input and was not saved.");

                //Return invalid request
                return result;
            }

            //Edit database using request
            using (Interface db = new Interface())
            {
                switch (editRequest.oper)
                {
                    case "edit":
                        {
                            result = db.editCLSOrder(editRequest);
                            break;
                        }
                    case "delete":
                        {
                            result = db.deleteCLSOrder(editRequest);
                            break;
                        }
                    case "new":
                        {
                            result = db.newCLSOrder(editRequest);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }

            return result;
        }

        /// <summary>
        /// performs an edit on multiple orders in the database
        /// </summary>
        /// <param name="oper">type of operation to perform</param>
        /// <param name="orders">array of order numbers</param>
        /// <returns>ResponseMsg object</returns>
        [WebMethod]
        public static ResponseMsg multiEditOrdGrid(string oper, string[] orders)
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

            //Edit database using request
            using (Interface db = new Interface())
            {
                switch (editRequest.oper)
                {
                    case "approve":
                        {
                            result = db.approveCLSAmOrders(editRequest);
                            break;
                        }
                    case "validate":
                        {
                            result = db.validateCLSOrders(editRequest);
                            break;
                        }
                    case "delete":
                        {
                            result = db.deleteCLSOrders(editRequest);
                            break;
                        }
                    case "reject":
                        {
                            result = db.rejectCLSOrders(editRequest);
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

            //Query database using request
            CLSManagerGrid results;
            using (Interface db = new Interface())
            {
                results = db.getCLSManagerGrid(gridRequest, true);
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
        /// <param name="Part_no">Current part_no to filter results with</param>
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
            gridRequest.part_no = Part_no.Trim().ToUpper();
            gridRequest.viewHistory = ViewHistory;

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Create an empty grid
                PartGrid emptyResult = new PartGrid();
                emptyResult.totalPages = 1;
                emptyResult.currentPage = 1;
                emptyResult.totalRows = 0;
                emptyResult.rows = new List<PartGrid.Part>();

                return emptyResult;
            }

            //Query database using request
            PartGrid result;
            using (Interface db = new Interface())
            {
                result = db.getCLSPartGrid(gridRequest, true);
            }

            return result;
        }
        
        /// <summary>
        /// Gets available requirements asset manager comments pop-up data for jqGrid as JSON
        /// </summary>
        /// <param name="RequestDTStamp">Timestamp of request as integer</param>
        /// <param name="Page">Current page if multiple pages</param>
        /// <param name="RowLimit">Number of rows on page</param>
        /// <param name="SortIndex">Field to sort by</param>
        /// <param name="SortOrder">Asc or Desc sort order</param>
        /// <param name="Comment">Current comment to filter results with</param>
        /// <returns>requested grid page data formatted as JSON</returns>
        /// <remarks>
        /// Parameter: "RequestDTStamp" is a timestamp number used to create a unique http request.
        /// jqGrid creates this number to prevent a browser from returning cache data.
        /// It is not used in the method.
        /// </remarks>
        [WebMethod]
        public static NBVarianceCommentGrid getVarianceCommentGrid(double RequestDTStamp,
            int Page, int RowLimit, string SortIndex, string SortOrder)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }

            //Create paging request
            NBVarianceCommentViewRequest gridRequest = new NBVarianceCommentViewRequest();
            gridRequest.page = Page;
            gridRequest.rows = RowLimit;
            gridRequest.index = SortIndex.ToLower();
            gridRequest.order = SortOrder.ToLower();

            //Validate request input
            if (!gridRequest._isValid())
            {
                //Return an empty grid
                NBVarianceCommentGrid emptyGrid = new NBVarianceCommentGrid();
                emptyGrid.totalPages = 1;
                emptyGrid.currentPage = 1;
                emptyGrid.totalRows = 0;
                emptyGrid.rows = new List<NBVarianceCommentGrid.NBVarianceComment>();

                return emptyGrid;
            }
            //Query database using request
            NBVarianceCommentGrid results;
            using (Interface db = new Interface())
            {
                results = db.getVarianceCommentGrid(gridRequest);
            }

            return results;
        }

        /// <summary>
        /// Updates New Buy Variance Comment for an order
        /// </summary>
        /// <param name="InternalOrderNo"></param>
        /// <param name="Comment"></param>
        /// <returns></returns>
        [WebMethod]
        public static ResponseMsg editNBVarianceComment(int[] internalOrders, string comment)
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }

            //Variables
            ResponseMsg result = new ResponseMsg();

            //Create New Buy Variance Comment edit request
            NBVarianceCommentEditRequest editRequest = new NBVarianceCommentEditRequest();
            editRequest.internalOrderNo = internalOrders;
            editRequest.comment = comment.Trim();

            //Validate request input
            if (!editRequest._isValid())
            {
                //set error message
                result.addError("Your request to edit this remark contains invalid input and was not saved.");

                //Return invalid request
                return result;
            }

            //Update database using request
            using (Interface db = new Interface())
            {
                result = db.editNBVarianceComment(editRequest);
            }

            return result;
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
        /// Edits an asset manager remark
        /// </summary>
        /// <param name="oper">edit operation</param>
        /// <param name="id">order number to edit</param>
        /// <param name="asset_manager_remark">remark to save</param>
        /// <returns></returns>
        [WebMethod]
        public static ResponseMsg editAmRemark(string oper, string id, string asset_manager_remark)
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
            editRequest.remark = asset_manager_remark.Trim();

            //Validate request input
            if (!editRequest._isValid())
            {
                //set error message
                result.addError("Your request to edit this remark contains invalid input and was not saved.");

                //Return invalid request
                return result;
            }

            //Edit database using request
            using (Interface db = new Interface())
            {
                if (editRequest.oper == "edit")
                {
                    result = db.editCLSAmRemark(editRequest);
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
        /// <param name="SortIndex"></param>
        /// <param name="SortOrder"></param>
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
        /// Get a list of Change Reasons formatted for a form Select box
        /// </summary>
        /// <returns>returns a string formatted for a form Select box</returns>
        [WebMethod]
        public static string getChangeReasons()
        {
            //Session Login Check 
            if (HttpContext.Current.Session["Connection"] == null)
            {
                throw new Exception("Session State Timeout");
            }

            string reasons = "";
            reasons = Helper.ToValueList(QtyMismatchComments.qty_mismatch_values);

            return reasons;
        }
    }
}