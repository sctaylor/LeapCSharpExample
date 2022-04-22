using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LeapCSharp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        ControllerLeap clMyLeap;

        public MainWindow()
        {
            InitializeComponent();

            clMyLeap = new ControllerLeap();

            clMyLeap.MotionEvent += HandleMotionEvent;
        }

        ~MainWindow()
        {
            if (clMyLeap != null)
                clMyLeap.MotionEvent -= HandleMotionEvent;
        }



        void HandleMotionEvent(object sender, Controller.MotionEventArgs e)
        {
            tbValue.Text = e.msState.fValue[(int)Controller.Motions.mLeftRight].ToString();
        }
    }
}
