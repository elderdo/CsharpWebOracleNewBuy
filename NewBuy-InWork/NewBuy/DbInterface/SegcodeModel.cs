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
    /// <summary>
    /// Used to request paged data from segcode grid
    /// </summary>
    internal class SegcodeViewRequest
    {
        public int page { get; set; } //Grid page requested (when multiple pages exist)
        public int rows { get; set; } //Number of rows to display
        public string index { get; set; } //Field to sort by
        public string order { get; set; } //"index" field sort order
        public string filterValue { get; set; } //filter grid by segcode
        public string filterField { get; set; } //filter grid by site

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

            if (!filterValue_isValid())
            {
                return false;
            }

            if (!filterField_isValid())
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
            Type req = typeof(SegcodeGrid.Segcode);
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

        public bool filterValue_isValid()
        {
            //If value exists
            if (!String.IsNullOrEmpty(filterValue) &&
                !String.IsNullOrEmpty(filterField))
            {
                /* Segcode */
                if (filterField == "seg_code")
                {
                    //Must be alphanumeric uppercase, dashes only, no spaces
                    filterValue = filterValue.ToUpper();
                    if (!Regex.IsMatch(filterValue, "^[A-Z0-9-]+$"))
                    {
                        return false;
                    }
                }

                /* Site */
                if (filterField == "site_location")
                {
                    //Must be alphanumeric, allowed: -,/&(space)
                    if (!Regex.IsMatch(filterValue, @"^[a-zA-Z0-9-,/&\s]+$"))
                    {
                        return false;
                    }
                }
            }

            //Valid
            return true;
        }

        public bool filterField_isValid()
        {
            //If value exists
            if (!String.IsNullOrEmpty(filterField))
            {
                //Must be in value list
                if (!Array.Exists(SegcodeSearchField.segcode_search_fields, v => v.Equals(filterField)))
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
    /// Segcode model formatted for data grid,
    /// uses .NET data types for conversion to JSON.
    /// </summary>
    [Serializable]
    public class SegcodeGrid
    {
        public int totalPages { get; set; } //Total pages
        public int currentPage { get; set; } //Current page
        public int totalRows { get; set; } //Total # of data records
        public List<Segcode> rows { get; set; } //List of data records

        /// <summary>
        /// Single grid row that represents a single data record.
        /// </summary>
        [Serializable]
        public class Segcode
        {
            public string seg_code { get; set; } //Display: Segcode
            public string program_code { get; set; } //Display: Program
            public string last_update_date { get; set; } //Display: Updated
            public string last_update_user { get; set; } //Display: Updated By
            public string buy_method { get; set; } //Display: Buy Method
            public string site_location { get; set; } //Display: Site
            public string include_in_tav_reporting { get; set; } //Display: TAV
            public string include_in_spo { get; set; } //Display: Include in SPO
            public string include_in_bolt { get; set; } //Display: Include in BOLT
        }
    }

    /// <summary>
    /// Used to request segcode edit
    /// </summary>
    internal class SegcodeEditRequest
    {
        public string oper { get; set; } //Type of edit
        public string id { get; set; } //Unique ID
        public string seg_code { get; set; } //Display: Segcode
        public string program_code { get; set; } //Display: Program
        public string buy_method { get; set; } //Display: Buy Method
        public string site_location { get; set; } //Display: Site
        public string include_in_tav_reporting { get; set; } //Display: TAV
        public string include_in_spo { get; set; } //Display: Include in SPO
        public string include_in_bolt { get; set; } //Display: Include in BOLT

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!oper_isValid())
            {
                return false;
            }

            if (!id_isValid())
            {
                return false;
            }

            if (!seg_code_isValid())
            {
                return false;
            }

            if (!program_code_isValid())
            {
                return false;
            }

            if (!buy_method_isValid())
            {
                return false;
            }

            if (!site_location_isValid())
            {
                return false;
            }

            if (!include_in_tav_reporting_isValid())
            {
                return false;
            }

            if (!include_in_spo_isValid())
            {
                return false;
            }

            if (!include_in_bolt_isValid())
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
            string[] values = { "edit", "delete", "new" };
            oper = oper.ToLower();
            if (!Array.Exists(values, v => v.Equals(oper)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool id_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(id))
            {
                return false;
            }

            //Must be alphanumeric, no spaces, dashes allowed
            if (!Regex.IsMatch(id, @"^[a-zA-Z0-9-]+$"))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool seg_code_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(seg_code))
            {
                return false;
            }

            //Must be alphanumeric, no spaces, dashes allowed
            if (!Regex.IsMatch(seg_code, @"^[a-zA-Z0-9-]+$"))
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

            //Max 3 characters
            if (program_code.Length > 3)
            {
                return false;
            }

            //Must be alphanumeric, no special characters
            if (!Regex.IsMatch(program_code, "^[a-zA-Z0-9]+$"))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool buy_method_isValid()
        {
            //If value exists
            if (!String.IsNullOrEmpty(buy_method))
            {
                //Must be in value list
                if (!Array.Exists(SegcodeBuyMethod.segcode_buy_methods, v => v.Equals(buy_method)))
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        public bool site_location_isValid()
        {
            //If value exists
            if (!String.IsNullOrEmpty(site_location))
            {
                //Must be alphanumeric, some special characters
                if (!Regex.IsMatch(site_location, @"^[a-zA-Z0-9-_/\,\s&]+$"))
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        public bool include_in_tav_reporting_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(include_in_tav_reporting))
            {
                return false;
            }

            //Must be in value list
            string[] values = { "Y", "N" };
            if (!Array.Exists(values, v => v.Equals(include_in_tav_reporting)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool include_in_spo_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(include_in_spo))
            {
                return false;
            }

            //Must be in value list
            string[] values = { "Y", "N" };
            if (!Array.Exists(values, v => v.Equals(include_in_spo)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool include_in_bolt_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(include_in_bolt))
            {
                return false;
            }

            //Must be in value list
            string[] values = { "Y", "N" };
            if (!Array.Exists(values, v => v.Equals(include_in_bolt)))
            {
                return false;
            }

            //Valid
            return true;
        }

        #endregion Validations
    }

    /// <summary>
    /// Used to request autocomplete suggestions
    /// </summary>
    internal class SegcodeSearchListRequest
    {
        public string filterValue { get; set; } //filter grid by segcode
        public string filterField { get; set; } //filter grid by site

        #region Validations

        public bool _isValid()
        {
            //Validate entire class
            if (!filterField_isValid())
            {
                return false;
            }
            
            if (!filterValue_isValid())
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool filterField_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(filterField))
            {
                return false;
            }

            //Must be in value list
            string[] values = { "seg_code", "site_location" };
            if (!Array.Exists(values, v => v.Equals(filterField)))
            {
                return false;
            }

            //Valid
            return true;
        }

        public bool filterValue_isValid()
        {
            //Value must exist
            if (String.IsNullOrEmpty(filterValue))
            {
                return false;
            }

            /* Segcode */
            if (filterField == "seg_code")
            {
                //Must be alphanumeric uppercase, dashes only, no spaces
                filterValue = filterValue.ToUpper();
                if (!Regex.IsMatch(filterValue, "^[A-Z0-9-]+$"))
                {
                    return false;
                }
            }

            /* Site */
            if (filterField == "site_location")
            {
                //Must be alphanumeric, allowed: -,/&(space)
                if (!Regex.IsMatch(filterValue, @"^[a-zA-Z0-9-,/&\s]+$"))
                {
                    return false;
                }
            }

            //Valid
            return true;
        }

        #endregion Validations
    }
}