using ConnectDBLib;
using GEN_CODE_BASE;
using GEN_CODE_BASE.DAL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GEN_CODE_BASE
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }
        private void FrmMain_Load(object sender, EventArgs e)
        {
            txtPath.Text = GEN_CODE_BASE.Properties.Settings.Default.PathSave;
            dgvListColumn.AutoGenerateColumns = false;
            dgvListTable.AutoGenerateColumns = false;
            using (SqlConnection _conn = ConnectDB.GetConnection())
            {
                LoadListTable(_conn);
            }
        }
        BindingList<TableInfo> _ListTableInfo = new BindingList<TableInfo>();
        BindingList<ColumnInfo> _ListColumnInfo = new BindingList<ColumnInfo>();
        /// <summary>
        /// Load danh sách các bảng
        /// </summary>
        /// <param name="_conn"></param>
        private void LoadListTable(SqlConnection _conn)
        {
            string _sql = "SELECT cast(0 as bit) Checkbox, [TABLE_NAME] FROM INFORMATION_SCHEMA.TABLES ORDER BY TABLE_NAME";
            DataTable _listtable = DataProvider.ExcuteDataSet(_conn, _sql).Tables[0];

            foreach (DataRow item in _listtable.Rows)
            {
                TableInfo _tableinfo = new TableInfo();
                _tableinfo.TableName = item[1].ToString();
                _tableinfo.Checkbox = (bool)item[0];
                _ListTableInfo.Add(_tableinfo);
            }
            dgvListTable.DataSource = _ListTableInfo;
        }
        /// <summary>
        /// Load danh sách các trường của bảng
        /// </summary>
        /// <param name="_conn"></param>
        /// <param name="_table"></param>
        private void LoadListColumnTable(SqlConnection _conn, string _table)
        {
            _ListColumnInfo.Clear();
            string _sql = @"SELECT[Column_NAME],[Data_Type],[CHARACTER_MAXIMUM_LENGTH]
            FROM INFORMATION_SCHEMA.Columns WHERE Table_Name = '" + _table + "'";
            DataTable _listcolumn = DataProvider.ExcuteDataSet(_conn, _sql).Tables[0];
           var schema = _conn.GetSchema("Columns");
            foreach (DataRow item in _listcolumn.Rows)
            {
                ColumnInfo _ColumnInfo = new ColumnInfo();
                _ColumnInfo.ColumnName = item[0].ToString();
                _ColumnInfo.DataType = item[1].ToString();
                var columnInfo = schema.Select("Table_Name='" + _table + "' and Column_Name ='" + item[0].ToString() + "'").ToList();
                if (columnInfo[0].ItemArray[6].ToString() == "NO")
                    _ColumnInfo.isNull = false;
                else
                    _ColumnInfo.isNull = true;
                _ColumnInfo.MaxLength = item[2].ToString();
                string sql = @"Select [Column_NAME]
                                   FROM INFORMATION_SCHEMA.Columns
                                   WHERE Table_Name='" + _table + "' AND COLUMNPROPERTY(OBJECT_ID('" + _table + "'),'" +
                                   _ColumnInfo.ColumnName + "','IsIdentity')=1";
                _ColumnInfo.IsIdentity = DataProvider.ExecuteScalar(_conn, sql) != null;
                _ListColumnInfo.Add(_ColumnInfo);
            }
            dgvListColumn.DataSource = _ListColumnInfo;
        }

        private void dgvListTable_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > -1)
            {
                using (SqlConnection _conn = ConnectDB.GetConnection())
                {
                    LoadListColumnTable(_conn, (dgvListTable.Rows[e.RowIndex].DataBoundItem as TableInfo).TableName);
                }
            }
        }

        private void chkAllColumn_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < dgvListColumn.Rows.Count; i++)
            {
                dgvListColumn.Rows[i].Cells[0].Value = checkBox1.Checked;
            }
        }
        private void btnGetPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fl = new FolderBrowserDialog();
            fl.SelectedPath = @"D:\";
            fl.ShowNewFolderButton = true;
            if (fl.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = fl.SelectedPath;
            }
            GEN_CODE_BASE.Properties.Settings.Default.PathSave = txtPath.Text;
            GEN_CODE_BASE.Properties.Settings.Default.Save();

        }
        /// <summary>
        /// Tao DTO
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGenDTO_Click(object sender, EventArgs e)
        {
            if (txtPath.Text.Trim() == "")
            {
                MessageBox.Show("Cần nhập vị trí lưu!");
                txtPath.Focus();
                return;
            }
            if (!Directory.Exists(txtPath.Text + "\\Domain"))
            {
                Directory.CreateDirectory(txtPath.Text + "\\Domain");
            }
            for (int i = 0; i < dgvListTable.Rows.Count; i++)
            {
                if ((bool)dgvListTable.Rows[i].Cells[0].Value)
                {
                    TableInfo _tableinfo = dgvListTable.Rows[i].DataBoundItem as TableInfo;
                    string _tablename = _tableinfo.TableName.Trim();
                    using (SqlConnection _conn = ConnectDB.GetConnection())
                    {
                        LoadListColumnTable(_conn, _tablename);
                    }
                    string _classname = _tableinfo.TableName.Trim();
                    var sb = new StringBuilder();
                    sb.AppendLine("using System;\n\nnamespace vLib.Core.Domain");
                    sb.AppendLine("{");
                    sb.AppendLine("\tpublic partial class " + _classname);
                    sb.AppendLine("\t{");
                    foreach (ColumnInfo item in _ListColumnInfo)
                    {
                        if (checkBox1.Checked)
                        {
                            string _dataType = ConnvertType.ChangeDataType(item.DataType);
                            bool allowNull = ((item.isNull && (ConnvertType.ChangeDataType(item.DataType) == "DateTime"
                            || ConnvertType.ChangeDataType(item.DataType) == "int"
                            || ConnvertType.ChangeDataType(item.DataType) == "bool"
                            || ConnvertType.ChangeDataType(item.DataType) == "decimal"
                            || ConnvertType.ChangeDataType(item.DataType) == "double"
                            || ConnvertType.ChangeDataType(item.DataType) == "float"
                            || ConnvertType.ChangeDataType(item.DataType) == "long"))) ? true : false;
                            if (allowNull)
                            {
                                if (ConnvertType.ChangeDataType(item.DataType) == "DateTime")
                                {
                                    _dataType = "Nullable<System.DateTime>";
                                }
                                else
                                {
                                    _dataType = "Nullable<" + ConnvertType.ChangeDataType(item.DataType) + ">";

                                }
                            }
                            sb.AppendLine("\t\tpublic " + _dataType + " " + item.ColumnName + " { get; set; }");
                        }
                        else
                        {
                            if (item.Active)
                            {
                                string _dataType = ConnvertType.ChangeDataType(item.DataType);                         
                                bool allowNull = ((item.isNull && (ConnvertType.ChangeDataType(item.DataType) == "DateTime"
                                || ConnvertType.ChangeDataType(item.DataType) == "int"
                                || ConnvertType.ChangeDataType(item.DataType) == "bool"
                                || ConnvertType.ChangeDataType(item.DataType) == "decimal"
                                || ConnvertType.ChangeDataType(item.DataType) == "double"
                                || ConnvertType.ChangeDataType(item.DataType) == "float"
                                || ConnvertType.ChangeDataType(item.DataType) == "long"))) ? true : false;
                                if (allowNull)
                                {
                                    if (ConnvertType.ChangeDataType(item.DataType) == "DateTime")
                                    {
                                        _dataType = "Nullable<System.DateTime>";
                                    }
                                    else
                                    {
                                        _dataType = "Nullable<" + ConnvertType.ChangeDataType(item.DataType) + ">";

                                    }
                                }
                                sb.AppendLine("\t\tpublic " + _dataType + " " + item.ColumnName + " {get;set;}");
                            }
                        }
                    }
                    //sb.AppendLine("\t\tpublic void CopyValue(" + _classname + " info)");
                    //sb.AppendLine("\t\t{");
                    //foreach (ColumnInfo item in _ListColumnInfo)
                    //{
                    //    if (item.Active)
                    //    {
                    //        sb.AppendLine("\t\t\tthis." + item.ColumnName + " = info." + item.ColumnName + ";");
                    //    }
                    //}
                    //sb.AppendLine("\t\t}");

                    //sb.AppendLine("\t\tpublic bool CheckChangeValue(" + _classname + " info)");
                    //sb.AppendLine("\t\t{");
                    //foreach (ColumnInfo item in _ListColumnInfo)
                    //{
                    //    if (item.Active)
                    //    {
                    //        if(ConnvertType.ChangeDataType(item.DataType)=="string")
                    //        {
                    //            sb.AppendLine("\t\t\tif ((string.IsNullOrEmpty(this." + item.ColumnName + ")?\"\":this."+ item.ColumnName + ") != (string.IsNullOrEmpty(info." + item.ColumnName + ")?\"\":info." + item.ColumnName+ ")) return true;");
                    //        }
                    //        else
                    //        {
                    //            sb.AppendLine("\t\t\tif (this." + item.ColumnName + " != info." + item.ColumnName+") return true;");
                    //        }

                    //    }
                    //}
                    //sb.AppendLine("\t\t\treturn false;");
                    //sb.AppendLine("\t\t}");


                    sb.AppendLine("\t}");
                    sb.AppendLine("}");
                    File.WriteAllText(txtPath.Text + "\\Domain\\" + _tableinfo.TableName.Trim() + ".cs", sb.ToString());
                }
            }         
            MessageBox.Show("OK !");
        }

        private void btnGenBLL_Click(object sender, EventArgs e)
        {
            TableInfo _tableinfo = dgvListTable.CurrentRow.DataBoundItem as TableInfo;
            string _tablename = _tableinfo.TableName.Trim();
            string _objectinfoname = _tableinfo.TableName.Trim() + "Info";
            string _objectfiltername = _tableinfo.TableName.Trim() + "Filter";
            var sb = new StringBuilder();
            sb.AppendLine("using System.Data.SqlClient;");
            sb.AppendLine("using DMS.ConnectDBLib;");
            sb.AppendLine("using DMS.ObjectCommon;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("namespace DMS.BusinessService");
            sb.AppendLine("{");
            sb.AppendLine("\tpublic partial  class " + _tablename + "Service : BaseService<" + _tablename + "Service>");
            sb.AppendLine("\t{");
            //--------------------------------------------------------------------------------------------------------------
            #region Class Filter
            sb.AppendLine("\t\tpublic class " + _objectfiltername);
            sb.AppendLine("\t\t{");
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Filter)
                {
                    string _typedata = ConnvertType.ChangeDataType(item.DataType);
                    sb.AppendLine("\t\t\tpublic " + _typedata +(_typedata=="string"?"":"?") + " " + item.ColumnName + " {get;set;}");
                }
            }
            sb.AppendLine("\t\t}");
            #endregion End Class Filter
            //--------------------------------------------------------------------------------------------------------------
            #region Insert
            sb.AppendLine("\t\tpublic bool Insert(SqlConnection lisSqlConnection, " + _objectinfoname + " _infoinsert)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tstring _sql=@\"INSERT INTO " + _tableinfo.TableName + "(");
            string _listcolum1 = "";
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    if (!item.IsIdentity)
                    {
                        _listcolum1 += "\t\t\t\t[" + item.ColumnName + "],\n";
                    }
                }
            }
            _listcolum1 = _listcolum1.Substring(0, _listcolum1.Length - 2);
            string _listcolum2 = "";
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    if (!item.IsIdentity)
                    {
                        _listcolum2 += "\t\t\t\t@" + item.ColumnName + ",\n";
                    }
                }
            }
            _listcolum2 = _listcolum2.Substring(0, _listcolum2.Length - 2);
            sb.AppendLine(_listcolum1 + ") VALUES (\n" + _listcolum2 + ")\";");
            sb.AppendLine("\t\t\tusing (var commmand = new SqlCommand(_sql, lisSqlConnection))");
            sb.AppendLine("\t\t\t{");

            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    if (!item.IsIdentity)
                    {
                        sb.AppendLine("\t\t\t\tAddSqlParameter(commmand, \"@" + item.ColumnName + "\", _infoinsert." + item.ColumnName + ", System.Data.SqlDbType." + ConnvertType.RepairDataType(item.DataType) + ");");
                    }
                }
            }
            sb.AppendLine("\t\t\t\tWriteLogExecutingCommand(commmand);");
            sb.AppendLine("\t\t\t\tif (commmand.ExecuteNonQuery() > 0)");
            sb.AppendLine("\t\t\t\t{");
            sb.AppendLine("\t\t\t\t\treturn true;");
            sb.AppendLine("\t\t\t\t}");
            sb.AppendLine("\t\t\t\telse");
            sb.AppendLine("\t\t\t\t{");
            sb.AppendLine("\t\t\t\t\treturn false;");
            sb.AppendLine("\t\t\t\t}");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t}");
            #endregion End Insert
            //--------------------------------------------------------------------------------------------------------------
            #region Update
            sb.AppendLine("\t\tpublic bool Update(SqlConnection lisSqlConnection, " + _objectinfoname + " _infoupdate)");
            sb.AppendLine("\t\t{");
            string _listcolum3 = "";
            string _listwhere = "";
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    if (!item.IsIdentity && !item.Key)
                    {
                        _listcolum3 += "\t\t\t\t[" + item.ColumnName + "]=@" + item.ColumnName + ",\n";
                    }
                    if (item.Key)
                    {
                        _listwhere += "\t\t\t\t[" + item.ColumnName + "]=@" + item.ColumnName + " AND \n";
                    }
                }
            }
            _listcolum3 = _listcolum3.Substring(0, _listcolum3.Length - 2);
            _listwhere = _listwhere.Substring(0, _listwhere.Length - 6);
            sb.AppendLine("\t\t\tstring _sql=@\"UPDATE " + _tableinfo.TableName + " SET ");
            sb.AppendLine(_listcolum3 + " WHERE \n" + _listwhere + "\";");
            sb.AppendLine("\t\t\tusing (var commmand = new SqlCommand(_sql, lisSqlConnection))");
            sb.AppendLine("\t\t\t{");

            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    if (!item.IsIdentity)
                    {
                        sb.AppendLine("\t\t\t\tAddSqlParameter(commmand, \"@" + item.ColumnName + "\", _infoupdate." + item.ColumnName + ", System.Data.SqlDbType." + ConnvertType.RepairDataType(item.DataType) + ");");
                    }
                }
            }
            sb.AppendLine("\t\t\t\tWriteLogExecutingCommand(commmand);");
            sb.AppendLine("\t\t\t\tif (commmand.ExecuteNonQuery() > 0)");
            sb.AppendLine("\t\t\t\t{");
            sb.AppendLine("\t\t\t\t\treturn true;");
            sb.AppendLine("\t\t\t\t}");
            sb.AppendLine("\t\t\t\telse");
            sb.AppendLine("\t\t\t\t{");
            sb.AppendLine("\t\t\t\t\treturn false;");
            sb.AppendLine("\t\t\t\t}");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t}");
            #endregion End Update
            //--------------------------------------------------------------------------------------------------------------
            #region Delete
            sb.AppendLine("\t\tpublic bool Delete(SqlConnection lisSqlConnection, " + _objectinfoname + " _infodelete)");
            sb.AppendLine("\t\t{");
            string _listwheredel = "";
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    if (item.Key)
                    {
                        _listwheredel += "\t\t\t\t[" + item.ColumnName + "]=@" + item.ColumnName + ",\n";
                    }
                }
            }
            _listwheredel = _listwheredel.Substring(0, _listwheredel.Length - 2);
            sb.AppendLine("\t\t\tstring _sql=@\"DELETE " + _tableinfo.TableName + " WHERE ");
            sb.AppendLine(_listwhere + "\";");
            sb.AppendLine("\t\t\tusing (var commmand = new SqlCommand(_sql, lisSqlConnection))");
            sb.AppendLine("\t\t\t{");

            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    if (item.Key)
                    {
                        sb.AppendLine("\t\t\t\tAddSqlParameter(commmand, \"@" + item.ColumnName + "\", _infodelete." + item.ColumnName + ", System.Data.SqlDbType." + ConnvertType.RepairDataType(item.DataType) + ");");
                    }
                }
            }
            sb.AppendLine("\t\t\t\tWriteLogExecutingCommand(commmand);");
            sb.AppendLine("\t\t\t\tif (commmand.ExecuteNonQuery() > 0)");
            sb.AppendLine("\t\t\t\t{");
            sb.AppendLine("\t\t\t\t\treturn true;");
            sb.AppendLine("\t\t\t\t}");
            sb.AppendLine("\t\t\t\telse");
            sb.AppendLine("\t\t\t\t{");
            sb.AppendLine("\t\t\t\t\treturn false;");
            sb.AppendLine("\t\t\t\t}");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t}");
            #endregion End Update
            //--------------------------------------------------------------------------------------------------------------
            #region Select Data
            sb.AppendLine("\t\tpublic List<" + _objectinfoname + "> GetDataList(SqlConnection lisSqlConnection, " + _objectfiltername + " _infofilter)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tvar result = new List<"+_objectinfoname+">();");
            
            string _listcolumnselect = "";
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    _listcolumnselect += "\t\t\t\t[" + item.ColumnName + "],\n";
                }
            }
            _listcolumnselect = _listcolumnselect.Substring(0, _listcolumnselect.Length - 2);
            sb.AppendLine("\t\t\tstring _sql=@\"SELECT \n" + _listcolumnselect + "\n\t\t\t\tFROM "+ _tableinfo.TableName +"\n\t\t\t\tWHERE 1=1\";");
            sb.AppendLine("\t\t\tusing (var commmand = new SqlCommand(_sql, lisSqlConnection))");
            sb.AppendLine("\t\t\t{");

            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    if (item.Filter)
                    {

                        if(ConnvertType.ChangeDataType(item.DataType)=="string")
                        {
                            sb.AppendLine("\t\t\t\tif (!string.IsNullOrEmpty(_infofilter."+ item.ColumnName + "))");
                            sb.AppendLine("\t\t\t\t{");
                        }
                        else
                        {
                            sb.AppendLine("\t\t\t\tif (_infofilter." + item.ColumnName + "!=null)");
                            sb.AppendLine("\t\t\t\t{");
                        }
                        sb.AppendLine("\t\t\t\t\tcommmand.CommandText += \" and " + item.ColumnName + " = @"+ item.ColumnName + " \";");
                        sb.AppendLine("\t\t\t\t\tAddSqlParameter(commmand, \"@" + item.ColumnName + "\", _infofilter." + item.ColumnName + ", System.Data.SqlDbType." + ConnvertType.RepairDataType(item.DataType) + ");");
                        sb.AppendLine("\t\t\t\t}");
                    }
                }
            }
            sb.AppendLine("\t\t\t\tWriteLogExecutingCommand(commmand);");
            sb.AppendLine("\t\t\t\tusing (var reader = commmand.ExecuteReader())");
            sb.AppendLine("\t\t\t\t{");
            sb.AppendLine("\t\t\t\t\twhile (reader.Read())");
            sb.AppendLine("\t\t\t\t\t{");

            sb.AppendLine("\t\t\t\t\t\tvar item = new "+_objectinfoname+"();");
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    sb.AppendLine("\t\t\t\t\t\titem."+ item.ColumnName+ " = GetDbReaderValue<"+ ConnvertType.ChangeDataType(item.DataType) + ">(reader[\""+item.ColumnName+"\"]);");
                }
            }
            sb.AppendLine("\t\t\t\t\t\tresult.Add(item);");

            sb.AppendLine("\t\t\t\t\t}");
            sb.AppendLine("\t\t\t\t}");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t\treturn result;");
            sb.AppendLine("\t\t}");
            #endregion End Select
            //-------------------------------------------------------------------------------------------------------------------
            #region Select 1 Row data
            sb.AppendLine("\t\tpublic " + _objectinfoname + " GetDataInfo(SqlConnection lisSqlConnection, " + _objectfiltername + " _infofilter)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tvar result = new "+ _objectinfoname + "();");

            _listcolumnselect = "";
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    _listcolumnselect += "\t\t\t\t[" + item.ColumnName + "],\n";
                }
            }
            _listcolumnselect = _listcolumnselect.Substring(0, _listcolumnselect.Length - 2);
            sb.AppendLine("\t\t\tstring _sql=@\"SELECT TOP 1 \n" + _listcolumnselect + "\n\t\t\t\tFROM " + _tableinfo.TableName + "\n\t\t\t\tWHERE 1=1\";");
            sb.AppendLine("\t\t\tusing (var commmand = new SqlCommand(_sql, lisSqlConnection))");
            sb.AppendLine("\t\t\t{");

            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    if (item.Filter)
                    {

                        if (ConnvertType.ChangeDataType(item.DataType) == "string")
                        {
                            sb.AppendLine("\t\t\t\tif (!string.IsNullOrEmpty(_infofilter." + item.ColumnName + "))");
                            sb.AppendLine("\t\t\t\t{");
                        }
                        else
                        {
                            sb.AppendLine("\t\t\t\tif (_infofilter." + item.ColumnName + "!=null)");
                            sb.AppendLine("\t\t\t\t{");
                        }
                        sb.AppendLine("\t\t\t\t\tcommmand.CommandText += \" and " + item.ColumnName + " = @" + item.ColumnName + " \";");
                        sb.AppendLine("\t\t\t\t\tAddSqlParameter(commmand, \"@" + item.ColumnName + "\", _infofilter." + item.ColumnName + ", System.Data.SqlDbType." + ConnvertType.RepairDataType(item.DataType) + ");");
                        sb.AppendLine("\t\t\t\t}");
                    }
                }
            }
            sb.AppendLine("\t\t\t\tWriteLogExecutingCommand(commmand);");
            sb.AppendLine("\t\t\t\tusing (var reader = commmand.ExecuteReader())");
            sb.AppendLine("\t\t\t\t{");
            sb.AppendLine("\t\t\t\t\twhile (reader.Read())");
            sb.AppendLine("\t\t\t\t\t{");
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    sb.AppendLine("\t\t\t\t\t\tresult." + item.ColumnName + " = GetDbReaderValue<" + ConnvertType.ChangeDataType(item.DataType) + ">(reader[\"" + item.ColumnName + "\"]);");
                }
            }
            sb.AppendLine("\t\t\t\t\t}");
            sb.AppendLine("\t\t\t\t}");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t\treturn result;");
            sb.AppendLine("\t\t}");



            //---------------------------------------------------------------
            string _listfilter = "";
            string _listfilter2 = "";
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    if (item.Key)
                    {
                        _listfilter += ConnvertType.ChangeDataType(item.DataType) + " _"+item.ColumnName.ToLower()+",";
                        _listfilter2 += item.ColumnName + "=@"+ item.ColumnName + " AND ";
                    }
                }
            }
            _listfilter = _listfilter.Substring(0, _listfilter.Length - 1);
            _listfilter2 = _listfilter2.Substring(0, _listfilter2.Length - 5);

            sb.AppendLine("\t\tpublic " + _objectinfoname + " GetDataInfo(SqlConnection lisSqlConnection, "+ _listfilter + ")");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tvar result = new " + _objectinfoname + "();");

            _listcolumnselect = "";
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    _listcolumnselect += "\t\t\t\t[" + item.ColumnName + "],\n";
                }
            }
            _listcolumnselect = _listcolumnselect.Substring(0, _listcolumnselect.Length - 2);
            sb.AppendLine("\t\t\tstring _sql=@\"SELECT TOP 1 \n" + _listcolumnselect + "\n\t\t\t\tFROM " + _tableinfo.TableName + "\n\t\t\t\tWHERE "+ _listfilter2 + "\";");
            sb.AppendLine("\t\t\tusing (var commmand = new SqlCommand(_sql, lisSqlConnection))");
            sb.AppendLine("\t\t\t{");

            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    if (item.Key)
                    {
                        sb.AppendLine("\t\t\t\tAddSqlParameter(commmand, \"@" + item.ColumnName + "\", _" + item.ColumnName.ToLower() + ", System.Data.SqlDbType." + ConnvertType.RepairDataType(item.DataType) + ");");
                    }
                }
            }
            sb.AppendLine("\t\t\t\tWriteLogExecutingCommand(commmand);");
            sb.AppendLine("\t\t\t\tusing (var reader = commmand.ExecuteReader())");
            sb.AppendLine("\t\t\t\t{");
            sb.AppendLine("\t\t\t\t\twhile (reader.Read())");
            sb.AppendLine("\t\t\t\t\t{");
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    sb.AppendLine("\t\t\t\t\t\tresult." + item.ColumnName + " = GetDbReaderValue<" + ConnvertType.ChangeDataType(item.DataType) + ">(reader[\"" + item.ColumnName + "\"]);");
                }
            }
            sb.AppendLine("\t\t\t\t\t}");
            sb.AppendLine("\t\t\t\t}");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t\treturn result;");
            sb.AppendLine("\t\t}");


            #endregion End Select
            //----------------------
            sb.AppendLine("\t}");
            sb.AppendLine("}");
            File.WriteAllText(txtPath.Text + "\\" + _tableinfo.TableName.Trim() + "Service.cs", sb.ToString());
            MessageBox.Show("OK !");
        }

        private void btnModel_Click(object sender, EventArgs e)
        {
            TableInfo _tableinfo = dgvListTable.CurrentRow.DataBoundItem as TableInfo;
            string _tablename = _tableinfo.TableName.Trim();
            string _tablenameinfo = _tableinfo.TableName.Trim()+"Info";
            string _classname = _tableinfo.TableName.Trim() + "Model";
            var sb = new StringBuilder();
            sb.AppendLine("using System;\nusing Code;\nusing DMS.ObjectCommon;\nusing DMS.BusinessService;\nusing System.Collections.Generic;\nusing System.Data.SqlClient;\n\nnamespace LabconnectWebPlus.Models");
            sb.AppendLine("{");
            sb.AppendLine("\tpublic class " + _classname+":"+ _tablenameinfo);
            sb.AppendLine("\t{");
            sb.AppendLine("\t\t#region Hàm mặc định");
            sb.AppendLine("\t\tpublic static " + _tablename + "Model ConvertToModel(" + _tablenameinfo + " info)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\t" + _tablename + "Model _itemmodel = new " + _tablename + "Model();");
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    sb.AppendLine("\t\t\t_itemmodel." + item.ColumnName + " = info." + item.ColumnName + ";");
                }
            }
            sb.AppendLine("\t\t\treturn _itemmodel;");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t\tpublic static List<" + _classname + "> ConvertToModel(List<" + _tablenameinfo + "> info)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tList<" + _classname + "> _itemmodel = new List<" + _classname + ">();");
            sb.AppendLine("\t\t\tforeach("+ _tablenameinfo + " item in info)");
            sb.AppendLine("\t\t\t{");
            sb.AppendLine("\t\t\t\t"+ _classname+" _model=new "+ _classname+"();");
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    sb.AppendLine("\t\t\t\t_model." + item.ColumnName + " = item." + item.ColumnName + ";");
                }
            }
            sb.AppendLine("\t\t\t\t_itemmodel.Add(_model);");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t\treturn _itemmodel;");
            sb.AppendLine("\t\t}");

            sb.AppendLine("\t\tpublic static List<" + _classname + "> GetDataList(SqlConnection lisSqlConnection,"+ _tablename + "Service." + _tablename + "Filter _infofilter)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\treturn ConvertToModel("+ _tablename + "Service.GetInstance().GetDataList(lisSqlConnection, new " + _tablename + "Service."+ _tablename + "Filter()");
            sb.AppendLine("\t\t\t{");
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Filter)
                {
                    sb.AppendLine("\t\t\t\t" + item.ColumnName + " = _infofilter." + item.ColumnName + ",");
                }
            }
            sb.AppendLine("\t\t\t}));");
            sb.AppendLine("\t\t}");

            sb.AppendLine("\t\tpublic static List<" + _classname + "> GetDataList(SqlConnection lisSqlConnection)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\treturn ConvertToModel(" + _tablename + "Service.GetInstance().GetDataList(lisSqlConnection, new " + _tablename + "Service." + _tablename + "Filter()));");
            sb.AppendLine("\t\t}");


            sb.AppendLine("\t\tpublic static " + _classname + " GetDataInfo(SqlConnection lisSqlConnection,"+ _tablename + "Service." + _tablename + "Filter _infofilter)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\treturn ConvertToModel(" + _tablename + "Service.GetInstance().GetDataInfo(lisSqlConnection, new " + _tablename + "Service." + _tablename + "Filter()");
            sb.AppendLine("\t\t\t{");
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Filter)
                {
                    sb.AppendLine("\t\t\t\t" + item.ColumnName + " = _infofilter." + item.ColumnName + ",");
                }
            }
            sb.AppendLine("\t\t\t}));");
            sb.AppendLine("\t\t}");

            string _listfilter = "";
            string _listfilter2 = "";
            foreach (ColumnInfo item in _ListColumnInfo)
            {
                if (item.Active)
                {
                    if (item.Key)
                    {
                        _listfilter += ConnvertType.ChangeDataType(item.DataType) + " _" + item.ColumnName.ToLower() + ",";
                        _listfilter2 += "_"+item.ColumnName.ToLower() + ",";
                    }
                }
            }
            _listfilter = _listfilter.Substring(0, _listfilter.Length - 1);
            _listfilter2 = _listfilter2.Substring(0, _listfilter2.Length - 1);

            sb.AppendLine("\t\tpublic static " + _classname + " GetDataInfo(SqlConnection lisSqlConnection," + _listfilter + ")");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\treturn ConvertToModel(" + _tablename + "Service.GetInstance().GetDataInfo(lisSqlConnection,"+ _listfilter2 + "));");
            sb.AppendLine("\t\t}");


            sb.AppendLine("\t\tpublic static bool Insert(SqlConnection lisSqlConnection," + _tablenameinfo + " _infoinsert)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\treturn " + _tablename + "Service.GetInstance().Insert(lisSqlConnection,_infoinsert);");
            sb.AppendLine("\t\t}");

            sb.AppendLine("\t\tpublic static bool Update(SqlConnection lisSqlConnection," + _tablenameinfo + " _infoupdate)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\treturn " + _tablename + "Service.GetInstance().Update(lisSqlConnection,_infoupdate);");
            sb.AppendLine("\t\t}");

            sb.AppendLine("\t\tpublic static bool Delete(SqlConnection lisSqlConnection," + _tablenameinfo + " _infodelete)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\treturn " + _tablename + "Service.GetInstance().Delete(lisSqlConnection,_infodelete);");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t\t#endregion End");
            sb.AppendLine("\t}");
            sb.AppendLine("}");
            File.WriteAllText(txtPath.Text + "\\" + _classname+".cs", sb.ToString());
            MessageBox.Show("OK !");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (txtPath.Text.Trim() == "")
            {
                MessageBox.Show("Cần nhập vị trí lưu!");
                txtPath.Focus();
                return;
            }
            if (!Directory.Exists(txtPath.Text + "\\Mapping"))
            {
                Directory.CreateDirectory(txtPath.Text + "\\Mapping");
            }
            for (int i = 0; i < dgvListTable.Rows.Count; i++)
            {
                if ((bool)dgvListTable.Rows[i].Cells[0].Value)
                {
                    TableInfo _tableinfo = dgvListTable.Rows[i].DataBoundItem as TableInfo;
                    string _tablename = _tableinfo.TableName.Trim();
                    using (SqlConnection _conn = ConnectDB.GetConnection())
                    {
                        LoadListColumnTable(_conn, _tablename);
                    }
                    string _classname = _tableinfo.TableName.Trim();
                    bool hasID = false;
                    bool hasMa = false;
                  
                    string nameID = "";
                    string nameMa = "";
                    string _sql = @"SELECT[Column_NAME]
            FROM INFORMATION_SCHEMA.Columns WHERE Table_Name = '" + _tableinfo.TableName.Trim() + "'";
                    using (SqlConnection _conn = ConnectDB.GetConnection())
                    {
                        DataTable _listcolumn = DataProvider.ExcuteDataSet(_conn, _sql).Tables[0];
                        for (int n = 0; n < _listcolumn.Rows.Count; n++)
                        {
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "id")
                            {
                                nameID = _listcolumn.Rows[n][0].ToString().Trim();
                                hasID = true;
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "ma")
                            {
                                nameMa = _listcolumn.Rows[n][0].ToString().Trim();
                                hasMa = true;
                            }
                        }
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine("using Microsoft.EntityFrameworkCore;\nusing Microsoft.EntityFrameworkCore.Metadata.Builders;\nusing vLib.Core.Domain;\nusing vLib.Data.Mapping.Configuration;\n\nnamespace vLib.Data.Mapping");
                    sb.AppendLine("{");
                    sb.AppendLine("\tpublic class " + _classname + "Map : vLibEntityTypeConfiguration<" + _classname + ">");
                    sb.AppendLine("\t{");
                    sb.AppendLine("\t\t#region Methods");
                    sb.AppendLine("");
                    if (hasID)
                    {
                        sb.AppendLine("\t\tpublic override void Configure(EntityTypeBuilder<" + _classname + "> builder)");
                        sb.AppendLine("\t\t{");
                        sb.AppendLine("\t\t\tbuilder.ToTable(nameof(" + _classname + "));");
                        sb.AppendLine("\t\t\tbuilder.Property(x => x." + nameID + ").ValueGeneratedOnAdd().HasColumnName(\"ID\");");
                        sb.AppendLine("");
                        sb.AppendLine("\t\t\tbase.Configure(builder);");
                        sb.AppendLine("\t\t}");
                    }
                    else if(hasMa)
                    {
                        sb.AppendLine("\t\tpublic override void Configure(EntityTypeBuilder<" + _classname + "> builder)");
                        sb.AppendLine("\t\t{");
                        sb.AppendLine("\t\t\tbuilder.ToTable(nameof(" + _classname + "));");
                        sb.AppendLine("\t\t\tbuilder.HasKey(x => x." + nameMa + ");");
                        sb.AppendLine("");
                        sb.AppendLine("\t\t\tbase.Configure(builder);");
                        sb.AppendLine("\t\t}");
                    }
                    sb.AppendLine("");
                    sb.AppendLine("\t\t#endregion");
                    sb.AppendLine("\t}");
                    sb.AppendLine("}");
                    File.WriteAllText(txtPath.Text + "\\Mapping\\" + _tableinfo.TableName.Trim() + "Map.cs", sb.ToString());

                }
            }

            MessageBox.Show("OK !");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (txtPath.Text.Trim() == "")
            {
                MessageBox.Show("Cần nhập vị trí lưu!");
                txtPath.Focus();
                return;
            }
            if (!Directory.Exists(txtPath.Text + "\\Implementation"))
            {
                Directory.CreateDirectory(txtPath.Text + "\\Implementation");
            }
            for (int i = 0; i < dgvListTable.Rows.Count; i++)
            {
                if ((bool)dgvListTable.Rows[i].Cells[0].Value)
                {
                    TableInfo _tableinfo = dgvListTable.Rows[i].DataBoundItem as TableInfo;
                    string _tablename = _tableinfo.TableName.Trim();
                    using (SqlConnection _conn = ConnectDB.GetConnection())
                    {
                        LoadListColumnTable(_conn, _tablename);
                    }
                    string _classname = _tableinfo.TableName.Trim();
                    bool hasID = false;
                    bool hasMa = false;
                    bool hasTen = false;
                    bool hasThutu = false;
                    bool hasTrangthai = false;
                    bool hasTruongMa = false;
                    bool hasSoMa = false;
                    bool hasPhongMa = false;
                    string nameID = "";
                    string nameTen = "";
                    string nameMa = "";
                    string nameTrangthai = "";
                    string nameThutu = "";
                    string nameTruongMa = "";
                    string nameSoMa = "";
                    string namePhongMa = "";
                    string _sql = @"SELECT[Column_NAME]
            FROM INFORMATION_SCHEMA.Columns WHERE Table_Name = '" + _tableinfo.TableName.Trim() + "'";
                    using (SqlConnection _conn = ConnectDB.GetConnection())
                    {
                        DataTable _listcolumn = DataProvider.ExcuteDataSet(_conn, _sql).Tables[0];
                        for(int n = 0;n< _listcolumn.Rows.Count; n++)
                        {
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "ten")
                            {
                                nameTen = _listcolumn.Rows[n][0].ToString().Trim();
                                hasTen = true;
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "id")
                            {
                                nameID = _listcolumn.Rows[n][0].ToString().Trim();
                                hasID = true;
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "ma")
                            {
                                nameMa = _listcolumn.Rows[n][0].ToString().Trim();
                                hasMa = true;
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "thutu")
                            {
                                hasThutu = true;
                                nameThutu = _listcolumn.Rows[n][0].ToString().Trim();
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "trangthai")
                            {
                                hasTrangthai = true;
                                nameTrangthai = _listcolumn.Rows[n][0].ToString().Trim();
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "truongma")
                            {
                                hasTruongMa = true;
                                nameTruongMa = _listcolumn.Rows[n][0].ToString().Trim();
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "soma")
                            {
                                hasSoMa = true;
                                nameSoMa = _listcolumn.Rows[n][0].ToString().Trim();
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "phongma")
                            {
                                hasPhongMa = true;
                                namePhongMa = _listcolumn.Rows[n][0].ToString().Trim();
                            }
                        }
                    }
                    string para = _classname[0].ToString().ToLower() + _classname.Substring(1, _classname.Length - 1);
                    if (_classname.Substring(0, 2) == "DM")
                    {
                        para = _classname.Substring(0, 2).ToLower() + _classname.Substring(2, _classname.Length - 2);
                    }
                    var sb = new StringBuilder();
                    sb.AppendLine("using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing vLib.Core.Caching;\nusing vLib.Core.Domain;\nusing vLib.Data;\nusing vLib.Services.Interface;\n\nnamespace vLib.Services.Implementation");
                    sb.AppendLine("{");
                    sb.AppendLine("\tpublic partial class " + _classname + "Service : I" + _classname + "Service");
                    sb.AppendLine("\t{");
                    sb.AppendLine("\t\t#region Fields");
                    sb.AppendLine("\t\tprivate readonly IRepository<" + _classname + "> _repository;");
                    sb.AppendLine("\t\tprivate readonly ICacheManager _cacheManager;");
                    sb.AppendLine("");
                    sb.AppendLine("\t\tprivate const string keyCachePrefix = \"" + _classname + "\";");
                    sb.AppendLine("\t\t#endregion");
                    sb.AppendLine("");
                    sb.AppendLine("\t\t#region Ctor");
                    sb.AppendLine("\t\tpublic " + _classname + "Service(IRepository<" + _classname + "> repository, ICacheManager cacheManager)");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t\t_repository = repository ?? throw new ArgumentNullException(nameof(repository));");
                    sb.AppendLine("\t\t\t_cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));");
                    sb.AppendLine("\t\t}");
                    sb.AppendLine("\t\t#endregion");
                    sb.AppendLine("");
                    sb.AppendLine("\t\t#region Methods");

                    sb.AppendLine("\t\t#region Get");
                    if (hasTruongMa)
                    {
                        sb.AppendLine("\t\tpublic IList<" + _classname + "> GetByTruong(" + (hasSoMa ? " string soMa, " : "") + "" + (hasPhongMa ? " string phongMa, " : "") + "string truongMa" + (hasMa ? ", string ma = \"\" " : "") + ", string ten = \"\"" + (hasTrangthai ? ", int ? trangThai = null" : "") + ")");
                        sb.AppendLine("\t\t{");
                        sb.AppendLine("\t\t\tvar cacheKey = _cacheManager.BuildCachedKey(keyCachePrefix, nameof(GetByTruong) ," + (hasSoMa ? "soMa, " : "") + "" + (hasPhongMa ? "phongMa, " : "") + "truongMa" + (hasMa ? ", ma " : "") + ", ten" + (hasTrangthai ? ", trangThai" : "") + ");");
                        sb.AppendLine("\t\t\treturn _cacheManager.Get(cacheKey, () =>");
                        sb.AppendLine("\t\t\t{");
                        sb.AppendLine("\t\t\t\tvar query = _repository.Table;");
                        sb.AppendLine("\t\t\t\tquery = query.Where(x => x."+nameTruongMa+" == truongMa);");
                        sb.AppendLine("");
                        if (hasMa)
                        {
                            sb.AppendLine("\t\t\t\tif (!string.IsNullOrEmpty(ma))");
                            sb.AppendLine("\t\t\t\t\tquery = query.Where(x => x." + nameMa + " == ma);");
                        }
                        sb.AppendLine("\t\t\t\tif (!string.IsNullOrEmpty(ten))");
                        sb.AppendLine("\t\t\t\t\tquery = query.Where(x => x." + nameTen + ".Contains(ten));");
                        if (hasTrangthai)
                        {
                            sb.AppendLine("\t\t\t\tif (trangThai!=null)");
                            sb.AppendLine("\t\t\t\t\tquery = query.Where(x => x." + nameTrangthai + " == trangThai);");
                        }
                        if (hasThutu)
                            sb.AppendLine("\t\t\t\tquery = query.OrderBy(x => x." + nameThutu + ").ThenBy(x => ten);");
                        sb.AppendLine("\t\t\t\treturn query.ToList();");
                        sb.AppendLine("\t\t\t});");
                        sb.AppendLine("\t\t}");
                    }
                    else if(hasTen)
                    {
                        sb.AppendLine("\t\tpublic IList<" + _classname + "> GetAll("+(hasMa? "string ma = \"\", " : "") + "string ten = \"\""+(hasTrangthai? ", int ? trangThai = null" : "") + ")");
                        sb.AppendLine("\t\t{");
                        sb.AppendLine("\t\t\tvar cacheKey = _cacheManager.BuildCachedKey(keyCachePrefix, nameof(GetAll)"+(hasMa?", ma ":"")+", ten" + (hasTrangthai ? ", trangThai" : "") +");");
                        sb.AppendLine("\t\t\treturn _cacheManager.Get(cacheKey, () =>");
                        sb.AppendLine("\t\t\t{");
                        sb.AppendLine("\t\t\t\tvar query = _repository.Table;");
                        sb.AppendLine("");
                        if (hasMa)
                        {
                            sb.AppendLine("\t\t\t\tif (!string.IsNullOrEmpty(ma))");
                            sb.AppendLine("\t\t\t\t\tquery = query.Where(x => x." + nameMa + " == ma);");
                        }
                        sb.AppendLine("\t\t\t\tif (!string.IsNullOrEmpty(ten))");
                        sb.AppendLine("\t\t\t\t\tquery = query.Where(x => x."+ nameTen + ".Contains(ten));");
                        if (hasTrangthai)
                        {
                            sb.AppendLine("\t\t\t\tif (!string.IsNullOrEmpty(trangThai))");
                            sb.AppendLine("\t\t\t\t\tquery = query.Where(x => x." + nameTrangthai + " == trangThai);");
                        }
                        if(hasThutu)
                        sb.AppendLine("\t\t\t\tquery = query.OrderBy(x => x."+ nameThutu + ");");
                        sb.AppendLine("\t\t\t\treturn query.ToList();");
                        sb.AppendLine("\t\t\t});");
                        sb.AppendLine("\t\t}");
                    }
                    if (hasMa)
                    {
                        sb.AppendLine("\t\tpublic " + _classname + " GetByMa(string ma)");
                        sb.AppendLine("\t\t{");
                        sb.AppendLine("\t\t\tif (string.IsNullOrEmpty(ma))");
                        sb.AppendLine("\t\t\t\treturn null;");
                        sb.AppendLine("\t\t\tvar cacheKey = _cacheManager.BuildCachedKey(keyCachePrefix, nameof(GetByMa), ma);");
                        sb.AppendLine("\t\t\treturn _cacheManager.Get(cacheKey, () => _repository.Table.FirstOrDefault(x => x.Ma == ma));");
                        sb.AppendLine("\t\t}");
                    }
                    if (hasID)
                    {
                        sb.AppendLine("\t\tpublic " + _classname + " GetById(long Id)");
                        sb.AppendLine("\t\t{");
                        sb.AppendLine("\t\t\tif (Id == 0) return null;");
                        sb.AppendLine("");
                        sb.AppendLine("\t\t\tvar cacheKey = _cacheManager.BuildCachedKey(keyCachePrefix, nameof(GetById), Id);");
                        sb.AppendLine("\t\t\treturn _cacheManager.Get(cacheKey, () => _repository.GetById(Id));");
                        sb.AppendLine("\t\t}");
                        sb.AppendLine("");
                        sb.AppendLine("\t\tpublic IList<" + _classname + "> GetByIds(long[] Ids)");
                        sb.AppendLine("\t\t{");
                        sb.AppendLine("\t\t\tif (Ids == null || Ids.Length == 0)");
                        sb.AppendLine("\t\t\t\treturn new List<" + _classname + ">();");
                        sb.AppendLine("");
                        sb.AppendLine("\t\t\tvar query = from p in _repository.Table");
                        sb.AppendLine("\t\t\t where Ids.Contains(p." + nameID + ")");
                        sb.AppendLine("\t\t\tselect p;");
                        sb.AppendLine("");
                        sb.AppendLine("\t\t\tvar listEntity = query.ToList();");
                        sb.AppendLine("");
                        sb.AppendLine("\t\t\tvar sortedList = new List<" + _classname + ">();");
                        sb.AppendLine("\t\t\tforeach (var id in Ids)");
                        sb.AppendLine("\t\t\t{");
                        sb.AppendLine("\t\t\t\tvar entity = listEntity.Find(x => x."+ nameID+" == id);");
                        sb.AppendLine("\t\t\t\tif (entity != null)");
                        sb.AppendLine("\t\t\t\t\tsortedList.Add(entity);");
                        sb.AppendLine("\t\t\t}");
                        sb.AppendLine("");
                        sb.AppendLine("\t\t\t return sortedList;");
                        sb.AppendLine("\t\t}");
                    }
                    sb.AppendLine("\t\t#endregion");

                    sb.AppendLine("\t\t#region Set");
                    sb.AppendLine("");
                    sb.AppendLine("\t\tpublic void Insert(" + _classname + " " + para + ")");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t\tif (" + para + " is null) throw new ArgumentNullException();");
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\t\t_repository.Insert(" + para + ");");
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\t// cache");
                    sb.AppendLine("\t\t\t_cacheManager.RemoveByPrefix(keyCachePrefix);");
                    sb.AppendLine("\t\t}");
                    sb.AppendLine("");
                    sb.AppendLine("\t\tpublic void Update(" + _classname + " " + para + ")");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t\tif (" + para + " is null) throw new ArgumentNullException();");
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\t\t_repository.Update(" + para + ");");
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\t// cache");
                    sb.AppendLine("\t\t\t_cacheManager.RemoveByPrefix(keyCachePrefix);");
                    sb.AppendLine("\t\t}");
                    sb.AppendLine("");
                    sb.AppendLine("\t\tpublic void Delete(" + _classname + " " + para + ")");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t\tif (" + para + " is null) throw new ArgumentNullException();");
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\t\t_repository.Delete(" + para + ");");
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\t// cache");
                    sb.AppendLine("\t\t\t_cacheManager.RemoveByPrefix(keyCachePrefix);");
                    sb.AppendLine("\t\t}");
                    sb.AppendLine("");
                    sb.AppendLine("\t\tpublic void DeleteList(List<" + _classname + "> list" + _classname + ")");
                    sb.AppendLine("\t\t{");
                    sb.AppendLine("\t\t\tif (list" + _classname + " is null) throw new ArgumentNullException();");
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\t\t_repository.Delete(list" + _classname + ");");
                    sb.AppendLine("");
                    sb.AppendLine("\t\t\t// cache");
                    sb.AppendLine("\t\t\t_cacheManager.RemoveByPrefix(keyCachePrefix);");
                    sb.AppendLine("\t\t}");
                    sb.AppendLine("");
                    sb.AppendLine("\t\t#endregion");
                    sb.AppendLine("\t\t#endregion");
                    sb.AppendLine("\t}");
                    sb.AppendLine("}");
                    File.WriteAllText(txtPath.Text + "\\Implementation\\" + _tableinfo.TableName.Trim() + "Service.cs", sb.ToString());
                }
            }
            MessageBox.Show("OK !");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (txtPath.Text.Trim() == "")
            {
                MessageBox.Show("Cần nhập vị trí lưu!");
                txtPath.Focus();
                return;
            }
            if (!Directory.Exists(txtPath.Text + "\\Interface"))
            {
                Directory.CreateDirectory(txtPath.Text + "\\Interface");
            }
            for (int i = 0; i < dgvListTable.Rows.Count; i++)
            {
                if ((bool)dgvListTable.Rows[i].Cells[0].Value)
                {
                    TableInfo _tableinfo = dgvListTable.Rows[i].DataBoundItem as TableInfo;
                    string _tablename = _tableinfo.TableName.Trim();
                    using (SqlConnection _conn = ConnectDB.GetConnection())
                    {
                        LoadListColumnTable(_conn, _tablename);
                    }
                    string _classname = _tableinfo.TableName.Trim();
                    bool hasID = false;
                    bool hasMa = false;
                    bool hasTen = false;
                    bool hasThutu = false;
                    bool hasTrangthai = false;
                    bool hasTruongMa = false;
                    bool hasSoMa = false;
                    bool hasPhongMa = false;
                    string nameID = "";
                    string nameTen = "";
                    string nameMa = "";
                    string nameTrangthai = "";
                    string nameThutu = "";
                    string nameTruongMa = "";
                    string nameSoMa = "";
                    string namePhongMa = "";
                    string _sql = @"SELECT[Column_NAME]
            FROM INFORMATION_SCHEMA.Columns WHERE Table_Name = '" + _tableinfo.TableName.Trim() + "'";
                    using (SqlConnection _conn = ConnectDB.GetConnection())
                    {
                        DataTable _listcolumn = DataProvider.ExcuteDataSet(_conn, _sql).Tables[0];
                        for (int n = 0; n < _listcolumn.Rows.Count; n++)
                        {
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "ten")
                            {
                                nameTen = _listcolumn.Rows[n][0].ToString().Trim();
                                hasTen = true;
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "id")
                            {
                                nameID = _listcolumn.Rows[n][0].ToString().Trim();
                                hasID = true;
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "ma")
                            {
                                nameMa = _listcolumn.Rows[n][0].ToString().Trim();
                                hasMa = true;
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "thutu")
                            {
                                hasThutu = true;
                                nameThutu = _listcolumn.Rows[n][0].ToString().Trim();
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "trangthai")
                            {
                                hasTrangthai = true;
                                nameTrangthai = _listcolumn.Rows[n][0].ToString().Trim();
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "truongma")
                            {
                                hasTruongMa = true;
                                nameTruongMa = _listcolumn.Rows[n][0].ToString().Trim();
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "soma")
                            {
                                hasSoMa = true;
                                nameSoMa = _listcolumn.Rows[n][0].ToString().Trim();
                            }
                            if (_listcolumn.Rows[n][0].ToString().ToLower().Trim() == "phongma")
                            {
                                hasPhongMa = true;
                                namePhongMa = _listcolumn.Rows[n][0].ToString().Trim();
                            }
                        }
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine("using System.Collections.Generic;\nusing vLib.Core.Domain;\n\nnamespace vLib.Services.Interface");
                    sb.AppendLine("{");
                    sb.AppendLine("\tpublic partial interface I" + _classname + "Service");
                    sb.AppendLine("\t{");
                    if (hasMa)
                        sb.AppendLine("\t\t" + _classname + " GetByMa(string ma);");
                    if (hasTruongMa)
                        sb.AppendLine("\t\tIList<" + _classname + "> GetByTruong(" + (hasSoMa ? " string soMa, " : "") + "" + (hasPhongMa ? " string phongMa, " : "") + "string truongMa" + (hasMa ? ", string ma = \"\" " : "") + ", string ten = \"\"" + (hasTrangthai ? ", int ? trangThai = null" : "") + ");");
                    else if (hasTen)
                        sb.AppendLine("\t\tIList<" + _classname + "> GetAll(" + (hasMa ? "string ma = \"\", " : "") + "string ten = \"\"" + (hasTrangthai ? ", int ? trangThai = null" : "") + ");");
                    string para = _classname[0].ToString().ToLower() + _classname.Substring(1, _classname.Length - 1);
                    if (_classname.Substring(0, 2) == "DM")
                    {
                        para = _classname.Substring(0, 2).ToLower() + _classname.Substring(2, _classname.Length - 2);
                    }
                    sb.AppendLine("\t\tvoid Insert(" + _classname + " " + para + ");");
                    sb.AppendLine("\t\tvoid Update(" + _classname + " " + para + ");");
                    sb.AppendLine("\t\tvoid Delete(" + _classname + " " + para + ");");
                    sb.AppendLine("\t\tvoid DeleteList(List<" + _classname + "> list" + _classname + ");");
                    if (hasID)
                    {
                        sb.AppendLine("\t\t" + _classname + " GetById(long Id);");
                        sb.AppendLine("\t\tIList<" + _classname + "> GetByIds(long[] Ids);");
                    }
                        
                    sb.AppendLine("\t}");
                    sb.AppendLine("}");
                    
                    File.WriteAllText(txtPath.Text + "\\Interface\\I" + _tableinfo.TableName.Trim() + "Service.cs", sb.ToString());
                }
            }
            MessageBox.Show("OK !");
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < dgvListTable.Rows.Count; i++)
            {
                dgvListTable.Rows[i].Cells[0].Value = checkBox2.Checked;
            }
        }
    }
}
