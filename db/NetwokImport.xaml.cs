using RR.Dataplane;
using RR.Properties;
using RR.ui;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace RR.db
{
    /// <summary>
    /// Interaction logic for NetwokImport.xaml
    /// </summary>
    public partial class NetwokImport : UserControl
    {
        public MainWindow MainWindow { set; get; }
        public List<ImportedSensor> ImportedSensorSensors = new List<ImportedSensor>();

        public UiImportTopology UiImportTopology { get; set; }
        public NetwokImport()
        {
            InitializeComponent();
        }

        public void Deploy(string name)
        {
            Settings.Default.NetworkName = name;
            Settings.Default.Save();
            PublicParamerters.SensingRangeRadius = ImportedSensorSensors[0].R;
            // set the size of the network:
            // area_300_m2
            string[] arr = Settings.Default.NetworkName.Split('_');
            //if (arr.Length == 3)
            //{
            //    PublicParamerters.NetworkSquareSideLength = System.Convert.ToDouble(arr[1]);
            //    MainWindow.Canvas_SensingFeild.Height = PublicParamerters.NetworkSquareSideLength;
            //    MainWindow.Canvas_SensingFeild.Width = PublicParamerters.NetworkSquareSideLength;
            //}
            //else
            //{
            //    PublicParamerters.NetworkSquareSideLength = 550;
            //    MainWindow.Canvas_SensingFeild.Height = PublicParamerters.NetworkSquareSideLength;
            //    MainWindow.Canvas_SensingFeild.Width = PublicParamerters.NetworkSquareSideLength;
            //}


            PublicParamerters.MainWindow.Canvas_SensingFeild.Width = System.Convert.ToDouble(arr[1]);
            MainWindow.Canvas_SensingFeild.Width = System.Convert.ToDouble(arr[1]);

            PublicParamerters.MainWindow.Canvas_SensingFeild.Height = System.Convert.ToDouble(arr[2]);
            MainWindow.Canvas_SensingFeild.Height = System.Convert.ToDouble(arr[2]);

            PublicParamerters.NetworkSquareSideLength = MainWindow.Canvas_SensingFeild.Height;

            foreach (ImportedSensor imsensor in ImportedSensorSensors)
            {
                Sensor node = new Sensor(imsensor.NodeID);
                node.MainWindow = MainWindow;
                Point p = new Point(imsensor.Pox, imsensor.Poy);
                node.Position = p;
                node.VisualizedRadius = imsensor.R;
                MainWindow.myNetWork.Add(node);
                MainWindow.Canvas_SensingFeild.Children.Add(node);


                node.ShowID(Settings.Default.ShowID);
                node.ShowSensingRange(Settings.Default.ShowSensingRange);
                node.ShowComunicationRange(Settings.Default.ShowComunicationRange);
                node.ShowBattery(Settings.Default.ShowBattry);
            }


            try
            {
                if (UiImportTopology != null)
                {
                    UiImportTopology.Close();
                }
            }
            catch
            {

            }
        }


        public void Deploy()
        {
            NetworkTopolgy.ImportNetwok(this);
            Settings.Default.NetworkName = lbl_network_name.Content.ToString();
            Settings.Default.Save();
            PublicParamerters.SensingRangeRadius = ImportedSensorSensors[0].R;
            // now add them to feild.


            // set the size of the network:
            // area_300_m2
            string[] arr = Settings.Default.NetworkName.Split('_');
            if (arr.Length == 3)
            {
                PublicParamerters.NetworkSquareSideLength = System.Convert.ToDouble(arr[1]);
                MainWindow.Canvas_SensingFeild.Height = PublicParamerters.NetworkSquareSideLength;
                MainWindow.Canvas_SensingFeild.Width = PublicParamerters.NetworkSquareSideLength;
            }
            else
            {
                PublicParamerters.NetworkSquareSideLength = 550;
                MainWindow.Canvas_SensingFeild.Height = PublicParamerters.NetworkSquareSideLength;
                MainWindow.Canvas_SensingFeild.Width = PublicParamerters.NetworkSquareSideLength;
            }


            //

            foreach (ImportedSensor imsensor in ImportedSensorSensors)
            {
                Sensor node = new Sensor(imsensor.NodeID);
                node.MainWindow = MainWindow;
                Point p = new Point(imsensor.Pox, imsensor.Poy);
                node.Position = p;
                node.VisualizedRadius = imsensor.R;
                MainWindow.myNetWork.Add(node);
                MainWindow.Canvas_SensingFeild.Children.Add(node);


                node.ShowID(Settings.Default.ShowID);
                node.ShowSensingRange(Settings.Default.ShowSensingRange);
                node.ShowComunicationRange(Settings.Default.ShowComunicationRange);
                node.ShowBattery(Settings.Default.ShowBattry);
            }


            try
            {
                if (UiImportTopology != null)
                {
                    UiImportTopology.Close();
                }
            }
            catch
            {

            }
        }

        private void brn_import_Click(object sender, RoutedEventArgs e)
        {


            Deploy();



        }
    }
}
