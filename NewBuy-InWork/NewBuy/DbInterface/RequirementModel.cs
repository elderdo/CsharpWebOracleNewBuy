using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.OracleClient;
using System.Collections;
using System.Web.Script.Serialization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NewBuy.DbInterface
{
    #region CLS

    #region Requirement Grid
    /// <summary>
    /// Used to request paged data from requirement grid
    /// </summary>
    internal class CLSRequirementViewRequest
    {
        public int page { get; set; } //Grid page requested (when multiple pages exist)
        public int rows { get; set; } //Number of rows to display
        public string index { get; set; } //Field to sort by
        public string order { get; set; } //"index" field sort order
        public string assetManager { get; set; } //User to filter results by
        public string part_no { get; set; } //Part to filter results by
        public string program { get; set; } //Program to filter results by
        public bool viewHistory { get; set; } //Program scope

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CLSRequirementViewRequest()
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
            Type req = typeof(CLSRequirementGrid.CLSRequirement);
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

        #endregion Validations
    }

    /// <summary>
    /// Requirement model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CLSRequirementGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CLSRequirement> rows { get; set; } //List of data records

        /// <summary>
        /// Single grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CLSRequirement
        {
            public int internal_order_no { get; set; } //Unique ID
            public string spo_export_user { get; set; } //Display: SPO User
            public string part_no { get; set; } //Display: Part #
            public string network { get; set; } //Display: Network
            public string description { get; set; } //Display: Part Name
            public string status { get; set; } //Display: Overall Status
            public string change_reason { get; set; } //Display: Change Reason
            public int spo_request_id { get; set; } //Display: Request ID
            public string program_code { get; set; } //Display: Program
            public string spo_export_date { get; set; } //Display: Export Date
            public string request_due_date { get; set; } //Display: Due Date
            public decimal item_cost { get; set; } //Display: Cost 
            // PRP 04272016 added for the new requirement grid colums
            public string order_quantity { get; set; } //Display: total order quanity
            public string min_buy_qty { get; set; } 
            public string total_monthly_capacity_qty { get; set; }  
            public string annual_buy_ind { get; set; }  

        }
    }
    #endregion Requirement Grid

    #region Manager Grid
    
    /// <summary>
    /// Used to request paged data for asset manager grid
    /// </summary>
    internal class CLSManagerViewRequest
    {
        public string assetManager { get; set; } //User to filter results by
        public bool viewHistory { get; set; } //Filter scope
        /// <summary>
        /// role to filter results by
        /// </summary>
        /// <remarks>Asset Manager ("am"), Change Control Board ("ccb")</remarks>
        public string role { get; set; }

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CLSManagerViewRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            if (!assetManager_isValid())
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

        #endregion Validations
    }

    /// <summary>
    /// Asset Manager model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CLSManagerGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CLSManager> rows { get; set; } //List of data records

        /// <summary>
        /// Single grid row that represents a single asset manager record.
        /// </summary>
        [Serializable]
        public class CLSManager
        {
            public string asset_manager_id { get; set; } //Display: ID
            public string asset_manager_id_name { get; set; } //Display: SPO User
        }
    }
    #endregion Manager Grid

    #region Program Grid

    /// <summary>
    /// Used to request paged data for program grid
    /// </summary>
    internal class CLSProgramViewRequest
    {
        public int page { get; set; } //Grid page requested (when multiple pages exist)
        public int rows { get; set; } //Number of rows to display
        public string index { get; set; } //Field to sort by
        public string order { get; set; } //"index" field sort order
        public string program { get; set; } //User to filter results by

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CLSProgramViewRequest()
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
            Type req = typeof(CLSProgramGrid.CLSProgram);
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
    /// Program model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CLSProgramGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CLSProgram> rows { get; set; } //List of data records

        /// <summary>
        /// Single grid row that represents a single asset manager record.
        /// </summary>
        [Serializable]
        public class CLSProgram
        {
            public string program_code { get; set; } //Display: Code
            public string program_name { get; set; } //Display: Name
        }
    }
    
    #endregion Program Grid

    #endregion CLS

    #region CCAD

    #region Requirement Grid
    /// <summary>
    /// Used to request paged data from requirement grid
    /// </summary>
    internal class CCADRequirementViewRequest
    {
        public int page { get; set; } //Grid page requested (when multiple pages exist)
        public int rows { get; set; } //Number of rows to display
        public string index { get; set; } //Field to sort by
        public string order { get; set; } //"index" field sort order
        public string spoUser { get; set; } //User to filter results by
        public string part_no { get; set; } //Part to filter results by
        public string program { get; set; } //Program to filter results by

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CCADRequirementViewRequest()
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

            if (!spoUser_isValid())
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
            Type req = typeof(CCADRequirementGrid.CCADRequirement);
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

        public bool spoUser_isValid()
        {
            //Optional field, only check if there is a value
            if (!String.IsNullOrEmpty(spoUser))
            {
                //Must be alphanumeric, no special characters, no spaces
                if (!Regex.IsMatch(spoUser, "^[a-zA-Z0-9]+$"))
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

        #endregion Validations
    }

    /// <summary>
    /// Requirement model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CCADRequirementGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CCADRequirement> rows { get; set; } //List of data records

        /// <summary>
        /// Single grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class CCADRequirement
        {
            public int internal_order_no { get; set; } //Unique ID
            public string spo_export_user { get; set; } //Display: SPO User
            public string part_no { get; set; } //Display: Part # 
            public string order_quantity { get; set; } //Display: Order Qty 
            public string status { get; set; } //Display: Overall Status
            public string change_reason { get; set; } //Display: Change Reason (Mismatch)
            public int spo_request_id { get; set; } //Display: Request ID
            public string spo_export_date { get; set; } //Display: Export Date
            public string request_due_date { get; set; } //Display: Due Date
            public decimal item_cost { get; set; } //Display: Cost
            public string site { get; set; } //Display: Site
            // PRP 05312016 added for the new requirement grid colums
            public string min_buy_qty { get; set; }
            public string total_monthly_capacity_qty { get; set; }
            public string annual_buy_ind { get; set; }
        }
    }
    #endregion Requirement Grid

    #region Manager Grid
    /// <summary>
    /// Used to request paged data for asset manager grid
    /// </summary>
    internal class CCADManagerViewRequest
    {
        public string assetManager { get; set; } //User to filter results by
        /// <summary>
        /// role to filter results by
        /// </summary>
        /// <remarks>Asset Manager ("am"), Change Control Board ("ccb")</remarks>
        public string role { get; set; }

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CCADManagerViewRequest()
        { }

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!assetManager_isValid())
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

        #endregion Validations
    }

    /// <summary>
    /// Asset Manager model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CCADManagerGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CCADManager> rows { get; set; } //List of data records

        /// <summary>
        /// Single grid row that represents a single asset manager record.
        /// </summary>
        [Serializable]
        public class CCADManager
        {
            public string asset_manager_id { get; set; } //Display: ID
            public string asset_manager_user_name { get; set; } //Display: SPO User
        }
    }
    #endregion Manager Grid
    
    #region Program Grid

    /// <summary>
    /// Used to request paged data for program grid
    /// </summary>
    internal class CCADProgramViewRequest
    {
        public int page { get; set; } //Grid page requested (when multiple pages exist)
        public int rows { get; set; } //Number of rows to display
        public string index { get; set; } //Field to sort by
        public string order { get; set; } //"index" field sort order
        public string program { get; set; } //User to filter results by

        /// <summary>
        /// Creates a new empty grid request object.
        /// </summary>
        public CCADProgramViewRequest()
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
            Type req = typeof(CCADProgramGrid.CCADProgram);
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
    /// Program model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class CCADProgramGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<CCADProgram> rows { get; set; } //List of data records

        /// <summary>
        /// Single grid row that represents a single asset manager record.
        /// </summary>
        [Serializable]
        public class CCADProgram
        {
            public string program_code { get; set; } //Display: Code
            public string program_name { get; set; } //Display: Name
        }
    }

    #endregion Program Grid

    #endregion CCAD

}