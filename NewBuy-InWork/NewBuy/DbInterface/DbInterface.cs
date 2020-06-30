using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using System.Data;
using System.Data.OracleClient;
using NewBuy.Helpers;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace NewBuy.DbInterface
{
    /// <summary>
    /// Contains application database interactions
    /// </summary>
    /// <version>1.0</version>
    /// <author>ESCM Mesa</author>
    /// <copyright>Copyright © 2012 The Boeing Company</copyright>
    public class Interface : IDisposable
    {
        #region Class Constructor and DB Connection Methods

        protected OracleConnection dbConnection;

        public Interface()
        {
            //Initialize connection object
            dbConnection = new OracleConnection();

            //Check if session exists/new session
            if (HttpContext.Current.Session != null)
            {
                //Set connection string from session
                string[] sessionValues = getSessionConnectionInfo();
                setDbConnectionString(sessionValues[0], sessionValues[1], sessionValues[2]);
            }
        }

        /// <summary>
        /// Dispose state.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Implements IDisposable.
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    dbConnection.Dispose();
                    this.dbConnection = null;
                }
                this.disposed = true;
            }
        }

        /// <summary>
        /// Disposes unmanaged resources manually before class goes out of scope.
        /// </summary>
        /// <remarks>Enable if a manual dispose is needed or called.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Parse connection information from session
        /// </summary>
        /// <returns>
        /// String array containing session info (DataSource, UserId, Password).
        /// returns array of "error" if bad session format
        /// </returns>
        private String[] getSessionConnectionInfo()
        {
            //Initialize return array
            string[] sessionValues = { "error", "error", "error" };

            //Check session key for null
            if (HttpContext.Current.Session["Connection"] != null)
            {
                //Get session info by key
                string sessionConnString = HttpContext.Current.Session["Connection"].ToString();

                //Check for correct number of values
                string[] sessionTest = sessionConnString.Split(';');
                if (sessionTest.Length == 3)
                {
                    sessionValues[0] = sessionTest[0];
                    sessionValues[1] = sessionTest[1];
                    sessionValues[2] = sessionTest[2];
                }
            }

            //Return connection string array
            return sessionValues;
        }

        /// <summary>
        /// creates and sets database connection object
        /// </summary>
        /// <param name="dataSource">Database name</param>
        /// <param name="userID">Database User ID</param>
        /// <param name="userPassword">Database Password</param>
        private void setDbConnectionString(String dataSource,
                                            String userID,
                                            String userPassword)
        {
            //Create connection string builder, add default values
            OracleConnectionStringBuilder connBuilder = new OracleConnectionStringBuilder();
            connBuilder.PersistSecurityInfo = false;
            connBuilder.Unicode = true;

            //Replace with params
            if (!String.IsNullOrEmpty(dataSource))
            {
                connBuilder.DataSource = dataSource;
            }

            if (!String.IsNullOrEmpty(userID))
            {
                connBuilder.UserID = userID;
            }

            if (!String.IsNullOrEmpty(userPassword))
            {
                connBuilder.Password = userPassword;
            }

            //Set connection
            dbConnection.ConnectionString = connBuilder.ConnectionString;
        }

        /// <summary>
        /// Set connection information into the session
        /// </summary>
        /// <param name="dataSource">Database name</param>
        /// <param name="userID">Database User ID</param>
        /// <param name="userPassword">Database Password</param>
        internal void setSessionConnectionInfo(String dataSource, String userID, String userPassword)
        {
            //Set class connection object connection string
            setDbConnectionString(dataSource, userID, userPassword);

            try
            {
                //Test connection
                dbConnection.Open();
                dbConnection.Close();

                //set session information
                HttpContext.Current.Session["Connection"] = dataSource + ";" + userID.ToUpper() + ";" + userPassword;
            }
            catch (OracleException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Set permission information into the session
        /// </summary>
        internal void setSessionPermissionInfo()
        {
            try
            {
                //ceate object to hold all roles
                List<string> roles = new List<string>();

                //gather list of roles
                using (OracleCommand dbCommand = dbConnection.CreateCommand())
                {
                    dbCommand.CommandText = "SELECT DISTINCT role FROM session_roles";

                    dbConnection.Open();
                    using (OracleDataReader reader = dbCommand.ExecuteReader())
                    {
                        while (reader.Read() == true)
                        {
                            if (!reader.IsDBNull(reader.GetOrdinal("role")))
                            {
                                string role = reader.GetString(reader.GetOrdinal("role"));
                                roles.Add(role.ToUpper());
                            }
                        }
                    }
                    dbConnection.Close();
                }

                //update session with roles
                roles.ForEach(delegate(string role)
                {
                    switch (role)
                    {
                        case "USERS_ESCM_NBCLS":
                            {
                                HttpContext.Current.Session["cls"] = true;
                                break;
                            }
                        case "USERS_ESCM_NBCCAD":
                            {
                                HttpContext.Current.Session["ccad"] = true;
                                break;
                            }
                        case "USERS_ESCM_NBUK":
                            {
                                HttpContext.Current.Session["uk"] = true;
                                break;
                            }
                        case "USERS_ESCM_REV":
                            {
                                HttpContext.Current.Session["rev"] = true;
                                break;
                            }
                        // to allow for testing in ESCM TEST ONLY
                        case "USERS_WEBUTIL_EXEC":
                        case "USERS_ESCM_BATCH":
                            {
                                HttpContext.Current.Session["cls"] = true;
                                HttpContext.Current.Session["ccad"] = true;
                                HttpContext.Current.Session["uk"] = true;
                                HttpContext.Current.Session["rev"] = true;
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                });
            }
            catch (OracleException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion Class Constructor and DB Connection Methods

        #region New Buy Package

        /// <summary>
        /// Gets a CCN (cost charge number) for supplied program and date
        /// </summary>
        /// <param name="ProgramCode">requirement/order program_code</param>
        /// <param name="DueDate">order due_date</param>
        /// <returns>oracle package result or error message</returns>
        private string getCCN(string ProgramCode, string DueDate, string PartNum)
        {
            string result = "";

            using (OracleCommand dbCommandPrcd = dbConnection.CreateCommand())
            {
                try
                {
                    dbCommandPrcd.CommandType = CommandType.StoredProcedure;
                    dbCommandPrcd.CommandText = "escm.escm_new_buy.get_cost_charge_number";
                    dbCommandPrcd.Parameters.Add("i_program_code", OracleType.VarChar).Value = ProgramCode;
                    dbCommandPrcd.Parameters.Add("i_due_date", OracleType.DateTime).Value = OracleDateTime.Parse(DueDate);
                    dbCommandPrcd.Parameters.Add("i_part_no", OracleType.VarChar).Value = PartNum;
                    dbCommandPrcd.Parameters.Add("v_Return", OracleType.VarChar, 15).Direction = ParameterDirection.ReturnValue;

                    dbConnection.Open();
                    dbCommandPrcd.ExecuteNonQuery();
                    dbConnection.Close();

                    result = dbCommandPrcd.Parameters["v_Return"].Value.ToString();
                }
                catch (OracleException)
                {
                    //failed
                    throw;
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Check if we got a non-empty result
            if (String.IsNullOrEmpty(result))
            {
                result = "CcnNotFound";
            }

            return result;
        }

        /// <summary>
        /// Gets segcode for supplied program
        /// </summary>
        /// <param name="ProgramCode">requirement/order program_code</param>
        /// <returns>oracle package result</returns>
        private string getSegcode(string ProgramCode)
        {
            string result = "";

            using (OracleCommand dbCommandPrcd = dbConnection.CreateCommand())
            {
                try
                {
                    dbCommandPrcd.CommandType = CommandType.StoredProcedure;
                    dbCommandPrcd.CommandText = "escm.escm_new_buy.get_seg_code";
                    dbCommandPrcd.Parameters.Add("i_program_code", OracleType.VarChar).Value = ProgramCode;
                    dbCommandPrcd.Parameters.Add("v_Return", OracleType.VarChar, 25).Direction = ParameterDirection.ReturnValue;

                    dbConnection.Open();
                    dbCommandPrcd.ExecuteNonQuery();
                    dbConnection.Close();

                    result = dbCommandPrcd.Parameters["v_Return"].Value.ToString();
                }
                catch (OracleException)
                {
                    //failed
                    throw;
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// validates part has a gold wharehouse record entry which implies correct setup in gold
        /// </summary>
        /// <param name="PartNum">Part Number</param>
        /// <param name="Segcode">Segcode for Part Number</param>
        /// <returns>true / false from stored procedure</returns>
        private bool is_part_in_gold_whse(string PartNum, string Segcode)
        {
            bool result = false;

            using (OracleCommand dbCommandPrcd = dbConnection.CreateCommand())
            {
                try
                {
                    dbCommandPrcd.CommandType = CommandType.StoredProcedure;
                    dbCommandPrcd.CommandText = "escm.escm_new_buy.is_part_in_gold_whse";
                    dbCommandPrcd.Parameters.Add("i_part_no", OracleType.VarChar).Value = PartNum;
                    dbCommandPrcd.Parameters.Add("i_seg_code", OracleType.VarChar).Value = Segcode;
                    dbCommandPrcd.Parameters.Add("v_Return", OracleType.VarChar, 5).Direction = ParameterDirection.ReturnValue;

                    dbConnection.Open();
                    dbCommandPrcd.ExecuteNonQuery();
                    dbConnection.Close();

                    if (dbCommandPrcd.Parameters["v_Return"].Value.ToString().ToLower() == "true")
                    {
                        result = true;
                    }
                }
                catch (OracleException)
                {
                    //returns fail by default
                }
                catch (Exception)
                {
                    //returns fail by default
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// validates ccn using oracle package function
        /// </summary>
        /// <param name="CCN">ccn to validate</param>
        /// <param name="ProgramCode">requirement/order program_code</param>
        /// <param name="DueDate">order due_date to validate ccn</param>
        /// <returns>oracle package result</returns>
        private bool validateCLSCCN(string CCN, string ProgramCode, string DueDate)
        {
            bool result = false;

            using (OracleCommand dbCommandPrcd = dbConnection.CreateCommand())
            {
                try
                {
                    dbCommandPrcd.CommandType = CommandType.StoredProcedure;
                    dbCommandPrcd.CommandText = "escm.escm_new_buy.validate_ccn";
                    dbCommandPrcd.Parameters.Add("i_ccn", OracleType.VarChar).Value = CCN;
                    dbCommandPrcd.Parameters.Add("i_program_code", OracleType.VarChar).Value = ProgramCode;
                    dbCommandPrcd.Parameters.Add("i_due_date", OracleType.DateTime).Value = OracleDateTime.Parse(DueDate);
                    dbCommandPrcd.Parameters.Add("v_Return", OracleType.VarChar, 5).Direction = ParameterDirection.ReturnValue;

                    dbConnection.Open();
                    dbCommandPrcd.ExecuteNonQuery();
                    dbConnection.Close();

                    if (dbCommandPrcd.Parameters["v_Return"].Value.ToString().ToLower() == "true")
                    {
                        result = true;
                    }
                }
                catch (OracleException)
                {
                    //failed
                    throw;
                }
                catch (Exception)
                {
                    //failed
                    throw;
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets ovhl spo remarks for supplied part number
        /// </summary>
        /// <param name="PartNum">part_no</param>
        /// <returns>oracle package result</returns>
        private string getOvhlPartComment(string PartNum)
        {
            string result = "";

            using (OracleCommand dbCommandPrcd = dbConnection.CreateCommand())
            {
                try
                {
                    dbCommandPrcd.CommandType = CommandType.StoredProcedure;
                    dbCommandPrcd.CommandText = "escm.escm_new_buy.get_ovhl_part_comment";
                    dbCommandPrcd.Parameters.Add("i_part_no", OracleType.VarChar).Value = PartNum;
                    dbCommandPrcd.Parameters.Add("v_comment", OracleType.VarChar, 12000).Direction = ParameterDirection.ReturnValue;

                    dbConnection.Open();
                    dbCommandPrcd.ExecuteNonQuery();
                    dbConnection.Close();

                    result = dbCommandPrcd.Parameters["v_comment"].Value.ToString();
                }
                catch (OracleException)
                {
                    //failed
                    throw;
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets an OVHL CCN (cost charge number) for supplied program and date
        /// </summary>
        /// <param name="ProgramCode">requirement/order program_code</param>
        /// <param name="DueDate">order due_date</param>
        /// <param name="PartNo">part number</param>
        /// <returns>oracle package result or error message</returns>
        private string getOVHLCCN(string ProgramCode, string DueDate, string PartNo)
        {
            string result = "";

            using (OracleCommand dbCommandPrcd = dbConnection.CreateCommand())
            {
                try
                {
                    dbCommandPrcd.CommandType = CommandType.StoredProcedure;
                    dbCommandPrcd.CommandText = "escm.escm_new_buy.get_ovhl_cost_charge_number";
                    dbCommandPrcd.Parameters.Add("i_program_code", OracleType.VarChar).Value = ProgramCode;
                    dbCommandPrcd.Parameters.Add("i_due_date", OracleType.DateTime).Value = OracleDateTime.Parse(DueDate);
                    dbCommandPrcd.Parameters.Add("i_part_no", OracleType.VarChar).Value = PartNo;
                    dbCommandPrcd.Parameters.Add("v_Return", OracleType.VarChar, 15).Direction = ParameterDirection.ReturnValue;

                    dbConnection.Open();
                    dbCommandPrcd.ExecuteNonQuery();
                    dbConnection.Close();

                    result = dbCommandPrcd.Parameters["v_Return"].Value.ToString();
                }
                catch (OracleException)
                {
                    //failed
                    throw;
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Check if we got a non-empty result
            if (String.IsNullOrEmpty(result))
            {
                result = "CcnNotFound";
            }

            return result;
        }

        /// <summary>
        /// Gets an OVHL segcode for supplied program
        /// </summary>
        /// <param name="ProgramCode">requirement/order program_code</param>
        /// <param name="PartNo">part number</param>
        /// <returns>oracle package result</returns>
        private string getOVHLSegcode(string ProgramCode, string PartNo)
        {
            string result = "";

            using (OracleCommand dbCommandPrcd = dbConnection.CreateCommand())
            {
                try
                {
                    dbCommandPrcd.CommandType = CommandType.StoredProcedure;
                    dbCommandPrcd.CommandText = "escm.escm_new_buy.get_ovhl_seg_code";
                    dbCommandPrcd.Parameters.Add("i_program_code", OracleType.VarChar).Value = ProgramCode;
                    dbCommandPrcd.Parameters.Add("i_part_no", OracleType.VarChar).Value = PartNo;
                    dbCommandPrcd.Parameters.Add("v_Return", OracleType.VarChar, 25).Direction = ParameterDirection.ReturnValue;

                    dbConnection.Open();
                    dbCommandPrcd.ExecuteNonQuery();
                    dbConnection.Close();

                    result = dbCommandPrcd.Parameters["v_Return"].Value.ToString();
                }
                catch (OracleException)
                {
                    //failed
                    throw;
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// validates OVHL ccn using oracle package function
        /// </summary>
        /// <param name="CCN">ccn to validate</param>
        /// <param name="ProgramCode">requirement/order program_code</param>
        /// <param name="DueDate">order due_date to validate ccn</param>
        /// <param name="PartNo">part_no</param>
        /// <returns>oracle package result</returns>
        private bool validateOVHLCCN(string CCN, string ProgramCode, string DueDate, string PartNo)
        {
            bool result = false;

            using (OracleCommand dbCommandPrcd = dbConnection.CreateCommand())
            {
                try
                {
                    dbCommandPrcd.CommandType = CommandType.StoredProcedure;
                    dbCommandPrcd.CommandText = "escm.escm_new_buy.validate_ovhl_ccn";
                    dbCommandPrcd.Parameters.Add("i_ccn", OracleType.VarChar).Value = CCN;
                    dbCommandPrcd.Parameters.Add("i_program_code", OracleType.VarChar).Value = ProgramCode;
                    dbCommandPrcd.Parameters.Add("i_due_date", OracleType.DateTime).Value = OracleDateTime.Parse(DueDate);
                    dbCommandPrcd.Parameters.Add("i_part_no", OracleType.VarChar).Value = PartNo;
                    dbCommandPrcd.Parameters.Add("v_Return", OracleType.VarChar, 5).Direction = ParameterDirection.ReturnValue;

                    dbConnection.Open();
                    dbCommandPrcd.ExecuteNonQuery();
                    dbConnection.Close();

                    if (dbCommandPrcd.Parameters["v_Return"].Value.ToString().ToLower() == "true")
                    {
                        result = true;
                    }
                }
                catch (OracleException)
                {
                    //failed
                    throw;
                }
                catch (Exception)
                {
                    //failed
                    throw;
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        #endregion New Buy Package

        #region Shared Application Methods

        /// <summary>
        /// Checks if current user (from session) has permissions to edit segcode
        /// </summary>
        /// <returns>true if user has role, false if user does not</returns>
        internal bool checkSegcodeEditPerm()
        {
            string currentUser = Helper.getSessionConnectionInfoById(1);
            bool result = false;
            List<string> editors = new List<string>();
            OracleDataReader reader = null;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                try
                {
                    dbCommand.CommandText = "select NVL(value_1, ' ') as value_1 " +
                                            "from escm.escm_lookup " +
                                            "where category = 'NEWBUY_SEGCODE_EDITOR'";

                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        string user = reader.GetString(reader.GetOrdinal("value_1"));
                        editors.Add(user);
                    }
                    reader.Close();
                    dbConnection.Close();

                    //User has permission if user ID exists
                    if (editors.Exists(user => user.ToUpper() == currentUser.ToUpper()))
                    {
                        result = true;
                    }
                }
                catch (OracleException)
                {
                    //failed
                    throw;
                }
                catch (Exception)
                {
                    //failed
                    throw;
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Check is order has been sent to gold and no longer editable
        /// </summary>
        /// <param name="OrderNumber"></param>
        /// <returns></returns>
        internal bool orderSent(string OrderNumber)
        {
            bool result = false;
            OracleDataReader reader = null;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                string query = "SELECT activity_status " +
                    "FROM escm.escm_new_buy_order " +
                    "WHERE order_no = :OrderNum";

                dbCommand.CommandText = query;
                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = OrderNumber;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        string status = reader.GetString(reader.GetOrdinal("activity_status"));

                        if (status == "Confirmed Order" ||
                            status == "Sent to the Execution System")
                        {
                            result = true;
                        }
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    //failed ignore
                }
                catch (Exception)
                {
                    //failed ignore
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Check is order has been sent to gold and no longer editable
        /// </summary>
        /// <param name="InternalOrderNumber"></param>
        /// <param name="ScheduleNumber"></param>
        /// <returns></returns>
        internal bool orderSent(int InternalOrderNumber, int ScheduleNumber)
        {
            bool result = false;
            OracleDataReader reader = null;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                string query = "SELECT activity_status " +
                    "FROM escm.escm_new_buy_order " +
                    "WHERE internal_order_no = :OrderNum " +
                        "AND requirement_schedule_no = :ScheduleNum";

                dbCommand.CommandText = query;
                dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = InternalOrderNumber;
                dbCommand.Parameters.Add(":ScheduleNum", OracleType.Int32).Value = ScheduleNumber;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        string status = reader.GetString(reader.GetOrdinal("activity_status"));

                        if (status == "Confirmed Order" ||
                            status == "Sent to the Execution System")
                        {
                            result = true;
                        }
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    //failed ignore
                }
                catch (Exception)
                {
                    //failed ignore
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        #region Mismatch
        /// <summary>
        /// Checks if a Change Reason comment is required
        /// </summary>
        /// <param name="InternalOrderNo">the internal order number</param>
        /// <returns>Returns True if newbuy change reason comment is required</returns>
        internal bool checkReasonRequired(int InternalOrderNo)
        {
            if (checkMismatch(InternalOrderNo))
            {
                if (!checkComment(InternalOrderNo))
                {
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Checks for Mismatch
        /// </summary>
        /// <param name="InternalOrderNo">the internal order number</param>
        /// <returns>Returns true if SPO and Order quantities do not match</returns>
        internal bool checkMismatch(int InternalOrderNo)
        {


            //Retreive quantities for comparison
            int[] quantities = getQuantities(InternalOrderNo);

            int order_quantity = quantities[0];
            int spo_quantity = quantities[1];


            if (spo_quantity != order_quantity)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if there is currently a Newbuy Variance Comment
        /// </summary>
        /// <param name="InternalOrderNo">the Internal Order Number</param>
        /// <returns>Returns true if there is an existing change reason comment</returns>
        internal bool checkComment(int InternalOrderNumber)
        {
            //Note - field in escm_new_buy_requirements table is named asset_managers_remark
            //The actual name of the field should be Newbuy Variance Comment

            OracleDataReader reader = null;
            int asm_remark_cnt = 0;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                string asmQuery = "SELECT count(asset_manager_remark) " +
                                  "FROM escm.escm_new_buy_requirement " +
                                  "WHERE internal_order_no = :InternalOrder_In";

                dbCommand.Parameters.Add(":InternalOrder_In", OracleType.Int32).Value = InternalOrderNumber;

                dbCommand.CommandText = asmQuery;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        asm_remark_cnt = reader.GetInt32(0);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    //failed
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            if (asm_remark_cnt == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the SPO Quantity and Total Order Quantity for a SPO Recommendation
        /// </summary>
        /// <param name="InternalOrderNo">the Internal Order No</param>
        /// <returns>Returns the Total Order Quantity and SPO Quantity</returns>
        internal int[] getQuantities(int InternalOrderNo)
        {
            OracleDataReader reader = null;

            int spo_quantity = 0;
            int order_quantity = 0;

            int[] quantities = new int[2];

            //Search database
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                string cntQuery = "SELECT NVL(req.requirement_quantity,0) as spo_quantity, NVL(ord.ord_ttl,0) as order_total " +
                                  "FROM escm.escm_new_buy_requirement req, " +
                                  "(SELECT internal_order_no, sum(order_quantity) as ord_ttl " +
                                  "FROM escm.escm_new_buy_order " +
                                  "WHERE internal_order_no = :InternalOrder_In " +
                                  "AND activity_status != 'Rejected by Asset Manager' " +
                                  "GROUP BY internal_order_no) ord " +
                                  "WHERE req.internal_order_no = ord.internal_order_no ";

                dbCommand.Parameters.Add(":InternalOrder_In", OracleType.Int32).Value = InternalOrderNo;

                dbCommand.CommandText = cntQuery;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        spo_quantity = reader.GetInt32(0);
                        order_quantity = reader.GetInt32(1);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    //failed
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            quantities[0] = order_quantity;
            quantities[1] = spo_quantity;

            return quantities;
        }

        /// <summary>
        /// Gets data for the requirement asset manager remarks grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal NBVarianceCommentGrid getVarianceCommentGrid(NBVarianceCommentViewRequest request)
        {
            //Variables
            NBVarianceCommentGrid grid = new NBVarianceCommentGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.rows = new List<NBVarianceCommentGrid.NBVarianceComment>();

            //Pull list of available variance comments
            for (int i = 1; i < QtyMismatchComments.qty_mismatch_values.Length; i++)
            {
                NBVarianceCommentGrid.NBVarianceComment gridVar = new NBVarianceCommentGrid.NBVarianceComment();
                gridVar.comment = QtyMismatchComments.qty_mismatch_values[i];
                grid.rows.Add(gridVar);
            }

            //Sort final list
            if (request.order == "asc")
            {
                grid.rows.Sort((x, y) => string.Compare(x.comment, y.comment));
            }
            else
            {
                grid.rows.Sort((x, y) => string.Compare(y.comment, x.comment));
            }

            //Count number of records found
            grid.totalRows = grid.rows.Count;

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// Updates New Buy variance comment using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg editNBVarianceComment(NBVarianceCommentEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            bool flagCommentNull = false;

            //Check for null remark
            if (String.IsNullOrEmpty(request.comment))
            {
                flagCommentNull = true;
            }

            //format list of requirement internal order numbers for query
            string internalOrdNums = request.internalOrderNo.ToDelimitedString(",");
            if (String.IsNullOrEmpty(internalOrdNums))
            {
                internalOrdNums = "";
            }

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {

                //Update change reason for the supplied orders
                if (flagCommentNull)
                {
                    dbCommand.CommandText = "UPDATE ESCM.escm_new_buy_requirement " +
                    "SET asset_manager_remark = null " +
                    "WHERE internal_order_no IN (" + internalOrdNums + ")";
                }
                else
                {
                    dbCommand.CommandText = "UPDATE ESCM.escm_new_buy_requirement " +
                        "SET asset_manager_remark = :VComment " +
                        "WHERE internal_order_no IN (" + internalOrdNums + ")";
                }

                //Add comment param if not null
                if (!flagCommentNull)
                {
                    dbCommand.Parameters.Add(":VComment", OracleType.VarChar).Value = request.comment;
                }

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to save New Buy variance comment.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }
            return result;
        }

        #endregion Mismatch

        #region LTA
        /// <summary>
        /// Gets data for the lta part grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal PartGrid getLtaPartGrid(PartViewRequest request)
        {
            //Variables
            PartGrid grid = new PartGrid();
            grid.totalPages = 0;
            grid.currentPage = request.page;
            grid.rows = new List<PartGrid.Part>();
            OracleDataReader reader = null;
            int rowStart = 0;
            int rowEnd = 0;

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT NVL(Count(*), 0) as totalParts FROM " +
                                        "( " +
                                          "SELECT DISTINCT part_no " +
                                          "FROM escm.escm_lta_pricebreak_mvw " +
                                          "WHERE part_no LIKE :PartFilter " +
                                        ")";
                dbCommand.Parameters.Add(":PartFilter", OracleType.VarChar).Value = request.part_no + "%";

                try
                {
                    dbConnection.Open();
                    var total = dbCommand.ExecuteScalar();
                    dbConnection.Close();

                    //Convert to int
                    grid.totalRows += Convert.ToInt32(total);
                }
                catch
                {
                    //Ignore Error
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }

                    //reset parameters
                    dbCommand.Parameters.Clear();
                }

                //Calc total pages
                grid.totalPages = (int)Math.Ceiling((double)grid.totalRows / (double)request.rows);

                //Handle first page differently
                if (request.page <= 1)
                {
                    //pagination start /end
                    rowStart = 1;
                    rowEnd = request.rows;

                    //Check if end has been reached
                    if (rowEnd > grid.totalRows)
                    {
                        rowEnd = grid.totalRows;
                    }
                }
                else
                {
                    //Check if valid page number, otherwise show last page
                    if (request.page > grid.totalPages)
                    {
                        request.page = grid.totalPages;
                    }

                    //Calculate pagination end
                    rowEnd = request.page * request.rows;

                    //Calculate pagination start
                    rowStart = (rowEnd - request.rows) + 1;

                    //Check if max row reached
                    if (rowEnd > grid.totalRows)
                    {
                        rowEnd = grid.totalRows;
                    }
                }

                dbCommand.CommandText = "SELECT rn.part_no " +
                                        "FROM " +
                                        "( " +
                                          "SELECT parts.part_no, rownum as rnum " +
                                          "FROM " +
                                          "( " +
                                            "SELECT DISTINCT part_no " +
                                            "FROM escm.escm_lta_pricebreak_mvw " +
                                            "WHERE part_no LIKE :PartFilter " +
                                            "ORDER BY " +
                                            (request.index_isValid() ? request.index : "part_no") +
                                            " " +
                                            (request.order_isValid() ? request.order : "asc") +
                                          ") parts " +
                                        ") rn " +
                                        "WHERE rn.rnum between " + rowStart.ToString() + " AND " + rowEnd.ToString();
                dbCommand.Parameters.Add(":PartFilter", OracleType.VarChar).Value = request.part_no + "%";

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        PartGrid.Part row = new PartGrid.Part();
                        row.part_no = reader.GetString(reader.GetOrdinal("part_no"));

                        grid.rows.Add(row);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// get lta grid using request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        internal LtaGrid getLtaGrid(LtaViewRequest request)
        {
            //Variables
            LtaGrid grid = new LtaGrid();
            OracleDataReader reader = null;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT lta, " +
                "NVL(pb_date_selection_ind, '0') as pb_date_selection_ind, " +
                "expired_calendar_date, " +
                  "start_price_break_cal_date, " +
                  "end_price_break_calendar_date, " +
                  "NVL(price_break_start_qty, 0) as price_break_start_qty, " +
                  "NVL(price_break_end_qty, 0) price_break_end_qty, " +
                  "NVL(unit_price, 0) as unit_price " +
                "FROM " +
                  "escm.escm_lta_pricebreak_mvw " +
                "WHERE " +
                  "part_no = :PartNum " +
                "ORDER BY " +
                  "lta asc, " +
                  "end_price_break_calendar_date asc, " +
                  "price_break_start_qty asc";
                dbCommand.Parameters.Add(":PartNum", OracleType.VarChar).Value = request.search;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        DateTime dateTemp;
                        LtaGrid.Lta gridLta = new LtaGrid.Lta();

                        gridLta.lta = reader.GetString(reader.GetOrdinal("lta"));

                        int ltaPriceInd = 0;
                        ltaPriceInd = reader.GetInt32(reader.GetOrdinal("pb_date_selection_ind"));
                        switch (ltaPriceInd)
                        {
                            case 1:
                                {
                                    gridLta.lta += " (Price based on Contract Delivery Date)";
                                    break;
                                }
                            case 2:
                                {
                                    gridLta.lta += " (Price based on Order Place Date)";
                                    break;
                                };
                            default:
                                {
                                    break;
                                }
                        }

                        dateTemp = reader.GetDateTime(reader.GetOrdinal("expired_calendar_date"));
                        gridLta.expired_calendar_date = (dateTemp.Month).ToString() + "/" +
                            (dateTemp.Day).ToString() + "/" +
                            (dateTemp.Year).ToString();

                        dateTemp = reader.GetDateTime(reader.GetOrdinal("start_price_break_cal_date"));
                        gridLta.start_price_break_cal_date = (dateTemp.Month).ToString() + "/" +
                            (dateTemp.Day).ToString() + "/" +
                            (dateTemp.Year).ToString();

                        dateTemp = reader.GetDateTime(reader.GetOrdinal("end_price_break_calendar_date"));
                        gridLta.end_price_break_calendar_date = (dateTemp.Month).ToString() + "/" +
                            (dateTemp.Day).ToString() + "/" +
                            (dateTemp.Year).ToString();

                        gridLta.price_break_start_qty = reader.GetInt32(reader.GetOrdinal("price_break_start_qty"));
                        gridLta.price_break_end_qty = reader.GetInt32(reader.GetOrdinal("price_break_end_qty"));
                        gridLta.unit_price = reader.GetDouble(reader.GetOrdinal("unit_price"));

                        grid.rows.Add(gridLta);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    throw;
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return grid;
        }

        /// <summary>
        /// get part detail for lta report page
        /// </summary>
        /// <param name="part_no"></param>
        /// <returns></returns>
        internal LtaPartDetail getLtaPartDetail(string part_no)
        {
            //Variables
            LtaPartDetail result = new LtaPartDetail();
            OracleDataReader reader = null;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT DISTINCT NVL(prt.Description, '') as Description, " +
                  "NVL(totals.Qty_Plo, 0) as Qty_Plo, " +
                  "NVL(totals.Qty_Pr, 0) as Qty_Pr, " +
                  "NVL(totals.Qty_Forecast, 0) as Qty_Forecast " +
                "FROM escm.escm_lta_Part_totals totals, " +
                  "escm.escm_lta_part prt " +
                "WHERE totals.Part_No(+) = prt.Part_No " +
                "AND totals.Forecast_Year(+) = To_Char(sysdate,'YYYY') + 1 " +
                "AND prt.part_no = :PartNo " +
                "AND rownum < 2";
                dbCommand.Parameters.Add(":PartNo", OracleType.VarChar).Value = part_no;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        result.description = reader.GetString(reader.GetOrdinal("Description"));
                        result.plo_qty = reader.GetDecimal(reader.GetOrdinal("Qty_Plo")).ToString();
                        result.pr_qty = reader.GetDecimal(reader.GetOrdinal("Qty_Pr")).ToString();
                        result.forecast_qty = reader.GetDecimal(reader.GetOrdinal("Qty_Forecast")).ToString();
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    //Ignore error and return nothing
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the last date the lta data was updated
        /// </summary>
        /// <returns>DateTime object</returns>
        internal DateTime getLtaLastUpdate()
        {
            //Variables
            DateTime result = new DateTime();

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT ibi_modified_date " +
                    "FROM ESCM.escm_lta_detail " +
                    "WHERE ibi_modified_date is not null " +
                      "AND rownum = 1";

                try
                {
                    dbConnection.Open();
                    var dbResult = dbCommand.ExecuteScalar();
                    dbConnection.Close();

                    result = Convert.ToDateTime(dbResult);
                }
                catch (OracleException)
                {
                    //Ignore error and return nothing
                }
                catch (Exception)
                {
                    //Ignore error and return nothing
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets UK Program Budget Info
        /// </summary>
        /// <param name="BudgetKey">Budget Table Primary Key</param>
        /// <returns></returns>
        internal BudgetInfo getUKBudgetInfo(string BudgetKey)
        {
            //Variables
            BudgetInfo result = new BudgetInfo();
            OracleDataReader reader = null;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT total_budget, budgeted_spent, non_budgeted_spent " +
                    "FROM escm.escm_uk_bdgt_info " +
                    "WHERE UPPER(order_type) = :Budget";
                dbCommand.Parameters.Add(":Budget", OracleType.VarChar).Value = BudgetKey.ToUpper();

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        result.totalBudget = reader.GetDecimal(reader.GetOrdinal("total_budget"));
                        result.budgetedSpent = reader.GetDecimal(reader.GetOrdinal("budgeted_spent"));
                        result.nonBudgetedSpent = reader.GetDecimal(reader.GetOrdinal("non_budgeted_spent"));
                    }
                    reader.Close();
                    dbConnection.Close();

                    //Round
                    result.totalBudget = Math.Round(result.totalBudget, 2);
                    result.budgetedSpent = Math.Round(result.budgetedSpent, 2);
                }
                catch (OracleException)
                {
                    //Ignore error and return nothing
                }
                catch (Exception)
                {
                    //Ignore error and return nothing
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the LTA pricepoint using the supplied parameters
        /// </summary>
        /// <param name="partNum">Part number</param>
        /// <param name="quantity">Quantity</param>
        /// /// <param name="program">3 Character Program Code</param>
        /// <returns>price, otherwise 0</returns>
        internal decimal getLtaPricePoint(string partNum, int quantity, string program)
        {
            //Variables
            decimal result = 0;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT unit_price " +
                "FROM " +
                    "( " +
                    //Get Order Date Based Price
                        "SELECT lta, NVL(pb_date_selection_ind, 1) as pb_date_selection_ind, expired_calendar_date, start_price_break_cal_date, " +
                          "end_price_break_calendar_date, NVL(price_break_start_qty, 0) as price_break_start_qty, " +
                          "NVL(price_break_end_qty, 0) price_break_end_qty, NVL(unit_price, 0) as unit_price " +
                        "FROM escm.escm_lta_pricebreak_mvw " +
                        "WHERE NVL(pb_date_selection_ind, 1) = 2 " +
                         "AND part_no = :partNumOne " +
                         "AND :qtyOne " +
                          "BETWEEN price_break_start_qty AND price_break_end_qty " +
                         "AND trunc(expired_calendar_date) >= trunc(sysdate) " +
                         "AND trunc(end_price_break_calendar_date) >= trunc(sysdate) " +

                        "UNION ALL " +

                        //Get Delivery Date Based Price
                        "SELECT pb.lta, NVL(pb.pb_date_selection_ind, 1) as pb_date_selection_ind, pb.expired_calendar_date, " +
                          "pb.start_price_break_cal_date, pb.end_price_break_calendar_date, " +
                          "NVL(pb.price_break_start_qty, 0) as price_break_start_qty, " +
                          "NVL(pb.price_break_end_qty, 0) price_break_end_qty, NVL(pb.unit_price, 0) as unit_price " +
                        "FROM escm.escm_lta_pricebreak_mvw pb, " +
                          "( " +
                            "SELECT (trunc(sysdate) + NVL(lead_time, 222)) as deliver_date " +
                            "FROM ESCM.escm_spo_cls_part_vw " +
                            "WHERE part_no = :partNumTwo " +
                            "AND program_code = :programCodeOne " +
                            "UNION ALL " +
                            "SELECT(trunc(sysdate) +  222) " +
                            "FROM dual " +
                            "WHERE NOT EXISTS (  SELECT (trunc(sysdate) + NVL(lead_time, 222)) as deliver_date " +
                                                "FROM ESCM.escm_spo_cls_part_vw " +
                                                "WHERE part_no = :partNumThree " +
                                                "AND program_code = :programCodeTwo ) " +
                          ") dd " +
                        "WHERE NVL(pb.pb_date_selection_ind, 1) = 1 " +
                         "AND pb.part_no = :partNumFour " +
                         "AND :qtyTwo " +
                          "BETWEEN pb.price_break_start_qty AND pb.price_break_end_qty " +
                         "AND trunc(pb.expired_calendar_date) >= dd.deliver_date " +
                         "AND trunc(pb.end_price_break_calendar_date) >= dd.deliver_date " +
                        "ORDER BY pb_date_selection_ind desc, lta asc, end_price_break_calendar_date asc, price_break_start_qty asc " +
                    ") " +
                "WHERE rownum < 2";
                dbCommand.Parameters.Add(":partNumOne", OracleType.VarChar).Value = partNum;
                dbCommand.Parameters.Add(":qtyOne", OracleType.Int32).Value = quantity;
                dbCommand.Parameters.Add(":partNumTwo", OracleType.VarChar).Value = partNum;
                dbCommand.Parameters.Add(":programCodeOne", OracleType.VarChar).Value = program;
                dbCommand.Parameters.Add(":partNumThree", OracleType.VarChar).Value = partNum;
                dbCommand.Parameters.Add(":programCodeTwo", OracleType.VarChar).Value = program;
                dbCommand.Parameters.Add(":partNumFour", OracleType.VarChar).Value = partNum;
                dbCommand.Parameters.Add(":qtyTwo", OracleType.Int32).Value = quantity;

                try
                {
                    dbConnection.Open();
                    var dbResult = dbCommand.ExecuteScalar();
                    dbConnection.Close();

                    result = Convert.ToDecimal(dbResult);
                }
                catch (OracleException)
                {
                    //Ignore error
                }
                catch (Exception)
                {
                    //Ignore error
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        #endregion LTA

        #endregion Shared Application Methods

        #region CLS

        /// <summary>
        /// Gets data for the CLS requirement grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal CLSRequirementGrid getCLSRequirementGrid(CLSRequirementViewRequest request)
        {
            //Variables
            CLSRequirementGrid grid = new CLSRequirementGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.rows = new List<CLSRequirementGrid.CLSRequirement>();
            OracleDataReader reader = null;
            bool useSpoUser = false;

            //If uk program, ignore program code
            if (request.program == "UKC")
            {
                if (String.IsNullOrEmpty(request.assetManager) && String.IsNullOrEmpty(request.part_no))
                {
                    useSpoUser = true;
                    request.assetManager = Helper.getSessionConnectionInfoById(1);
                }
            }
            else
            {
                if (String.IsNullOrEmpty(request.assetManager) && String.IsNullOrEmpty(request.part_no) && String.IsNullOrEmpty(request.program))
                {
                    useSpoUser = true;
                    request.assetManager = Helper.getSessionConnectionInfoById(1);
                }
            }

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //string reqQuery = "SELECT DISTINCT  nb.internal_order_no, " +
                //                                  " nb.spo_export_user, " +
                //                                  " nb.part_no,  " +
                //                                  " NVL(prt.network,' ') as network,  " +
                //                                  " NVL(prt.network_description,' ') as network_description,  " +
                //                                  " NVL(prt.nomenclature,' ') as nomenclature, " +
                //                                  " DECODE (COUNT(DISTINCT sts.activity_status), 0, ' ', 1, ord.activity_status, 'Various') as status, " +
                //                                  " NVL(nb.asset_manager_remark,' ') as change_reason, " +
                //                                  " spo_request_id, " +
                //                                  " nb.program_code, " +
                //                                  " nb.spo_export_date, " +
                //                                  " nb.request_due_date,  " +
                //                                  " nb.item_cost, " +
                //                                  // PRP 4-22 added for new requirement columns
                //                                  " qty.MIN_BUY_QTY, qty.SUPPLIER_MONTHLY_CAPACITY_QTY, qty.ANNUAL_BUY_IND_DESC " +
                //                                  "FROM escm.escm_new_buy_requirement nb, " +
                //                                  " escm.escm_new_buy_order ord, " +
                //                                  " escm.escm_cls_part prt, " +
                //                                  " ( SELECT internal_order_no, activity_status " +
                //                                  "   FROM escm.escm_new_buy_order " +
                //                                  "   WHERE activity_status != 'Confirmed Order' " +
                //                                  "   GROUP BY internal_order_no, activity_status)  sts, " +
                //                                  " ( SELECT sysdate-value_1 as cls_date " +
                //                                  "   FROM escm.escm_lookup " +
                //                                  "   WHERE category = 'NEWBUY_CLS_DAYS') days, " +
                //                                  // PRP 4-22 added for new requirement columns
                //                                  " ( Select PART, MIN_BUY_QTY, SUPPLIER_MONTHLY_CAPACITY_QTY, ANNUAL_BUY_IND_DESC from msms.msms_compass_padd_vw ) qty " +
                //                                  " WHERE nb.internal_order_no = ord.internal_order_no (+) " +
                //                                  " AND nb.internal_order_no = sts.internal_order_no " +
                //                                  " AND nb.part_no = prt.part_no(+) " +
                //                                  " AND nb.program_code = prt.program_code(+) " +
                //                                    ((request.program != "UKC") ? " AND nb.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') " : "") +
                //                                  " AND nb.spo_export_date >= days.cls_date " +
                //                                  // PRP 4-22 added for new requirement columns
                //                                  " AND nb.part_no = qty.part(+) ";
                string reqQuery = "SELECT DISTINCT " +
                                       "internal_order_no, " +
                                       "spo_export_user, " +
                                       "part_no, " +
                                       "ord_ttl, " +
                                       "network, " +
                                       "network_description, " +
                                       "nomenclature, " +
                                       "status, " +
                                       "change_reason, " +
                                       "spo_request_id, " +
                                       "program_code, " +
                                       "spo_export_date, " +
                                       "request_due_date, " +
                                       "item_cost, " +
 //                                      "DECODE(min_buy_qty, '0', ' ', min_buy_qty) as min_buy_qty , " +
 //                                      "DECODE(total_monthly_capacity_qty,'0', ' ', total_monthly_capacity_qty) as total_monthly_capacity_qty, " +
                                       "min_buy_qty, " +
                                       "total_monthly_capacity_qty, " +
                                       "annual_buy_ind";

                if (request.program == "UKC")
                    reqQuery +=  " FROM escm.escm_nb_getUKCReqGrid_VW nb WHERE 1=1 ";
                else reqQuery += " FROM escm.escm_nb_getClsReqGrid_VW nb WHERE 1=1 ";

                if (!String.IsNullOrEmpty(request.assetManager) && useSpoUser)
                {
                    reqQuery += "AND nb.spo_export_user = :spoUser ";
                    dbCommand.Parameters.Add(":spoUser", OracleType.VarChar).Value = request.assetManager;
                }
                else if (!String.IsNullOrEmpty(request.assetManager) && !useSpoUser)
                {
                    reqQuery += "AND UPPER(nb.asset_manager_id)  = UPPER(:asset_manager) ";
                    dbCommand.Parameters.Add(":asset_manager", OracleType.VarChar).Value = request.assetManager;
                }

                if (!String.IsNullOrEmpty(request.part_no))
                {
                    reqQuery += "AND nb.part_no = :part_no ";
                    dbCommand.Parameters.Add(":part_no", OracleType.VarChar).Value = request.part_no;
                }

                if (!String.IsNullOrEmpty(request.program))
                {
                    reqQuery += "AND nb.program_code = :program ";
                    dbCommand.Parameters.Add(":program", OracleType.VarChar).Value = request.program;
                }

                if (!request.viewHistory)
                {
                    reqQuery += "AND ( nb.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ";
                // PRP 122016 - JIRA Legacy 37 - change interface to display the calendar month and any unprocessed order
                    reqQuery += "OR EXISTS (SELECT 1 FROM escm.escm_new_buy_order nbo WHERE nbo.internal_order_no = nb.internal_order_no AND nbo.activity_status in ( 'Awaiting Asset Manager Review'))) ";
                }
                // PRP 4-22 modified for new requirement columns
                reqQuery += "GROUP BY nb.internal_order_no, nb.spo_export_user, nb.part_no,ord_ttl, nb.network, nb.network_description, nb.nomenclature, nb.status, nb.spo_request_id, nb.item_cost, nb.program_code, nb.spo_export_date, nb.request_due_date, nb.change_reason, nb.MIN_BUY_QTY, nb.TOTAL_MONTHLY_CAPACITY_QTY, nb.ANNUAL_BUY_IND " +
                            "ORDER BY " +
                             (request.index_isValid() ? request.index : "spo_export_date") +
                             " " +
                             (request.order_isValid() ? request.order : "desc");

                dbCommand.CommandText = reqQuery;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        DateTime dateTemp;

                        CLSRequirementGrid.CLSRequirement gridReq = new CLSRequirementGrid.CLSRequirement();
                        gridReq.internal_order_no = reader.GetInt32(reader.GetOrdinal("internal_order_no"));
                        gridReq.spo_export_user = reader.GetString(reader.GetOrdinal("spo_export_user"));
                        gridReq.part_no = reader.GetString(reader.GetOrdinal("part_no"));
                        gridReq.network = reader.GetString(reader.GetOrdinal("network_description"));
                        gridReq.description = reader.GetString(reader.GetOrdinal("nomenclature"));
                        gridReq.status = reader.GetString(reader.GetOrdinal("status"));
                        gridReq.change_reason = reader.GetString(reader.GetOrdinal("change_reason"));
                        gridReq.spo_request_id = reader.GetInt32(reader.GetOrdinal("spo_request_id"));
                        gridReq.program_code = reader.GetString(reader.GetOrdinal("program_code"));
                        // PRP 4-22 modified for new requirement columns
                        gridReq.order_quantity = reader.GetString(reader.GetOrdinal("ord_ttl"));
                        gridReq.min_buy_qty = reader.GetString(reader.GetOrdinal("min_buy_qty"));
                        //                            gridReq.min_buy_qty = "THIS IS A PLACE HOLDER";
                        gridReq.total_monthly_capacity_qty = reader.GetString(reader.GetOrdinal("total_monthly_capacity_qty"));
                        //                            gridReq.total_monthly_capacity_qty = "THIS IS A PLACE HOLDER TOO"; ;
                        gridReq.annual_buy_ind = reader.GetString(reader.GetOrdinal("annual_buy_ind"));

                        dateTemp = reader.GetDateTime(reader.GetOrdinal("spo_export_date"));
                        gridReq.spo_export_date = (dateTemp.Month).ToString() + "/" +
                            (dateTemp.Day).ToString() + "/" +
                            (dateTemp.Year).ToString();

                        dateTemp = reader.GetDateTime(reader.GetOrdinal("request_due_date"));
                        gridReq.request_due_date = (dateTemp.Month).ToString() + "/" +
                            (dateTemp.Day).ToString() + "/" +
                            (dateTemp.Year).ToString();

                        gridReq.item_cost = reader.GetDecimal(reader.GetOrdinal("item_cost"));

                        grid.rows.Add(gridReq);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    //Ignore Error???
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Count number of records found
            grid.totalRows = grid.rows.Count;

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// Gets data for the CLS order grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal CLSOrderGrid getCLSOrderGrid(CLSOrderViewRequest request)
        {
            //Variables
            CLSOrderGrid grid = new CLSOrderGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.rows = new List<CLSOrderGrid.CLSOrder>();
            OracleDataReader reader = null;

            //format list of requirement internal order numbers for query
            string requirementOrderNum = request.orderNumKey.ToDelimitedString(",");
            if (String.IsNullOrEmpty(requirementOrderNum))
            {
                requirementOrderNum = "";
            }

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "WITH ord_ttl AS ( " +
                    "SELECT internal_order_no, sum(order_quantity) as ord_ttl " +
                    "FROM escm.escm_new_buy_order " +
                    "WHERE internal_order_no IN (" + requirementOrderNum + ") " +
                    "AND activity_status != 'Rejected by Asset Manager' " +
                    "GROUP BY internal_order_no) " +
                    "SELECT " +
                    "nb.internal_order_no," +
                    "nb.requirement_schedule_no," +
                    "req.spo_request_id," +
                    "nb.due_date," +
                    "NVL(nb.order_quantity, 0) as order_quantity," +
                    "nb.priority," +
                    "NVL(nb.cost_charge_number, ' ') as cost_charge_number," +
                    "NVL(req.asset_manager_remark,' ') as change_reason," +
                    "nb.order_no," +
                    "nb.activity_status," +
                    "nb.part_no, " +
                    "NVL2(pb.part_no, 'true', 'false') as pricebreak, " +
                    "req.requirement_quantity as spo_qty, " +
                    "NVL(ord.ord_ttl, 0) as order_total " +
                    "FROM escm.escm_new_buy_order nb, " +
                    "     escm.escm_new_buy_requirement req, " +
                    "     (select distinct part_no from escm.escm_lta_pricebreak_mvw) pb, " +
                    "     ord_ttl ord " +
                    "WHERE nb.internal_order_no = ord.internal_order_no(+)" +
                    "AND req.internal_order_no  = nb.internal_order_no " +
                    "AND nb.part_no = pb.part_no(+) " +
                    "AND nb.internal_order_no IN (" + requirementOrderNum + ") " +
                    "ORDER BY spo_request_id asc, " + request.index + " " + request.order;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        DateTime dateTemp;

                        CLSOrderGrid.CLSOrder gridOrd = new CLSOrderGrid.CLSOrder();
                        gridOrd.internal_order_no = reader.GetInt32(reader.GetOrdinal("internal_order_no"));
                        gridOrd.requirement_schedule_no = reader.GetInt32(reader.GetOrdinal("requirement_schedule_no"));
                        gridOrd.spo_request_id = reader.GetInt32(reader.GetOrdinal("spo_request_id"));

                        dateTemp = reader.GetDateTime(reader.GetOrdinal("due_date"));
                        gridOrd.due_date = (dateTemp.Month).ToString() + "/" +
                            (dateTemp.Day).ToString() + "/" +
                            (dateTemp.Year).ToString();

                        gridOrd.part_no = reader.GetString(reader.GetOrdinal("part_no"));
                        gridOrd.order_no = reader.GetString(reader.GetOrdinal("order_no"));
                        gridOrd.order_quantity = reader.GetInt32(reader.GetOrdinal("order_quantity"));
                        gridOrd.priority = reader.GetInt32(reader.GetOrdinal("priority"));
                        gridOrd.cost_charge_number = reader.GetString(reader.GetOrdinal("cost_charge_number"));
                        gridOrd.change_reason = reader.GetString(reader.GetOrdinal("change_reason"));
                        gridOrd.activity_status = reader.GetString(reader.GetOrdinal("activity_status"));
                        gridOrd.spo_qty = reader.GetInt32(reader.GetOrdinal("spo_qty"));
                        gridOrd.order_total = reader.GetInt32(reader.GetOrdinal("order_total"));

                        var hasLTA = reader.GetString(reader.GetOrdinal("pricebreak"));
                        gridOrd.pricebreak = Convert.ToBoolean(hasLTA);

                        gridOrd.pricePoint = 0;

                        grid.rows.Add(gridOrd);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }

                //Get pricepoint for each LTA part
                foreach (CLSOrderGrid.CLSOrder ord in grid.rows)
                {
                    //Only check parts that have an LTA
                    if (ord.pricebreak)
                    {
                        try
                        {
                            //Find pricepoint
                            ord.pricePoint = getLtaPricePoint(ord.part_no, ord.order_quantity, ord.order_no.Substring(0, 3));
                        }
                        catch (Exception)
                        {
                            //Ignore Errors and Move on
                        }
                    }
                }
            }

            //Count number of records found
            grid.totalRows = grid.rows.Count;

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// Edits a cls order using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg editCLSOrder(CLSOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            string status = "";
            string changeReason = request.change_reason;
            bool flagSetApprovedDate = false;

            //Check order is still editable
            if (orderSent(request.internal_order_no, request.requirement_schedule_no))
            {
                result.addError("Order changes not allowed after sent to gold.");
                return result;
            }

            //Check order is still editable
            if (orderSent(request.internal_order_no, request.requirement_schedule_no))
            {
                result.addError("Order changes not allowed after sent to gold.");
                return result;
            }

            //Get requirement relationship keys
            CLSOrderReqData reqData = getCLSOrderReqData(request.internal_order_no);

            //Clear CCN Error Message
            if (request.cost_charge_number.ToUpper() == "CCNNOTFOUND")
            {
                request.cost_charge_number = "";
            }

            //get CCN when blank
            if (String.IsNullOrEmpty(request.cost_charge_number))
            {
                //Find ccn
                request.cost_charge_number = getCCN(reqData.program_code, request.due_date, reqData.part_no);
            }

            //get activity status
            switch (request.activity_status)
            {
                case 20: //Review
                    {
                        status = OrderActivityStatus.order_status_values[0];
                        break;
                    }
                case 30: //Approved
                    {
                        //Validate CCN
                        if (validateCLSCCN(request.cost_charge_number, reqData.program_code, request.due_date))
                        {
                            //Return validation to user
                            result.addReturnData((reqData.program_code + reqData.spo_request_id + request.requirement_schedule_no.ToString("00")),
                                "true");
                            status = OrderActivityStatus.order_status_values[1];
                            flagSetApprovedDate = true;
                        }
                        else
                        {
                            //Return validation to user
                            result.addReturnData((reqData.program_code + reqData.spo_request_id + request.requirement_schedule_no.ToString("00")),
                                "false");
                            result.addError("Your order was saved but the CCN was invalid. Status changed to awaiting review.");
                            status = OrderActivityStatus.order_status_values[0];
                        }
                        break;
                    }
                case 40: //Rejected
                    {
                        status = OrderActivityStatus.order_status_values[2];
                        flagSetApprovedDate = true;

                        //If no comment is specified, Rejected is default
                        if (changeReason == "")
                        {
                            changeReason = "Rejected";
                        }

                        break;
                    }
                default:
                    {
                        status = OrderActivityStatus.order_status_values[0];
                        break;
                    }
            }

            /* Update the Orders Table */
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                string query = "UPDATE escm.escm_new_buy_order " +
                    "SET due_date = :DueDate, " +
                    "order_quantity = :Qty, " +
                    "priority = :Priority, ";

                if (flagSetApprovedDate)
                {
                    query += "amgr_approval_date = :AmgrApproval, ";
                }
                else
                {
                    query += "amgr_approval_date = null, ";
                }

                query += "cost_charge_number = :CCN, " +
                    "activity_status = :Status " +
                    "WHERE internal_order_no = :OrderNum " +
                        "AND requirement_schedule_no = :ScheduleNum";

                dbCommand.CommandText = query;
                dbCommand.Parameters.Add(":DueDate", OracleType.DateTime).Value = OracleDateTime.Parse(request.due_date);
                dbCommand.Parameters.Add(":Qty", OracleType.Int32).Value = request.order_quantity;
                dbCommand.Parameters.Add(":Priority", OracleType.Int32).Value = request.priority;
                if (flagSetApprovedDate)
                {
                    dbCommand.Parameters.Add(":AmgrApproval", OracleType.DateTime).Value = DateTime.Today;
                }
                dbCommand.Parameters.Add(":CCN", OracleType.VarChar).Value = request.cost_charge_number;
                dbCommand.Parameters.Add(":Status", OracleType.VarChar).Value = status;
                dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = request.internal_order_no;
                dbCommand.Parameters.Add(":ScheduleNum", OracleType.Int32).Value = request.requirement_schedule_no;

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to save order changes.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            /* If Approved and Order quantities match, set Variance Comment to 'Accept' */
            if (String.IsNullOrEmpty(changeReason) &&
                !checkMismatch(request.internal_order_no))
            {
                if (status == OrderActivityStatus.order_status_values[1])
                {
                    changeReason = "Accept";
                }
            }


            /* Update the Requirements table - Variance Comment - For Non-prompted changes*/
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                string query = "UPDATE escm.escm_new_buy_requirement " +
                               "SET asset_manager_remark = :ChangeReason " +
                               "WHERE internal_order_no = :OrderNum ";

                dbCommand.CommandText = query;
                dbCommand.Parameters.Add(":ChangeReason", OracleType.VarChar).Value = changeReason;
                dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = request.internal_order_no;

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to save order changes.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            /* Check for mismatch between SPO and Ordered */
            if (checkReasonRequired(request.internal_order_no))
            {
                string internalOrderNo = request.internal_order_no.ToString();

                Dictionary<string, string> required = new Dictionary<string, string>();
                required.Add(internalOrderNo, "true");

                //Flag for call back, 
                //0 position for Variance Comment dialog
                result.addCbk(required);
            }

            return result;
        }

        /// <summary>
        /// Deletes a cls order using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg deleteCLSOrder(CLSOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();

            //Check order is still editable
            if (orderSent(request.internal_order_no, request.requirement_schedule_no))
            {
                result.addError("Order changes not allowed after sent to gold.");
                return result;
            }

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "DELETE FROM escm.escm_new_buy_order " +
                    "WHERE internal_order_no = :OrderNum " +
                        "AND requirement_schedule_no = :ScheduleNum";
                dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = request.internal_order_no;
                dbCommand.Parameters.Add(":ScheduleNum", OracleType.Int32).Value = request.requirement_schedule_no;

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to delete order.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Adds a new cls order using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg newCLSOrder(CLSOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            string orderNum = "";
            string segCode = "";
            string scheduleTemp = "";
            string status = "";
            string changeReason = "";
            bool flagSetApprovedDate = false;

            //Get requirement relationship keys
            CLSOrderReqData reqData = getCLSOrderReqData(request.internal_order_no);

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                try
                {
                    changeReason = request.change_reason;

                    //Get max schedule number
                    dbCommand.CommandText = "SELECT MAX(requirement_schedule_no) as req " +
                                            "FROM escm.escm_new_buy_order " +
                                            "WHERE internal_order_no = :OrderNum";
                    dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = request.internal_order_no;

                    dbConnection.Open(); //Open Connection
                    var maxScheduleNo = dbCommand.ExecuteScalar(); //Execute query
                    dbConnection.Close(); //Close Connection
                    dbCommand.Parameters.Clear(); //Clear params for next query

                    //Convert to int datatype
                    if (maxScheduleNo.GetType() == DBNull.Value.GetType())
                    {
                        maxScheduleNo = 0;
                    }
                    request.requirement_schedule_no = Convert.ToInt32(maxScheduleNo);

                    //Increment by 1 for new record
                    request.requirement_schedule_no = request.requirement_schedule_no + 1;

                    //Generate Order number
                    scheduleTemp = request.requirement_schedule_no.ToString();
                    if (scheduleTemp.Length == 1)
                    {
                        scheduleTemp = "0" + scheduleTemp;
                    }
                    orderNum = reqData.program_code + reqData.spo_request_id.ToString() + scheduleTemp;

                    //get segcode, set to " " if no result
                    segCode = getSegcode(reqData.program_code);
                    if (String.IsNullOrEmpty(segCode))
                    {
                        segCode = " ";
                    }

                    //get CCN when blank
                    if (String.IsNullOrEmpty(request.cost_charge_number))
                    {
                        request.cost_charge_number = getCCN(reqData.program_code, request.due_date, reqData.part_no);
                    }

                    //get activity status
                    switch (request.activity_status)
                    {
                        case 20: //Review
                            {
                                status = OrderActivityStatus.order_status_values[0];
                                break;
                            }
                        case 30: //Approved
                            {
                                if (validateCLSCCN(request.cost_charge_number, reqData.program_code, request.due_date))
                                {
                                    //Return validation to user
                                    result.addReturnData((reqData.program_code + reqData.spo_request_id + request.requirement_schedule_no.ToString("00")),
                                        "true");
                                    status = OrderActivityStatus.order_status_values[1];
                                    flagSetApprovedDate = true;
                                }
                                else
                                {
                                    //Return validation to user
                                    result.addReturnData((reqData.program_code + reqData.spo_request_id + request.requirement_schedule_no.ToString("00")),
                                        "false");
                                    result.addError("Your order was saved but the CCN was invalid. Status changed to awaiting review.");
                                    status = OrderActivityStatus.order_status_values[0];
                                }
                                break;
                            }
                        case 40: //Rejected
                            {
                                status = OrderActivityStatus.order_status_values[2];
                                flagSetApprovedDate = true;

                                //If no comment already specified, Rejected is default
                                if (!checkComment(request.internal_order_no))
                                {
                                    if (String.IsNullOrEmpty(changeReason))
                                    {
                                        changeReason = "Rejected";
                                    }
                                }

                                break;
                            }
                        default:
                            {
                                status = OrderActivityStatus.order_status_values[0];
                                break;
                            }
                    }

                    //Insert new record
                    string query = "INSERT INTO escm.escm_new_buy_order " +
                        "(INTERNAL_ORDER_NO, " +
                        "REQUIREMENT_SCHEDULE_NO, " +
                        "ORDER_NO, " +
                        "PART_NO, " +
                        "SEG_CODE, " +
                        "AMGR_APPROVAL_DATE, " +
                        "ORDER_QUANTITY, " +
                        "DUE_DATE, " +
                        "COST_CHARGE_NUMBER, " +
                        "ACTIVITY_STATUS, " +
                        "PRIORITY, " +
                        "VALIDATION_STATUS) " +
                        "VALUES ( " +
                            ":IntOrderNum, " +//INTERNAL_ORDER_NO
                            ":ReqSchNum, " + //REQUIREMENT_SCHEDULE_NO
                            ":OrderNum, " + //ORDER_NO
                            ":PartNum, " + //PART_NO
                            ":SegCode, "; //SEG_CODE

                    if (flagSetApprovedDate)
                    {
                        query += ":AmgrApproval, "; //AMGR_APPROVAL_DATE
                    }
                    else
                    {
                        query += "null, "; //AMGR_APPROVAL_DATE
                    }

                    query += ":Qty, " + //ORDER_QUANTITY
                            ":DueDate, " + //DUE_DATE
                            ":CCN, " + //COST_CHARGE_NUMBER
                            ":Status, " + //ACTIVITY_STATUS
                            ":Priority, " + //PRIORITY
                            ":ValidStatus )"; //VALIDATION_STATUS

                    dbCommand.CommandText = query;
                    dbCommand.Parameters.Add(":IntOrderNum", OracleType.Int32).Value = request.internal_order_no;
                    dbCommand.Parameters.Add(":ReqSchNum", OracleType.Int32).Value = request.requirement_schedule_no;
                    dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = orderNum;
                    dbCommand.Parameters.Add(":PartNum", OracleType.VarChar).Value = reqData.part_no;
                    dbCommand.Parameters.Add(":SegCode", OracleType.VarChar).Value = segCode;
                    if (flagSetApprovedDate)
                    {
                        dbCommand.Parameters.Add(":AmgrApproval", OracleType.DateTime).Value = DateTime.Today;
                    }
                    dbCommand.Parameters.Add(":Qty", OracleType.Int32).Value = request.order_quantity;
                    dbCommand.Parameters.Add(":DueDate", OracleType.DateTime).Value = OracleDateTime.Parse(request.due_date);
                    dbCommand.Parameters.Add(":CCN", OracleType.VarChar).Value = request.cost_charge_number;
                    dbCommand.Parameters.Add(":Status", OracleType.VarChar).Value = status;
                    dbCommand.Parameters.Add(":Priority", OracleType.Int32).Value = request.priority;
                    dbCommand.Parameters.Add(":ValidStatus", OracleType.VarChar).Value = "F";

                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to save new order.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            /* If Approved and Order quantities match, set Variance Comment to 'Accept' when blank */
            if (String.IsNullOrEmpty(changeReason) && !checkMismatch(request.internal_order_no))
            {
                if (status == OrderActivityStatus.order_status_values[1])
                {
                    changeReason = "Accept";
                }
            }

            /* Update the Requirements table - SPO Mismatch (Change Reason) Comment*/
            if (changeReason != "")
            {
                using (OracleCommand dbCommand = dbConnection.CreateCommand())
                {
                    string query = "UPDATE escm.escm_new_buy_requirement " +
                                   "SET asset_manager_remark = :ChangeReason " +
                                   "WHERE internal_order_no = :OrderNum ";

                    dbCommand.CommandText = query;
                    dbCommand.Parameters.Add(":ChangeReason", OracleType.VarChar).Value = changeReason;
                    dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = request.internal_order_no;

                    try
                    {
                        dbConnection.Open();
                        dbCommand.ExecuteNonQuery();
                        dbConnection.Close();
                    }
                    catch (OracleException oe)
                    {
                        result.addError("Unable to save order changes.");
                        result.addError(oe.ToString());
                    }
                    finally
                    {
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }
                    }
                }
            }


            if (checkReasonRequired(request.internal_order_no))
            {
                string internalOrderNo = request.internal_order_no.ToString();

                Dictionary<string, string> required = new Dictionary<string, string>();
                required.Add(internalOrderNo, "true");

                //Flag for call back, 
                //0 position for Variance Comment dialog
                result.addCbk(required);
            }

            return result;
        }

        /// <summary>
        /// Deletes multiple cls orders using the supplied request
        /// </summary>
        /// <param name="request">validated request object</param>
        /// <returns>ResponseMsg object</returns>
        internal ResponseMsg deleteCLSOrders(CLSMultiOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            int seqNum = 0;
            bool attemptLastOrderDelete = false;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //process each order number for deletion
                request.orders.ForEach(delegate(string order)
                {
                    try
                    {
                        //clear params
                        dbCommand.Parameters.Clear();

                        //Check order is still editable
                        if (orderSent(order))
                        {
                            result.addMsg("Cannot edit " + order + ", order already sent to gold.");
                        }
                        else
                        {
                            //get sequence number (last two characters)
                            seqNum = Convert.ToInt32(order.Substring(order.Length - 2, 2));

                            //set order to rejected if sequence 1, otherwise delete
                            if (seqNum < 2)
                            {
                                //Flag message to user
                                attemptLastOrderDelete = true;

                                //set status
                                dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                    "set activity_status = :activitySts, " +
                                    "amgr_approval_date = sysdate " +
                                    "where order_no = :orderNum";

                                //set param
                                dbCommand.Parameters.AddWithValue(":activitySts", OrderActivityStatus.order_status_values[2]);
                                dbCommand.Parameters.AddWithValue(":orderNum", order);
                            }
                            else
                            {
                                //set command
                                dbCommand.CommandText = "DELETE FROM escm.escm_new_buy_order " +
                                    "WHERE order_no = :orderNum";

                                //set param
                                dbCommand.Parameters.AddWithValue(":orderNum", order);
                            }

                            //run command
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();
                        }
                    }
                    catch (Exception)
                    {
                        result.addError("Error Deleting Order: " + order);
                    }
                    finally
                    {
                        //Close connection
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }
                    }
                });
            }

            //Add message if last order was set to reject
            if (attemptLastOrderDelete)
            {
                result.addMsg("Recommendations must have at least 1 associated order. " +
                    "Orders ending in '01' have been marked as rejected.");
            }

            return result;
        }

        /// <summary>
        /// Rejects multiple cls orders using the supplied request
        /// </summary>
        /// <param name="request">validated request object</param>
        /// <returns>ResponseMsg object</returns>
        internal ResponseMsg rejectCLSOrders(CLSMultiOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //process each order number for deletion
                request.orders.ForEach(delegate(string order)
                {
                    try
                    {
                        //Check order is still editable
                        if (orderSent(order))
                        {
                            result.addMsg("Cannot edit " + order + ", order already sent to gold.");
                        }
                        else
                        {
                            //set command
                            dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                "set activity_status = :activitySts, " +
                                "amgr_approval_date = sysdate " +
                                "where order_no = :orderNum";

                            //set param
                            dbCommand.Parameters.AddWithValue(":activitySts", OrderActivityStatus.order_status_values[2]);
                            dbCommand.Parameters.Add(":orderNum", OracleType.VarChar, 20);

                            //set params value
                            dbCommand.Parameters[":orderNum"].Value = order;

                            //run command
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();

                            //Clear params for next query 
                            dbCommand.Parameters.Clear();

                            //set command to update requirement table
                            dbCommand.CommandText = "update ESCM.escm_new_buy_requirement " +
                                                     "set asset_manager_remark = 'Rejected' " +
                                                     "where internal_order_no in (select req.internal_order_no " +
                                                            "from ESCM.escm_new_buy_requirement req, ESCM.escm_new_buy_order ord " +
                                                            "where ord.internal_order_no = req.internal_order_no  " +
                                                            "and ord.order_no = :orderNum " +
                                                            "and (req.asset_manager_remark is null or req.asset_manager_remark = 'Accept')) ";

                            //set param
                            dbCommand.Parameters.Add(":orderNum", OracleType.VarChar, 20);
                            //set params value
                            dbCommand.Parameters[":orderNum"].Value = order;

                            //run command
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();
                        }
                    }
                    catch (Exception)
                    {
                        result.addError("Error Rejecting Order: " + order);
                    }
                    finally
                    {
                        //Close connection
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }
                    }
                });
            }

            return result;
        }

        /// <summary>
        /// Validates multiple cls orders using the supplied request
        /// </summary>
        /// <param name="request">validated request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg validateCLSOrders(CLSMultiOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            OracleDataReader reader = null;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //approve each order_no
                request.orders.ForEach(delegate(string order)
                {
                    try
                    {
                        //Order processing variables
                        DateTime dateTemp;
                        string ccn = "";
                        string programCode = "";
                        string dueDate = "";

                        //set command object properties
                        dbCommand.CommandText = "select NVL(ord.cost_charge_number, '') as cost_charge_number, " +
                            "NVL(req.program_code, '') as program_code, NVL(ord.due_date, '') as due_date " +
                            "from ESCM.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                            "where ord.order_no = :OrderNum " +
                                "and ord.internal_order_no = req.internal_order_no(+)";
                        dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;

                        //Get required new buy validation parameters
                        dbConnection.Open();
                        reader = dbCommand.ExecuteReader();
                        while (reader.Read() == true)
                        {
                            ccn = reader.GetString(reader.GetOrdinal("cost_charge_number"));
                            programCode = reader.GetString(reader.GetOrdinal("program_code"));

                            dateTemp = reader.GetDateTime(reader.GetOrdinal("due_date"));
                            dueDate = (dateTemp.Month).ToString() + "/" +
                                (dateTemp.Day).ToString() + "/" +
                                (dateTemp.Year).ToString();
                        }
                        reader.Close();
                        dbConnection.Close();

                        //Clear params for next query
                        dbCommand.Parameters.Clear();

                        //Validate ccn
                        if (validateCLSCCN(ccn, programCode, dueDate))
                        {
                            //Return validation to user
                            result.addReturnData(order, "true");
                        }
                        else
                        {
                            //Return validation to user
                            result.addReturnData(order, "false");
                        }
                    }
                    catch (Exception)
                    {
                        result.addMsg("Error Processing Order: " + order);
                    }
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Close();
                        }

                        //Make sure connection is closed
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }

                        //Make sure parameters are reset
                        if (dbCommand.Parameters.Count > 0)
                        {
                            dbCommand.Parameters.Clear();
                        }
                    }

                });
            }

            return result;
        }

        /// <summary>
        /// Approves multiple cls asset manager orders using the supplied request
        /// </summary>
        /// <param name="request">validated request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg approveCLSAmOrders(CLSMultiOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            OracleDataReader reader = null;
            int invalidCCNCount = 0;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //approve each order_no
                request.orders.ForEach(delegate(string order)
                {
                    try
                    {
                        //Check order is still editable
                        if (orderSent(order))
                        {
                            result.addMsg("Cannot edit " + order + ", order already sent to gold.");
                        }
                        else
                        {
                            //Order processing variables
                            DateTime dateTemp;
                            int internalOrderNo = 0;

                            string ccn = "";
                            string programCode = "";
                            string dueDate = "";
                            string partNum = "";
                            bool validCCN = false;
                            string changeReason = "";

                            //set command object properties
                            dbCommand.CommandText = "select ord.internal_order_no, NVL(ord.cost_charge_number, '') as cost_charge_number, " +
                                "NVL(req.program_code, '') as program_code, ord.part_no, NVL(ord.due_date, '') as due_date, " +
                                "NVL(req.asset_manager_remark, '') as reason_code " +
                                "from ESCM.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                                "where ord.order_no = :OrderNum " +
                                    "and ord.internal_order_no = req.internal_order_no(+)";
                            dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;

                            //Get required new buy validation parameters
                            dbConnection.Open();
                            reader = dbCommand.ExecuteReader();
                            while (reader.Read() == true)
                            {
                                internalOrderNo = reader.GetInt32(reader.GetOrdinal("internal_order_no"));

                                ccn = reader.GetString(reader.GetOrdinal("cost_charge_number"));
                                programCode = reader.GetString(reader.GetOrdinal("program_code"));
                                partNum = reader.GetString(reader.GetOrdinal("part_no"));

                                if (!reader.IsDBNull(reader.GetOrdinal("reason_code")))
                                {
                                    changeReason = reader.GetString(reader.GetOrdinal("reason_code"));
                                }


                                dateTemp = reader.GetDateTime(reader.GetOrdinal("due_date"));
                                dueDate = (dateTemp.Month).ToString() + "/" +
                                    (dateTemp.Day).ToString() + "/" +
                                    (dateTemp.Year).ToString();
                            }
                            reader.Close();
                            dbConnection.Close();

                            //Get ccn if ccn not found message present
                            if (ccn.ToUpper() == "CCNNOTFOUND")
                            {
                                //Get CCN
                                ccn = getCCN(programCode, dueDate, partNum);

                                //Clear params for next query
                                dbCommand.Parameters.Clear();

                                //Create save query
                                dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                    "set cost_charge_number = :CCN " +
                                    "where order_no = :OrderNum";
                                dbCommand.Parameters.Add(":CCN", OracleType.VarChar).Value = ccn;
                                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;

                                //Save to database
                                dbConnection.Open();
                                dbCommand.ExecuteNonQuery();
                                dbConnection.Close();
                            }

                            //Clear params for next query
                            dbCommand.Parameters.Clear();

                            //Validate ccn
                            validCCN = validateCLSCCN(ccn, programCode, dueDate);

                            //apply ccn logic
                            if (validCCN)
                            {
                                //Return validation to user
                                result.addReturnData(order, "true");

                                //set status and approval date
                                dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                    "set activity_status = :ActivitySts, " +
                                    "amgr_approval_date = :ApprvDate " +
                                    "where order_no = :OrderNum";
                                dbCommand.Parameters.Add(":ActivitySts", OracleType.VarChar).Value = OrderActivityStatus.order_status_values[1];
                                dbCommand.Parameters.Add(":ApprvDate", OracleType.DateTime).Value = DateTime.Today;
                                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;
                            }
                            else
                            {
                                //Return validation to user
                                result.addReturnData(order, "false");
                                invalidCCNCount++;

                                //Clear approval and status
                                dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                    "set activity_status = :ActivitySts, " +
                                    "amgr_approval_date = null " +
                                    "where order_no = :OrderNum";
                                dbCommand.Parameters.Add(":ActivitySts", OracleType.VarChar).Value = OrderActivityStatus.order_status_values[0];
                                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;
                            }

                            //Save to database
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();

                            //Clear params for next query
                            dbCommand.Parameters.Clear();

                            /* Change Variance remarks to Accept if Order quantity matches SPO and reason blank */
                            if (validCCN && String.IsNullOrEmpty(changeReason))//
                            {
                                if (!checkMismatch(internalOrderNo))
                                {
                                    dbCommand.CommandText = "UPDATE escm.escm_new_buy_requirement " +
                                                            "SET asset_manager_remark = 'Accept' " +
                                                            "WHERE internal_order_no = :OrderNum ";

                                    dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = internalOrderNo;

                                    //Save to database
                                    dbConnection.Open();
                                    dbCommand.ExecuteNonQuery();
                                    dbConnection.Close();

                                    //Clear params for next query 
                                    dbCommand.Parameters.Clear();

                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        result.addMsg("Error Processing Order: " + order);
                    }
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Close();
                        }

                        //Make sure connection is closed
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }

                        //Make sure parameters are reset
                        if (dbCommand.Parameters.Count > 0)
                        {
                            dbCommand.Parameters.Clear();
                        }
                    }

                });
            }

            //Inform user of any invalid ccns
            if (invalidCCNCount > 0)
            {
                result.addError(invalidCCNCount.ToString() + " order(s) contain an invalid CCN. These orders were not approved.");
            }

            return result;
        }

        /// <summary>
        /// Approves multiple cls ccb orders using the supplied request
        /// </summary>
        /// <param name="request">validated request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg approveCLSCcbOrders(CLSMultiOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            OracleDataReader reader = null;
            int invalidCCNCount = 0;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //approve each order_no
                request.orders.ForEach(delegate(string order)
                {
                    try
                    {
                        //Check order is still editable
                        if (orderSent(order))
                        {
                            result.addMsg("Cannot edit " + order + ", order already sent to gold.");
                        }
                        else
                        {
                            //Order processing variables
                            DateTime dateTemp;
                            string ccn = "";
                            string programCode = "";
                            string dueDate = "";

                            //set command object properties
                            dbCommand.CommandText = "select NVL(ord.cost_charge_number, '') as cost_charge_number, " +
                                "NVL(req.program_code, '') as program_code, NVL(ord.due_date, '') as due_date " +
                                "from ESCM.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                                "where ord.order_no = :OrderNum " +
                                    "and ord.internal_order_no = req.internal_order_no(+)";
                            dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;

                            //Get required new buy validation parameters
                            dbConnection.Open();
                            reader = dbCommand.ExecuteReader();
                            while (reader.Read() == true)
                            {
                                ccn = reader.GetString(reader.GetOrdinal("cost_charge_number"));
                                programCode = reader.GetString(reader.GetOrdinal("program_code"));

                                dateTemp = reader.GetDateTime(reader.GetOrdinal("due_date"));
                                dueDate = (dateTemp.Month).ToString() + "/" +
                                    (dateTemp.Day).ToString() + "/" +
                                    (dateTemp.Year).ToString();
                            }
                            reader.Close();
                            dbConnection.Close();

                            //Clear params for next query
                            dbCommand.Parameters.Clear();

                            //Validate ccn
                            if (validateCLSCCN(ccn, programCode, dueDate))
                            {
                                //Return validation to user
                                result.addReturnData(order, "true");

                                //set status and approval date
                                dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                    "set activity_status = :ActivitySts, " +
                                    "reviewer_approval_date = :ApprvDate " +
                                    "where order_no = :OrderNum";
                                dbCommand.Parameters.Add(":ActivitySts", OracleType.VarChar).Value = CcbActivityStatus.ccb_status_values[1];
                                dbCommand.Parameters.Add(":ApprvDate", OracleType.DateTime).Value = DateTime.Today;
                                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;
                            }
                            else
                            {
                                //Return validation to user
                                result.addReturnData(order, "false");
                                invalidCCNCount++;

                                //Clear approval and status
                                dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                    "set activity_status = :ActivitySts, " +
                                    "reviewer_approval_date = null " +
                                    "where order_no = :OrderNum";
                                dbCommand.Parameters.Add(":ActivitySts", OracleType.VarChar).Value = CcbActivityStatus.ccb_status_values[0];
                                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;
                            }

                            //Save to database
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();

                            //Clear params for next query
                            dbCommand.Parameters.Clear();
                        }
                    }
                    catch (Exception)
                    {
                        result.addMsg("Error Processing Order: " + order);
                    }
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Close();
                        }

                        //Make sure connection is closed
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }

                        //Make sure parameters are reset
                        if (dbCommand.Parameters.Count > 0)
                        {
                            dbCommand.Parameters.Clear();
                        }
                    }
                });
            }

            //Inform user of any invalid ccns
            if (invalidCCNCount > 0)
            {
                result.addError(invalidCCNCount.ToString() + " order(s) contain an invalid CCN. These orders were not approved.");
            }

            return result;
        }

        /// <summary>
        /// Approves multiple cls ccb orders using the supplied request
        /// </summary>
        /// <param name="request">validated request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg rejectCLSCcbOrders(CLSMultiOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            OracleDataReader reader = null;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //approve each order_no
                request.orders.ForEach(delegate(string order)
                {
                    try
                    {
                        //Check order is still editable
                        if (orderSent(order))
                        {
                            result.addMsg("Cannot edit " + order + ", order already sent to gold.");
                        }
                        else
                        {
                            //set status
                            dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                                    "set activity_status = :ActivitySts, " +
                                                    "reviewer_approval_date = null " +
                                                    "where order_no = :OrderNum";
                            dbCommand.Parameters.Add(":ActivitySts", OracleType.VarChar).Value = CcbActivityStatus.ccb_status_values[2];
                            dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;

                            //Save to database
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();

                            //Clear params for next query
                            dbCommand.Parameters.Clear();

                            //set command to update requirement table
                            dbCommand.CommandText = "update ESCM.escm_new_buy_requirement " +
                                                     "set asset_manager_remark = 'Rejected' " +
                                                     "where internal_order_no in (select req.internal_order_no " +
                                                            "from ESCM.escm_new_buy_requirement req, ESCM.escm_new_buy_order ord " +
                                                            "where ord.internal_order_no = req.internal_order_no  " +
                                                            "and ord.order_no = :orderNum " +
                                                            "and (req.asset_manager_remark is null or req.asset_manager_remark = 'Accept')) ";

                            //set param
                            dbCommand.Parameters.Add(":orderNum", OracleType.VarChar, 20);
                            //set params value
                            dbCommand.Parameters[":orderNum"].Value = order;

                            //run command
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();
                        }
                    }
                    catch (Exception)
                    {
                        result.addMsg("Error Processing Order: " + order);
                    }
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Close();
                        }

                        //Make sure connection is closed
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }

                        //Make sure parameters are reset
                        if (dbCommand.Parameters.Count > 0)
                        {
                            dbCommand.Parameters.Clear();
                        }
                    }

                });
            }

            return result;
        }

        /// <summary>
        /// Gets data for the asset manager grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal CLSManagerGrid getCLSManagerGrid(CLSManagerViewRequest request, Boolean isUK = false)
        {
            //Variables
            CLSManagerGrid grid = new CLSManagerGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.rows = new List<CLSManagerGrid.CLSManager>();
            OracleDataReader reader = null;

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                if (request.role == "ccb")
                {
                    //Pull applicable activity statuses and format for Where clause
                    string statusClause = "";

                    for (int i = 0; i < CcbActivityStatus.ccb_status_values.Length; i++)
                    {
                        if (i != CcbActivityStatus.ccb_status_values.Length - 1)
                        {
                            statusClause += " ccb.activity_status = '" + CcbActivityStatus.ccb_status_values[i] + "' OR";
                        }
                        else
                        {
                            statusClause += " ccb.activity_status = '" + CcbActivityStatus.ccb_status_values[i] + "' ";
                        }
                    }

                    dbCommand.CommandText = "SELECT DISTINCT ccb.asset_manager_id, NVL(ccb.full_name, ' ') as full_name " +
                        "FROM " +
                            "( " +
                              "SELECT r.asset_manager_id, u.full_name, o.activity_status " +
                              "FROM escm.escm_new_buy_requirement r, " +
                              "escm.escm_spo_cls_user u, " +
                              "escm.escm_new_buy_order o, " +
                              "(SELECT sysdate-value_1 as cls_date FROM escm.escm_lookup WHERE category = 'NEWBUY_CLS_DAYS') days " +
                              "WHERE r.internal_order_no = o.internal_order_no " +
                                "AND r.asset_manager_id = u.escm_logon_id(+) " +
                                // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                                // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                                (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                                "AND r.spo_export_date >= days.cls_date " +
                                //                               (request.viewHistory ? "" : "AND r.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ") +
                                //                            ") ccb " +
                                 (request.viewHistory ? "" : "AND ( r.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ") +
                                 "   OR o.AMGR_APPROVAL_DATE between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy')) " +
                                 ") ccb " +
                        "WHERE (" + statusClause + ") " +
                        "ORDER BY full_name asc";
                }
                else
                {
                    dbCommand.CommandText = "SELECT DISTINCT r.asset_manager_id, NVL(u.full_name, ' ') as full_name " +
                        "FROM escm.escm_new_buy_requirement r, escm.escm_spo_cls_user u, " +
                            "(SELECT sysdate-value_1 as cls_date FROM escm.escm_lookup WHERE category = 'NEWBUY_CLS_DAYS') days " +
                        "WHERE r.asset_manager_id = u.escm_logon_id(+) " +
                             // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                             // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW','CSN') ") +
                            (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                            "AND r.spo_export_date >= days.cls_date " +
                            // Legacy - 37 PRP
                            (request.viewHistory ? "" : "AND (r.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') " +
                                                        "OR EXISTS (SELECT 1 FROM escm.escm_new_buy_order nbo WHERE nbo.internal_order_no = r.internal_order_no AND nbo.activity_status in ('Awaiting Asset Manager Review' ))) ") +
                        "ORDER BY full_name asc";
                }

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        CLSManagerGrid.CLSManager row = new CLSManagerGrid.CLSManager();
                        row.asset_manager_id = reader.GetString(reader.GetOrdinal("asset_manager_id"));
                        row.asset_manager_id_name = reader.GetString(reader.GetOrdinal("full_name"));

                        //If id has no matching name value, use id
                        if (String.IsNullOrEmpty(row.asset_manager_id_name))
                        {
                            row.asset_manager_id_name = row.asset_manager_id;
                        }

                        grid.rows.Add(row);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Count number of records found
            grid.totalRows = grid.rows.Count;

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// Gets data for the part grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal PartGrid getCLSPartGrid(PartViewRequest request, Boolean isUK = false)
        {
            //Variables
            PartGrid grid = new PartGrid();
            grid.totalPages = 0;
            grid.currentPage = request.page;
            grid.rows = new List<PartGrid.Part>();
            OracleDataReader reader = null;
            int rowStart = 0;
            int rowEnd = 0;

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                if (request.role == "ccb")
                {
                    //Pull applicable activity statuses and format for Where clause
                    string statusClause = "";

                    for (int i = 0; i < CcbActivityStatus.ccb_status_values.Length; i++)
                    {
                        if (i != CcbActivityStatus.ccb_status_values.Length - 1)
                        {
                            statusClause += " ccb.activity_status = '" + CcbActivityStatus.ccb_status_values[i] + "' OR";
                        }
                        else
                        {
                            statusClause += " ccb.activity_status = '" + CcbActivityStatus.ccb_status_values[i] + "' ";
                        }
                    }

                    //get total rows
                    dbCommand.CommandText = "SELECT NVL(COUNT(*), 0) as totalRows " +
                        "FROM (SELECT DISTINCT ccb.part_no " +
                        "FROM " +
                            "( " +
                              "SELECT r.part_no, o.activity_status " +
                              "FROM escm.escm_new_buy_requirement r, " +
                              "escm.escm_new_buy_order o, " +
                              "(SELECT sysdate-value_1 as cls_date FROM escm.escm_lookup WHERE category = 'NEWBUY_CLS_DAYS') days " +
                              "WHERE r.internal_order_no = o.internal_order_no " +
                            // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                            // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                            (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                                "AND r.spo_export_date >= days.cls_date " +
                                 // JIRA Legacy 37 - PRP
                                 //                                (request.viewHistory ? "" : "AND r.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ") +
                                 (request.viewHistory ? "" : "AND ( r.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ") +
                               "   OR o.AMGR_APPROVAL_DATE between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy')) " +
                                "AND r.part_no LIKE :PartFilter " +
                            ") ccb " +
                        "WHERE (" + statusClause + ") )";
                    dbCommand.Parameters.Add(":PartFilter", OracleType.VarChar).Value = request.part_no + "%";

                    try
                    {
                        dbConnection.Open();
                        var total = dbCommand.ExecuteScalar();
                        dbConnection.Close();

                        //Convert to int
                        grid.totalRows += Convert.ToInt32(total);
                    }
                    catch
                    {
                        //Ignore Error
                    }
                    finally
                    {
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }

                        //reset parameters
                        dbCommand.Parameters.Clear();
                    }

                    //Calc total pages
                    grid.totalPages = (int)Math.Ceiling((double)grid.totalRows / (double)request.rows);

                    //Handle first page differently
                    if (request.page <= 1)
                    {
                        //pagination start /end
                        rowStart = 1;
                        rowEnd = request.rows;

                        //Check if end has been reached
                        if (rowEnd > grid.totalRows)
                        {
                            rowEnd = grid.totalRows;
                        }
                    }
                    else
                    {
                        //Check if valid page number, otherwise show last page
                        if (request.page > grid.totalPages)
                        {
                            request.page = grid.totalPages;
                        }

                        //Calculate pagination end
                        rowEnd = request.page * request.rows;

                        //Calculate pagination start
                        rowStart = (rowEnd - request.rows) + 1;

                        //Check if max row reached
                        if (rowEnd > grid.totalRows)
                        {
                            rowEnd = grid.totalRows;
                        }
                    }

                    dbCommand.CommandText = "SELECT ds.part_no " +
                        "FROM (" +
                        "SELECT rn.part_no, rownum as rnum " +
                        "FROM (" +
                            "SELECT DISTINCT ccb.part_no " +
                            "FROM " +
                                "( " +
                                  "SELECT r.part_no, o.activity_status " +
                                  "FROM escm.escm_new_buy_requirement r, " +
                                  "escm.escm_new_buy_order o, " +
                                  "(SELECT sysdate-value_1 as cls_date FROM escm.escm_lookup WHERE category = 'NEWBUY_CLS_DAYS') days " +
                                  "WHERE r.internal_order_no = o.internal_order_no " +
                                    // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                                    // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                                    (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                                    "AND r.spo_export_date >= days.cls_date " +
                                  // JIRA Legacy 37 - PRP
                                  // (request.viewHistory ? "" : "AND r.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ") +
                                     (request.viewHistory ? "" : "AND ( r.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ") +
                                  "   OR o.AMGR_APPROVAL_DATE between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy')) " +
                                   "AND r.part_no LIKE :PartFilter " +
                                ") ccb " +
                            "WHERE (" + statusClause + ") " +
                            "ORDER BY ccb." +
                            (request.index_isValid() ? request.index : "part_no") +
                            " " +
                            (request.order_isValid() ? request.order : "asc") +
                        ") rn ) ds " +
                        "WHERE rnum between " + rowStart.ToString() + " AND " + rowEnd.ToString();
                    dbCommand.Parameters.Add(":PartFilter", OracleType.VarChar).Value = request.part_no + "%";
                }
                // NOT CCB
                else
                {

                    //get total rows
                    dbCommand.CommandText = "SELECT NVL(Count(*), 0) as totalParts FROM " +
                                        "( " +
                                        "SELECT DISTINCT r.part_no " +
                                        "FROM  escm.escm_new_buy_requirement r, " +
                                        "      (SELECT * FROM  escm.escm_new_buy_order WHERE activity_status != 'Confirmed Order' ) o, " +
                                        "      (SELECT sysdate - value_1 as cls_date FROM escm.escm_lookup WHERE category = 'NEWBUY_CLS_DAYS') days " +
                                        "WHERE r.internal_order_no = o.internal_order_no(+) " +
                                        // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                                        // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                                        (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                                        "  AND r.spo_export_date >= days.cls_date " +
                                        // JIRA Legacy 37 - PRP
                                        // (request.viewHistory ? "" : " AND r.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ") +
                                    (request.viewHistory ? "" : "AND ( r.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ") +
                                  "   OR o.AMGR_APPROVAL_DATE between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy')) " +
                                        "  AND r.part_no LIKE :PartFilter " +
                                        ")";
                    dbCommand.Parameters.Add(":PartFilter", OracleType.VarChar).Value = request.part_no + "%";

                    try
                    {
                        dbConnection.Open();
                        var total = dbCommand.ExecuteScalar();
                        dbConnection.Close();

                        //Convert to int
                        grid.totalRows += Convert.ToInt32(total);
                    }
                    catch
                    {
                        //Ignore Error
                    }
                    finally
                    {
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }

                        //reset parameters
                        dbCommand.Parameters.Clear();
                    }

                    //Calc total pages
                    grid.totalPages = (int)Math.Ceiling((double)grid.totalRows / (double)request.rows);

                    //Handle first page differently
                    if (request.page <= 1)
                    {
                        //pagination start /end
                        rowStart = 1;
                        rowEnd = request.rows;

                        //Check if end has been reached
                        if (rowEnd > grid.totalRows)
                        {
                            rowEnd = grid.totalRows;
                        }
                    }
                    else
                    {
                        //Check if valid page number, otherwise show last page
                        if (request.page > grid.totalPages)
                        {
                            request.page = grid.totalPages;
                        }

                        //Calculate pagination end
                        rowEnd = request.page * request.rows;

                        //Calculate pagination start
                        rowStart = (rowEnd - request.rows) + 1;

                        //Check if max row reached
                        if (rowEnd > grid.totalRows)
                        {
                            rowEnd = grid.totalRows;
                        }
                    }

                    dbCommand.CommandText = "SELECT ds.part_no FROM " +
                        "( " +
                            "SELECT rn.part_no, rownum as rnum FROM " +
                            "( " +
                            "SELECT DISTINCT r.part_no " +
                            "FROM  escm.escm_new_buy_requirement r, " +
                            "      (SELECT * FROM  escm.escm_new_buy_order WHERE activity_status != 'Confirmed Order' ) o, " +
                            "      (SELECT sysdate - value_1 as cls_date FROM escm.escm_lookup WHERE category = 'NEWBUY_CLS_DAYS') days " +
                            "WHERE r.internal_order_no = o.internal_order_no(+) " +
                            // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                            // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                            (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                            "  AND r.spo_export_date >= days.cls_date " +
                                    // JIRA Legacy 37 - PRP
                                    // (request.viewHistory ? "" : " AND r.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ") +
                                    (request.viewHistory ? "" : "AND ( r.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ") +
                                  "   OR o.AMGR_APPROVAL_DATE between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy')) " +
                            "  AND r.part_no LIKE :PartFilter " +
                            "ORDER BY r." +
                                (request.index_isValid() ? request.index : "part_no") +
                                " " +
                                (request.order_isValid() ? request.order : "asc") +
                            ") rn " +
                        ") ds " +
                        "WHERE rnum between " + rowStart.ToString() + " AND " + rowEnd.ToString();
                    dbCommand.Parameters.Add(":PartFilter", OracleType.VarChar).Value = request.part_no + "%";
                }

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        PartGrid.Part row = new PartGrid.Part();
                        row.part_no = reader.GetString(reader.GetOrdinal("part_no"));

                        grid.rows.Add(row);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// Gets data for the program grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal CLSProgramGrid getCLSProgramGrid(CLSProgramViewRequest request)
        {
            //Variables
            CLSProgramGrid grid = new CLSProgramGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.rows = new List<CLSProgramGrid.CLSProgram>();
            OracleDataReader reader = null;

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT DISTINCT r.program_code, r.program_name " +
                                        "FROM escm.escm_program_code r " +
                                        // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                                        // "WHERE r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW','CSN') " +
                                        "WHERE r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') " +
                                            "AND r.active_status = 'Y' " +
                                        "ORDER BY r." +
                                         (request.index_isValid() ? request.index : "program_code") +
                                         " " +
                                         (request.order_isValid() ? request.order : "asc");

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        CLSProgramGrid.CLSProgram row = new CLSProgramGrid.CLSProgram();
                        row.program_code = reader.GetString(reader.GetOrdinal("program_code"));
                        row.program_name = reader.GetString(reader.GetOrdinal("program_name"));

                        //If id has no matching name value, use id
                        if (String.IsNullOrEmpty(row.program_name))
                        {
                            row.program_name = row.program_code;
                        }

                        grid.rows.Add(row);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Count number of records found
            grid.totalRows = grid.rows.Count;

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// Gets a CLS order's requirement relationship data
        /// </summary>
        /// <param name="InternalOrderNo">internal order no</param>
        /// <returns>related requirement data</returns>
        private CLSOrderReqData getCLSOrderReqData(int InternalOrderNo)
        {
            CLSOrderReqData result = new CLSOrderReqData();
            OracleDataReader reader = null;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                try
                {
                    dbCommand.CommandText = "SELECT " +
                    "spo_request_id, " +
                    "program_code, " +
                    "part_no " +
                    "FROM escm.escm_new_buy_requirement " +
                    "WHERE internal_order_no = :OrderNum";
                    dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = InternalOrderNo;

                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        result.spo_request_id = reader.GetInt32(reader.GetOrdinal("spo_request_id"));
                        result.program_code = reader.GetString(reader.GetOrdinal("program_code"));
                        result.part_no = reader.GetString(reader.GetOrdinal("part_no"));
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    //failed
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets data for the CLS ccb grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal CLSccbGrid getCLSccbGrid(CLSccbViewRequest request)
        {
            //Variables
            CLSccbGrid grid = new CLSccbGrid();
            grid.totalPages = 0;
            grid.currentPage = request.page;
            grid.rows = new List<CLSccbGrid.CLSccb>();
            OracleDataReader reader = null;
            int rowStart = 0;
            int rowEnd = 0;

            //Pull applicable activity statuses and format for Where clause
            string statusClause = "";

            for (int i = 0; i < CcbActivityStatus.ccb_status_values.Length; i++)
            {
                if (i != CcbActivityStatus.ccb_status_values.Length - 1)
                {
                    statusClause += " ccb.activity_status = '" + CcbActivityStatus.ccb_status_values[i] + "' OR";
                }
                else
                {
                    statusClause += " ccb.activity_status = '" + CcbActivityStatus.ccb_status_values[i] + "' ";
                }
            }

            //Pull record count for query to be used in paging
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                string cnt_query = "SELECT NVL(count(ccb.order_no), 0) as totalRows FROM ( " +
                                      "SELECT DISTINCT " +
                                        "ORD.ORDER_NO, " +
                                        "REQ.PART_NO, " +
                                        "req.program_code, " +
                                        "ORD.Activity_Status, " +
                                        "REQ.asset_manager_id " +
                                      "FROM " +
                                        "ESCM.ESCM_NEW_BUY_REQUIREMENT REQ, " +
                                        "ESCM.ESCM_NEW_BUY_ORDER ORD, " +
                                        "ESCM.escm_cls_part prt, " +
                                        "(SELECT sysdate-value_1 as ovhl_date FROM escm.escm_lookup WHERE category = 'NEWBUY_OVHL_DAYS') days, " +
                                        "(select distinct part_no from escm.escm_lta_pricebreak_mvw) pb " +
                                      "WHERE " +
                                        "REQ.INTERNAL_ORDER_NO = ORD.INTERNAL_ORDER_NO " +
                                        "AND req.part_no = pb.part_no(+) " +
                                        "AND req.part_no = prt.part_no " +
                                        // Legacy 37 - PRP
                                        "AND ( req.spo_export_date >= days.ovhl_date " +
                                        "   OR ORD.AMGR_APPROVAL_DATE between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy')) " +
                                        // Legacy 37 - PRP
                                        ( request.viewHistory ? "" : "AND ( req.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ") +
                                        "   OR ORD.AMGR_APPROVAL_DATE between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy')) " +
                                    ") ccb " +
                                     // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                                     // ((request.program != "UKC") ? "WHERE ccb.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') " : "WHERE ccb.program_code = 'UKC' ") +
                                     ((request.program != "UKC") ? "WHERE ccb.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') " : "WHERE ccb.program_code = 'UKC' ") +
                                    "AND (" + statusClause + ") ";

                if (!String.IsNullOrEmpty(request.assetManager))
                {
                    cnt_query += "AND ccb.asset_manager_id = :asset_manager ";
                    dbCommand.Parameters.Add(":asset_manager", OracleType.VarChar).Value = request.assetManager;
                }

                if (!String.IsNullOrEmpty(request.part_no))
                {
                    cnt_query += "AND ccb.part_no = :part_no ";
                    dbCommand.Parameters.Add(":part_no", OracleType.VarChar).Value = request.part_no;
                }

                if (!String.IsNullOrEmpty(request.activity_status))
                {
                    cnt_query += "AND ccb.activity_status = :activity ";
                    dbCommand.Parameters.Add(":activity", OracleType.VarChar).Value = request.activity_status;
                }

                if (!String.IsNullOrEmpty(request.program))
                {
                    cnt_query += "AND ccb.program_code = :program ";
                    dbCommand.Parameters.Add(":program", OracleType.VarChar).Value = request.program;
                }

                dbCommand.CommandText = cnt_query;

                try
                {
                    dbConnection.Open();
                    var total = dbCommand.ExecuteScalar();
                    dbConnection.Close();

                    //Convert to int
                    grid.totalRows += Convert.ToInt32(total);
                }
                catch
                {
                    //Ignore Error
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }

                    //reset parameters
                    dbCommand.Parameters.Clear();
                }
            }

            //Calc total pages
            grid.totalPages = (int)Math.Ceiling((double)grid.totalRows / (double)request.rows);

            //Calculate pagination start
            rowStart = ((request.page - 1) * request.rows);

            //Check if 1 or less
            if (rowStart < 1)
            {
                rowStart = 0;
            }

            //Calculate pagination end
            rowEnd = (rowStart + (request.rows)) > (grid.totalRows - rowStart) ? rowEnd = grid.totalRows : rowStart + (request.rows);

            //Check if less than or equal to 1
            if (rowStart <= 1)
            {
                rowStart = 1;
            }
            else
            {
                rowStart += 1;
            }

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                string query = "SELECT rn.* FROM " +
                                "( SELECT a.*, rownum as rnum " +
                                "FROM ( " +
                                "SELECT DISTINCT * FROM ( " +
                                  "SELECT " +
                                    "ORD.ORDER_NO, " +
                                    "REQ.PART_NO, " +
                                    "NVL2(pb.part_no, 'true', 'false') as pricebreak, " +
                                    "req.program_code, " +
                                    "NVL(prt.NOMENCLATURE, ' ') as nomenclature, " +
                                    "REQ.ITEM_COST   AS SPO_COST, " +
                                    "NVL(ORD.order_quantity,0) as order_quantity, " +
                                    "NVL(req.Item_Cost,0) * NVL(ord.order_Quantity,0) AS EXTENDED_COST, " +
                                    "ORD.DUE_DATE, " +
                                    "NVL(ORD.COST_CHARGE_NUMBER, ' ') as COST_CHARGE_NUMBER, " +
                                    "ORD.Activity_Status, " +
                                    "REQ.asset_manager_id " +
                                  "FROM " +
                                    "ESCM.ESCM_NEW_BUY_REQUIREMENT REQ, " +
                                    "ESCM.ESCM_NEW_BUY_ORDER ORD, " +
                                    "ESCM.escm_cls_part prt, " +
                                    "(SELECT sysdate-value_1 as ovhl_date FROM escm.escm_lookup WHERE category = 'NEWBUY_OVHL_DAYS') days, " +
                                    "(select distinct part_no from escm.escm_lta_pricebreak_mvw) pb " +
                                  "WHERE " +
                                    "REQ.INTERNAL_ORDER_NO = ORD.INTERNAL_ORDER_NO " +
                                    "AND req.part_no = pb.part_no(+) " +
                                    "AND req.part_no = prt.part_no " +
                                    "AND req.program_code = prt.program_code " +
                                        // Legacy 37 - PRP
                                        "AND ( req.spo_export_date >= days.ovhl_date " +
                                        "   OR ORD.AMGR_APPROVAL_DATE between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy')) " +
                                        // PRP Legacy 37                                    (request.viewHistory ? "" : "AND req.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ") +
                                        (request.viewHistory ? "" : "AND ( req.spo_export_date between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy') ") +
                                        "   OR ORD.AMGR_APPROVAL_DATE between to_date('" + CurrentAcctPeriod.Start + "', 'mm/dd/yyyy') AND to_date('" + CurrentAcctPeriod.End + "', 'mm/dd/yyyy')) " +
                                ") ccb " +
                                     // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                                     // ((request.program != "UKC") ? "WHERE ccb.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') " : "WHERE ccb.program_code = 'UKC' ") +
                                     ((request.program != "UKC") ? "WHERE ccb.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') " : "WHERE ccb.program_code = 'UKC' ") +
                                "AND (" + statusClause + ") ";

                if (!String.IsNullOrEmpty(request.assetManager))
                {
                    query += "AND ccb.asset_manager_id = :asset_manager ";
                    dbCommand.Parameters.Add(":asset_manager", OracleType.VarChar).Value = request.assetManager;
                }

                if (!String.IsNullOrEmpty(request.part_no))
                {
                    query += "AND ccb.part_no = :part_no ";
                    dbCommand.Parameters.Add(":part_no", OracleType.VarChar).Value = request.part_no;
                }

                if (!String.IsNullOrEmpty(request.activity_status))
                {
                    query += "AND ccb.activity_status = :activity ";
                    dbCommand.Parameters.Add(":activity", OracleType.VarChar).Value = request.activity_status;
                }

                if (!String.IsNullOrEmpty(request.program))
                {
                    query += "AND ccb.program_code = :program ";
                    dbCommand.Parameters.Add(":program", OracleType.VarChar).Value = request.program;
                }

                query += "ORDER BY " +
                        (request.index_isValid() ? "ccb." + request.index : "ccb.order_no") +
                        " " +
                        (request.order_isValid() ? request.order : "asc") +
                        " ) a) rn WHERE rn.rnum between " + rowStart.ToString() + " and " + rowEnd.ToString();

                dbCommand.CommandText = query;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        DateTime dateTemp;

                        CLSccbGrid.CLSccb gridReq = new CLSccbGrid.CLSccb();
                        gridReq.order_no = reader.GetString(reader.GetOrdinal("order_no"));
                        gridReq.part_no = reader.GetString(reader.GetOrdinal("part_no"));
                        gridReq.activity_status = reader.GetString(reader.GetOrdinal("activity_status"));
                        gridReq.cost_charge_number = reader.GetString(reader.GetOrdinal("cost_charge_number"));
                        gridReq.nomenclature = reader.GetString(reader.GetOrdinal("nomenclature"));
                        gridReq.spo_cost = reader.GetDecimal(reader.GetOrdinal("spo_cost"));
                        gridReq.order_quantity = reader.GetInt32(reader.GetOrdinal("order_quantity"));
                        gridReq.extended_cost = reader.GetDecimal(reader.GetOrdinal("extended_cost"));

                        dateTemp = reader.GetDateTime(reader.GetOrdinal("due_date"));
                        gridReq.due_date = (dateTemp.Month).ToString() + "/" +
                            (dateTemp.Day).ToString() + "/" +
                            (dateTemp.Year).ToString();

                        var hasLTA = reader.GetString(reader.GetOrdinal("pricebreak"));
                        gridReq.pricebreak = Convert.ToBoolean(hasLTA);

                        gridReq.pricePoint = 0;

                        grid.rows.Add(gridReq);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    //Ignore Error???
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Get pricepoint for each LTA part
            foreach (CLSccbGrid.CLSccb ord in grid.rows)
            {
                //Only check parts that have an LTA
                if (ord.pricebreak)
                {
                    try
                    {
                        //Find pricepoint
                        ord.pricePoint = getLtaPricePoint(ord.part_no, ord.order_quantity, ord.order_no.Substring(0, 3));
                    }
                    catch (Exception)
                    {
                        //Ignore Errors and Move on
                    }
                }
            }

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// get list of possible CCB activity status
        /// </summary>
        /// <param name="request">request</param>
        /// <returns></returns>
        internal CLSccbStatusGrid getCLSccbStatusGrid(CLSccbStatusViewRequest request)
        {
            CLSccbStatusGrid grid = new CLSccbStatusGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.rows = new List<CLSccbStatusGrid.CLSccbStatus>();

            //Fill grid with data
            foreach (string value in CcbActivityStatus.ccb_status_values)
            {
                CLSccbStatusGrid.CLSccbStatus status = new CLSccbStatusGrid.CLSccbStatus();
                status.activity_status = value;
                grid.rows.Add(status);
            }

            //Count number of records found
            grid.totalRows = grid.rows.Count;

            return grid;
        }

        /// <summary>
        /// get asset manager remark
        /// </summary>
        /// <param name="request">request</param>
        /// <returns></returns>
        internal CLSAmRemarkGrid getCLSAmRemark(CLSRemarkViewRequest request)
        {
            CLSAmRemarkGrid grid = new CLSAmRemarkGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.totalRows = 1;
            grid.rows = new List<CLSAmRemarkGrid.CLSAmRemark>();

            CLSAmRemarkGrid.CLSAmRemark row = new CLSAmRemarkGrid.CLSAmRemark();
            row.order_no = request.order_no;

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT NVL(asset_manager_remark, ' ') as asset_manager_remark " +
                    "FROM ESCM.escm_new_buy_order " +
                    "WHERE order_no = :OrderNo " +
                        "AND rownum < 2";
                dbCommand.Parameters.Add(":OrderNo", OracleType.VarChar).Value = request.order_no;

                dbConnection.Open(); //Open Connection
                row.asset_manager_remark = (string)dbCommand.ExecuteScalar(); //Execute query
                dbConnection.Close(); //Close Connection
            }

            //If remark null, add 1 space for jqGrid to render correctly
            if (String.IsNullOrEmpty(row.asset_manager_remark))
            {
                row.asset_manager_remark = " ";
            }

            //Add row
            grid.rows.Add(row);

            return grid;
        }

        /// <summary>
        /// Edits cls asset manager remark using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg editCLSAmRemark(CLSRemarkEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            bool flagRemarkNull = false;

            //Check for null remark
            if (String.IsNullOrEmpty(request.remark))
            {
                flagRemarkNull = true;
            }

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                if (flagRemarkNull)
                {
                    dbCommand.CommandText = "UPDATE ESCM.escm_new_buy_order " +
                    "SET asset_manager_remark = null, " +
                    "asset_manager_remark_date = :RemarkDate " +
                    "WHERE order_no = :OrderNum";
                }
                else
                {
                    dbCommand.CommandText = "UPDATE ESCM.escm_new_buy_order " +
                        "SET asset_manager_remark = :Remark, " +
                        "asset_manager_remark_date = :RemarkDate " +
                        "WHERE order_no = :OrderNum";
                }

                //Add remark param if not null
                if (!flagRemarkNull)
                {
                    dbCommand.Parameters.Add(":Remark", OracleType.VarChar).Value = request.remark;
                }
                dbCommand.Parameters.Add(":RemarkDate", OracleType.DateTime).Value = DateTime.Today;
                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = request.order_no;

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to save Asset Manager remarks.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Edits cls ccb remark using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg editCLSccbRemark(CLSRemarkEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            bool flagRemarkNull = false;

            //Check for null remark
            if (String.IsNullOrEmpty(request.remark))
            {
                flagRemarkNull = true;
            }

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                if (flagRemarkNull)
                {
                    dbCommand.CommandText = "UPDATE ESCM.escm_new_buy_order " +
                    "SET review_board_remark = null, " +
                    "review_board_remark_date = :RemarkDate " +
                    "WHERE order_no = :OrderNum";
                }
                else
                {
                    dbCommand.CommandText = "UPDATE ESCM.escm_new_buy_order " +
                        "SET review_board_remark = :Remark, " +
                        "review_board_remark_date = :RemarkDate " +
                        "WHERE order_no = :OrderNum";
                }

                //Add remark param if not null
                if (!flagRemarkNull)
                {
                    dbCommand.Parameters.Add(":Remark", OracleType.VarChar).Value = request.remark;
                }
                dbCommand.Parameters.Add(":RemarkDate", OracleType.DateTime).Value = DateTime.Today;
                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = request.order_no;

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to save remark.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// get ccb remark
        /// </summary>
        /// <param name="request">request</param>
        /// <returns></returns>
        internal CLSCcbRemarkGrid getCLSCcbRemark(CLSRemarkViewRequest request)
        {
            CLSCcbRemarkGrid grid = new CLSCcbRemarkGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.totalRows = 1;
            grid.rows = new List<CLSCcbRemarkGrid.CLSCcbRemark>();

            CLSCcbRemarkGrid.CLSCcbRemark row = new CLSCcbRemarkGrid.CLSCcbRemark();
            row.order_no = request.order_no;

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT NVL(review_board_remark, ' ') as review_board_remark " +
                    "FROM ESCM.escm_new_buy_order " +
                    "WHERE order_no = :OrderNo " +
                        "AND rownum < 2";
                dbCommand.Parameters.Add(":OrderNo", OracleType.VarChar).Value = request.order_no;

                dbConnection.Open(); //Open Connection
                row.review_board_remark = (string)dbCommand.ExecuteScalar(); //Execute query
                dbConnection.Close(); //Close Connection
            }

            //If remark null, add 1 space for jqGrid to render correctly
            if (String.IsNullOrEmpty(row.review_board_remark))
            {
                row.review_board_remark = " ";
            }

            //Add row
            grid.rows.Add(row);

            return grid;
        }

        /// <summary>
        /// Gets data for the CLS summary by program grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal CLSProgramSummaryGrid getCLSSummaryProgramGrid(CLSSummaryProgramViewRequest request, Boolean isUK = false)
        {
            //Variables
            CLSProgramSummaryGrid grid = new CLSProgramSummaryGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.rows = new List<CLSProgramSummaryGrid.CLSProgramSummary>();
            OracleDataReader reader = null;

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT DISTINCT req.program_code as ProgramCode, " +
                  "NVL(AcceptPart.AcceptPartNumCount, 0) as TotalAcceptPartNumCount, " +
                  "NVL(AcceptCost.AcceptExtCostSum, 0) as TotalAcceptExtCostSum, " +
                  "NVL(RejectPart.RejectNumCount, 0) as TotalRejectNumCount, " +
                  "NVL(RejectCost.RejectExtCostSum, 0) as TotalRejectExtCostSum, " +
                  "(NVL(AcceptPart.AcceptPartNumCount, 0) + NVL(RejectPart.RejectNumCount, 0)) as TotalPartCount, " +
                  "(NVL(AcceptCost.AcceptExtCostSum, 0) + NVL(RejectCost.RejectExtCostSum, 0)) as TotalExtCostSum " +
                "FROM escm.escm_new_buy_order ord, " +
                  "escm.escm_new_buy_requirement req, " +
                  "( " +
                    "SELECT DISTINCT req.program_code as ProgramCode, " +
                      "Count(req.part_no) as AcceptPartNumCount " +
                    "FROM escm.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                    "WHERE req.internal_order_no = ord.internal_order_no " +
                      "AND trunc(req.spo_export_date) >= trunc(:StartDate1) " +
                      "AND trunc(req.spo_export_date) <= trunc(:EndDate1) " +
                    // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                    // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                    (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                      (request.isCCAD ? "AND req.program_code = 'DCC' " : "AND req.program_code != 'DCC' ") +
                      "AND ord.activity_status IN ('Approved by Asset Manager', 'Approved by Review Board', 'Sent to the Execution System', 'Confirmed Order') " +
                    "GROUP By req.program_code " +
                  ") AcceptPart, " +
                  "( " +
                    "SELECT req.program_code as ProgramCode, " +
                      "Round(Sum(req.item_cost * ord.order_quantity), 2) as AcceptExtCostSum " +
                    "FROM escm.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                    "WHERE req.internal_order_no = ord.internal_order_no " +
                      "AND trunc(req.spo_export_date) >= trunc(:StartDate2) " +
                      "AND trunc(req.spo_export_date) <= trunc(:EndDate2) " +
                    // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                    // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                    (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                      (request.isCCAD ? "AND req.program_code = 'DCC' " : "AND req.program_code != 'DCC' ") +
                      "AND ord.activity_status IN ('Approved by Asset Manager', 'Approved by Review Board', 'Sent to the Execution System', 'Confirmed Order') " +
                    "GROUP By req.program_code " +
                  ") AcceptCost, " +
                  "( " +
                    "SELECT DISTINCT req.program_code as ProgramCode, " +
                      "Count(req.part_no) as RejectNumCount " +
                    "FROM escm.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                    "WHERE req.internal_order_no = ord.internal_order_no " +
                      "AND trunc(req.spo_export_date) >= trunc(:StartDate3) " +
                      "AND trunc(req.spo_export_date) <= trunc(:EndDate3) " +
                        // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                        // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                        (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                      (request.isCCAD ? "AND req.program_code = 'DCC' " : "AND req.program_code != 'DCC' ") +
                      "AND ord.activity_status IN ('Rejected by Asset Manager', 'Rejected by Review Board', 'Rejected by the System') " +
                    "GROUP By req.program_code " +
                  ") RejectPart, " +
                  "( " +
                    "SELECT req.program_code as ProgramCode, " +
                      "Round(Sum(req.item_cost * ord.order_quantity), 2) as RejectExtCostSum " +
                    "FROM escm.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                    "WHERE req.internal_order_no = ord.internal_order_no " +
                      "AND trunc(req.spo_export_date) >= trunc(:StartDate4) " +
                      "AND trunc(req.spo_export_date) <= trunc(:EndDate4) " +
                    // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                    // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                    (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                      (request.isCCAD ? "AND req.program_code = 'DCC' " : "AND req.program_code != 'DCC' ") +
                      "AND ord.activity_status IN ('Rejected by Asset Manager', 'Rejected by Review Board', 'Rejected by the System') " +
                    "GROUP By req.program_code " +
                  ") RejectCost " +
                "WHERE req.internal_order_no = ord.internal_order_no " +
                  "AND trunc(req.spo_export_date) >= trunc(:StartDate5) " +
                  "AND trunc(req.spo_export_date) <= trunc(:EndDate5) " +
                    // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                    // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                    (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                  (request.isCCAD ? "AND req.program_code = 'DCC' " : "AND req.program_code != 'DCC' ") +
                  "AND req.program_code = AcceptPart.ProgramCode(+) " +
                  "AND req.program_code = AcceptCost.ProgramCode(+) " +
                  "AND req.program_code = RejectPart.ProgramCode(+) " +
                  "AND req.program_code = RejectCost.ProgramCode(+) " +
                "ORDER BY req.program_code desc";

                //add params
                dbCommand.Parameters.Add(":StartDate1", OracleType.DateTime).Value = request.startDate;
                dbCommand.Parameters.Add(":EndDate1", OracleType.DateTime).Value = request.endDate;
                dbCommand.Parameters.Add(":StartDate2", OracleType.DateTime).Value = request.startDate;
                dbCommand.Parameters.Add(":EndDate2", OracleType.DateTime).Value = request.endDate;
                dbCommand.Parameters.Add(":StartDate3", OracleType.DateTime).Value = request.startDate;
                dbCommand.Parameters.Add(":EndDate3", OracleType.DateTime).Value = request.endDate;
                dbCommand.Parameters.Add(":StartDate4", OracleType.DateTime).Value = request.startDate;
                dbCommand.Parameters.Add(":EndDate4", OracleType.DateTime).Value = request.endDate;
                dbCommand.Parameters.Add(":StartDate5", OracleType.DateTime).Value = request.startDate;
                dbCommand.Parameters.Add(":EndDate5", OracleType.DateTime).Value = request.endDate;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        CLSProgramSummaryGrid.CLSProgramSummary gridSumry = new CLSProgramSummaryGrid.CLSProgramSummary();

                        gridSumry.program_code = reader.GetString(reader.GetOrdinal("ProgramCode"));
                        gridSumry.total_accept_part_count = reader.GetInt32(reader.GetOrdinal("TotalAcceptPartNumCount"));
                        gridSumry.total_accept_part_cost = reader.GetDecimal(reader.GetOrdinal("TotalAcceptExtCostSum"));
                        gridSumry.total_reject_part_count = reader.GetInt32(reader.GetOrdinal("TotalRejectNumCount"));
                        gridSumry.total_reject_part_cost = reader.GetDecimal(reader.GetOrdinal("TotalRejectExtCostSum"));
                        gridSumry.total_part_count = reader.GetInt32(reader.GetOrdinal("TotalPartCount"));
                        gridSumry.total_part_cost = reader.GetDecimal(reader.GetOrdinal("TotalExtCostSum"));

                        grid.rows.Add(gridSumry);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Count number of records found
            grid.totalRows = grid.rows.Count;

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// Gets data for the CLS summary by reason grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal CLSReasonSummaryGrid getCLSSummaryReasonGrid(CLSSummaryReasonViewRequest request, Boolean isUK = false)
        {
            //Variables
            CLSReasonSummaryGrid grid = new CLSReasonSummaryGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.rows = new List<CLSReasonSummaryGrid.CLSReasonSummary>();
            OracleDataReader reader = null;

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT DISTINCT req.program_code as ProgramCode, " +
                  "NVL(req.asset_manager_remark, ' ') as ChangeCode, " +
                  "NVL(AcceptPart.AcceptPartNumCount, 0) as AcceptPartNumCount, " +
                  "NVL(AcceptCost.AcceptExtCostSum, 0) as AcceptExtCostSum, " +
                  "NVL(RejectPart.RejectNumCount, 0) as RejectNumCount, " +
                  "NVL(RejectCost.RejectExtCostSum, 0) as RejectExtCostSum " +
                "FROM escm.escm_new_buy_order ord, " +
                  "escm.escm_new_buy_requirement req, " +
                  "( " +
                    "SELECT DISTINCT req.program_code as ProgramCode, " +
                      "NVL(req.asset_manager_remark, ' ') as ChangeCode, " +
                      "Count(req.part_no) as AcceptPartNumCount " +
                    "FROM escm.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                    "WHERE req.internal_order_no = ord.internal_order_no " +
                      "AND trunc(req.spo_export_date) >= trunc(:StartDate1) " +
                      "AND trunc(req.spo_export_date) <= trunc(:EndDate1) " +
                    // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                    // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                    (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                      (request.isCCAD ? "AND req.program_code = 'DCC' " : "AND req.program_code != 'DCC' ") +
                      "AND ord.activity_status IN ('Approved by Asset Manager', 'Approved by Review Board', 'Sent to the Execution System', 'Confirmed Order') " +
                    "GROUP By req.program_code, NVL(req.asset_manager_remark, ' ') " +
                  ") AcceptPart, " +
                  "(  " +
                    "SELECT req.program_code as ProgramCode, " +
                      "NVL(req.asset_manager_remark, ' ') as ChangeCode, " +
                      "Round(Sum(req.item_cost * ord.order_quantity), 2) as AcceptExtCostSum " +
                    "FROM escm.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                    "WHERE req.internal_order_no = ord.internal_order_no " +
                      "AND trunc(req.spo_export_date) >= trunc(:StartDate2) " +
                      "AND trunc(req.spo_export_date) <= trunc(:EndDate2) " +
                    // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                    // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                    (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                      (request.isCCAD ? "AND req.program_code = 'DCC' " : "AND req.program_code != 'DCC' ") +
                      "AND ord.activity_status IN ('Approved by Asset Manager', 'Approved by Review Board', 'Sent to the Execution System', 'Confirmed Order') " +
                    "GROUP By req.program_code, NVL(req.asset_manager_remark, ' ') " +
                  ") AcceptCost, " +
                  "( " +
                    "SELECT DISTINCT req.program_code as ProgramCode, " +
                      "NVL(req.asset_manager_remark, ' ') as ChangeCode, " +
                      "Count(req.part_no) as RejectNumCount " +
                    "FROM escm.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                    "WHERE req.internal_order_no = ord.internal_order_no " +
                      "AND trunc(req.spo_export_date) >= trunc(:StartDate3) " +
                      "AND trunc(req.spo_export_date) <= trunc(:EndDate3) " +
                    // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                    // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                    (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                      (request.isCCAD ? "AND req.program_code = 'DCC' " : "AND req.program_code != 'DCC' ") +
                      "AND ord.activity_status IN ('Rejected by Asset Manager', 'Rejected by Review Board', 'Rejected by the System') " +
                    "GROUP By req.program_code, NVL(req.asset_manager_remark, ' ') " +
                  ") RejectPart, " +
                  "( " +
                    "SELECT req.program_code as ProgramCode, " +
                      "NVL(req.asset_manager_remark, ' ') as ChangeCode, " +
                      "Round(Sum(req.item_cost * ord.order_quantity), 2) as RejectExtCostSum " +
                    "FROM escm.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                    "WHERE req.internal_order_no = ord.internal_order_no " +
                      "AND trunc(req.spo_export_date) >= trunc(:StartDate4) " +
                      "AND trunc(req.spo_export_date) <= trunc(:EndDate4) " +
                    // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                    // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                    (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                      (request.isCCAD ? "AND req.program_code = 'DCC' " : "AND req.program_code != 'DCC' ") +
                      "AND ord.activity_status IN ('Rejected by Asset Manager', 'Rejected by Review Board', 'Rejected by the System') " +
                    "GROUP By req.program_code, NVL(req.asset_manager_remark, ' ') " +
                  ") RejectCost " +
                "WHERE req.internal_order_no = ord.internal_order_no " +
                  "AND trunc(req.spo_export_date) >= trunc(:StartDate5) " +
                  "AND trunc(req.spo_export_date) <= trunc(:EndDate5) " +
                    // PRP: 07072017 - TFS 31504 Setup New Buy Order Processing for AH6I SANG MNG
                    // (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in ('CUS', 'CKU', 'CRN', 'CSA', 'CUA', 'CTW', 'CSN') ") +
                    (isUK ? "AND r.program_code = 'UKC' " : "AND r.program_code in (SELECT VALUE_2 FROM ESCM.ESCM_LOOKUP  WHERE category = 'NEW_BUY_PROGRAM') ") +
                 (request.isCCAD ? "AND req.program_code = 'DCC' " : "AND req.program_code != 'DCC' ") +
                  "AND req.program_code = :Program " +
                  "AND req.program_code = AcceptPart.ProgramCode(+) " +
                  "AND NVL(req.asset_manager_remark, ' ') = AcceptPart.ChangeCode(+) " +
                  "AND req.program_code = AcceptCost.ProgramCode(+) " +
                  "AND NVL(req.asset_manager_remark, ' ') = AcceptCost.ChangeCode(+) " +
                  "AND req.program_code = RejectPart.ProgramCode(+) " +
                  "AND NVL(req.asset_manager_remark, ' ') = RejectPart.ChangeCode(+) " +
                  "AND req.program_code = RejectCost.ProgramCode(+) " +
                  "AND NVL(req.asset_manager_remark, ' ') = RejectCost.ChangeCode(+) " +
                "ORDER BY req.program_code desc, NVL(req.asset_manager_remark, ' ') asc";

                //add params
                dbCommand.Parameters.Add(":StartDate1", OracleType.DateTime).Value = request.startDate;
                dbCommand.Parameters.Add(":EndDate1", OracleType.DateTime).Value = request.endDate;
                dbCommand.Parameters.Add(":StartDate2", OracleType.DateTime).Value = request.startDate;
                dbCommand.Parameters.Add(":EndDate2", OracleType.DateTime).Value = request.endDate;
                dbCommand.Parameters.Add(":StartDate3", OracleType.DateTime).Value = request.startDate;
                dbCommand.Parameters.Add(":EndDate3", OracleType.DateTime).Value = request.endDate;
                dbCommand.Parameters.Add(":StartDate4", OracleType.DateTime).Value = request.startDate;
                dbCommand.Parameters.Add(":EndDate4", OracleType.DateTime).Value = request.endDate;
                dbCommand.Parameters.Add(":StartDate5", OracleType.DateTime).Value = request.startDate;
                dbCommand.Parameters.Add(":EndDate5", OracleType.DateTime).Value = request.endDate;
                dbCommand.Parameters.Add(":Program", OracleType.VarChar).Value = request.program_code;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        CLSReasonSummaryGrid.CLSReasonSummary gridSumry = new CLSReasonSummaryGrid.CLSReasonSummary();

                        gridSumry.program_code = reader.GetString(reader.GetOrdinal("ProgramCode"));
                        gridSumry.change_code = reader.GetString(reader.GetOrdinal("ChangeCode"));
                        gridSumry.accept_part_count = reader.GetInt32(reader.GetOrdinal("AcceptPartNumCount"));
                        gridSumry.accept_part_cost = reader.GetDecimal(reader.GetOrdinal("AcceptExtCostSum"));
                        gridSumry.reject_part_count = reader.GetInt32(reader.GetOrdinal("RejectNumCount"));
                        gridSumry.reject_part_cost = reader.GetDecimal(reader.GetOrdinal("RejectExtCostSum"));

                        grid.rows.Add(gridSumry);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Count number of records found
            grid.totalRows = grid.rows.Count;

            //Return grid with results
            return grid;
        }

        #endregion CLS

        #region CCAD
        /// <summary>
        /// Gets data for the CCAD requirement grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal CCADRequirementGrid getCCADRequirementGrid(CCADRequirementViewRequest request)
        {
            //Variables
            CCADRequirementGrid grid = new CCADRequirementGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.rows = new List<CCADRequirementGrid.CCADRequirement>();
            OracleDataReader reader = null;
            bool useSpoUser = false;

            //If all params empty use spo export user, not asset manager in query
            if (String.IsNullOrEmpty(request.spoUser) && String.IsNullOrEmpty(request.part_no))
            {
                useSpoUser = true;
                request.spoUser = Helper.getSessionConnectionInfoById(1);
            }

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //string reqQuery = "SELECT nb.internal_order_no, " +
                //                  " req.spo_export_user, " +
                //                  " req.part_no, " +
                //                  " req.status, " +
                //                  " NVL(nb.asset_manager_remark,' ') as change_reason, " +
                //                  " req.spo_request_id, " +
                //                  " req.program_code, " +
                //                  " nb.spo_export_date, " +
                //                  " nb.request_due_date, " +
                //                  " nb.item_cost, " +
                //                  " NVL(prt.purchasing_site, 'Not Found') as site " +
                //                  "FROM escm.escm_new_buy_requirement nb, " +
                //                  "     escm.escm_cbom_part prt, " +
                //                  "   ( SELECT DISTINCT nb.spo_export_user, " +
                //                  "                     nb.part_no, " +
                //                  "                     DECODE (COUNT(DISTINCT sts.activity_status), 0, ' ', 1, ord.activity_status, 'Various') as status, " +
                //                  "                     spo_request_id, " +
                //                  "                     nb.program_code, " +
                //                  "                     nb.item_cost " +
                //                  "     FROM escm.escm_new_buy_requirement nb, " +
                //                  "          escm.escm_new_buy_order ord, " +
                //                  "        ( SELECT internal_order_no, activity_Status " +
                //                  "          FROM escm.escm_new_buy_order " +
                //                  "          GROUP BY internal_order_no, activity_status)  sts, " +
                //                  "        ( SELECT sysdate-value_1 as ovhl_date " +
                //                  "          FROM escm.escm_lookup " +
                //                  "          WHERE category = 'NEWBUY_OVHL_DAYS') days " +
                //                  "WHERE nb.internal_order_no = ord.internal_order_no (+) " +
                //                  " AND ord.internal_order_no = sts.internal_order_no  (+) " +
                //                  " AND nb.program_code = 'DCC' " +
                //                  " AND nb.spo_export_date >= days.ovhl_date ";
                string reqQuery = "SELECT internal_order_no, " +
                                                  " spo_export_user, " +
                                                  " part_no, " +
                                                  " status, " +
                                                  " change_reason, " +
                                                  " spo_request_id, " +
                                                  " program_code, " +
                                                  " spo_export_date, " +
                                                  " request_due_date, " +
                                                  " item_cost, " +
                                                  " site ," +
                                                  " order_ttl, " +
                                                  " min_buy_qty, " +
                                                  " total_monthly_capacity_qty, " +
                                                  " annual_buy_ind " +
                                  "FROM ESCM.ESCM_NB_GETCCADREQGRID_VW nb WHERE 1=1 ";

                if (!String.IsNullOrEmpty(request.spoUser) && useSpoUser)
                {
                    reqQuery += "AND nb.spo_export_user = :spoUser ";
                    dbCommand.Parameters.Add(":spoUser", OracleType.VarChar).Value = request.spoUser;
                }
                else if (!String.IsNullOrEmpty(request.spoUser) && !useSpoUser)
                {
                    reqQuery += "AND nb.asset_manager_id  = :spoUser ";
                    dbCommand.Parameters.Add(":spoUser", OracleType.VarChar).Value = request.spoUser;
                }

                if (!String.IsNullOrEmpty(request.part_no))
                {
                    reqQuery += "AND nb.part_no = :part_no ";
                    dbCommand.Parameters.Add(":part_no", OracleType.VarChar).Value = request.part_no;
                }

                //reqQuery += " GROUP BY nb.spo_export_user, nb.part_no, ord.activity_status, nb.spo_request_id, nb.program_code, nb.item_cost " +
                //            " ORDER BY nb.part_no) req " +
                //            "WHERE req.spo_request_id  = nb.spo_request_id " +
                //            "  AND nb.part_no = prt.part_no " +
                //            "  AND req.status != 'Confirmed Order' " +
                reqQuery += " ORDER BY " +
                             (request.index_isValid() ? request.index : "spo_export_date") +
                             " " +
                             (request.order_isValid() ? request.order : "desc");

                dbCommand.CommandText = reqQuery;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        DateTime dateTemp;

                        CCADRequirementGrid.CCADRequirement gridReq = new CCADRequirementGrid.CCADRequirement();
                        gridReq.internal_order_no = reader.GetInt32(reader.GetOrdinal("internal_order_no"));
                        gridReq.spo_export_user = reader.GetString(reader.GetOrdinal("spo_export_user"));
                        gridReq.part_no = reader.GetString(reader.GetOrdinal("part_no"));
                        gridReq.status = reader.GetString(reader.GetOrdinal("status"));
                        gridReq.change_reason = reader.GetString(reader.GetOrdinal("change_reason"));
                        gridReq.spo_request_id = reader.GetInt32(reader.GetOrdinal("spo_request_id"));

                        dateTemp = reader.GetDateTime(reader.GetOrdinal("spo_export_date"));
                        gridReq.spo_export_date = (dateTemp.Month).ToString() + "/" +
                            (dateTemp.Day).ToString() + "/" +
                            (dateTemp.Year).ToString();

                        dateTemp = reader.GetDateTime(reader.GetOrdinal("request_due_date"));
                        gridReq.request_due_date = (dateTemp.Month).ToString() + "/" +
                            (dateTemp.Day).ToString() + "/" +
                            (dateTemp.Year).ToString();

                        gridReq.item_cost = reader.GetDecimal(reader.GetOrdinal("item_cost"));
                        gridReq.site = reader.GetString(reader.GetOrdinal("site"));
                        // PRP 5-31 modified for new requirement columns
                        gridReq.order_quantity = reader.GetString(reader.GetOrdinal("order_ttl"));
                        gridReq.min_buy_qty = reader.GetString(reader.GetOrdinal("min_buy_qty"));
                        gridReq.total_monthly_capacity_qty = reader.GetString(reader.GetOrdinal("total_monthly_capacity_qty"));
                        gridReq.annual_buy_ind = reader.GetString(reader.GetOrdinal("annual_buy_ind"));

                        grid.rows.Add(gridReq);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    //Ignore Error???
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Count number of records found
            grid.totalRows = grid.rows.Count;

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// Gets data for the CCAD order grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal CCADOrderGrid getCCADOrderGrid(CCADOrderViewRequest request)
        {
            //Variables
            CCADOrderGrid grid = new CCADOrderGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.rows = new List<CCADOrderGrid.CCADOrder>();
            OracleDataReader reader = null;

            //format list of requirement internal order numbers for query
            string requirementOrderNum = request.orderNumKey.ToDelimitedString(",");
            if (String.IsNullOrEmpty(requirementOrderNum))
            {
                requirementOrderNum = "";
            }

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "WITH ord_ttl AS ( " +
                    "SELECT internal_order_no, sum(order_quantity) as ord_ttl " +
                    "FROM escm.escm_new_buy_order " +
                    "WHERE internal_order_no IN (" + requirementOrderNum + ") " +
                    "AND activity_status != 'Rejected by Asset Manager' " +
                    "GROUP BY internal_order_no) " +
                    "SELECT " +
                    "nb.internal_order_no," +
                    "nb.requirement_schedule_no," +
                    "req.spo_request_id," +
                    "nb.due_date," +
                    "NVL(nb.order_quantity, 0) as order_quantity," +
                    "nb.priority," +
                    "NVL(nb.cost_charge_number, ' ') as cost_charge_number," +
                    "NVL(req.asset_manager_remark,' ') as change_reason," +
                    "nb.order_no," +
                    "nb.activity_status," +
                    "nb.part_no, " +
                    "NVL2(pb.part_no, 'true', 'false') as pricebreak, " +
                    "req.requirement_quantity as spo_qty, " +
                    "NVL(ord.ord_ttl, 0) as order_total " +
                    "FROM escm.escm_new_buy_order nb, " +
                    "     escm.escm_new_buy_requirement req, " +
                    "     (select distinct part_no from escm.escm_lta_pricebreak_mvw) pb, " +
                    "     ord_ttl ord " +
                    "WHERE nb.internal_order_no = ord.internal_order_no(+)" +
                    "AND nb.part_no = pb.part_no(+) " +
                    "AND req.internal_order_no  = nb.internal_order_no " +
                    "AND nb.internal_order_no IN (" + requirementOrderNum + ") " +
                    "ORDER BY spo_request_id asc, " + request.index + " " + request.order;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        DateTime dateTemp;

                        CCADOrderGrid.CCADOrder gridOrd = new CCADOrderGrid.CCADOrder();
                        gridOrd.internal_order_no = reader.GetInt32(reader.GetOrdinal("internal_order_no"));
                        gridOrd.requirement_schedule_no = reader.GetInt32(reader.GetOrdinal("requirement_schedule_no"));
                        gridOrd.spo_request_id = reader.GetInt32(reader.GetOrdinal("spo_request_id"));

                        dateTemp = reader.GetDateTime(reader.GetOrdinal("due_date"));
                        gridOrd.due_date = (dateTemp.Month).ToString() + "/" +
                            (dateTemp.Day).ToString() + "/" +
                            (dateTemp.Year).ToString();

                        gridOrd.order_quantity = reader.GetInt32(reader.GetOrdinal("order_quantity"));
                        gridOrd.priority = reader.GetInt32(reader.GetOrdinal("priority"));
                        gridOrd.cost_charge_number = reader.GetString(reader.GetOrdinal("cost_charge_number"));
                        gridOrd.change_reason = reader.GetString(reader.GetOrdinal("change_reason"));
                        gridOrd.order_no = reader.GetString(reader.GetOrdinal("order_no"));
                        gridOrd.activity_status = reader.GetString(reader.GetOrdinal("activity_status"));
                        gridOrd.spo_qty = reader.GetInt32(reader.GetOrdinal("spo_qty"));
                        gridOrd.order_total = reader.GetInt32(reader.GetOrdinal("order_total"));
                        gridOrd.part_no = reader.GetString(reader.GetOrdinal("part_no"));

                        var hasLTA = reader.GetString(reader.GetOrdinal("pricebreak"));
                        gridOrd.pricebreak = Convert.ToBoolean(hasLTA);

                        gridOrd.pricePoint = 0;

                        grid.rows.Add(gridOrd);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    //Ignore Error???
                    throw;
                }
                catch (Exception)
                {
                    //Ignore Error???
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Get pricepoint for each LTA part
            foreach (CCADOrderGrid.CCADOrder ord in grid.rows)
            {
                //Only check parts that have an LTA
                if (ord.pricebreak)
                {
                    try
                    {
                        //Find pricepoint
                        ord.pricePoint = getLtaPricePoint(ord.part_no, ord.order_quantity, ord.order_no.Substring(0, 3));
                    }
                    catch (Exception)
                    {
                        //Ignore Errors and Move on
                    }
                }
            }

            //Count number of records found
            grid.totalRows = grid.rows.Count;

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// Edits a CCAD order using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg editCCADOrder(CCADOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            string status = "";
            string changeReason = request.change_reason;
            bool flagSetApprovedDate = false;

            //Check order is still editable
            if (orderSent(request.internal_order_no, request.requirement_schedule_no))
            {
                result.addError("Order changes not allowed after sent to gold.");
                return result;
            }

            //Get requirement relationship keys
            CCADOrderReqData reqData = getCCADOrderReqData(request.internal_order_no);

            //Clear CCN Error Message
            if (request.cost_charge_number.ToUpper() == "CCNNOTFOUND")
            {
                request.cost_charge_number = "";
            }

            //get CCN when blank
            if (String.IsNullOrEmpty(request.cost_charge_number))
            {
                //Find ccn
                request.cost_charge_number = getOVHLCCN(reqData.program_code, request.due_date, request.part_no);
            }

            //get activity status
            switch (request.activity_status)
            {
                case 20: //Review
                    {
                        status = OrderActivityStatus.order_status_values[0];
                        break;
                    }
                case 30: //Approved
                    {
                        if (validateOVHLCCN(request.cost_charge_number, reqData.program_code, request.due_date, request.part_no))
                        {
                            result.addReturnData((reqData.program_code + reqData.spo_request_id + request.requirement_schedule_no.ToString("00")),
                                "true");
                            status = OrderActivityStatus.order_status_values[1];
                            flagSetApprovedDate = true;
                        }
                        else
                        {
                            result.addReturnData((reqData.program_code + reqData.spo_request_id + request.requirement_schedule_no.ToString("00")),
                                "false");
                            result.addError("Your order was saved but the CCN was invalid. Status changed to awaiting review.");
                            status = OrderActivityStatus.order_status_values[0];
                        }

                        break;
                    }
                case 40: //Rejected
                    {
                        status = OrderActivityStatus.order_status_values[2];
                        flagSetApprovedDate = true;

                        //If no comment is specified, Rejected is default
                        if (!checkComment(request.internal_order_no))
                        {
                            if (changeReason == "")
                            {
                                changeReason = "Rejected";
                            }
                        }

                        break;
                    }
                default:
                    {
                        status = OrderActivityStatus.order_status_values[0];
                        break;
                    }
            }

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                string query = "UPDATE escm.escm_new_buy_order " +
                    "SET due_date = :DueDate, " +
                    "order_quantity = :Qty, " +
                    "priority = :Priority, ";

                if (flagSetApprovedDate)
                {
                    query += "amgr_approval_date = :AmgrApproval, ";
                }
                else
                {
                    query += "amgr_approval_date = null, ";
                }

                query += "cost_charge_number = :CCN, " +
                    "activity_status = :Status " +
                    "WHERE internal_order_no = :OrderNum " +
                        "AND requirement_schedule_no = :ScheduleNum";

                dbCommand.CommandText = query;
                dbCommand.Parameters.Add(":DueDate", OracleType.DateTime).Value = OracleDateTime.Parse(request.due_date);
                dbCommand.Parameters.Add(":Qty", OracleType.Int32).Value = request.order_quantity;
                dbCommand.Parameters.Add(":Priority", OracleType.Int32).Value = request.priority;
                if (flagSetApprovedDate)
                {
                    dbCommand.Parameters.Add(":AmgrApproval", OracleType.DateTime).Value = DateTime.Today;
                }
                dbCommand.Parameters.Add(":CCN", OracleType.VarChar).Value = request.cost_charge_number;
                dbCommand.Parameters.Add(":Status", OracleType.VarChar).Value = status;
                dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = request.internal_order_no;
                dbCommand.Parameters.Add(":ScheduleNum", OracleType.Int32).Value = request.requirement_schedule_no;

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to save order changes.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            /* If Approved and Order quantities match, set Variance Comment to 'Accept' when empty */
            if (String.IsNullOrEmpty(changeReason) && !checkMismatch(request.internal_order_no))
            {
                if (status == OrderActivityStatus.order_status_values[1])
                {
                    changeReason = "Accept";
                }
            }

            /* Update the Requirements table - Variance Comment - For non-prompted comments*/
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                string query = "UPDATE escm.escm_new_buy_requirement " +
                    "SET asset_manager_remark = :ChangeReason " +
                    "WHERE internal_order_no = :OrderNum ";

                dbCommand.CommandText = query;
                dbCommand.Parameters.Add(":ChangeReason", OracleType.VarChar).Value = changeReason;
                dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = request.internal_order_no;

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to save order changes.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            /* Check for mismatch between SPO and Ordered */
            if (checkReasonRequired(request.internal_order_no))
            {
                string internalOrderNo = request.internal_order_no.ToString();

                Dictionary<string, string> required = new Dictionary<string, string>();
                required.Add(internalOrderNo, "true");

                //Flag for call back, 
                //0 position for Variance Comment dialog
                result.addCbk(required);
            }

            return result;
        }

        /// <summary>
        /// Deletes a CCAD order using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg deleteCCADOrder(CCADOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();

            //Check order is still editable
            if (orderSent(request.internal_order_no, request.requirement_schedule_no))
            {
                result.addError("Order changes not allowed after sent to gold.");
                return result;
            }

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "DELETE FROM escm.escm_new_buy_order " +
                    "WHERE internal_order_no = :OrderNum " +
                        "AND requirement_schedule_no = :ScheduleNum";
                dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = request.internal_order_no;
                dbCommand.Parameters.Add(":ScheduleNum", OracleType.Int32).Value = request.requirement_schedule_no;

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to delete order.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Adds a new CCAD order using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns>ResponseMsg object</returns>
        internal ResponseMsg newCCADOrder(CCADOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            string orderNum = "";
            string segCode = "";
            string scheduleTemp = "";
            string status = "";
            string changeReason = "";
            bool flagSetApprovedDate = false;

            //Get requirement relationship keys
            CCADOrderReqData reqData = getCCADOrderReqData(request.internal_order_no);

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                try
                {
                    changeReason = request.change_reason;

                    //Get max schedule number
                    dbCommand.CommandText = "SELECT MAX(requirement_schedule_no) as req " +
                        "FROM escm.escm_new_buy_order " +
                        "WHERE internal_order_no = :OrderNum";
                    dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = request.internal_order_no;

                    dbConnection.Open(); //Open Connection
                    var maxScheduleNo = dbCommand.ExecuteScalar(); //Execute query
                    dbConnection.Close(); //Close Connection
                    dbCommand.Parameters.Clear(); //Clear params for next query

                    //Convert to int datatype
                    if (maxScheduleNo.GetType() == DBNull.Value.GetType())
                    {
                        maxScheduleNo = 0;
                    }
                    request.requirement_schedule_no = Convert.ToInt32(maxScheduleNo);

                    //Increment by 1 for new record
                    request.requirement_schedule_no = request.requirement_schedule_no + 1;

                    //Generate Order number
                    scheduleTemp = request.requirement_schedule_no.ToString();
                    if (scheduleTemp.Length == 1)
                    {
                        scheduleTemp = "0" + scheduleTemp;
                    }
                    orderNum = reqData.program_code + reqData.spo_request_id.ToString() + scheduleTemp;

                    //get segcode, set to " " if no result
                    segCode = getOVHLSegcode(reqData.program_code, request.part_no);
                    if (String.IsNullOrEmpty(segCode))
                    {
                        segCode = " ";
                    }

                    //get CCN when blank
                    if (String.IsNullOrEmpty(request.cost_charge_number))
                    {
                        request.cost_charge_number = getOVHLCCN(reqData.program_code, request.due_date, request.part_no);
                    }

                    //get activity status
                    switch (request.activity_status)
                    {
                        case 20: //Review
                            {
                                status = OrderActivityStatus.order_status_values[0];
                                break;
                            }
                        case 30: //Approved
                            {
                                if (validateOVHLCCN(request.cost_charge_number, reqData.program_code, request.due_date, request.part_no))
                                {
                                    result.addReturnData((reqData.program_code + reqData.spo_request_id + request.requirement_schedule_no.ToString("00")),
                                        "true");
                                    status = OrderActivityStatus.order_status_values[1];
                                    flagSetApprovedDate = true;
                                }
                                else
                                {
                                    result.addReturnData((reqData.program_code + reqData.spo_request_id + request.requirement_schedule_no.ToString("00")),
                                        "false");
                                    result.addError("Your order was saved but the CCN was invalid. Status changed to awaiting review.");
                                    status = OrderActivityStatus.order_status_values[0];
                                }
                                break;
                            }
                        case 40: //Rejected
                            {
                                status = OrderActivityStatus.order_status_values[2];
                                flagSetApprovedDate = true;

                                //If no comment already specified, Rejected is default
                                if (!checkComment(request.internal_order_no))
                                {
                                    if (String.IsNullOrEmpty(changeReason))
                                    {
                                        changeReason = "Rejected";
                                    }
                                }

                                break;
                            }
                        default:
                            {
                                status = OrderActivityStatus.order_status_values[0];
                                break;
                            }
                    }

                    //Insert new record
                    string query = "INSERT INTO escm.escm_new_buy_order " +
                        "(INTERNAL_ORDER_NO, " +
                        "REQUIREMENT_SCHEDULE_NO, " +
                        "ORDER_NO, " +
                        "PART_NO, " +
                        "SEG_CODE, " +
                        "AMGR_APPROVAL_DATE, " +
                        "ORDER_QUANTITY, " +
                        "DUE_DATE, " +
                        "COST_CHARGE_NUMBER, " +
                        "ACTIVITY_STATUS, " +
                        "PRIORITY, " +
                        "VALIDATION_STATUS) " +
                        "VALUES ( " +
                            ":IntOrderNum, " + //INTERNAL_ORDER_NO
                            ":ReqSchNum, " + //REQUIREMENT_SCHEDULE_NO
                            ":OrderNum, " + //ORDER_NO
                            ":PartNum, " + //PART_NO
                            ":SegCode, "; //SEG_CODE

                    if (flagSetApprovedDate)
                    {
                        query += ":AmgrApproval, "; //AMGR_APPROVAL_DATE
                    }
                    else
                    {
                        query += "null, "; //AMGR_APPROVAL_DATE
                    }

                    query += ":Qty, " + //ORDER_QUANTITY
                            ":DueDate, " + //DUE_DATE
                            ":CCN, " + //COST_CHARGE_NUMBER
                            ":Status, " + //ACTIVITY_STATUS
                            ":Priority, " + //PRIORITY
                            ":ValidStatus )"; //VALIDATION_STATUS

                    dbCommand.CommandText = query;
                    dbCommand.Parameters.Add(":IntOrderNum", OracleType.Int32).Value = request.internal_order_no;
                    dbCommand.Parameters.Add(":ReqSchNum", OracleType.Int32).Value = request.requirement_schedule_no;
                    dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = orderNum;
                    dbCommand.Parameters.Add(":PartNum", OracleType.VarChar).Value = reqData.part_no;
                    dbCommand.Parameters.Add(":SegCode", OracleType.VarChar).Value = segCode;
                    if (flagSetApprovedDate)
                    {
                        dbCommand.Parameters.Add(":AmgrApproval", OracleType.DateTime).Value = DateTime.Today;
                    }
                    dbCommand.Parameters.Add(":Qty", OracleType.Int32).Value = request.order_quantity;
                    dbCommand.Parameters.Add(":DueDate", OracleType.DateTime).Value = OracleDateTime.Parse(request.due_date);
                    dbCommand.Parameters.Add(":CCN", OracleType.VarChar).Value = request.cost_charge_number;
                    dbCommand.Parameters.Add(":Status", OracleType.VarChar).Value = status;
                    dbCommand.Parameters.Add(":Priority", OracleType.Int32).Value = request.priority;
                    dbCommand.Parameters.Add(":ValidStatus", OracleType.VarChar).Value = "F";

                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to save new order.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            /* If Approved and Order quantities match, set Variance Comment to 'Accept' when empty */
            if (String.IsNullOrEmpty(changeReason) && !checkMismatch(request.internal_order_no))
            {
                if (status == OrderActivityStatus.order_status_values[1])
                {
                    changeReason = "Accept";
                }
            }

            if (changeReason != "")
            {
                /* Update the Requirements table - SPO Mismatch (Change Reason) Comment*/
                using (OracleCommand dbCommand = dbConnection.CreateCommand())
                {
                    string query = "UPDATE escm.escm_new_buy_requirement " +
                        "SET asset_manager_remark = :ChangeReason " +
                        "WHERE internal_order_no = :OrderNum ";

                    dbCommand.CommandText = query;
                    dbCommand.Parameters.Add(":ChangeReason", OracleType.VarChar).Value = changeReason;
                    dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = request.internal_order_no;

                    try
                    {
                        dbConnection.Open();
                        dbCommand.ExecuteNonQuery();
                        dbConnection.Close();
                    }
                    catch (OracleException oe)
                    {
                        result.addError("Unable to save order changes.");
                        result.addError(oe.ToString());
                    }
                    finally
                    {
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }
                    }
                }
            }

            // Now that orders are in ESCM lets check for a mismatch
            if (checkReasonRequired(request.internal_order_no))
            {
                string internalOrderNo = request.internal_order_no.ToString();

                Dictionary<string, string> required = new Dictionary<string, string>();
                required.Add(internalOrderNo, "true");

                //Flag for call back, 
                //0 position for Variance Comment dialog
                result.addCbk(required);
            }

            return result;
        }

        /// <summary>
        /// Deletes multiple ccad orders using the supplied request
        /// </summary>
        /// <param name="request">validated request object</param>
        /// <returns>ResponseMsg object</returns>
        internal ResponseMsg deleteCCADOrders(CCADMultiOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            int seqNum = 0;
            bool attemptLastOrderDelete = false;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //process each order number for deletion
                request.orders.ForEach(delegate(string order)
                {
                    try
                    {
                        //clear params
                        dbCommand.Parameters.Clear();

                        //Check order is still editable
                        if (orderSent(order))
                        {
                            result.addMsg("Cannot edit " + order + ", order already sent to gold.");
                        }
                        else
                        {
                            //get sequence number (last two characters)
                            seqNum = Convert.ToInt32(order.Substring(order.Length - 2, 2));

                            //set order to rejected if sequence 1, otherwise delete
                            if (seqNum < 2)
                            {
                                //Flag message to user
                                attemptLastOrderDelete = true;

                                //set status
                                dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                    "set activity_status = :activitySts, " +
                                    "amgr_approval_date = sysdate " +
                                    "where order_no = :orderNum";

                                //set param
                                dbCommand.Parameters.AddWithValue(":activitySts", OrderActivityStatus.order_status_values[2]);
                                dbCommand.Parameters.AddWithValue(":orderNum", order);
                            }
                            else
                            {
                                //set command
                                dbCommand.CommandText = "DELETE FROM escm.escm_new_buy_order " +
                                    "WHERE order_no = :orderNum";

                                //set param
                                dbCommand.Parameters.AddWithValue(":orderNum", order);
                            }

                            //run command
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();
                        }
                    }
                    catch (Exception)
                    {
                        result.addError("Error Deleting Order: " + order);
                    }
                    finally
                    {
                        //Close connection
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }
                    }
                });
            }

            //Add message if last order was set to reject
            if (attemptLastOrderDelete)
            {
                result.addMsg("Recommendations must have at least 1 associated order. " +
                    "Orders ending in '01' have been marked as rejected.");
            }

            return result;
        }

        /// <summary>
        /// Rejects multiple ccad orders using the supplied request
        /// </summary>
        /// <param name="request">validated request object</param>
        /// <returns>ResponseMsg object</returns>
        internal ResponseMsg rejectCCADOrders(CCADMultiOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //process each order number for deletion
                request.orders.ForEach(delegate(string order)
                {
                    try
                    {
                        //Check order is still editable
                        if (orderSent(order))
                        {
                            result.addMsg("Cannot edit " + order + ", order already sent to gold.");
                        }
                        else
                        {
                            //set command to update orders table
                            dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                "set activity_status = :activitySts, " +
                                "amgr_approval_date = sysdate " +
                                "where order_no = :orderNum";

                            //set param
                            dbCommand.Parameters.AddWithValue(":activitySts", OrderActivityStatus.order_status_values[2]);
                            dbCommand.Parameters.Add(":orderNum", OracleType.VarChar, 20);
                            //set params value
                            dbCommand.Parameters[":orderNum"].Value = order;

                            //run command
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();

                            //Clear params for next query 
                            dbCommand.Parameters.Clear();

                            //set command to update requirement table
                            dbCommand.CommandText = "update ESCM.escm_new_buy_requirement " +
                                                     "set asset_manager_remark = 'Rejected' " +
                                                     "where internal_order_no in (select req.internal_order_no " +
                                                            "from ESCM.escm_new_buy_requirement req, ESCM.escm_new_buy_order ord " +
                                                            "where ord.internal_order_no = req.internal_order_no  " +
                                                            "and ord.order_no = :orderNum " +
                                                            "and (req.asset_manager_remark is null or req.asset_manager_remark = 'Accept')) ";

                            //set param
                            dbCommand.Parameters.Add(":orderNum", OracleType.VarChar, 20);
                            //set params value
                            dbCommand.Parameters[":orderNum"].Value = order;

                            //run command
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();
                        }
                    }
                    catch (Exception)
                    {
                        result.addError("Error Rejecting Order: " + order);
                    }
                    finally
                    {
                        //Close connection
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }
                    }
                });
            }

            return result;
        }

        /// <summary>
        /// Validates multiple ccad orders using the supplied request
        /// </summary>
        /// <param name="request">validated request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg validateCCADOrders(CCADMultiOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            OracleDataReader reader = null;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //approve each order_no
                request.orders.ForEach(delegate(string order)
                {
                    try
                    {
                        //Order processing variables
                        DateTime dateTemp;
                        string ccn = "";
                        string programCode = "";
                        string dueDate = "";
                        string part = "";

                        //set command object properties
                        dbCommand.CommandText = "select NVL(ord.cost_charge_number, '') as cost_charge_number, " +
                            "NVL(req.program_code, '') as program_code, NVL(ord.due_date, '') as due_date, " +
                            "NVL(ord.part_no, '') as part_no " +
                            "from ESCM.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                            "where ord.order_no = :OrderNum " +
                                "and ord.internal_order_no = req.internal_order_no(+)";
                        dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;

                        //Get required new buy validation parameters
                        dbConnection.Open();
                        reader = dbCommand.ExecuteReader();
                        while (reader.Read() == true)
                        {
                            ccn = reader.GetString(reader.GetOrdinal("cost_charge_number"));
                            programCode = reader.GetString(reader.GetOrdinal("program_code"));

                            dateTemp = reader.GetDateTime(reader.GetOrdinal("due_date"));
                            dueDate = (dateTemp.Month).ToString() + "/" +
                                (dateTemp.Day).ToString() + "/" +
                                (dateTemp.Year).ToString();

                            part = reader.GetString(reader.GetOrdinal("part_no"));
                        }
                        reader.Close();
                        dbConnection.Close();

                        //Clear params for next query
                        dbCommand.Parameters.Clear();

                        //Validate ccn
                        if (validateOVHLCCN(ccn, programCode, dueDate, part))
                        {
                            //Return validation to user
                            result.addReturnData(order, "true");
                        }
                        else
                        {
                            //Return validation to user
                            result.addReturnData(order, "false");
                        }
                    }
                    catch (Exception)
                    {
                        result.addError("Error Processing: " + order);
                    }
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Close();
                        }

                        //Make sure connection is closed
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }

                        //Make sure parameters are reset
                        if (dbCommand.Parameters.Count > 0)
                        {
                            dbCommand.Parameters.Clear();
                        }
                    }

                });
            }

            return result;
        }

        /// <summary>
        /// Approves multiple ccad asset manager orders using the supplied request
        /// </summary>
        /// <param name="request">validated request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg approveCCADAmOrders(CCADMultiOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            OracleDataReader reader = null;
            int invalidCCNCount = 0;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //approve each order_no
                request.orders.ForEach(delegate(string order)
                {
                    try
                    {
                        //Check order is still editable
                        if (orderSent(order))
                        {
                            result.addMsg("Cannot edit " + order + ", order already sent to gold.");
                        }
                        else
                        {
                            //Order processing variables
                            DateTime dateTemp;
                            int internalOrderNo = 0;

                            string ccn = "";
                            string programCode = "";
                            string dueDate = "";
                            string part = "";
                            string changeReason = "";
                            bool validCCN = false;

                            //set command object properties
                            dbCommand.CommandText = "select ord.internal_order_no, NVL(ord.cost_charge_number, '') as cost_charge_number, " +
                                "NVL(req.program_code, '') as program_code, NVL(ord.due_date, '') as due_date, " +
                                "NVL(ord.part_no, '') as part_no, NVL(req.asset_manager_remark, '') as reason_code " +
                                "from ESCM.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                                "where ord.order_no = :OrderNum " +
                                    "and ord.internal_order_no = req.internal_order_no(+)";
                            dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;

                            //Get required new buy validation parameters
                            dbConnection.Open();
                            reader = dbCommand.ExecuteReader();
                            while (reader.Read() == true)
                            {
                                internalOrderNo = reader.GetInt32(reader.GetOrdinal("internal_order_no"));

                                ccn = reader.GetString(reader.GetOrdinal("cost_charge_number"));
                                programCode = reader.GetString(reader.GetOrdinal("program_code"));

                                dateTemp = reader.GetDateTime(reader.GetOrdinal("due_date"));
                                dueDate = (dateTemp.Month).ToString() + "/" +
                                    (dateTemp.Day).ToString() + "/" +
                                    (dateTemp.Year).ToString();

                                part = reader.GetString(reader.GetOrdinal("part_no"));

                                if (!reader.IsDBNull(reader.GetOrdinal("reason_code")))
                                {
                                    changeReason = reader.GetString(reader.GetOrdinal("reason_code"));
                                }
                            }
                            reader.Close();
                            dbConnection.Close();

                            //Get ccn if ccn not found message present
                            if (ccn.ToUpper() == "CCNNOTFOUND")
                            {
                                //Get CCN
                                ccn = getOVHLCCN(programCode, dueDate, part);

                                //Clear params for next query
                                dbCommand.Parameters.Clear();

                                //Create save query
                                dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                    "set cost_charge_number = :CCN " +
                                    "where order_no = :OrderNum";
                                dbCommand.Parameters.Add(":CCN", OracleType.VarChar).Value = ccn;
                                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;

                                //Save to database
                                dbConnection.Open();
                                dbCommand.ExecuteNonQuery();
                                dbConnection.Close();
                            }

                            //Clear params for next query
                            dbCommand.Parameters.Clear();

                            //Validate ccn
                            validCCN = validateOVHLCCN(ccn, programCode, dueDate, part);

                            //Perform update based on valid ccn
                            if (validCCN)
                            {
                                //Return validation to user
                                result.addReturnData(order, "true");

                                //set status and approval date
                                dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                    "set activity_status = :ActivitySts, " +
                                    "amgr_approval_date = :ApprvDate " +
                                    "where order_no = :OrderNum";
                                dbCommand.Parameters.Add(":ActivitySts", OracleType.VarChar).Value = OrderActivityStatus.order_status_values[1];
                                dbCommand.Parameters.Add(":ApprvDate", OracleType.DateTime).Value = DateTime.Today;
                                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;
                            }
                            else
                            {
                                //Return validation to user
                                result.addReturnData(order, "false");

                                //Clear approval and status
                                dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                    "set activity_status = :ActivitySts, " +
                                    "amgr_approval_date = null " +
                                    "where order_no = :OrderNum";
                                dbCommand.Parameters.Add(":ActivitySts", OracleType.VarChar).Value = OrderActivityStatus.order_status_values[0];
                                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;
                                invalidCCNCount++;
                            }

                            //Save to database
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();

                            //Clear params for next query 
                            dbCommand.Parameters.Clear();

                            /* Change Variance remarks to Accept if Order quantity matches SPO and reason blank*/
                            if (validCCN && String.IsNullOrEmpty(changeReason))
                            {
                                if (!checkMismatch(internalOrderNo))
                                {
                                    dbCommand.CommandText = "UPDATE escm.escm_new_buy_requirement " +
                                                            "SET asset_manager_remark = 'Accept' " +
                                                            "WHERE internal_order_no = :OrderNum ";

                                    dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = internalOrderNo;

                                    //Save to database
                                    dbConnection.Open();
                                    dbCommand.ExecuteNonQuery();
                                    dbConnection.Close();

                                    //Clear params for next query 
                                    dbCommand.Parameters.Clear();

                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        result.addMsg("Error Processing Order: " + order);
                    }
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Close();
                        }

                        //Make sure connection is closed
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }

                        //Make sure parameters are reset
                        if (dbCommand.Parameters.Count > 0)
                        {
                            dbCommand.Parameters.Clear();
                        }
                    }

                });
            }

            //Inform user of any invalid ccns
            if (invalidCCNCount > 0)
            {
                result.addError(invalidCCNCount.ToString() + " order(s) contain an invalid CCN. These orders were not approved.");
            }

            return result;
        }

        /// <summary>
        /// Approves multiple ccad ccb orders using the supplied request
        /// </summary>
        /// <param name="request">validated request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg approveCCADCcbOrders(CCADMultiOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            OracleDataReader reader = null;
            int invalidCCNCount = 0;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //approve each order_no
                request.orders.ForEach(delegate(string order)
                {
                    try
                    {
                        //Check order is still editable
                        if (orderSent(order))
                        {
                            result.addMsg("Cannot edit " + order + ", order already sent to gold.");
                        }
                        else
                        {
                            //Order processing variables
                            DateTime dateTemp;
                            string ccn = "";
                            string programCode = "";
                            string dueDate = "";
                            string part = "";

                            //set command object properties
                            dbCommand.CommandText = "select NVL(ord.cost_charge_number, '') as cost_charge_number, " +
                                "NVL(req.program_code, '') as program_code, NVL(ord.due_date, '') as due_date, " +
                                "NVL(ord.part_no, '') as part_no " +
                                "from ESCM.escm_new_buy_order ord, escm.escm_new_buy_requirement req " +
                                "where ord.order_no = :OrderNum " +
                                    "and ord.internal_order_no = req.internal_order_no(+)";
                            dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;

                            //Get required new buy validation parameters
                            dbConnection.Open();
                            reader = dbCommand.ExecuteReader();
                            while (reader.Read() == true)
                            {
                                ccn = reader.GetString(reader.GetOrdinal("cost_charge_number"));
                                programCode = reader.GetString(reader.GetOrdinal("program_code"));

                                dateTemp = reader.GetDateTime(reader.GetOrdinal("due_date"));
                                dueDate = (dateTemp.Month).ToString() + "/" +
                                    (dateTemp.Day).ToString() + "/" +
                                    (dateTemp.Year).ToString();

                                part = reader.GetString(reader.GetOrdinal("part_no"));
                            }
                            reader.Close();
                            dbConnection.Close();

                            //Clear params for next query
                            dbCommand.Parameters.Clear();
                            var t = validateOVHLCCN(ccn, programCode, dueDate, part);
                            //Validate ccn
                            if (validateOVHLCCN(ccn, programCode, dueDate, part))
                            {
                                //Return validation to user
                                result.addReturnData(order, "true");

                                //set status and approval date
                                dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                    "set activity_status = :ActivitySts, " +
                                    "reviewer_approval_date = :ApprvDate " +
                                    "where order_no = :OrderNum";
                                dbCommand.Parameters.Add(":ActivitySts", OracleType.VarChar).Value = CcbActivityStatus.ccb_status_values[1];
                                dbCommand.Parameters.Add(":ApprvDate", OracleType.DateTime).Value = DateTime.Today;
                                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;
                            }
                            else
                            {
                                //Return validation to user
                                result.addReturnData(order, "false");
                                invalidCCNCount++;

                                //Clear approval and status
                                dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                    "set activity_status = :ActivitySts, " +
                                    "reviewer_approval_date = null " +
                                    "where order_no = :OrderNum";
                                dbCommand.Parameters.Add(":ActivitySts", OracleType.VarChar).Value = CcbActivityStatus.ccb_status_values[0];
                                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;
                            }

                            //Save to database
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();

                            //Clear params for next query
                            dbCommand.Parameters.Clear();
                        }
                    }
                    catch (Exception)
                    {
                        result.addMsg("Error Processing Order: " + order);
                    }
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Close();
                        }

                        //Make sure connection is closed
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }

                        //Make sure parameters are reset
                        if (dbCommand.Parameters.Count > 0)
                        {
                            dbCommand.Parameters.Clear();
                        }
                    }

                });
            }

            //Inform user of any invalid ccns
            if (invalidCCNCount > 0)
            {
                result.addError(invalidCCNCount.ToString() + " order(s) contain an invalid CCN. These orders were not approved.");
            }

            return result;
        }

        /// <summary>
        /// Approves multiple ccad ccb orders using the supplied request
        /// </summary>
        /// <param name="request">validated request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg rejectCCADCcbOrders(CCADMultiOrderEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            OracleDataReader reader = null;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //approve each order_no
                request.orders.ForEach(delegate(string order)
                {
                    try
                    {
                        //Check order is still editable
                        if (orderSent(order))
                        {
                            result.addMsg("Cannot edit " + order + ", order already sent to gold.");
                        }
                        else
                        {
                            //set status
                            dbCommand.CommandText = "update ESCM.escm_new_buy_order " +
                                "set activity_status = :ActivitySts, " +
                                "reviewer_approval_date = null " +
                                "where order_no = :OrderNum";
                            dbCommand.Parameters.Add(":ActivitySts", OracleType.VarChar).Value = CcbActivityStatus.ccb_status_values[2];
                            dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = order;

                            //Save to database
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();

                            //Clear params for next query
                            dbCommand.Parameters.Clear();

                            //set command to update requirement table
                            dbCommand.CommandText = "update ESCM.escm_new_buy_requirement " +
                                                     "set asset_manager_remark = 'Rejected' " +
                                                     "where internal_order_no in (select req.internal_order_no " +
                                                            "from ESCM.escm_new_buy_requirement req, ESCM.escm_new_buy_order ord " +
                                                            "where ord.internal_order_no = req.internal_order_no  " +
                                                            "and ord.order_no = :orderNum " +
                                                            "and (req.asset_manager_remark is null or req.asset_manager_remark = 'Accept')) ";

                            //set param
                            dbCommand.Parameters.Add(":orderNum", OracleType.VarChar, 20);
                            //set params value
                            dbCommand.Parameters[":orderNum"].Value = order;

                            //run command
                            dbConnection.Open();
                            dbCommand.ExecuteNonQuery();
                            dbConnection.Close();
                        }
                    }
                    catch (Exception)
                    {
                        result.addMsg("Error Processing Order: " + order);
                    }
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Close();
                        }

                        //Make sure connection is closed
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }

                        //Make sure parameters are reset
                        if (dbCommand.Parameters.Count > 0)
                        {
                            dbCommand.Parameters.Clear();
                        }
                    }

                });
            }

            return result;
        }

        /// <summary>
        /// Gets data for the asset manager grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal CCADManagerGrid getCCADManagerGrid(CCADManagerViewRequest request)
        {
            //Variables
            CCADManagerGrid grid = new CCADManagerGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.rows = new List<CCADManagerGrid.CCADManager>();
            OracleDataReader reader = null;

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                if (request.role == "ccb") // ccb list
                {
                    //Pull applicable activity statuses and format for Where clause
                    string statusClause = "";

                    for (int i = 0; i < CcbActivityStatus.ccb_status_values.Length; i++)
                    {
                        if (i != CcbActivityStatus.ccb_status_values.Length - 1)
                        {
                            statusClause += " am.activity_status = '" + CcbActivityStatus.ccb_status_values[i] + "' OR";
                        }
                        else
                        {
                            statusClause += " am.activity_status = '" + CcbActivityStatus.ccb_status_values[i] + "' ";
                        }
                    }

                    //set query
                    dbCommand.CommandText = "SELECT DISTINCT am.asset_manager_id, NVL(am.full_name, ' ') as full_name " +
                        "FROM " +
                        "(SELECT DISTINCT r.asset_manager_id, u.full_name, o.activity_status " +
                        "FROM escm.escm_new_buy_requirement r, escm.escm_new_buy_order o, escm.escm_spo_ovhl_user_vw u, " +
                          "(SELECT sysdate-value_1 as ovhl_date FROM escm.escm_lookup WHERE category = 'NEWBUY_OVHL_DAYS') days " +
                        "WHERE r.asset_manager_id = u.escm_logon_id(+) " +
                          "AND r.program_code = 'DCC' " +
                          "AND r.spo_export_date >= days.ovhl_date " +
                          "AND r.internal_order_no = o.internal_order_no) am " +
                        "WHERE (" + statusClause +
                        ") ORDER BY full_name asc";
                }
                else //Asset manager list
                {
                    dbCommand.CommandText = "SELECT DISTINCT r.asset_manager_id, NVL(u.full_name, ' ') as full_name " +
                                            " FROM escm.escm_new_buy_requirement r, escm.escm_spo_ovhl_user_vw u, " +
                                            " (SELECT sysdate-value_1 as ovhl_date " +
                                            "  FROM escm.escm_lookup " +
                                            "  WHERE category = 'NEWBUY_OVHL_DAYS') days " +
                                            "  WHERE r.asset_manager_id = u.escm_logon_id(+) " +
                                            "  AND r.program_code = 'DCC' " +
                                            "  AND r.spo_export_date >= days.ovhl_date  " +
                                            "  ORDER BY full_name asc";
                }

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        CCADManagerGrid.CCADManager row = new CCADManagerGrid.CCADManager();
                        row.asset_manager_id = reader.GetString(reader.GetOrdinal("asset_manager_id"));
                        row.asset_manager_user_name = reader.GetString(reader.GetOrdinal("full_name"));

                        //If id has no matching name value, use id
                        if (String.IsNullOrEmpty(row.asset_manager_user_name))
                        {
                            row.asset_manager_user_name = row.asset_manager_id;
                        }

                        grid.rows.Add(row);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Count number of records found
            grid.totalRows = grid.rows.Count;

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// Gets data for the part grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal PartGrid getCCADPartGrid(PartViewRequest request)
        {
            //Variables
            PartGrid grid = new PartGrid();
            grid.totalPages = 0;
            grid.currentPage = request.page;
            grid.rows = new List<PartGrid.Part>();

            OracleDataReader reader = null;

            int rowStart = 0;
            int rowEnd = 0;

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                //Pull applicable activity statuses and format for Where clause
                string statusClause = "";

                if (request.role == "ccb") // ccb list
                {
                    for (int i = 0; i < CcbActivityStatus.ccb_status_values.Length; i++)
                    {
                        if (i != CcbActivityStatus.ccb_status_values.Length - 1)
                        {
                            statusClause += " am.activity_status = '" + CcbActivityStatus.ccb_status_values[i] + "' OR";
                        }
                        else
                        {
                            statusClause += " am.activity_status = '" + CcbActivityStatus.ccb_status_values[i] + "' ";
                        }
                    }

                    dbCommand.CommandText = "SELECT NVL(count(am.part_no), 0) as totalRows " +
                                       "FROM " +
                                       "(SELECT DISTINCT r.part_no, o.activity_status " +
                                       "FROM escm.escm_new_buy_requirement r, escm.escm_new_buy_order o, " +
                                       "(SELECT sysdate-value_1 as ovhl_date FROM escm.escm_lookup WHERE category = 'NEWBUY_OVHL_DAYS') days " +
                                       "WHERE r.program_code = 'DCC' " +
                                       "AND r.spo_export_date >= days.ovhl_date " +
                                       "AND r.part_no LIKE :PartFilter " +
                                       "AND r.internal_order_no = o.internal_order_no(+)) am " +
                                       "WHERE (" + statusClause + ")";
                    dbCommand.Parameters.Add(":PartFilter", OracleType.VarChar).Value = request.part_no + "%";

                    try
                    {
                        dbConnection.Open();
                        var total = dbCommand.ExecuteScalar();
                        dbConnection.Close();

                        //Convert to int
                        grid.totalRows += Convert.ToInt32(total);
                    }
                    catch
                    {
                        //Ignore Error
                    }
                    finally
                    {
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }

                        //reset parameters
                        dbCommand.Parameters.Clear();
                    }

                    //Calc total pages
                    grid.totalPages = (int)Math.Ceiling((double)grid.totalRows / (double)request.rows);

                    //Handle first page differently
                    if (request.page <= 1)
                    {
                        //pagination start /end
                        rowStart = 1;
                        rowEnd = request.rows;

                        //Check if end has been reached
                        if (rowEnd > grid.totalRows)
                        {
                            rowEnd = grid.totalRows;
                        }
                    }
                    else
                    {
                        //Check if valid page number, otherwise show last page
                        if (request.page > grid.totalPages)
                        {
                            request.page = grid.totalPages;
                        }

                        //Calculate pagination end
                        rowEnd = request.page * request.rows;

                        //Calculate pagination start
                        rowStart = (rowEnd - request.rows) + 1;

                        //Check if max row reached
                        if (rowEnd > grid.totalRows)
                        {
                            rowEnd = grid.totalRows;
                        }
                    }

                    //set query
                    dbCommand.CommandText = "SELECT rn.* FROM ( " +
                        "SELECT am.part_no, rownum as rnum " +
                        "FROM " +
                        "(SELECT DISTINCT r.part_no, o.activity_status " +
                        "FROM escm.escm_new_buy_requirement r, escm.escm_new_buy_order o, " +
                          "(SELECT sysdate-value_1 as ovhl_date FROM escm.escm_lookup WHERE category = 'NEWBUY_OVHL_DAYS') days " +
                        "WHERE r.program_code = 'DCC' " +
                          "AND r.spo_export_date >= days.ovhl_date " +
                          "AND r.part_no LIKE :PartFilter " +
                          "AND r.internal_order_no = o.internal_order_no(+)) am " +
                          "WHERE (" + statusClause +
                        ") ORDER BY am." +
                        (request.index_isValid() ? request.index : "part_no") +
                        " " +
                        (request.order_isValid() ? request.order : "asc") +
                        " ) rn WHERE rn.rnum between " + rowStart.ToString() + " and " + rowEnd.ToString();

                    dbCommand.Parameters.Add(":PartFilter", OracleType.VarChar).Value = request.part_no + "%";
                }
                else
                {
                    string cnt_query = " SELECT  NVL(count(r.part_no), 0) as totalRows " +
                                       " FROM ( SELECT DISTINCT r.part_no " +
                                       "        FROM  escm.escm_new_buy_requirement r, " +
                                       "              (SELECT * FROM  escm.escm_new_buy_order WHERE activity_status != 'Confirmed Order' ) o, " +
                                       "              (SELECT sysdate-value_1 as ovhl_date " +
                                       "               FROM escm.escm_lookup " +
                                       "               WHERE category = 'NEWBUY_OVHL_DAYS') days " +
                                       "         WHERE r.internal_order_no = o.internal_order_no(+) " +
                                       "           AND r.program_code = 'DCC' " +
                                       "           AND r.spo_export_date >= days.ovhl_date) r ";

                    dbCommand.CommandText = cnt_query;

                    try
                    {
                        dbConnection.Open();
                        var total = dbCommand.ExecuteScalar();
                        dbConnection.Close();

                        //Convert to int
                        grid.totalRows += Convert.ToInt32(total);
                    }
                    catch
                    {
                        //Ignore Error
                    }
                    finally
                    {
                        if (dbConnection.State.Equals(ConnectionState.Open))
                        {
                            dbConnection.Close();
                        }

                        //reset parameters
                        dbCommand.Parameters.Clear();
                    }

                    //Calc total pages
                    grid.totalPages = (int)Math.Ceiling((double)grid.totalRows / (double)request.rows);

                    //Handle first page differently
                    if (request.page <= 1)
                    {
                        //pagination start /end
                        rowStart = 1;
                        rowEnd = request.rows;

                        //Check if end has been reached
                        if (rowEnd > grid.totalRows)
                        {
                            rowEnd = grid.totalRows;
                        }
                    }
                    else
                    {
                        //Check if valid page number, otherwise show last page
                        if (request.page > grid.totalPages)
                        {
                            request.page = grid.totalPages;
                        }

                        //Calculate pagination end
                        rowEnd = request.page * request.rows;

                        //Calculate pagination start
                        rowStart = (rowEnd - request.rows) + 1;

                        //Check if max row reached
                        if (rowEnd > grid.totalRows)
                        {
                            rowEnd = grid.totalRows;
                        }
                    }

                    dbCommand.CommandText = "SELECT rn.* FROM ( " +
                                            "SELECT pn.part_no, rownum as rnum FROM ( " +
                                            "SELECT DISTINCT r.part_no " +
                                            "FROM  escm.escm_new_buy_requirement r, " +
                                            "      (SELECT * FROM  escm.escm_new_buy_order WHERE activity_status != 'Confirmed Order' ) o, " +
                                            "      (SELECT sysdate-value_1 as ovhl_date FROM escm.escm_lookup WHERE category = 'NEWBUY_OVHL_DAYS') days " +
                                            "WHERE r.internal_order_no = o.internal_order_no(+) " +
                                            " AND r.program_code = 'DCC' " +
                                            " AND r.spo_export_date >= days.ovhl_date " +
                                            " AND r.part_no LIKE :PartFilter " +
                                            "ORDER BY r." +
                                             (request.index_isValid() ? request.index : "part_no") +
                                             " " +
                                             (request.order_isValid() ? request.order : "asc") +
                                             " ) pn ) rn WHERE rn.rnum between " + rowStart.ToString() + " and " + rowEnd.ToString();

                    dbCommand.Parameters.Add(":PartFilter", OracleType.VarChar).Value = request.part_no + "%";
                }

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        PartGrid.Part row = new PartGrid.Part();
                        row.part_no = reader.GetString(reader.GetOrdinal("part_no"));

                        grid.rows.Add(row);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// Gets a CCCAD order's requirement relationship data
        /// </summary>
        /// <param name="InternalOrderNo">internal order no</param>
        /// <returns>related requirement data</returns>
        private CCADOrderReqData getCCADOrderReqData(int InternalOrderNo)
        {
            CCADOrderReqData result = new CCADOrderReqData();
            OracleDataReader reader = null;

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                try
                {
                    dbCommand.CommandText = "SELECT " +
                    "spo_request_id, " +
                    "program_code, " +
                    "part_no " +
                    "FROM escm.escm_new_buy_requirement " +
                    "WHERE internal_order_no = :OrderNum";
                    dbCommand.Parameters.Add(":OrderNum", OracleType.Int32).Value = InternalOrderNo;

                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        result.spo_request_id = reader.GetInt32(reader.GetOrdinal("spo_request_id"));
                        result.program_code = reader.GetString(reader.GetOrdinal("program_code"));
                        result.part_no = reader.GetString(reader.GetOrdinal("part_no"));
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    //failed
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets data for the CCAD requirement grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal CCADccbGrid getCCADccbGrid(CCADccbViewRequest request)
        {
            //Variables
            CCADccbGrid grid = new CCADccbGrid();
            grid.totalPages = 0;
            grid.currentPage = request.page;
            grid.rows = new List<CCADccbGrid.CCADccb>();
            OracleDataReader reader = null;
            int rowStart = 0;
            int rowEnd = 0;

            //Pull applicable activity statuses and format for Where clause
            string statusClause = "";

            for (int i = 0; i < CcbActivityStatus.ccb_status_values.Length; i++)
            {
                if (i != CcbActivityStatus.ccb_status_values.Length - 1)
                {
                    statusClause += " activity_status = '" + CcbActivityStatus.ccb_status_values[i] + "' OR";
                }
                else
                {
                    statusClause += " activity_status = '" + CcbActivityStatus.ccb_status_values[i] + "' ";
                }
            }

            //Pull record count for query to be used in paging
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                string cnt_query = "SELECT NVL(count(ord.order_no), 0) as totalRows " +
                    "FROM ESCM.escm_new_buy_order ord, " +
                    "ESCM.escm_new_buy_requirement req, " +
                    "(SELECT sysdate-value_1 as ovhl_date FROM escm.escm_lookup WHERE category = 'NEWBUY_OVHL_DAYS') days " +
                    "WHERE ord.internal_order_no = req.internal_order_no " +
                    "AND req.program_code = 'DCC' " +
                    "AND req.spo_export_date >= days.ovhl_date " +
                    "AND (" + statusClause + ")";

                if (!String.IsNullOrEmpty(request.assetManager))
                {
                    cnt_query += " AND req.asset_manager_id = :assetManager";
                    dbCommand.Parameters.Add(":assetManager", OracleType.VarChar).Value = request.assetManager;
                }

                if (!String.IsNullOrEmpty(request.part_no))
                {
                    cnt_query += " AND ord.part_no = :part_no";
                    dbCommand.Parameters.Add(":part_no", OracleType.VarChar).Value = request.part_no;
                }

                if (!String.IsNullOrEmpty(request.activity_status))
                {
                    cnt_query += " AND ord.activity_status = :activity";
                    dbCommand.Parameters.Add(":activity", OracleType.VarChar).Value = request.activity_status;
                }

                dbCommand.CommandText = cnt_query;

                try
                {
                    dbConnection.Open();
                    var total = dbCommand.ExecuteScalar();
                    dbConnection.Close();

                    //Convert to int
                    grid.totalRows += Convert.ToInt32(total);
                }
                catch
                {
                    //Ignore Error
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }

                    //reset parameters
                    dbCommand.Parameters.Clear();
                }
            }

            //Calc total pages
            grid.totalPages = (int)Math.Ceiling((double)grid.totalRows / (double)request.rows);

            //Calculate pagination start
            rowStart = ((request.page - 1) * request.rows);

            //Check if 1 or less
            if (rowStart < 1)
            {
                rowStart = 0;
            }

            //Calculate pagination end
            rowEnd = (rowStart + (request.rows)) > (grid.totalRows - rowStart) ? rowEnd = grid.totalRows : rowStart + (request.rows);

            //Check if less than or equal to 1
            if (rowStart <= 1)
            {
                rowStart = 1;
            }
            else
            {
                rowStart += 1;
            }

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                string query = "SELECT rn.* FROM " +
                    "(SELECT a.*, rownum as rnum " +
                    "FROM " +
                        "(SELECT * " +
                        "FROM " +
                            "( " +
                                "SELECT " +
                                "ORD.ORDER_NO, " +
                                "REQ.PART_NO, " +
                                "NVL2(pb.part_no, 'true', 'false') as pricebreak, " +
                                "req.program_code, " +
                                "NVL(prt.NOMENCLATURE, ' ') as nomenclature, " +
                                "REQ.ITEM_COST AS SPO_COST, " +
                                "NVL(ORD.order_quantity,0) as order_quantity, " +
                                "NVL(req.Item_Cost,0) * NVL(ord.order_Quantity,0) AS EXTENDED_COST, " +
                                "ORD.DUE_DATE, " +
                                "NVL(ORD.COST_CHARGE_NUMBER, ' ') as COST_CHARGE_NUMBER, " +
                                "ORD.Activity_Status, " +
                                "REQ.asset_manager_id " +
                                "FROM " +
                                "ESCM.ESCM_NEW_BUY_REQUIREMENT REQ, " +
                                "ESCM.ESCM_NEW_BUY_ORDER ORD, " +
                                "ESCM.escm_cbom_part prt, " +
                                "(SELECT sysdate-value_1 as ovhl_date FROM escm.escm_lookup WHERE category = 'NEWBUY_OVHL_DAYS') days, " +
                                "(select distinct part_no from escm.escm_lta_pricebreak_mvw) pb " +
                                "WHERE " +
                                "REQ.INTERNAL_ORDER_NO = ORD.INTERNAL_ORDER_NO " +
                                "AND req.part_no = prt.part_no " +
                                "AND req.part_no = pb.part_no(+) " +
                                "AND req.spo_export_date >= days.ovhl_date " +
                            ") ccb " +
                    "WHERE ccb.program_code = 'DCC' " +
                    "AND (" + statusClause + ")";

                if (!String.IsNullOrEmpty(request.assetManager))
                {
                    query += "AND ccb.asset_manager_id = :assetManager ";
                    dbCommand.Parameters.Add(":assetManager", OracleType.VarChar).Value = request.assetManager;
                }

                if (!String.IsNullOrEmpty(request.part_no))
                {
                    query += "AND ccb.part_no = :part_no ";
                    dbCommand.Parameters.Add(":part_no", OracleType.VarChar).Value = request.part_no;
                }

                if (!String.IsNullOrEmpty(request.activity_status))
                {
                    query += "AND ccb.activity_status = :activity ";
                    dbCommand.Parameters.Add(":activity", OracleType.VarChar).Value = request.activity_status;
                }

                query += "ORDER BY " +
                        (request.index_isValid() ? "ccb." + request.index : "ccb.order_no") +
                        " " +
                        (request.order_isValid() ? request.order : "asc") +
                        " ) a) rn WHERE rn.rnum between " + rowStart.ToString() + " and " + rowEnd.ToString();

                dbCommand.CommandText = query;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        DateTime dateTemp;

                        CCADccbGrid.CCADccb gridReq = new CCADccbGrid.CCADccb();
                        gridReq.order_no = reader.GetString(reader.GetOrdinal("order_no"));
                        gridReq.part_no = reader.GetString(reader.GetOrdinal("part_no"));
                        gridReq.activity_status = reader.GetString(reader.GetOrdinal("activity_status"));
                        gridReq.cost_charge_number = reader.GetString(reader.GetOrdinal("cost_charge_number"));
                        gridReq.nomenclature = reader.GetString(reader.GetOrdinal("nomenclature"));
                        gridReq.spo_cost = reader.GetDecimal(reader.GetOrdinal("spo_cost"));
                        gridReq.order_quantity = reader.GetInt32(reader.GetOrdinal("order_quantity"));
                        gridReq.extended_cost = reader.GetDecimal(reader.GetOrdinal("extended_cost"));

                        dateTemp = reader.GetDateTime(reader.GetOrdinal("due_date"));
                        gridReq.due_date = (dateTemp.Month).ToString() + "/" +
                            (dateTemp.Day).ToString() + "/" +
                            (dateTemp.Year).ToString();

                        var hasLTA = reader.GetString(reader.GetOrdinal("pricebreak"));
                        gridReq.pricebreak = Convert.ToBoolean(hasLTA);
                        gridReq.pricePoint = 0;
                        grid.rows.Add(gridReq);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    //Ignore Error???
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Get pricepoint for each LTA part
            foreach (CCADccbGrid.CCADccb ord in grid.rows)
            {
                //Only check parts that have an LTA
                if (ord.pricebreak)
                {
                    try
                    {
                        //Find pricepoint
                        ord.pricePoint = getLtaPricePoint(ord.part_no, ord.order_quantity, ord.order_no.Substring(0, 3));
                    }
                    catch (Exception)
                    {
                        //Ignore Errors and Move on
                    }
                }
            }

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// get list of possible CCB activity status
        /// </summary>
        /// <param name="request">request</param>
        /// <returns></returns>
        internal CCADccbStatusGrid getCCADccbStatusGrid(CCADccbStatusViewRequest request)
        {
            CCADccbStatusGrid grid = new CCADccbStatusGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.rows = new List<CCADccbStatusGrid.CCADccbStatus>();

            //Fill grid with data
            foreach (string value in CcbActivityStatus.ccb_status_values)
            {
                CCADccbStatusGrid.CCADccbStatus status = new CCADccbStatusGrid.CCADccbStatus();
                status.activity_status = value;
                grid.rows.Add(status);
            }

            //Count number of records found
            grid.totalRows = grid.rows.Count;

            return grid;
        }

        /// <summary>
        /// get asset manager remark
        /// </summary>
        /// <param name="request">request</param>
        /// <returns></returns>
        internal CCADAmRemarkGrid getCCADAmRemark(CCADRemarkViewRequest request)
        {
            CCADAmRemarkGrid grid = new CCADAmRemarkGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.totalRows = 1;
            grid.rows = new List<CCADAmRemarkGrid.CCADAmRemark>();

            CCADAmRemarkGrid.CCADAmRemark row = new CCADAmRemarkGrid.CCADAmRemark();
            row.order_no = request.order_no;

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT NVL(asset_manager_remark, ' ') as asset_manager_remark " +
                    "FROM ESCM.escm_new_buy_order " +
                    "WHERE order_no = :OrderNo " +
                        "AND rownum < 2";
                dbCommand.Parameters.Add(":OrderNo", OracleType.VarChar).Value = request.order_no;

                dbConnection.Open(); //Open Connection
                row.asset_manager_remark = (string)dbCommand.ExecuteScalar(); //Execute query
                dbConnection.Close(); //Close Connection
            }

            //If remark null, add 1 space for jqGrid to render correctly
            if (String.IsNullOrEmpty(row.asset_manager_remark))
            {
                row.asset_manager_remark = " ";
            }

            //Add row
            grid.rows.Add(row);

            return grid;
        }

        /// <summary>
        /// Edits CCAD asset manager remark using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg editCCADAmRemark(CCADRemarkEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            bool flagRemarkNull = false;

            //Check for null remark
            if (String.IsNullOrEmpty(request.remark))
            {
                flagRemarkNull = true;
            }

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                if (flagRemarkNull)
                {
                    dbCommand.CommandText = "UPDATE ESCM.escm_new_buy_order " +
                    "SET asset_manager_remark = null, " +
                    "asset_manager_remark_date = :RemarkDate " +
                    "WHERE order_no = :OrderNum";
                }
                else
                {
                    dbCommand.CommandText = "UPDATE ESCM.escm_new_buy_order " +
                        "SET asset_manager_remark = :Remark, " +
                        "asset_manager_remark_date = :RemarkDate " +
                        "WHERE order_no = :OrderNum";
                }

                //Add remark param if not null
                if (!flagRemarkNull)
                {
                    dbCommand.Parameters.Add(":Remark", OracleType.VarChar).Value = request.remark;
                }
                dbCommand.Parameters.Add(":RemarkDate", OracleType.DateTime).Value = DateTime.Today;
                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = request.order_no;

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to save Asset Manager remarks.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Edits CCAD ccb remark using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns>true for success</returns>
        internal ResponseMsg editCCADccbRemark(CCADRemarkEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();
            bool flagRemarkNull = false;

            //Check for null remark
            if (String.IsNullOrEmpty(request.remark))
            {
                flagRemarkNull = true;
            }

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                if (flagRemarkNull)
                {
                    dbCommand.CommandText = "UPDATE ESCM.escm_new_buy_order " +
                    "SET review_board_remark = null, " +
                    "review_board_remark_date = :RemarkDate " +
                    "WHERE order_no = :OrderNum";
                }
                else
                {
                    dbCommand.CommandText = "UPDATE ESCM.escm_new_buy_order " +
                        "SET review_board_remark = :Remark, " +
                        "review_board_remark_date = :RemarkDate " +
                        "WHERE order_no = :OrderNum";
                }

                //Add remark param if not null
                if (!flagRemarkNull)
                {
                    dbCommand.Parameters.Add(":Remark", OracleType.VarChar).Value = request.remark;
                }
                dbCommand.Parameters.Add(":RemarkDate", OracleType.DateTime).Value = DateTime.Today;
                dbCommand.Parameters.Add(":OrderNum", OracleType.VarChar).Value = request.order_no;

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to save remark.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// get ccb remark
        /// </summary>
        /// <param name="request">request</param>
        /// <returns></returns>
        internal CCADCcbRemarkGrid getCCADCcbRemark(CCADRemarkViewRequest request)
        {
            CCADCcbRemarkGrid grid = new CCADCcbRemarkGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.totalRows = 1;
            grid.rows = new List<CCADCcbRemarkGrid.CCADCcbRemark>();

            CCADCcbRemarkGrid.CCADCcbRemark row = new CCADCcbRemarkGrid.CCADCcbRemark();
            row.order_no = request.order_no;

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT NVL(review_board_remark, ' ') as review_board_remark " +
                    "FROM ESCM.escm_new_buy_order " +
                    "WHERE order_no = :OrderNo " +
                        "AND rownum < 2";
                dbCommand.Parameters.Add(":OrderNo", OracleType.VarChar).Value = request.order_no;

                dbConnection.Open(); //Open Connection
                row.review_board_remark = (string)dbCommand.ExecuteScalar(); //Execute query
                dbConnection.Close(); //Close Connection
            }

            //If remark null, add 1 space for jqGrid to render correctly
            if (String.IsNullOrEmpty(row.review_board_remark))
            {
                row.review_board_remark = " ";
            }

            //Add row
            grid.rows.Add(row);

            return grid;
        }

        /// <summary>
        /// get spo remark
        /// </summary>
        /// <param name="request">request</param>
        /// <returns>last three remarks from spo</returns>
        internal CCADSpoRemarkGrid getOvhlSpoRemark(CCADRemarkViewRequest request)
        {
            CCADSpoRemarkGrid grid = new CCADSpoRemarkGrid();
            grid.totalPages = 1;
            grid.currentPage = 1;
            grid.totalRows = 1;
            grid.rows = new List<CCADSpoRemarkGrid.CCADSpoRemark>();
            CCADSpoRemarkGrid.CCADSpoRemark row = new CCADSpoRemarkGrid.CCADSpoRemark();
            row.order_no = request.order_no;

            int internal_order_num = 0;
            OracleDataReader reader = null;

            if (row.order_no.Length >= 3)
            {
                if (Regex.IsMatch(row.order_no.Substring(0, 3), "[0-9]"))
                {
                    internal_order_num = Convert.ToInt32(row.order_no);
                }
                else
                {
                    //Search database, find internal order number
                    using (OracleCommand dbCommand = dbConnection.CreateCommand())
                    {
                        try
                        {
                            dbCommand.CommandText = "SELECT NVL(internal_order_no, 0) as internal_order_no " +
                            "FROM ESCM.escm_new_buy_order " +
                            "WHERE order_no = :OrderNo " +
                                "AND rownum < 2";
                            dbCommand.Parameters.Add(":OrderNo", OracleType.VarChar).Value = request.order_no;

                            dbConnection.Open();
                            reader = dbCommand.ExecuteReader();
                            while (reader.Read() == true)
                            {
                                internal_order_num = reader.GetInt32(reader.GetOrdinal("internal_order_no"));
                            }
                            reader.Close();
                            dbConnection.Close();
                        }
                        catch (OracleException)
                        {
                            //failed
                            throw;
                        }
                        finally
                        {
                            if (reader != null)
                            {
                                reader.Close();
                            }
                            if (dbConnection.State.Equals(ConnectionState.Open))
                            {
                                dbConnection.Close();
                            }
                        }
                    }
                }
            }

            //If order null or 0, no need to lookup comments
            if (internal_order_num > 0)
            {
                //Get requirement relationship keys
                CCADOrderReqData reqData = getCCADOrderReqData(internal_order_num);

                //If null, no need to lookup comments
                if (!String.IsNullOrEmpty(reqData.part_no))
                {
                    //Get spo remarks from package
                    row.spo_remark = getOvhlPartComment(reqData.part_no);
                }
            }

            //If remark null, add 1 space for jqGrid to render correctly
            if (String.IsNullOrEmpty(row.spo_remark))
            {
                row.spo_remark = " ";
            }

            //Add row
            grid.rows.Add(row);

            return grid;
        }

        #endregion CCAD

        #region Segcode
        /// <summary>
        /// Gets data for the segcode grid using the supplied request
        /// </summary>
        /// <param name="request">Grid request object</param>
        /// <returns>A JSON serializable data grid</returns>
        internal SegcodeGrid getSegcodeGrid(SegcodeViewRequest request)
        {
            //Variables
            SegcodeGrid grid = new SegcodeGrid();
            grid.totalPages = 0;
            grid.currentPage = request.page;
            grid.totalRows = 0;
            grid.rows = new List<SegcodeGrid.Segcode>();
            OracleDataReader reader = null;
            int rowStart = 0;
            int rowEnd = 0;

            //Search database, fill grid
            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT NVL(count(seg_code), 0) as totalRows " +
                    "FROM escm.escm_seg_code " +
                    "WHERE " + request.filterField + " LIKE :Filter";
                dbCommand.Parameters.Add(":Filter", OracleType.VarChar).Value = request.filterValue + "%";

                try
                {
                    dbConnection.Open();
                    var total = dbCommand.ExecuteScalar();
                    dbConnection.Close();

                    //Convert to int
                    grid.totalRows += Convert.ToInt32(total);
                }
                catch
                {
                    //Ignore Error
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }

                    //reset parameters
                    dbCommand.Parameters.Clear();
                }

                //Calc total pages
                grid.totalPages = (int)Math.Ceiling((double)grid.totalRows / (double)request.rows);

                //Calculate pagination start
                rowStart = ((request.page - 1) * request.rows);

                //Check if 1 or less
                if (rowStart < 1)
                {
                    rowStart = 0;
                }

                //Calculate pagination end
                rowEnd = (rowStart + (request.rows)) > (grid.totalRows - rowStart) ? rowEnd = grid.totalRows : rowStart + (request.rows);

                //Check if less than or equal to 1
                if (rowStart <= 1)
                {
                    rowStart = 1;
                }
                else
                {
                    rowStart += 1;
                }

                //get segcode data
                dbCommand.CommandText = "SELECT rn.* FROM (SELECT a.*, rownum as rnum " +
                    "FROM " +
                    "(SELECT s.seg_code, " +
                      "s.program_code, " +
                      "s.last_update_date, " +
                      "s.last_update_user, " +
                      "NVL(s.buy_method, ' ') as buy_method, " +
                      "NVL(s.site_location, ' ') as site_location, " +
                      "s.include_in_tav_reporting, " +
                      "s.include_in_spo, " +
                      "s.include_in_bolt " +
                    "FROM escm.escm_seg_code s " +
                    "WHERE s." + request.filterField + " LIKE :Filter " +
                     "ORDER BY " +
                        (request.index_isValid() ? "s." + request.index : "s.last_update_date") +
                        " " +
                        (request.order_isValid() ? request.order : "desc") +
                        " ) a) rn WHERE rn.rnum between " + rowStart.ToString() + " and " + rowEnd.ToString();
                dbCommand.Parameters.Add(":Filter", OracleType.VarChar).Value = request.filterValue + "%";

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        DateTime dateTemp;

                        SegcodeGrid.Segcode row = new SegcodeGrid.Segcode();
                        row.seg_code = reader.GetString(reader.GetOrdinal("seg_code"));
                        row.program_code = reader.GetString(reader.GetOrdinal("program_code"));

                        dateTemp = reader.GetDateTime(reader.GetOrdinal("last_update_date"));
                        row.last_update_date = (dateTemp.Month).ToString() + "/" +
                            (dateTemp.Day).ToString() + "/" +
                            (dateTemp.Year).ToString();

                        row.last_update_user = reader.GetString(reader.GetOrdinal("last_update_user"));
                        row.buy_method = reader.GetString(reader.GetOrdinal("buy_method"));
                        row.site_location = reader.GetString(reader.GetOrdinal("site_location"));
                        row.include_in_tav_reporting = reader.GetString(reader.GetOrdinal("include_in_tav_reporting"));
                        row.include_in_spo = reader.GetString(reader.GetOrdinal("include_in_spo"));
                        row.include_in_bolt = reader.GetString(reader.GetOrdinal("include_in_bolt"));

                        grid.rows.Add(row);
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            //Return grid with results
            return grid;
        }

        /// <summary>
        /// Edits a segcode using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns></returns>
        internal ResponseMsg editSegcode(SegcodeEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "UPDATE escm.escm_seg_code " +
                    "SET seg_code = :Segcode, " +
                    "program_code = :Program, " +
                    "last_update_date = :UpdateDate, " +
                    "last_update_user = :UpdateUser, " +
                    "buy_method = :BuyMethod, " +
                    "site_location = :Site, " +
                    "include_in_tav_reporting = :TAV, " +
                    "include_in_spo = :SPO, " +
                    "include_in_bolt = :BOLT " +
                    "WHERE seg_code = :SegcodeID";
                dbCommand.Parameters.Add(":Segcode", OracleType.VarChar).Value = request.seg_code;
                dbCommand.Parameters.Add(":Program", OracleType.VarChar).Value = request.program_code;
                dbCommand.Parameters.Add(":UpdateDate", OracleType.DateTime).Value = DateTime.Today;
                dbCommand.Parameters.Add(":UpdateUser", OracleType.VarChar).Value = Helper.getSessionConnectionInfoById(1).ToUpper();
                dbCommand.Parameters.Add(":BuyMethod", OracleType.VarChar).Value = request.buy_method;
                dbCommand.Parameters.Add(":Site", OracleType.VarChar).Value = request.site_location;
                dbCommand.Parameters.Add(":TAV", OracleType.VarChar).Value = request.include_in_tav_reporting;
                dbCommand.Parameters.Add(":SPO", OracleType.VarChar).Value = request.include_in_spo;
                dbCommand.Parameters.Add(":BOLT", OracleType.VarChar).Value = request.include_in_bolt;
                dbCommand.Parameters.Add(":SegcodeID", OracleType.VarChar).Value = request.id;

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to edit segcode.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Adds a new segcode using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns></returns>
        internal ResponseMsg newSegcode(SegcodeEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "INSERT INTO escm.escm_seg_code " +
                    "(seg_code, " +
                    "program_code, " +
                    "last_update_date, " +
                    "last_update_user, " +
                    "buy_method, " +
                    "site_location, " +
                    "include_in_tav_reporting, " +
                    "include_in_spo, " +
                    "include_in_bolt) " +
                    "VALUES (" +
                        ":Segcode, " +
                        ":Program, " +
                        ":UpdateDate, " +
                        ":UpdateUser, " +
                        ":BuyMethod, " +
                        ":Site, " +
                        ":TAV, " +
                        ":SPO, " +
                        ":BOLT)";
                dbCommand.Parameters.Add(":Segcode", OracleType.VarChar).Value = request.seg_code;
                dbCommand.Parameters.Add(":Program", OracleType.VarChar).Value = request.program_code;
                dbCommand.Parameters.Add(":UpdateDate", OracleType.DateTime).Value = DateTime.Today;
                dbCommand.Parameters.Add(":UpdateUser", OracleType.VarChar).Value = Helper.getSessionConnectionInfoById(1).ToUpper();
                dbCommand.Parameters.Add(":BuyMethod", OracleType.VarChar).Value = request.buy_method;
                dbCommand.Parameters.Add(":Site", OracleType.VarChar).Value = request.site_location;
                dbCommand.Parameters.Add(":TAV", OracleType.VarChar).Value = request.include_in_tav_reporting;
                dbCommand.Parameters.Add(":SPO", OracleType.VarChar).Value = request.include_in_spo;
                dbCommand.Parameters.Add(":BOLT", OracleType.VarChar).Value = request.include_in_bolt;

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to add segcode.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Deletes a segcode using the supplied request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns></returns>
        internal ResponseMsg deleteSegcode(SegcodeEditRequest request)
        {
            //Variables
            ResponseMsg result = new ResponseMsg();

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "DELETE FROM escm.escm_seg_code WHERE seg_code = :Segcode";
                dbCommand.Parameters.Add(":Segcode", OracleType.VarChar).Value = request.id;

                try
                {
                    dbConnection.Open();
                    dbCommand.ExecuteNonQuery();
                    dbConnection.Close();
                }
                catch (OracleException oe)
                {
                    result.addError("Unable to delete segcode.");
                    result.addError(oe.ToString());
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets top 6 unique autocomplete by request
        /// </summary>
        /// <param name="request">search values</param>
        /// <returns>list of suggestions</returns>
        internal SearchList getSegAutocomplete(SegcodeSearchListRequest request)
        {
            //Variables
            SearchList result = new SearchList();
            OracleDataReader reader = null;
            string SegFilter = request.filterValue + "%";
            string SegField = (request.filterField_isValid() ? request.filterField : "seg_code");

            using (OracleCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = "SELECT seg." + SegField + " " +
                    "FROM " +
                    "( " +
                      "SELECT DISTINCT sc." + SegField + " " +
                      "FROM escm.escm_seg_code sc " +
                      "WHERE UPPER(sc." + SegField + ") LIKE UPPER(:SegFilter) " +
                      "ORDER BY sc." + SegField + " " +
                    ") seg " +
                    "WHERE ROWNUM < 7";
                dbCommand.Parameters.Add(":SegFilter", OracleType.VarChar).Value = SegFilter;

                try
                {
                    dbConnection.Open();
                    reader = dbCommand.ExecuteReader();
                    while (reader.Read() == true)
                    {
                        result.listValues.Add(reader.GetString(reader.GetOrdinal(SegField)));
                    }
                    reader.Close();
                    dbConnection.Close();
                }
                catch (OracleException)
                {
                    throw;
                }
                finally
                {
                    if (dbConnection.State.Equals(ConnectionState.Open))
                    {
                        dbConnection.Close();
                    }
                }
            }

            return result;
        }

        #endregion Segcode

    }

}