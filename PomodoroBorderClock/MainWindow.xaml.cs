using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PomodoroBorderTimer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon notify_icon;

        public MainWindow()
        {
            InitializeComponent();

            notify_icon = new System.Windows.Forms.NotifyIcon();
            notify_icon.BalloonTipText = "Right-click for options.";
            notify_icon.BalloonTipTitle = "Pomodoro Border Timer";
            notify_icon.Text = "Pomodoro Border Timer";
            notify_icon.Icon = new System.Drawing.Icon("Pomodoro.ico");
            notify_icon.Click += Notify_icon_Click;
            notify_icon.Visible = true;
        }

        private void Notify_icon_Click(object sender, EventArgs e)
        {
            if ((e as System.Windows.Forms.MouseEventArgs).Button == System.Windows.Forms.MouseButtons.Right)
            {
                menu.IsOpen = true;
                menu.Dispatcher.BeginInvoke((Action) async delegate {
                    await Task.Delay(1500);
                    menu.IsOpen = false;
                });
            }
        }

        private void PomodoroBorderTimer_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            notify_icon.Dispose();
            notify_icon = null;
        }

        private void Restart_Timer_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
