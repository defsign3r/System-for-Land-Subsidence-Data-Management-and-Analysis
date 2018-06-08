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
using MySystem.Properties;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.GeoAnalyst;
using System.Threading;

namespace MySystem
{
    public partial class FormOfVisualize : Form
    {
        public FormOfVisualize()
        {
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            InitializeComponent();
            label2.Text = "";
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult dr = MessageBox.Show("是否退出?", "提示:", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

            if (dr == DialogResult.OK)   //如果单击“是”按钮
            {
                //关闭此窗口，显示上一层窗口
                MySystem.Form1.ActiveForm.Show();

            }
            else if (dr == DialogResult.Cancel)
            {
                e.Cancel = true;//不执行操作
            }
        }
        public string database_path;
        private void button1_Click(object sender, EventArgs e)
        {
            label2.Text = "正在生成shp文件...";
            OleDbConnection conn_access = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;" + "Data Source=" + database_path + ";" + "Persist Security Info=False;");
            conn_access.Open();
            //获取表名
            string sql_sheet = "select * from["+comboBox1.Text+"]";
            OleDbDataAdapter ada = new OleDbDataAdapter(sql_sheet, conn_access);
            DataTable dt = new DataTable();
            ada.Fill(dt);
            string strShapeFolder = "E:\\毕业设计\\txttoshp";
            string strShapeFile = "myshp.shp";
            string shapeFileName = strShapeFolder + strShapeFile;
            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();
            IFeatureWorkspace pFeatureWorkspace = (IFeatureWorkspace)pWorkspaceFactory.OpenFromFile(strShapeFolder, 0);

            IFeatureClass pFeatureClass;

            IPoint pPoint;
            IFields pFields = new Fields();
            IFieldsEdit pFieldsEdit = (IFieldsEdit)pFields;
            IField pField = new Field();
            IFieldEdit pFieldEdit = (IFieldEdit)pField;
            ISpatialReferenceFactory spatialReferenceFactory = new SpatialReferenceEnvironment();
            IGeographicCoordinateSystem pGCS;
            pGCS = spatialReferenceFactory.CreateGeographicCoordinateSystem
                ((int)esriSRGeoCS2Type.esriSRGeoCS_AuthalicSphere_GRS1980);
            pFieldEdit.Name_2 = "SHAPE";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;

            IGeometryDefEdit pGeoDef = new GeometryDef() as IGeometryDefEdit;
            IGeometryDefEdit pGeoDefEdit = (IGeometryDefEdit)pGeoDef;
            pGeoDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPoint;
            pGeoDefEdit.SpatialReference_2 = pGCS;
            pFieldsEdit.AddField(pField);
            pFieldEdit.GeometryDef_2 = pGeoDef;
            //添加字段
            #region
            pField = new Field();
            pFieldEdit = (IFieldEdit)pField;
            pFieldEdit.Name_2 = "X坐标值";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField);

            pField = new Field();
            pFieldEdit = (IFieldEdit)pField;
            pFieldEdit.Name_2 = "Y坐标值";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField);

            pField = new Field();
            pFieldEdit = (IFieldEdit)pField;
            pFieldEdit.Name_2 = "高程";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeDouble;
            pFieldsEdit.AddField(pField);
            #endregion
            //创建shp
            #region
            try
            {
                pFeatureClass = pFeatureWorkspace.CreateFeatureClass(strShapeFile, pFields, null, null, esriFeatureType.esriFTSimple, "SHAPE", "");
            }
            catch
            {
                pFeatureClass = pFeatureWorkspace.CreateFeatureClass(strShapeFile, pFields, null, null, esriFeatureType.esriFTSimple, "SHAPE", "");
            }

            progressBar1.Value = 0;
            progressBar1.Maximum = dt.Rows.Count;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                pPoint = new ESRI.ArcGIS.Geometry.Point();
                pPoint.X = Convert.ToDouble(dt.Rows[i][1]);
                pPoint.Y = Convert.ToDouble(dt.Rows[i][2]);
                pPoint.Z = Convert.ToDouble(dt.Rows[i][3]);
                IFeature pFeature = pFeatureClass.CreateFeature();
                pFeature.Shape = pPoint;
                pFeature.set_Value(pFeature.Fields.FindField("X坐标值"), dt.Rows[i][1]);
                pFeature.set_Value(pFeature.Fields.FindField("Y坐标值"), dt.Rows[i][2]);
                pFeature.set_Value(pFeature.Fields.FindField("高程"), dt.Rows[i][3]);
                pFeature.Store();
                progressBar1.Value++;
            }

            IFeatureLayer pFeaturelayer = new FeatureLayer();
            pFeaturelayer.FeatureClass = pFeatureClass;
            pFeaturelayer.Name = "layer";
            axMapControl1.Map.AddLayer(pFeaturelayer);
            #endregion
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //通过IDW插值生成栅格图层
            #region
            label2.Text = "正在进行IDW插值";
            IFeatureLayer pFeatureLayer_point = axMapControl1.Map.Layer[0] as IFeatureLayer;//获取点图层
            IRasterRadius pRadius = new RasterRadiusClass();
            object missing = Type.Missing;
            pRadius.SetVariable(12, ref missing);
            IFeatureClassDescriptor pFCDescriptor = new FeatureClassDescriptorClass();
            pFCDescriptor.Create(pFeatureLayer_point.FeatureClass, null, "高程");

            object cellSizeProvider = 185.244192;
            IInterpolationOp pInterpolationOp = new RasterInterpolationOpClass();
            IRasterAnalysisEnvironment pEnv = pInterpolationOp as IRasterAnalysisEnvironment;
            pEnv.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, ref  cellSizeProvider);
            IRaster pOutRaster;
            
            try
            {
                pOutRaster = pInterpolationOp.IDW(pFCDescriptor as IGeoDataset, 2, pRadius, ref missing) as IRaster;
            }
            catch
            {
                pOutRaster = pInterpolationOp.IDW(pFCDescriptor as IGeoDataset, 2, pRadius, ref missing) as IRaster;
            }

            //Add output into ArcMap as a raster layer    
            RasterLayer pOutRasLayer = new RasterLayerClass();
            pOutRasLayer.CreateFromRaster(pOutRaster);
            pOutRasLayer.Name = "栅格";
            axMapControl1.Map.AddLayer(pOutRasLayer);
            #endregion
            //提取等值线
            #region
            label2.Text = "正在生成等值线...";
            IGeoDataset pGeoDataSet = pOutRaster as IGeoDataset;
            IWorkspaceFactory pWorkspaceFactory1 = new ShapefileWorkspaceFactory();
            string file_path = System.IO.Path.GetDirectoryName(database_path);
            IWorkspace pShpWorkspace = pWorkspaceFactory1.OpenFromFile(file_path, 0);
            ISurfaceOp2 pSurfaceOp2 = new RasterSurfaceOpClass();
            IRasterAnalysisEnvironment pRasterAnalysisEnvironment = pSurfaceOp2 as IRasterAnalysisEnvironment;

            pRasterAnalysisEnvironment.Reset();
            pRasterAnalysisEnvironment.SetCellSize(esriRasterEnvSettingEnum.esriRasterEnvValue, ref cellSizeProvider);
            pRasterAnalysisEnvironment.OutWorkspace = pShpWorkspace;
            double dInterval = 0.8;  //间距 
            IGeoDataset pOutputDataSet = pSurfaceOp2.Contour(pGeoDataSet, dInterval, ref missing, ref missing);

            IFeatureClass pFeatureClass1 = pOutputDataSet as IFeatureClass;
            IFeatureLayer pFeatureLayer = new FeatureLayerClass();
            pFeatureLayer.FeatureClass = pFeatureClass1;

            IGeoFeatureLayer pGeoFeatureLayer = pFeatureLayer as IGeoFeatureLayer;
            pGeoFeatureLayer.DisplayAnnotation = true;
            pGeoFeatureLayer.DisplayField = "Contour";
            pGeoFeatureLayer.Name = "高程等值线";
            axMapControl1.Map.AddLayer(pGeoFeatureLayer);
            label2.Text = "完毕";
            #endregion
        }
    }
}
