using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;
using System.Windows.Forms.DataVisualization.Charting;

namespace MySystem
{
    public partial class FormOfCalculate : Form
    {
        public FormOfCalculate()
        {
            InitializeComponent();
        }

        public string[] sheet_name;
        public string database_path;
        public int point_number;
        DataTable DT = new DataTable();
        DataColumn DC = null;
        DataRow DR;
        private void button1_Click(object sender, EventArgs e)
        {
            //判断输入点的合法性
            bool tag = false;
            for (int i = 0; i < point_number; i++)
            {
                if (i == Convert.ToInt32(textBox1.Text))
                {
                    tag = true;
                }
                else
                {
                    continue;
                }
            }
            if (tag == false)
            {
                MessageBox.Show("输入点不合法！");
            }
            try
            {
                DC = DT.Columns.Add("测期", Type.GetType("System.String"));
                DC = DT.Columns.Add("高程值", Type.GetType("System.String"));
            }
            catch
            {
                //如果已存在上述字段，则继续
            }
            
            //逐表获取各测期高程值
            OleDbConnection conn_access = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;" + "Data Source=" + database_path + ";" + "Persist Security Info=False;");
            conn_access.Open();
            //在dataGridView控件中显示监测点各测期的测值
            for (int i = 0; i < sheet_name.Length - 1; i++)
            {
                string sql_height = "select 高程值 from [" + sheet_name[i] + "] where 监测点编号 = '" + Convert.ToDouble(textBox1.Text) + "'";
                OleDbDataAdapter ada = new OleDbDataAdapter(sql_height, conn_access);
                DataTable dt = new DataTable();
                ada.Fill(dt);
                try
                {
                    DR = DT.NewRow();
                    DR["测期"] = Convert.ToString(sheet_name[i]);
                    DR["高程值"] = Convert.ToString(dt.Rows[0][0]);
                    DT.Rows.Add(DR);
                }
                catch
                {
                    continue;
                }
            }
            conn_access.Close();
            dataGridView1.Refresh();
            dataGridView1.DataSource = DT;

            //利用chart控件将表格信息可视化
            chart1.DataSource = DT;
            chart1.Series[0].ChartType = SeriesChartType.Line;
            chart1.Series[0].Points.DataBindXY(DT.AsDataView(), "测期", DT.AsDataView(), "高程值");

            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == null)
            {
                MessageBox.Show("请先选择监测点！");
                return;
            }
            if (textBox_k == null)
            {
                MessageBox.Show("请输入k值！");
                return;
            }
            //计算B矩阵
            double[] x_0 = new double[DT.Rows.Count];
            //获取原始序列x_0
            for (int i = 0; i < x_0.Length; i++)
            {
                x_0[i] = Convert.ToDouble(DT.Rows[i][1]);
            }
            //x_1
            double[] x_1 = new double[x_0.Length];
            double sum = 0;
            for (int i = 0; i < x_1.Length; i++)
            {
                sum += x_0[i];
                x_1[i] = sum;
            }
            //B
            double[,] B = new double[x_1.Length - 1, 2];
            for (int i = 0; i < x_1.Length - 1; i++)
            {
                B[i, 0] = -0.5 * (x_1[i + 1] + x_1[i]);
                B[i, 1] = 1;
            }
            //B_T即矩阵B的转置矩阵
            double[,] B_T = new double[B.GetLength(1), B.GetLength(0)];
            Matrix_Operation.MatrixInver(B, ref B_T);
            //YN
            double[,] YN = new double[x_1.Length - 1, 1];
            for (int i = 0; i < x_1.Length - 1; i++)
            {
                YN[i, 0] = x_0[i + 1];
            }
            //计算求得a和u
            double[,] tempmatrix0;
            double[,] tempmatrix1;
            tempmatrix0 = new double[B_T.GetLength(0), B.GetLength(1)];
            Matrix_Operation.MatrixMultiply(B_T, B, ref tempmatrix0);
            tempmatrix1 = new double[tempmatrix0.GetLength(0), tempmatrix0.GetLength(0)];
            Matrix_Operation.MatrixOpp(tempmatrix0, ref tempmatrix1);
            tempmatrix0 = new double[tempmatrix1.GetLength(0), B_T.GetLength(1)];
            Matrix_Operation.MatrixMultiply(tempmatrix1, B_T, ref tempmatrix0);
            tempmatrix1 = new double[tempmatrix0.GetLength(0), YN.GetLength(1)];
            Matrix_Operation.MatrixMultiply(tempmatrix0, YN, ref tempmatrix1);
            double a = tempmatrix1[0, 0];
            double u = tempmatrix1[1, 0];
            double E = 2.718281828459;
            //计算模拟值
            double x_predicted = (1 - Math.Pow(E, a)) * (x_0[0] - u / a) * Math.Pow(E, (-a * Convert.ToDouble(textBox_k.Text)));
            double[] x_simulated = new double[x_0.Length];
            for (int i = 0; i < x_simulated.Length; i++)
            {
                x_simulated[i] = (1 - Math.Pow(E, a)) * (x_0[0] - u / a) * Math.Pow(E, (-a * (i + 1)));
            }

            //进行后验差检验
            //计算残差
            double[] ee = new double[x_0.Length];
            for (int i = 0; i < ee.Length; i++)
            {
                ee[i] = x_0[i] - x_simulated[i];
            }
            double sum0 = 0;
            for (int i = 0; i < x_0.Length; i++)
            {
                sum0 += x_0[i];
            }
            double x_0_ave = sum0 / x_0.Length;
            double sum1 = 0;
            for (int i = 0; i < x_0.Length; i++)
            {
                sum1 += (x_0[i] - x_0_ave) * (x_0[i] - x_0_ave);
            }
            double S1 = Math.Sqrt(sum1 / Convert.ToDouble(x_0.Length));
            double sum2 = 0;
            double sum3 = 0;
            for (int i = 0; i < ee.Length; i++)
            {
                sum2 += ee[i];
            }
            double ee_ave = sum2 / ee.Length;
            for (int i = 0; i < ee.Length; i++)
            {
                sum3 += (ee[i] - ee_ave) * (ee[i] - ee_ave);
            }
            double S2 = Math.Sqrt(sum3 / Convert.ToDouble(ee.Length));
            double C = S2 / S1;
            //计算小误差概率P
            double p = 0;
            for (int i = 0; i < ee.Length; i++)
            {
                if (ee[i] < (0.6745 * S1))
                {
                    p++;
                }
                else
                {
                    continue;
                }
            }
            double P = p / Convert.ToDouble(ee.Length);
            textBox_a.Text = Convert.ToString(a);
            textBox_u.Text = Convert.ToString(u);
            textBox_result.Text = Convert.ToString(x_predicted);
            textBox_C.Text = Convert.ToString(C);
            textBox_P.Text = Convert.ToString(P);
            //判断精度等级
            int C_rank = 0;
            if (C <= 0.35)
            {
                C_rank = 1;
            }
            if (C <= 0.5&&C>0.35)
            {
                C_rank = 2;
            }
            if (C <= 0.65&&C>0.5)
            {
                C_rank = 3;
            }
            if (C >0.65)
            {
                C_rank = 4;
            }

            int P_rank = 0;
            if (P >= 0.95)
            {
                P_rank = 1;
            }
            if (P >= 0.85&&P<0.95)
            {
                P_rank = 2;
            }
            if (P >= 0.7&&P<0.8)
            {
                P_rank = 3;
            }
            if (P <0.7)
            {
                P_rank = 4;
            }
            textBox_rank.Text = Convert.ToString(Math.Max(P_rank, C_rank));
        }
    }
}
