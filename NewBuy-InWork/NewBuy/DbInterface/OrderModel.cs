using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.OracleClient;
using System.Collections;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;

namespace NewBuy.DbInterface
{
    #region Shared
    
    #region New Buy Variance Comment Grid

    /// <summary>
    /// Used to request paged data for the New Buy Variance Comments grid
    /// </summary>
    internal class NBVarianceCommentViewRequest
    {
        public int page { get; set; } //Grid page requested (when multiple pages exist)
        public int rows { get; set; } //Number of rows to display
        public string index { get; set; } //Field to sort by
        public string order { get; set; } //"index" field sort order
        public string comment { get; set; } // comment
        /// <summary>
        /// role to filter results by
        /// </summary>
        /// <remarks>Asset Manager ("am"), Change Control Board ("ccb")</remarks>
        public string role { get; set; }

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public NBVarianceCommentViewRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!page_isValid())
            {
                return false;
            }

            if (!rows_isValid())
            {
                return false;
            }

            if (!index_isValid())
            {
                return false;
            }

            if (!order_isValid())
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool page_isValid()
        {
            //Value must be greater than 0
            if (page < 1)
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool rows_isValid()
        {
            //Value must be greater than 0
            if (rows < 1)
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool index_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(index))
            {
                return false;
            }

            //Must match a field name from grid model
            Type req = typeof(NBVarianceCommentGrid.NBVarianceComment);
            var fields = req.GetProperties().Select(p => p.Name).ToArray();
            var idx = index.ToLower();
            if (!Array.Exists(fields, v => v.Equals(idx)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool order_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(order))
            {
                return false;
            }

            //Must be in value list
            string[] values = { "asc", "desc" };
            string ord = order.ToLower();
            if (!Array.Exists(values, v => v.Equals(ord)))
            {
                return false;
            }

            //Valid
            return true;
        }
        #endregion Validations
    }

    /// <summary>
    /// Available Requirment Asset Manager Remarks formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class NBVarianceCommentGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<NBVarianceComment> rows { get; set; } //List of data records

        /// <summary>
        /// Single grid row that represents a single part record.
        /// </summary>
        [Serializable]
        public class NBVarianceComment
        {
            public string comment { get; set; } //Display: Variance Comment           
        }
    }

    /// <summary>
    /// Used to request remark edit, shared by all remark grids
    /// </summary>
    internal class NBVarianceCommentEditRequest
    {
        public int[] internalOrderNo { get; set; } //internal order number to update
        public string comment { get; set; } //remark text

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public NBVarianceCommentEditRequest()
        {
        }

        #region Validations

        public bool _isValid()
        {
            if (!internalOrderNo_isValid())
            {
                return false;
            }

            if (!comment_isValid())
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool internalOrderNo_isValid()
        {
            //Must contain atleast 1 element
            if (internalOrderNo.Length < 1)
            {
                return false;
            }

            //Value must be 0 or greater for each element
            foreach (int i in internalOrderNo)
            {
                if (i < 1)
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        public bool comment_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(comment))
            {
                return false;
            }

            //Must be in value list
            string[] values = QtyMismatchComments.qty_mismatch_values;
            if (!Array.Exists(values, v => v.Equals(comment)))
            {
                return false;
            }

            //Valid
            return true;
        }

        #endregion Validations
    }

    #endregion New Buy Variance Comment Remark Grid

    #region LTA

    /// <summary>
    /// Used to request paged data from order grid
    /// </summary>
    internal class LtaViewRequest
    {
        public string search { get; set; } //part_no to search for

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public LtaViewRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!search_isValid())
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool search_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(search))
            {
                return false;
            }

            //Must be alphanumeric, no special characters w/ exception of -()/, no spaces
            if (!Regex.IsMatch(search, @"^[a-zA-Z0-9-()/\.]+$"))
            {
                return false;
            }

            //Valid
            return true;
        }

        #endregion Validations
    }

    /// <summary>
    /// Long-Term Agreement(LTA) model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class LtaGrid
    {
        public const int totalPages = 1; //Total pages
        public const int currentPage = 1; //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<Lta> rows { get; set; } //List of data records

        /// <summary>
        /// Default constructor
        /// </summary>
        public LtaGrid()
        {
            //Instantiate class variables
            this.rows = new List<Lta>();
        }

        /// <summary>
        /// Single grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class Lta
        {
            public string lta { get; set; } //Display: LTA
            public string expired_calendar_date { get; set; } //Display: LTA Expired
            public string start_price_break_cal_date { get; set; } //Display: Price Start
            public string end_price_break_calendar_date { get; set; } //Display: Price End
            public int price_break_start_qty { get; set; } //Display: Start Qty
            public int price_break_end_qty { get; set; } //Display: End Qty
            public double unit_price { get; set; } //Display: Unit Price
        }
    }

    /// <summary>
    /// Returns part details for lta price break report
    /// </summary>
    [Serializable]
    public class LtaPartDetail
    {
        public string description { get; set; } //Part Description
        public string plo_qty { get; set; } //PLO Qty
        public string pr_qty { get; set; } //PR Qty
        public string forecast_qty { get; set; } //Forecast Qty

        /// <summary>
        /// Default constructor
        /// </summary>
        public LtaPartDetail()
        {
            description = "";
            plo_qty = "";
            pr_qty = "";
            forecast_qty = "";
        }
    }

    #endregion LTA

    #region Part Grid
    /// <summary>
    /// Used to request paged data for the part grid
    /// </summary>
    internal class PartViewRequest
    {
        public int page { get; set; } //Grid page requested (when multiple pages exist)
        public int rows { get; set; } //Number of rows to display
        public string index { get; set; } //Field to sort by
        public string order { get; set; } //"index" field sort order
        public string part_no { get; set; } //User to filter results by
        /// <summary>
        /// role to filter results by
        /// </summary>
        /// <remarks>Asset Manager ("am"), Change Control Board ("ccb")</remarks>
        public string role { get; set; }
        public bool viewHistory { get; set; } //scope to limit results

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public PartViewRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!page_isValid())
            {
                return false;
            }

            if (!rows_isValid())
            {
                return false;
            }

            if (!index_isValid())
            {
                return false;
            }

            if (!order_isValid())
            {
                return false;
            }

            if (!part_no_isValid())
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool page_isValid()
        {
            //Value must be greater than 0
            if (page < 1)
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool rows_isValid()
        {
            //Value must be greater than 0
            if (rows < 1)
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool index_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(index))
            {
                return false;
            }

            //Must match a field name from grid model
            Type req = typeof(PartGrid.Part);
            var fields = req.GetProperties().Select(p => p.Name).ToArray();
            var idx = index.ToLower();
            if (!Array.Exists(fields, v => v.Equals(idx)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool order_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(order))
            {
                return false;
            }

            //Must be in value list
            string[] values = { "asc", "desc" };
            string ord = order.ToLower();
            if (!Array.Exists(values, v => v.Equals(ord)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool part_no_isValid()
        {
            //Optional field, only check if there is a value
            if (!String.IsNullOrEmpty(part_no))
            {
                //Must be alphanumeric, no special characters w/ exception of -()/, no spaces
                if (!Regex.IsMatch(part_no, @"^[a-zA-Z0-9-()/\.]+$"))
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        #endregion Validations
    }

    /// <summary>
    /// Part model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class PartGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<Part> rows { get; set; } //List of data records

        /// <summary>
        /// Single grid row that represents a single part record.
        /// </summary>
        [Serializable]
        public class Part
        {
            public string part_no { get; set; } //Display: Part_no            
        }
    }

    #endregion Part Grid

    #endregion Shared

    #region CLS

    #region Order Grid
    /// <summary>
    /// Used to request paged data from order grid
    /// </summary>
    internal class CLSOrderViewRequest
    {
        public int page { get; set; } //Grid page requested (when multiple pages exist)
        public int rows { get; set; } //Number of rows to display
        public string index { get; set; } //Field to sort by
        public string order { get; set; } //"index" field sort order
        public int[] orderNumKey { get; set; } //internal record numbers to filter by

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CLSOrderViewRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!page_isValid())
            {
                return false;
            }

            if (!rows_isValid())
            {
                return false;
            }

            if (!index_isValid())
            {
                return false;
            }

            if (!order_isValid())
            {
                return false;
            }

            if (!orderNumKey_isValid())
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool page_isValid()
        {
            //Value must be greater than 0
            if (page < 1)
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool rows_isValid()
        {
            //Value must be greater than 0
            if (rows < 1)
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool index_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(index))
            {
                return false;
            }

            //Must match a field name from grid model
            Type req = typeof(CLSOrderGrid.CLSOrder);
            var fields = req.GetProperties().Select(p => p.Name).ToArray();
            var idx = index.ToLower();
            if (!Array.Exists(fields, v => v.Equals(idx)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool order_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(order))
            {
                return false;
            }

            //Must be in value list
            string[] values = { "asc", "desc" };
            string ord = order.ToLower();
            if (!Array.Exists(values, v => v.Equals(ord)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool orderNumKey_isValid()
        {
            //Must contain atleast 1 element
            if (orderNumKey.Length < 1)
            {
                return false;
            }

            //Value must be 0 or greater for each element
            foreach (int i in orderNumKey)
            {
                if (i < 1)
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        #endregion Validations
    }

    /// <summary>
    /// Used to request order edit
    /// </summary>
    internal class CLSOrderEditRequest
    {
        public string cost_charge_number { get; set; } //ccn to use
        public string due_date { get; set; } //date order is due
        public int internal_order_no { get; set; } //requirement internal order number
        public string oper { get; set; } //denotes edit or delete operation
        public int order_quantity { get; set; } //order quantity
        public int priority { get; set; } //order priority
        public int requirement_schedule_no { get; set; } //order number for parent requirement
        public int activity_status { get; set; } //activity status
        public string change_reason { get; set; } // change reason (justify mismatch)

        /// <summary>
        /// Creates a new empty order edit request object.
        /// </summary>
        public CLSOrderEditRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!cost_charge_number_isValid())
            {
                return false;
            }

            if (!due_date_isValid())
            {
                return false;
            }

            if (!internal_order_no_isValid())
            {
                return false;
            }

            if (!oper_isValid())
            {
                return false;
            }
            
            if (!order_quantity_isValid())
            {
                return false;
            }

            if (!priority_isValid())
            {
                return false;
            }

            if (!requirement_schedule_no_isValid())
            {
                return false;
            }

            if (!activity_status_isValid())
            {
                return false;
            }

            if (!change_reason_isValid())
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool cost_charge_number_isValid()
        {
            //Value can be empty
            if (!String.IsNullOrEmpty(cost_charge_number))
            {
                //Must be alphanumeric, dashes only, no spaces
                if (!Regex.IsMatch(cost_charge_number, "^[a-zA-Z0-9-]+$"))
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        public bool due_date_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(due_date))
            {
                return false;
            }

            //Must be a string date in format "mm/dd/yyyy"
            if (!Regex.IsMatch(due_date, "^[0-9]?[0-9][/][0-9]?[0-9][/][0-9][0-9][0-9][0-9]$"))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool internal_order_no_isValid()
        {
            //If not a new record
            if (oper.ToLower() != "new")
            {
                //Value must exist
                if (internal_order_no < 1)
                {
                    return false;
                }
            }
            
            //Valid
            return true;
        }

        public bool oper_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(oper))
            {
                return false;
            }

            //Must be in value list
            string[] values = { "edit", "delete", "new" };
            oper = oper.ToLower();
            if (!Array.Exists(values, v => v.Equals(oper)))
            {
                return false;
            }
            
            //Valid
            return true;
        }

        public bool order_quantity_isValid()
        {
            //Value must exist
            if (order_quantity < 1)
            {
                return false;
            }
            
            //Valid
            return true;
        }

        public bool priority_isValid()
        {
            //Value must exist
            if (priority < 1 || priority > 3)
            {
                return false;
            }
            
            //Valid
            return true;
        }

        public bool requirement_schedule_no_isValid()
        {            
            //If not a new record
            if (oper.ToLower() != "new")
            {
                //Value must exist
                if (requirement_schedule_no < 1)
                {
                    return false;
                }          
            }
            
            //Valid
            return true;
        }

        public bool activity_status_isValid()
        {
            //Only check if not deleting record
            if (oper.ToLower() != "delete")
            {
                //Must be in value list
                if (activity_status != 20 &&
                    activity_status != 30 &&
                    activity_status != 40)
                {
                    return false;
                }
            }
            
            //Valid
            return true;
        }
        
        public bool change_reason_isValid()
        {
            string[] mismatchValues = QtyMismatchComments.qty_mismatch_values;

            //Only check if not deleting record
            if (oper.ToLower() != "delete")
            {
                if(Array.IndexOf(mismatchValues, change_reason) == -1)
                {                
                    return false;
                }
            }

            //Valid
            return true;
        }
        #endregion Validations
    }

    /// <summary>
    /// Used to request multiple order edits
    /// </summary>
    internal class CLSMultiOrderEditRequest
    {
        public string oper { get; set; } //operation to perform
        public List<string> orders { get; set; } //order_no to process

        /// <summary>
        /// Creates a new empty order edit request object with instantiated variables
        /// </summary>
        public CLSMultiOrderEditRequest()
        {
            orders = new List<string>();
        }

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!oper_isValid())
            {
                return false;
            }

            if (!orders_isValid())
            {
                return false;
            }
            
            //Valid
            return true;
        }
                
        public bool oper_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(oper))
            {
                return false;
            }

            //Must be in value list
            string[] values = { "approve", "validate", "reject", "delete" };
            if (!Array.Exists(values, v => v.Equals(oper)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool orders_isValid()
        {
                        
            //Must have at least 1 value
            if (orders.Count < 1)
            {
                return false;
            }

            bool test = true;
            orders.ForEach(delegate(string order)
            {
                //Value must exist
                if (String.IsNullOrEmpty(order))
                {
                    test = false;
                }

                //Must be alphanumeric
                if (!Regex.IsMatch(order, "^[A-Z0-9]+$"))
                {
                    test = false;
                }
            });
            if (!test) return false;

            //Valid
            return true;
        }

        #endregion Validations
    }

    /// <summary>
    /// Order model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CLSOrderGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CLSOrder> rows { get; set; } //List of data records

        /// <summary>
        /// Single grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CLSOrder
        {
            public int internal_order_no { get; set; } //Unique ID
            public int requirement_schedule_no { get; set; } //Display: (Hidden)
            public int spo_request_id { get; set; } //Display: (Hidden)
            public string part_no { get; set; } //Display: Part No
            public string order_no { get; set; } //Display: Order No
            public string due_date { get; set; } //Display: Due Date
            public int order_quantity { get; set; } //Display: Order Quantity 
            public int priority { get; set; } //Display: Priority
            public string cost_charge_number { get; set; } //Display: CCN
            public string change_reason { get; set; } //Display: Mismatch Reason
            public string activity_status { get; set; } //Display: Activity Status
            public int spo_qty { get; set; }//Display: SPO Qty
            public int order_total { get; set; }//Display: Order Total
            public bool pricebreak { get; set; } //Display: (Hidden)
            public decimal pricePoint { get; set; } //Display: LTA
        }
    }

    /// <summary>
    /// Data from Requirements table as related to an Order
    /// </summary>
    internal class CLSOrderReqData
    {
        public int spo_request_id { get; set; } //From Requirements
        public string program_code { get; set; } //From Requirements
        public string part_no { get; set; } //From Requirements
    }

    #endregion Order Grid

    #endregion CLS

    #region CCAD

    #region Order Grid

    /// <summary>
    /// Used to request paged data from order grid
    /// </summary>
    internal class CCADOrderViewRequest
    {
        public int page { get; set; } //Grid page requested (when multiple pages exist)
        public int rows { get; set; } //Number of rows to display
        public string index { get; set; } //Field to sort by
        public string order { get; set; } //"index" field sort order
        public int[] orderNumKey { get; set; } //internal record numbers to filter by

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CCADOrderViewRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!page_isValid())
            {
                return false;
            }

            if (!rows_isValid())
            {
                return false;
            }

            if (!index_isValid())
            {
                return false;
            }

            if (!order_isValid())
            {
                return false;
            }

            if (!orderNumKey_isValid())
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool page_isValid()
        {
            //Value must be greater than 0
            if (page < 1)
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool rows_isValid()
        {
            //Value must be greater than 0
            if (rows < 1)
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool index_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(index))
            {
                return false;
            }

            //Must match a field name from grid model
            Type req = typeof(CCADOrderGrid.CCADOrder);
            var fields = req.GetProperties().Select(p => p.Name).ToArray();
            var idx = index.ToLower();
            if (!Array.Exists(fields, v => v.Equals(idx)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool order_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(order))
            {
                return false;
            }

            //Must be in value list
            string[] values = { "asc", "desc" };
            string ord = order.ToLower();
            if (!Array.Exists(values, v => v.Equals(ord)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool orderNumKey_isValid()
        {
            //Must contain atleast 1 element
            if (orderNumKey.Length < 1)
            {
                return false;
            }
            
            //Value must be 0 or greater for each element
            foreach (int i in orderNumKey)
            {
                if (i < 1)
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        #endregion Validations
    }

    /// <summary>
    /// Used to request order edit
    /// </summary>
    internal class CCADOrderEditRequest
    {
        public string cost_charge_number { get; set; } //ccn to use
        public string due_date { get; set; } //date order is due
        public int internal_order_no { get; set; } //requirement internal order number
        public string oper { get; set; } //denotes edit or delete operation
        public int order_quantity { get; set; } //order quantity
        public int priority { get; set; } //order priority
        public int requirement_schedule_no { get; set; } //order number for parent requirement
        public int activity_status { get; set; } //activity status
        public string change_reason { get; set; } //change reason
        public string part_no { get; set; } // part no

        /// <summary>
        /// Creates a new empty order edit request object.
        /// </summary>
        public CCADOrderEditRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!cost_charge_number_isValid())
            {
                return false;
            }

            if (!due_date_isValid())
            {
                return false;
            }

            if (!internal_order_no_isValid())
            {
                return false;
            }

            if (!oper_isValid())
            {
                return false;
            }

            if (!order_quantity_isValid())
            {
                return false;
            }

            if (!priority_isValid())
            {
                return false;
            }

            if (!requirement_schedule_no_isValid())
            {
                return false;
            }

            if (!activity_status_isValid())
            {
                return false;
            }

            if (!part_no_isValid())
            {
                return false;
            }

            if (!change_reason_isValid())
            {
                return false;
            }
            //Valid
            return true;
        }

        public bool cost_charge_number_isValid()
        {
            //Value can be empty
            if (!String.IsNullOrEmpty(cost_charge_number))
            {
                //Must be alphanumeric, no special characters, no spaces
                if (!Regex.IsMatch(cost_charge_number, "^[a-zA-Z0-9-]+$"))
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        public bool due_date_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(due_date))
            {
                return false;
            }

            //Must be a string date in format "mm/dd/yyyy"
            if (!Regex.IsMatch(due_date, "^[0-9]?[0-9][/][0-9]?[0-9][/][0-9][0-9][0-9][0-9]$"))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool internal_order_no_isValid()
        {
            //If not a new record
            if (oper.ToLower() != "new")
            {
                //Value must exist
                if (internal_order_no < 1)
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        public bool oper_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(oper))
            {
                return false;
            }

            //Must be in value list
            string[] values = { "edit", "delete", "new" };
            oper = oper.ToLower();
            if (!Array.Exists(values, v => v.Equals(oper)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool order_quantity_isValid()
        {
            //Value must exist
            if (order_quantity < 1)
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool priority_isValid()
        {
            //Value must exist
            if (priority < 1 || priority > 3)
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool requirement_schedule_no_isValid()
        {
            //If not a new record
            if (oper.ToLower() != "new")
            {
                //Value must exist
                if (requirement_schedule_no < 1)
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        public bool activity_status_isValid()
        {
            //Only check if not deleting record
            if (oper.ToLower() != "delete")
            {
                //Must be in value list
                if (activity_status != 20 &&
                    activity_status != 30 &&
                    activity_status != 40)
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        public bool part_no_isValid()
        {
            //Optional field, only check if there is a value
            if (!String.IsNullOrEmpty(part_no))
            {
                //Must be alphanumeric, no special characters w/ exception of -()/
                if (!Regex.IsMatch(part_no, @"^[a-zA-Z0-9-()/ ]+$"))
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        public bool change_reason_isValid()
        {
            string[] mismatchValues = QtyMismatchComments.qty_mismatch_values;

            //Only check if not deleting record
            if (oper.ToLower() != "delete")
            {
                if (Array.IndexOf(mismatchValues, change_reason) == -1)
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        #endregion Validations
    }

    /// <summary>
    /// Used to request multiple order edits
    /// </summary>
    internal class CCADMultiOrderEditRequest
    {
        public string oper { get; set; } //operation to perform
        public List<string> orders { get; set; } //order_no to process

        /// <summary>
        /// Creates a new empty order edit request object with instantiated variables
        /// </summary>
        public CCADMultiOrderEditRequest()
        {
            orders = new List<string>();
        }

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!oper_isValid())
            {
                return false;
            }

            if (!orders_isValid())
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool oper_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(oper))
            {
                return false;
            }

            //Must be in value list
            string[] values = { "approve", "validate", "reject", "delete" };
            if (!Array.Exists(values, v => v.Equals(oper)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool orders_isValid()
        {

            //Must have at least 1 value
            if (orders.Count < 1)
            {
                return false;
            }

            bool test = true;
            orders.ForEach(delegate(string order)
            {
                //Value must exist
                if (String.IsNullOrEmpty(order))
                {
                    test = false;
                }

                //Must be alphanumeric
                if (!Regex.IsMatch(order, "^[A-Z0-9]+$"))
                {
                    test = false;
                }
            });
            if (!test) return false;

            //Valid
            return true;
        }

        #endregion Validations
    }

    /// <summary>
    /// Requirement model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CCADOrderGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CCADOrder> rows { get; set; } //List of data records

        /// <summary>
        /// Single grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CCADOrder
        {
            public int internal_order_no { get; set; } //Unique ID
            public int requirement_schedule_no { get; set; } //Display: (Hidden)
            public int spo_request_id { get; set; } //Display: (Hidden)
            public string due_date { get; set; } //Display: Due Date
            public int order_quantity { get; set; } //Display: Order Quantity 
            public int priority { get; set; } //Display: Priority
            public string cost_charge_number { get; set; } //Display: CCN
            public string change_reason { get; set; } // Display: Mismatch reason
            public string order_no { get; set; } //Display: Order No
            public string activity_status { get; set; } //Display: Activity Status
            public int spo_qty { get; set; }//Display: SPO Qty
            public int order_total { get; set; }//Display: Order Total
            public string part_no { get; set; } //Display: Part No
            public bool pricebreak { get; set; } //Display: (Hidden)
            public decimal pricePoint { get; set; } //Display: LTA
        }
    }

    /// <summary>
    /// Data from Requirements table as related to an Order
    /// </summary>
    internal class CCADOrderReqData
    {
        public int spo_request_id { get; set; } //From Requirements
        public string program_code { get; set; } //From Requirements
        public string part_no { get; set; } //From Requirements
    }

    #endregion Order Grid

    #endregion CCAD
}