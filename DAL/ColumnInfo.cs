using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEN_CODE_BASE.DAL
{
    public class ColumnInfo
    {
        public string ColumnName { get; set; }
        public string MaxLength { get; set; }
        public string DataType { get; set; }
        public bool isNull { get; set; }
        public bool Filter { get; set; }
        public bool Active { get; set; }
        public bool Key { get; set; }
        public bool IsIdentity { get; set; }
    }
}
