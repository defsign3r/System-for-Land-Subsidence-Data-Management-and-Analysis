using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;
using ADOX;

namespace MySystem
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            InitializeComponent();
            label4.Text = "";
            //FormOfVisualize form2 = new FormOfVisualize();
        }
        FormOfVisualize form2 = new FormOfVisualize();
        FormOfCalculate form3 = new FormOfCalculate();
        private void button1_Click(object sender, EventArgs e)
        {
            //获取Excel文件路径
            OpenFileDialog ExcelOpenFileDialog = new OpenFileDialog();
            ExcelOpenFileDialog.Multiselect = false;
            ExcelOpenFileDialog.Title = "打开Excel文件";
            ExcelOpenFileDialog.Filter = "Excel文件（*.xls）|*.xls";
            if (ExcelOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox_Excel.Text = ExcelOpenFileDialog.FileName;
            }
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            
            //创建access数据库
            #region
            if (textBox_Excel.Text.Length == 0)
            {
                MessageBox.Show("请选择目标Excel文件");
            }
            SaveFileDialog AccessSaveFileDialog = new SaveFileDialog();
            AccessSaveFileDialog.Title = "创建Access文件";
            AccessSaveFileDialog.Filter = "Access文件（*.accdb）|*.accdb";
            #endregion
            
            //获取access数据库路径
            #region
            if (File.Exists(textBox_Excel.Text))
            {
                AccessSaveFileDialog.FileName = System.IO.Path.GetFileNameWithoutExtension(textBox_Excel.Text);
            }
            if (AccessSaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox_Access.Text = AccessSaveFileDialog.FileName;
                form2.database_path = textBox_Access.Text;
                form3.database_path = textBox_Access.Text;
            }
            #endregion

            //Access创建
            //Access表构造
            #region
            //不存在则创建
            //获取excel表名作为access表中字段名
            //excel_sheetname数组存放表名
            OleDbConnection conn_excel = new OleDbConnection
                ("Provider=Microsoft.ACE.OLEDB.12.0;" + "Data Source=" 
                + textBox_Excel.Text + ";" + "Extended Properties=Excel 8.0;");
            conn_excel.Open();
            DataTable dt_sheetname = conn_excel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            string []excel_sheetname = new string[dt_sheetname.Rows.Count];
            int sheets_number = 0;
            foreach (DataRow row in dt_sheetname.Rows)
            {
                excel_sheetname[sheets_number] = 
                    row["TABLE_NAME"].ToString().Replace("'", "").Replace("$", "");
                form2.comboBox1.Items.Add(excel_sheetname[sheets_number]);
                sheets_number++;
            }//获取excel表名完毕
            form3.sheet_name = excel_sheetname;
            //开始创建access
            ADOX.Catalog catalog = new Catalog();
            catalog.Create("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" 
                + AccessSaveFileDialog.FileName + ";" + "Jet OLEDB:Engine Type=5");
            ADODB.Connection ado_conn = new ADODB.Connection();
            ado_conn.Open("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" 
                + AccessSaveFileDialog.FileName, null, null, -1);
            catalog.ActiveConnection = ado_conn;
            ADOX.TableClass tb = new ADOX.TableClass();
            tb.Name = "监测点坐标";
            tb.Columns.Append("监测点编号");
            tb.Columns.Append("X坐标");
            tb.Columns.Append("Y坐标");
            catalog.Tables.Append(tb);
            for (int i = 0; i < excel_sheetname.Length - 1; i++)
            {
                ADOX.TableClass TB = new ADOX.TableClass();
                TB.Name = excel_sheetname[i];
                TB.Columns.Append("监测点编号");
                TB.Columns.Append("X坐标");
                TB.Columns.Append("Y坐标");
                TB.Columns.Append("高程值");
                catalog.Tables.Append(TB);
            }
            //access数据库构造完毕
            #endregion
            
            //向数据库中录入数据
            #region
            //打开“坐标”表，先输入所有点号和对应坐标
            string sql_coordinate_sheet_open="select*from[坐标$]";
            OleDbDataAdapter ada_coordinate_sheet_open = new OleDbDataAdapter
                (sql_coordinate_sheet_open, conn_excel);
            DataTable dt_coordinate_sheet_open = new DataTable();
            ada_coordinate_sheet_open.Fill(dt_coordinate_sheet_open);
            form3.point_number = dt_coordinate_sheet_open.Rows.Count;
            string sql_conn_access = "Provider=Microsoft.ACE.OLEDB.12.0;" + "Data Source=" 
                + textBox_Access.Text + ";" + "Persist Security Info=False;";
            OleDbConnection conn_access = new OleDbConnection(sql_conn_access);
            conn_access.Open();
            //设置进度条 
            progressBar1.Value = 0;
            progressBar1.Maximum = dt_coordinate_sheet_open.Rows.Count;
            label4.Text = "正在导入监测点坐标...";
            try
            {
                for (int i = 0; i < dt_coordinate_sheet_open.Rows.Count; i++)
                {
                    string sql_add_into_access = "insert into 监测点坐标(监测点编号,X坐标,Y坐标) values ('" 
                        + Convert.ToDouble(dt_coordinate_sheet_open.Rows[i][0]) 
                        + "','" + Convert.ToDouble(dt_coordinate_sheet_open.Rows[i][2]) 
                        + "','" + Convert.ToDouble(dt_coordinate_sheet_open.Rows[i][3]) + "') ";
                    OleDbCommand com = new OleDbCommand(sql_add_into_access, conn_access);
                    com.ExecuteNonQuery();
                    progressBar1.Value++;
                }
            }
            catch
            {
                MessageBox.Show("点坐标导入失败!");
            }
            //导入高程值
            label4.Text = "正在导入各期高程测值...";
            progressBar1.Value = 0;
            progressBar1.Maximum = excel_sheetname.Length - 1;
            for (int i = 0; i < excel_sheetname.Length-1; i++)
            {
                string  sql_height_from_excel="select 监测点编号,横坐标,纵坐标,高程值 from["+excel_sheetname[i]+"$]";
                OleDbDataAdapter Ada = new OleDbDataAdapter(sql_height_from_excel,conn_excel);
                DataTable DT = new DataTable();
                Ada.Fill(DT);
                //导入access数据库
                for (int j = 0; j < DT.Rows.Count; j++)
                {
                    string sql_insert_height_into_access = "insert into " + excel_sheetname[i] + "(监测点编号,X坐标,Y坐标,高程值) values ('" + Convert.ToDouble(DT.Rows[j][0]) + "','" + Convert.ToDouble(DT.Rows[j][1]) + "','" + Convert.ToDouble(DT.Rows[j][2]) + "','" + Convert.ToDouble(DT.Rows[j][3]) + "') ";
                    OleDbCommand COM = new OleDbCommand(sql_insert_height_into_access, conn_access);
                    COM.ExecuteNonQuery();
                    //progressBar1.Value++;
                }
                progressBar1.Value++;
                //MessageBox.Show("导入完毕！");
            }
            label4.Text = "数据导入完毕！";
            ado_conn.Close();
            conn_excel.Close();
            conn_access.Close();
            #endregion
        }
        
        //生成并显示地图
        private void button3_Click(object sender, EventArgs e)
        {
            //FormOfVisualize form = new FormOfVisualize();
            form2.Show();
            //this.Hide();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            form3.Show();
            //this.Hide();
        }
    }
}
