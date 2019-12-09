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
    #region CLS

    /// <summary>
    /// Used to request paged data from ccb grid
    /// </summary>
    internal class CLSccbViewRequest
    {
        public int page { get; set; } //Grid page requested (when multiple pages exist)
        public int rows { get; set; } //Number of rows to display
        public string index { get; set; } //Field to sort by
        public string order { get; set; } //"index" field sort order
        public string assetManager { get; set; } //User to filter results by
        public string part_no { get; set; } //Part to filter results by
        public string activity_status { get; set; } //Status to filter results by
        public string program { get; set; } //Program code to filter results by
        public bool viewHistory { get; set; } //search scope to filter by

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CLSccbViewRequest()
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

            if (!assetManager_isValid())
            {
                return false;
            }

            if (!part_no_isValid())
            {
                return false;
            }

            if (!activity_status_isValid())
            {
                return false;
            }

            if (!program_isValid())
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
            Type req = typeof(CLSccbGrid.CLSccb);
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
            order = order.ToLower();
            if (!Array.Exists(values, v => v.Equals(order)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool assetManager_isValid()
        {
            //Optional field, only check if there is a value
            if (!String.IsNullOrEmpty(assetManager))
            {
                //Must be alphanumeric, no special characters, no spaces
                if (!Regex.IsMatch(assetManager, "^[a-zA-Z0-9]+$"))
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
                //Must be alphanumeric, no special characters w/ exception of -()/, no spaces
                if (!Regex.IsMatch(part_no, @"^[a-zA-Z0-9-()/]+$"))
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        public bool activity_status_isValid()
        {
            //Optional field, only check if there is a value
            if (!String.IsNullOrEmpty(activity_status) && activity_status != "0")
            {
                //Must be in value list
                if (!Array.Exists(CcbActivityStatus.ccb_status_values, v => v.Equals(activity_status)))
                {
                    return false;
                }
            }
            
            //Valid
            return true;
        }

        public bool program_isValid()
        {
            //Optional field, only check if there is a value
            if (!String.IsNullOrEmpty(program))
            {
                //Must be alphanumeric, no special characters except hyphen and space
                if (!Regex.IsMatch(program, @"[a-zA-Z0-9-\s]"))
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
    /// CCB model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CLSccbGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CLSccb> rows { get; set; } //List of data records

        /// <summary>
        /// Single ccb grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CLSccb
        {
            public string order_no { get; set; } //Unique ID
            public string part_no { get; set; } //Display: Part # 
            public string activity_status { get; set; } //Display: Overall Status
            public string cost_charge_number { get; set; } //Display: CCN
            public string nomenclature { get; set; } //Display: Nomenclature
            public decimal spo_cost { get; set; } //Display: Cost
            public int order_quantity { get; set; } //Display: Order Quantity
            public decimal extended_cost { get; set; } //Display: Extended Cost
            public string due_date { get; set; } //Display: Due Date
            public bool pricebreak { get; set; } //Display: (Hidden)
            public decimal pricePoint { get; set; } //Display: LTA
        }
    }

    /// <summary>
    /// Used to request paged data for an Activity Status grid
    /// </summary>
    internal class CLSccbStatusViewRequest
    {
        public int page { get; set; } //Grid page requested (when multiple pages exist)
        public int rows { get; set; } //Number of rows to display
        public string index { get; set; } //Field to sort by
        public string order { get; set; } //"index" field sort order

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CLSccbStatusViewRequest()
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
            Type req = typeof(CLSccbStatusGrid.CLSccbStatus);
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
            order = order.ToLower();
            if (!Array.Exists(values, v => v.Equals(order)))
            {
                return false;
            }

            //Valid
            return true;
        }
        
        #endregion Validations
    }

    /// <summary>
    /// Activity Status model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CLSccbStatusGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CLSccbStatus> rows { get; set; } //List of data records

        /// <summary>
        /// Single ActivityStatus grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CLSccbStatus
        {
            public string activity_status { get; set; } //Activity Status
        }
    }

    /// <summary>
    /// Used to request remark view, shared by all remark grids
    /// </summary>
    internal class CLSRemarkViewRequest
    {
        public string order_no { get; set; } //order number to update

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CLSRemarkViewRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            if (!order_no_isValid())
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool order_no_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(order_no))
            {
                return false;
            }

            //Must be alphanumeric, no special characters, no spaces
            if (!Regex.IsMatch(order_no, "^[a-zA-Z0-9]+$"))
            {
                return false;
            }

            //Valid
            return true;
        }

        #endregion Validations
    }

    /// <summary>
    /// Used to request remark edit, shared by all remark grids
    /// </summary>
    internal class CLSRemarkEditRequest
    {
        public string oper { get; set; } //edit type
        public string order_no { get; set; } //order number to update
        public string remark { get; set; } //remark text

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CLSRemarkEditRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            if (!oper_isValid())
            {
                return false;
            }
            
            if (!order_no_isValid())
            {
                return false;
            }

            if (!remark_isValid())
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
            string[] values = { "edit" };
            oper = oper.ToLower();
            if (!Array.Exists(values, v => v.Equals(oper)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool order_no_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(order_no))
            {
                return false;
            }

            //Must be alphanumeric, no special characters, no spaces
            if (!Regex.IsMatch(order_no, "^[a-zA-Z0-9]+$"))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool remark_isValid()
        {
            //If value exists, check
            if (!String.IsNullOrEmpty(remark))
            {
                //Max length 200 characters
                if (remark.Length > 200)
                {
                    return false;
                }
                
                //No < > ' "
                if (Regex.IsMatch(remark, "[<>'\"]"))
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
    /// Asset manager remark model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CLSAmRemarkGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CLSAmRemark> rows { get; set; } //List of data records

        /// <summary>
        /// Single Remark grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CLSAmRemark
        {
            public string order_no { get; set; } //Unique ID
            public string asset_manager_remark { get; set; } //Display: AM Note
        }
    }

    /// <summary>
    /// CCB remark model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CLSCcbRemarkGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CLSCcbRemark> rows { get; set; } //List of data records

        /// <summary>
        /// Single Remark grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CLSCcbRemark
        {
            public string order_no { get; set; } //Unique ID
            public string review_board_remark { get; set; } //Display: CCB Note
        }
    }

    /// <summary>
    /// SPO remark model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CLSSpoRemarkGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CLSSpoRemark> rows { get; set; } //List of data records

        /// <summary>
        /// Single Remark grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CLSSpoRemark
        {
            public string order_no { get; set; } //Unique ID
            public string spo_remark { get; set; } //Display: SPO Note
        }
    }

    #region Summary Grid

    /// <summary>
    /// Used to request paged data for summary grid
    /// </summary>
    internal class CLSSummaryProgramViewRequest
    {
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public Boolean isCCAD { get; set; }

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CLSSummaryProgramViewRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!startDate_isValid())
            {
                return false;
            }

            if (!endDate_isValid())
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool startDate_isValid()
        {
            //variables
            var today = System.DateTime.Today;
            
            //Required within reasonable time frame
            if(startDate > today.AddYears(2) ||
                startDate < today.AddYears(-10))
            {
                return false;
            }
            
            //Valid
            return true;
        }

        public bool endDate_isValid()
        {
            //variables
            var today = System.DateTime.Today;

            //must be greater than start
            if (endDate < startDate)
            {
                return false;
            }

            //Required within reasonable time frame
            if (endDate > today.AddYears(2) ||
                endDate < today.AddYears(-10))
            {
                return false;
            }
            
            //Valid
            return true;
        }

        #endregion Validations
    }

    /// <summary>
    /// Summary by program model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CLSProgramSummaryGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CLSProgramSummary> rows { get; set; } //List of data records

        /// <summary>
        /// Single grid row that represents a single asset manager record.
        /// </summary>
        [Serializable]
        public class CLSProgramSummary
        {
            public string program_code { get; set; } //Display: Program Code
            public int total_accept_part_count { get; set; }
            public decimal total_accept_part_cost { get; set; }
            public int total_reject_part_count { get; set; }
            public decimal total_reject_part_cost { get; set; }
            public int total_part_count { get; set; }
            public decimal total_part_cost { get; set; }
        }
    }

    /// <summary>
    /// Used to request paged data for summary grid
    /// </summary>
    internal class CLSSummaryReasonViewRequest
    {
        public string program_code { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public Boolean isCCAD { get; set; }

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CLSSummaryReasonViewRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!program_code_isValid())
            {
                return false;
            }
            if (!startDate_isValid())
            {
                return false;
            }

            if (!endDate_isValid())
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool program_code_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(program_code))
            {
                return false;
            }

            /* Must be alphanumeric, no special characters, no spaces
             * Upper case only
             * exactly three characters
             */
            if (!Regex.IsMatch(program_code, "^[A-Z][A-Z][A-Z]$"))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool startDate_isValid()
        {
            //variables
            var today = System.DateTime.Today;

            //Required within reasonable time frame
            if (startDate > today.AddYears(2) ||
                startDate < today.AddYears(-10))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool endDate_isValid()
        {
            //variables
            var today = System.DateTime.Today;

            //must be greater than start
            if (endDate < startDate)
            {
                return false;
            }

            //Required within reasonable time frame
            if (endDate > today.AddYears(2) ||
                endDate < today.AddYears(-10))
            {
                return false;
            }

            //Valid
            return true;
        }
        
        #endregion Validations
    }

    /// <summary>
    /// Summary by reason model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CLSReasonSummaryGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CLSReasonSummary> rows { get; set; } //List of data records

        /// <summary>
        /// Single grid row that represents a single asset manager record.
        /// </summary>
        [Serializable]
        public class CLSReasonSummary
        {
            public string program_code { get; set; } //Display: Program Code
            public string change_code { get; set; } //Display: 
            public int accept_part_count { get; set; }
            public decimal accept_part_cost { get; set; }
            public int reject_part_count { get; set; }
            public decimal reject_part_cost { get; set; }
        }
    }

    #endregion Summary Grid

    #endregion CLS

    #region CCAD

    /// <summary>
    /// Used to request paged data from ccb grid
    /// </summary>
    internal class CCADccbViewRequest
    {
        public int page { get; set; } //Grid page requested (when multiple pages exist)
        public int rows { get; set; } //Number of rows to display
        public string index { get; set; } //Field to sort by
        public string order { get; set; } //"index" field sort order
        public string assetManager { get; set; } //User to filter results by
        public string part_no { get; set; } //Part to filter results by
        public string activity_status { get; set; } //Status to filter results by

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CCADccbViewRequest()
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

            if (!assetManager_isValid())
            {
                return false;
            }

            if (!part_no_isValid())
            {
                return false;
            }

            if (!activity_status_isValid())
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
            Type req = typeof(CCADccbGrid.CCADccb);
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
            order = order.ToLower();
            if (!Array.Exists(values, v => v.Equals(order)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool assetManager_isValid()
        {
            //Optional field, only check if there is a value
            if (!String.IsNullOrEmpty(assetManager))
            {
                //Must be alphanumeric, no special characters, no spaces
                if (!Regex.IsMatch(assetManager, "^[a-zA-Z0-9]+$"))
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
                //Must be alphanumeric, no special characters w/ exception of -()/, no spaces
                if (!Regex.IsMatch(part_no, @"^[a-zA-Z0-9-()/]+$"))
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        public bool activity_status_isValid()
        {
            //Optional field, only check if there is a value
            if (!String.IsNullOrEmpty(activity_status) && activity_status != "0")
            {
                //Must be in value list
                if (!Array.Exists(CcbActivityStatus.ccb_status_values, v => v.Equals(activity_status)))
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
    /// CCB model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CCADccbGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CCADccb> rows { get; set; } //List of data records

        /// <summary>
        /// Single ccb grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CCADccb
        {
            public string order_no { get; set; } //Unique ID
            public string part_no { get; set; } //Display: Part # 
            public string activity_status { get; set; } //Display: Overall Status
            public string cost_charge_number { get; set; } //Display: CCN
            public string nomenclature { get; set; } //Display: Nomenclature
            public decimal spo_cost { get; set; } //Display: Cost
            public int order_quantity { get; set; } //Display: Order Quantity
            public decimal extended_cost { get; set; } //Display: Extended Cost
            public string due_date { get; set; } //Display: Due Date
            public bool pricebreak { get; set; } //Display: (Hidden)
            public decimal pricePoint { get; set; } //Display: LTA
        }
    }

    /// <summary>
    /// Used to request paged data for an Activity Status grid
    /// </summary>
    internal class CCADccbStatusViewRequest
    {
        public int page { get; set; } //Grid page requested (when multiple pages exist)
        public int rows { get; set; } //Number of rows to display
        public string index { get; set; } //Field to sort by
        public string order { get; set; } //"index" field sort order

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CCADccbStatusViewRequest()
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
            Type req = typeof(CCADccbStatusGrid.CCADccbStatus);
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
            order = order.ToLower();
            if (!Array.Exists(values, v => v.Equals(order)))
            {
                return false;
            }

            //Valid
            return true;
        }

        #endregion Validations
    }

    /// <summary>
    /// Activity Status model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CCADccbStatusGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CCADccbStatus> rows { get; set; } //List of data records

        /// <summary>
        /// Single ActivityStatus grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CCADccbStatus
        {
            public string activity_status { get; set; } //Activity Status
        }
    }

    /// <summary>
    /// Used to request remark view, shared by all remark grids
    /// </summary>
    internal class CCADRemarkViewRequest
    {
        public string order_no { get; set; } //order number to update

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CCADRemarkViewRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            if (!order_no_isValid())
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool order_no_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(order_no))
            {
                return false;
            }

            //Must be alphanumeric, no special characters, no spaces
            if (!Regex.IsMatch(order_no, "^[a-zA-Z0-9]+$"))
            {
                return false;
            }

            //Valid
            return true;
        }

        #endregion Validations
    }

    /// <summary>
    /// Used to request remark edit, shared by all remark grids
    /// </summary>
    internal class CCADRemarkEditRequest
    {
        public string oper { get; set; } //edit type
        public string order_no { get; set; } //order number to update
        public string remark { get; set; } //remark text

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CCADRemarkEditRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            if (!oper_isValid())
            {
                return false;
            }

            if (!order_no_isValid())
            {
                return false;
            }

            if (!remark_isValid())
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
            string[] values = { "edit" };
            oper = oper.ToLower();
            if (!Array.Exists(values, v => v.Equals(oper)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool order_no_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(order_no))
            {
                return false;
            }

            //Must be alphanumeric, no special characters, no spaces
            if (!Regex.IsMatch(order_no, "^[a-zA-Z0-9]+$"))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool remark_isValid()
        {
            //If value exists, check
            if (!String.IsNullOrEmpty(remark))
            {
                //Max length 200 characters
                if (remark.Length > 200)
                {
                    return false;
                }

                //No < > ' "
                if (Regex.IsMatch(remark, "[<>'\"]"))
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
    /// Asset manager remark model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CCADAmRemarkGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CCADAmRemark> rows { get; set; } //List of data records

        /// <summary>
        /// Single Remark grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CCADAmRemark
        {
            public string order_no { get; set; } //Unique ID
            public string asset_manager_remark { get; set; } //Display: AM Note
        }
    }

    /// <summary>
    /// CCB remark model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CCADCcbRemarkGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CCADCcbRemark> rows { get; set; } //List of data records

        /// <summary>
        /// Single Remark grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CCADCcbRemark
        {
            public string order_no { get; set; } //Unique ID
            public string review_board_remark { get; set; } //Display: CCB Note
        }
    }

    /// <summary>
    /// SPO remark model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CCADSpoRemarkGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CCADSpoRemark> rows { get; set; } //List of data records

        /// <summary>
        /// Single Remark grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CCADSpoRemark
        {
            public string order_no { get; set; } //Unique ID
            public string spo_remark { get; set; } //Display: SPO Note
        }
    }

    #endregion CCAD

}