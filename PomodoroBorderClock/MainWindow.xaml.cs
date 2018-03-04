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
using System.Configuration;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace PomodoroBorderTimer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon notify_icon;
        private DispatcherTimer dispatcher_timer = new DispatcherTimer();
        private TimerInfo[] timer_info;

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

        private void PomodoroBorderTimer_Loaded(object sender, RoutedEventArgs e)
        {
            // Set everything up
            var timer_sequence = ConfigurationManager.AppSettings.Get("TimerSequence");
            timer_info = ParseTimerSequence(timer_sequence);
            var work_colors = ConfigurationManager.AppSettings.Get("WorkColors");
            var break_colors = ConfigurationManager.AppSettings.Get("BreakColors");

            // Start the timer
            dispatcher_timer.Tick += Dispatcher_timer_Tick;
            dispatcher_timer.Interval = new TimeSpan(0, 0, 1);
            dispatcher_timer.Start();
        }

        public enum TimerTypes { Work, Break };

        public struct TimerInfo
        {
            public TimerTypes type;
            public TimeSpan time;
        }

        private TimerInfo[] ParseTimerSequence(string sequence)
        {
            const string pattern = @"(\);)|([WB]:\d+)(?:, ?)?|(\d+)x\(";
            Match m = Regex.Match(sequence, pattern);
            Stack<KeyValuePair<int, List<TimerInfo>>> stack = new Stack<KeyValuePair<int, List<TimerInfo>>>();
            List<TimerInfo> cur = new List<TimerInfo>();
            while (m.Success)
            {
                if (m.Groups[1].Value != string.Empty)
                {
                    // a repeat group was ended
                    var temp = stack.Pop();
                    var next_cur = temp.Value;
                    for (int i = 0; i < temp.Key; i++)
                        next_cur.AddRange(cur);
                    cur = next_cur;
                }
                else if (m.Groups[2].Value != string.Empty)
                {
                    var val = m.Groups[2].Value;
                    var type = val[0] == 'W' ? TimerTypes.Work : TimerTypes.Break;
                    var min = int.Parse(val.Substring(2));
                    // a regular value was found
                    cur.Add(new TimerInfo()
                    {
                        type = type,
                        time = new TimeSpan(0, min, 0)
                    });
                }
                else if (m.Groups[3].Value != string.Empty)
                {
                    var times = int.Parse(m.Groups[3].Value);
                    stack.Push(new KeyValuePair<int, List<TimerInfo>>(times, cur));
                    cur = new List<TimerInfo>();
                }
                else
                {
                    Debug.Assert(false, "We should never get here");
                }
                m = m.NextMatch();
            }
            Debug.Assert(stack.Count == 0, "The stack should be empty");
            return cur.ToArray();
        }

        private void SetColor(Color c)
        {
            outline.BorderBrush = new SolidColorBrush(c);
        }

        private void Dispatcher_timer_Tick(object sender, EventArgs e)
        {
            // update
        }

        private void Notify_icon_Click(object sender, EventArgs e)
        {
            if ((e as System.Windows.Forms.MouseEventArgs).Button == System.Windows.Forms.MouseButtons.Right)
            {
                menu.IsOpen = true;
                menu.Dispatcher.BeginInvoke((Action)async delegate
                {
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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Restart_Timer_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (dispatcher_timer.IsEnabled)
                dispatcher_timer.Stop();
            else
                dispatcher_timer.Start();
        }
    }
}
