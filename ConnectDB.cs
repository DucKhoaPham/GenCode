using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEN_CODE_BASE
{
    public class ConnectDB
    {
        /// <summary>
        /// Get Connection to file Config
        /// </summary>
        /// <returns></returns>
        public static string GetStringConnect()
        {
            string _stringconnect = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            _stringconnect = _stringconnect.Replace("@UserName", "");
            _stringconnect = _stringconnect.Replace("@Password", "");
            return _stringconnect;
        }
        /// <summary>
        /// Mở kế nối
        /// </summary>
        /// <returns></returns>
        public static SqlConnection OpenConnection(SqlConnection conn)
        {
            try
            {
                if (conn.State == ConnectionState.Closed || conn.State == ConnectionState.Broken)
                {
                    conn.Open();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return conn;
        }
        /// <summary>
        /// Đóng kế nối 
        /// </summary>
        /// <returns></returns>
        public static SqlConnection CloseConnection(SqlConnection conn)
        {
            try
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return conn;
        }
        public static SqlConnection GetConnection()
        {
            try
            {
                SqlConnection _con = new SqlConnection(GetStringConnect());
                _con = OpenConnection(_con);
                return _con;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }
    }
}
