using System;
using System.Linq;
using System.Web;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;

namespace NewBuy.DbInterface
{
    /// <summary>
    /// Get values for order page activity status 
    /// </summary>
    internal static class OrderActivityStatus
    {
        private static string[] _order_status_values = new string[]
            { 
                "Awaiting Asset Manager Review", //20
                "Approved by Asset Manager", //30
                "Rejected by Asset Manager" //40
            };

        internal static string[] order_status_values { get { return _order_status_values; } }
    }
    
    /// <summary>
    /// Get values for ccb page activity status
    /// </summary>
    internal static class CcbActivityStatus
    {
        private static string[] _ccb_status_values = new string[]
            { 
                "Approved by Asset Manager", //30
                "Approved by Review Board", //60
                "Rejected by Review Board" //70
            };

        internal static string[] ccb_status_values { get { return _ccb_status_values; } }
    }

    /// <summary>
    /// Get values for ccb page activity status
    /// </summary>
    internal static class QtyMismatchComments
    {
        private static string[] _qty_mismatch_values = new string[]
            { 
                //"",
                //"Annual Buy",
                //"BER Replacement",
                //"Budget Constraints",
                //"Capacity Split Orders",
                //"Capacity Reductions",
                //"Capacity Delay",
                //"Configuration Change",
                //"Economic Buy",
                //"Minimum Buy",
                //"No Valid CCN",
                //"Not on Contract",
                //"Obsolete",
                //"SPO Qty High",
                //"SPO Qty Low",
                //"Field/Special Req-Manual",
                //"No SPO Rec-Manual",
                //"Not on Contract-Manual",
                //"Repair Scheme-Manual",
                //"Chose to Repair",
                //"SPO Rep Flag Incorrect",
                //"Plenty of Stock",
                //"BUYDS Stock Avl-Mesa",
                //"No Demand History"
                "",
                "Accept",
                "BER Replacement",
                "Budget Constraints",
                "BUYDS Stock Avl-Mesa",
                "Capacity Constraint",
                "Chose to Repair",
                "Configuration Change",
                "Economic Buy",
                "Manual Export",
                "Minimum Buy",
                "No Demand History",
                "No Valid CCN",
                "Not on Contract",
                "Obsolete",
                "Stock Supports Through Lead Time",
                "New Buy Qty High",
                "New Buy Qty Low",
                "Rep Flag Incorrect",
                "Incorrect Unit of Measure"
            };

        internal static string[] qty_mismatch_values { get { return _qty_mismatch_values; } }
    }

    /// <summary>
    /// Get values for segcode buy method
    /// </summary>
    internal static class SegcodeBuyMethod
    {
        private static string[] _segcode_buy_methods = new string[]
            { 
                "COC",
                "DD250"
            };

        internal static string[] segcode_buy_methods { get { return _segcode_buy_methods; } }
    }

    /// <summary>
    /// Get values for segcode autocomplete fields
    /// </summary>
    internal static class SegcodeSearchField
    {
        private static string[] _segcode_search_fields = new string[]
            { 
                "seg_code",
                "site_location"
            };

        internal static string[] segcode_search_fields { get { return _segcode_search_fields; } }
    }

    /// <summary>
    /// Get values for niin autocomplete fields
    /// </summary>
    internal static class NiinSearchField
    {
        private static string[] _niin_search_fields = new string[]
            { 
                "ccad_niin",
                "part_no",
                "prime_part_no"
            };

        internal static string[] niin_search_fields { get { return _niin_search_fields; } }
    }

    /// <summary>
    /// Get dates for current accounting period
    /// </summary>
    public static class CurrentAcctPeriod
    {
        private static string StartDate()
        {
            // JIRA Legacy 37: PRP change window to display the calendar month records.
            //Variables
            var startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            return startDate.Month + "/" + startDate.Day + "/" + startDate.Year; ; 
/* old code
            var lastDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
            var lastThursOfMonth = lastDayOfMonth;
            
            //Find last thursday of current month
            while (lastThursOfMonth.DayOfWeek != DayOfWeek.Thursday)
            {
                lastThursOfMonth = lastThursOfMonth.AddDays(-1);
            }

            if (DateTime.Today < lastThursOfMonth)
            {
                //find previous month last thursday
                var lastDayOfMonthPrev = new DateTime((DateTime.Today).AddMonths(-1).Year, (DateTime.Today).AddMonths(-1).Month, DateTime.DaysInMonth((DateTime.Today).AddMonths(-1).Year, (DateTime.Today).AddMonths(-1).Month));
                var lastThursOfMonthPrev = lastDayOfMonthPrev;

                //Find last thursday of current month
                while (lastThursOfMonthPrev.DayOfWeek != DayOfWeek.Thursday)
                {
                    lastThursOfMonthPrev = lastThursOfMonthPrev.AddDays(-1);
                }

                return lastThursOfMonthPrev.Month + "/" + lastThursOfMonthPrev.Day + "/" + lastThursOfMonthPrev.Year;
            }
            else
            {
                return lastThursOfMonth.Month + "/" + lastThursOfMonth.Day + "/" + lastThursOfMonth.Year;
            }
*/
        }

        private static string EndDate()
        {
            // JIRA Legacy 37: PRP change window to display the calendar month records.
            //Variables
            var endDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
            return endDate.Month + "/" + endDate.Day + "/" + endDate.Year;

            /* Old code
            var lastDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
            var lastThursOfMonth = lastDayOfMonth;

            //Find last thursday of current month
            while (lastThursOfMonth.DayOfWeek != DayOfWeek.Thursday)
            {
                lastThursOfMonth = lastThursOfMonth.AddDays(-1);
            }

            if (DateTime.Today > lastThursOfMonth)
            {
                //find previous month last thursday
                var lastDayOfMonthNext = new DateTime((DateTime.Today).AddMonths(1).Year, (DateTime.Today).AddMonths(1).Month, DateTime.DaysInMonth((DateTime.Today).AddMonths(1).Year, (DateTime.Today).AddMonths(1).Month));
                var lastThursOfMonthNext = lastDayOfMonthNext;

                //Find last thursday of current month
                while (lastThursOfMonthNext.DayOfWeek != DayOfWeek.Thursday)
                {
                    lastThursOfMonthNext = lastThursOfMonthNext.AddDays(-1);
                }

                return lastThursOfMonthNext.Month + "/" + lastThursOfMonthNext.Day + "/" + lastThursOfMonthNext.Year;
            }
            else
            {
                return lastThursOfMonth.Month + "/" + lastThursOfMonth.Day + "/" + lastThursOfMonth.Year;
            }
            */
        }
        public static string Start { get { return StartDate(); } }
        public static string End { get { return EndDate(); } }
    }

    /// <summary>
    /// Generic response class to return message data.
    /// </summary>
    [Serializable]
    public class ResponseMsg
    {
        //Variables;
        private List<string> _msgList;
        private List<string> _errorList;
        private List<Dictionary<string, string>> _callbackList;
        private Dictionary<string, string> _returnData;
        
        /// <summary>
        /// List of success or confirmation message(s) to display to user
        /// </summary>
        public List<string> msgList { get { return _msgList; } }

        /// <summary>
        /// List of error messages to display
        /// </summary>
        public List<string> errorList { get { return _errorList; } }
        
        /// <summary>
        /// List of callbacks to display
        /// </summary>
        public List<Dictionary<string, string>> callbackList { get { return _callbackList; } }

        /// <summary>
        /// Dictionary of return data
        /// </summary>
        public Dictionary<string, string> returnData { get { return _returnData; } }

        /// <summary>
        /// Boolean to indicate if user message(s) exist
        /// </summary>
        public bool hasMsg { get { if (this._msgList.Count > 0) { return true; } else { return false; } } }

        /// <summary>
        /// Boolean to indicate if error message(s) exist
        /// </summary>
        public bool hasError { get { if (this._errorList.Count > 0) { return true; } else { return false; } } }

        /// <summary>
        /// Boolean to indicate if call back(s) exist
        /// </summary>
        public bool hasCallback { get { if (this._callbackList.Count > 0) { return true; } else { return false; } } }

        /// <summary>
        /// Boolean to indicate if return data exists
        /// </summary>
        public bool hasReturnData { get { if (this._returnData.Count > 0) { return true; } else { return false; } } }

        /// <summary>
        /// Creates a new instance of the ResponseMsg class.
        /// </summary>
        public ResponseMsg()
        {
            this._msgList = new List<string>();
            this._errorList = new List<string>();
            this._callbackList = new List<Dictionary<string, string>>();
            this._returnData = new Dictionary<string, string>();
        }

        /// <summary>
        /// Adds an error message to the response object.
        /// </summary>
        /// <param name="msg">error message to add</param>
        public void addError(string msg)
        {
            //Add error
            _errorList.Add(msg);            
        }

        /// <summary>
        /// Adds a user message to the response object.
        /// </summary>
        /// <param name="msg">user message to add</param>
        public void addMsg(string msg)
        {
            //Add error
            _msgList.Add(msg);
        }
        
        /// <summary>
        /// Indicate callback to specific dialog and returns offending order.
        /// </summary>
        /// <param name="callback">callback? true/false</param>
        public void addCbk(Dictionary<string,string> callback)
        {
            //Add error
            _callbackList.Add(callback);
        }

        /// <summary>
        /// Add return data to the return data dictionary.
        /// </summary>
        /// <param name="key">Must be a unique dictionary key for current variable instance</param>
        /// <param name="data">data to add to key</param>
        public void addReturnData(string key, string data)
        {
            //Add error
            _returnData.Add(key, data);
        }
    }

    /// <summary>
    /// Used to fill autocomplete dialog
    /// </summary>
    [Serializable]
    public class SearchList
    {
        /// <summary>
        /// List of autocomplete suggestions to display
        /// </summary>
        public List<string> listValues { get; set; }

        /// <summary>
        /// Creates a new instance of the class and initialize the list.
        /// </summary>
        public SearchList()
        {
            this.listValues = new List<string>();
        }
    }


    /// <summary>
    /// Object holds a single field and field sort order pair
    /// </summary>
    public class FieldOrder
    {
        /// <summary>
        /// field name to sort by
        /// </summary>
        public string field { get; set; }
        
        /// <summary>
        /// order (asc or desc) to sort field by
        /// </summary>
        public string order { get; set; }

        /// <summary>
        /// Creates a new object instance that is empty
        /// </summary>
        public FieldOrder()
        {}

        /// <summary>
        /// Creates a new object instance with field and order populated.
        /// </summary>
        /// <param name="sort_field">field name to sort by</param>
        /// <param name="sort_order">order (asc or desc) to sort field by</param>
        public FieldOrder(string sort_field, string sort_order)
        {
            this.field = sort_field;
            this.order = sort_order;
        }
    }
    
    /// <summary>
    /// Object holds all values from the group enabled jqGrid sort field parameter
    /// </summary>
    public class GroupSortParam
    {
        /// <summary>
        /// List of fields to group by
        /// </summary>
        public List<FieldOrder> GroupFields { get; set; }

        /// <summary>
        /// List of fields to sort by
        /// </summary>
        public FieldOrder SortField { get; set; }

        /// <summary>
        /// Creates a new instance of the class and initialize the list.
        /// </summary>
        public GroupSortParam()
        {
            this.GroupFields = new List<FieldOrder>();
            this.SortField = new FieldOrder();
        }

        /// <summary>
        /// Creates a new instance of the class and 
        /// initialize the list with values parsed from parameter.
        /// </summary>
        /// <param name="groupSortParam">string to parse sort values from</param>
        public GroupSortParam(string groupSortParam)
        {
            this.GroupFields = new List<FieldOrder>();
            this.SortField = new FieldOrder();

            parseGroupSortParam(groupSortParam);
        }

        /// <summary>
        /// Fills an instantiated object with values 
        /// parsed from supplied parameter string.
        /// </summary>
        /// <param name="groupSortParam">string to parse sort values from</param>
        public void parseGroupSortParam(string groupSortParam)
        {
            try
            {
                //Variables
                int sortPairsIndex = 1;
                
                //Split tring by comma
                string[] sortPairs = groupSortParam.Trim().Split(',');
                
                foreach (string sortPair in sortPairs)
                {
                    //process each grouping, last row is for table sort
                    if (sortPairsIndex < sortPairs.Length)
                    {
                        //Split tring by space
                        string[] fieldOrders = sortPair.Trim().Split(' ');
                        
                        //Add to group list
                        FieldOrder grp = new FieldOrder(fieldOrders[0].Trim(), fieldOrders[1].Trim());
                        this.GroupFields.Add(grp);

                        //increment index
                        sortPairsIndex++;
                    }
                    else
                    {
                        //Add to sort
                        this.SortField.field = sortPair.Trim();                        
                    }
                }
            }
            catch
            {
                throw;
            }
        }
    }

    public class BudgetInfo
    {
        /// <summary>
        /// total budget
        /// </summary>
        public decimal totalBudget { get; set; }

        /// <summary>
        /// total budgeted amount spent
        /// </summary>
        public decimal budgetedSpent { get; set; }

        /// <summary>
        /// total budgeted amount spent
        /// </summary>
        public decimal nonBudgetedSpent { get; set; }

        /// <summary>
        /// Total spent included budgedted and non budgeted.
        /// </summary>
        public decimal totalSpent
        {
            get
            {
                return (this.budgetedSpent + this.nonBudgetedSpent);
            }
        }
        
        /// <summary>
        /// Creates a new budget object instance that is empty
        /// </summary>
        public BudgetInfo()
        {
            this.totalBudget = 0;
            this.budgetedSpent = 0;
            this.nonBudgetedSpent = 0;
        }
    }
}