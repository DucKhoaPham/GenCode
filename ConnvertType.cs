using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEN_CODE_BASE
{
    public class ConnvertType
    {
        /// <summary>
        /// CONNVET TYPE SQL TO C#
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ChangeDataType(string s)
        {
            if (s == "char" || s == "nchar" || s == "varchar" || s == "nvarchar" || s == "text" || s == "ntext")
                return "string";
            if (s == "int" || s == "smallint" )
                return "int";
            if (s == "tinyint")
                return "byte";
            if (s == "bigint")
                return "long";
            if (s == "float" || s == "real")
                return "double";
            if (s == "decimal" || s == "money" || s == "numeric")
                return "decimal";
            if (s == "image" || s == "varbinary")
                return "byte[]";
            if (s == "bit")
                return "bool";
            if (s == "date" || s == "datetime" || s == "smalldatetime")
                return "DateTime";
            return "";
        }
        /// <summary>
        /// CONVERT TO DataType
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RepairDataType(string s)
        {
            if (s == "nvarchar") return "NVarChar";
            if (s == "int") return "Int";
            if (s == "bigint") return "BigInt";
            if (s == "char") return "Char";
            if (s == "nchar") return "NChar";
            if (s == "varchar") return "VarChar";
            if (s == "tinyint") return "TinyInt";
            if (s == "float") return "Float";
            if (s == "ntext") return "NText";
            if (s == "text") return "Text";
            if (s == "image") return "Image";
            if (s == "date") return "Date";
            if (s == "datetime") return "DateTime";
            if (s == "decimal") return "Decimal";
            if (s == "money") return "Money";
            if (s == "smallint") return "SmallInt";
            if (s == "real") return "Real";
            if (s == "bit") return "Bit";
            if (s == "numeric") return "Decimal";
            if (s == "varbinary") return "VarBinary";
            if (s == "smalldatetime") return "SmallDateTime";
            return "";
        }
    }
}
