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
        private TimerInfo[] timer_sequence;
        private Color[] work_colors;
        private Color[] break_colors;
        private double width;
        private TimeSpan time_remaining;
        private DateTime then;
        private int current_timer_index = 0;

        private string GetCurrentStatus() {
            var type = timer_sequence[current_timer_index].type == TimerTypes.Work ? "Work" : "Break";
            var remaing =  time_remaining.ToString(@"mm\:ss");
            return $"{type}: {remaing} [{current_timer_index}]";
        }

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

            DataContext = this;
        }

        private void PomodoroBorderTimer_Loaded(object sender, RoutedEventArgs e)
        {
            // Set everything up
            var timer_sequence_str = ConfigurationManager.AppSettings.Get("TimerSequence");
            timer_sequence = ParseTimerSequence(timer_sequence_str);
            var work_colors_str = ConfigurationManager.AppSettings.Get("WorkColors");
            work_colors = ParseColors(work_colors_str);
            var break_colors_str = ConfigurationManager.AppSettings.Get("BreakColors");
            break_colors = ParseColors(break_colors_str);
            var outline_width_str = ConfigurationManager.AppSettings.Get("OutlineWidth");
            width = double.Parse(outline_width_str);
            SetWidth(width);

            // where to Start
            then = DateTime.Now;
            time_remaining = timer_sequence[0].time;

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

        private Color[] ParseColors(string colors)
        {
            List<Color> result = new List<Color>();
            const string pattern = @"(rgba?)\((\d{1,3}), ?(\d{1,3}), ?(\d{1,3})(?:, ?(\d{1,3}))?\)";
            Match m = Regex.Match(colors, pattern);
            while (m.Success)
            {
                var type = m.Groups[1].Value;
                var r = byte.Parse(m.Groups[2].Value);
                var g = byte.Parse(m.Groups[3].Value);
                var b = byte.Parse(m.Groups[4].Value);
                if (type == "rgb")
                    result.Add(Color.FromRgb(r, g, b));
                else
                {
                    var a = byte.Parse(m.Groups[5].Value);
                    result.Add(Color.FromArgb(a, r, g, b));
                }
                m = m.NextMatch();
            }
            return result.ToArray();
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
            outline.InvalidateVisual();
        }

        private void SetWidth(double width)
        {
            outline.BorderThickness = new Thickness(width, width, width, 0);
            outline.InvalidateVisual();
        }

        private void Dispatcher_timer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            var diff = now - then;
            then = now;
            time_remaining -= diff;
            if (time_remaining.TotalSeconds <= 0)
            {
                current_timer_index = ++current_timer_index % timer_sequence.Length;
                time_remaining = timer_sequence[current_timer_index].time;
                outline.Dispatcher.BeginInvoke((Action)async delegate
                {
                    const int enlarge_to = 5;
                    for (int i = 1; i < enlarge_to; i++)
                    {
                        SetWidth(width * i);
                        await Task.Delay(120);
                    }
                    for (int i = enlarge_to; i > 0; i--)
                    {
                        SetWidth(width * i);
                        await Task.Delay(120);
                    }
                });
            }
            else
            {
                Color c = default(Color);
                var info = timer_sequence[current_timer_index];
                var color_arr = info.type == TimerTypes.Work ? work_colors : break_colors;

                var part_complete = (info.time.TotalSeconds - time_remaining.TotalSeconds) / info.time.TotalSeconds;
                var color_split = color_arr.Length - 1;
                if (color_split > 0)
                {
                    var split_part = color_split * part_complete;
                    var lower_index = (int)split_part;
                    var color_diff = split_part - lower_index;
                    var fadeout_color = Color.Multiply(color_arr[lower_index], 1 - (float)color_diff);
                    var fadein_color = Color.Multiply(color_arr[lower_index+1], (float)color_diff);
                    c = Color.Add(fadeout_color, fadein_color);
                }
                else if (color_split == 0)
                {
                    c = color_arr[0];
                }
                SetColor(c);
            }
        }

        private void Notify_icon_Click(object sender, EventArgs e)
        {
            if ((e as System.Windows.Forms.MouseEventArgs).Button == System.Windows.Forms.MouseButtons.Right)
            {
                label.Content = GetCurrentStatus();
                menu.IsOpen = true;
                menu.Dispatcher.BeginInvoke((Action)async delegate
                {
                    await Task.Delay(5000);
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
            current_timer_index = 0;
            time_remaining = timer_sequence[current_timer_index].time;
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (dispatcher_timer.IsEnabled)
            {
                dispatcher_timer.Stop();
                PlayPause.Header = "Start Timer";
            }
            else
            {
                dispatcher_timer.Start();
                PlayPause.Header = "Pause Timer";
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            current_timer_index = ++current_timer_index % timer_sequence.Length;
            time_remaining = timer_sequence[current_timer_index].time;
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            current_timer_index = (--current_timer_index + timer_sequence.Length) % timer_sequence.Length;
            time_remaining = timer_sequence[current_timer_index].time;
        }
    }
}
