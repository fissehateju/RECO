using RR.Dataplane;
using RR.Dataplane.NOS;
using System.Collections.Generic;
using System.Windows;

namespace RR.ui
{
    /// <summary>
    /// Interaction logic for UiRecievedPackertsBySink.xaml
    /// </summary>
    public partial class UiRecievedPackertsBySink : Window
    {
       
        public UiRecievedPackertsBySink(List<Packet> MyLiST)
        {
            InitializeComponent();
            List<Packet> packets = new List<Packet>();
            packets.AddRange(MyLiST);
            dg_packets.ItemsSource = packets;
        }
    }
    
}
