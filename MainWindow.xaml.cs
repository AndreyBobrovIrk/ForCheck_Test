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
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.ComponentModel;

namespace MyVideoPlayer
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    public MainWindow()
    {
      InitializeComponent();

      m_bookMarkPanel.Width = new GridLength(0);

      m_seekTimer.Interval = TimeSpan.FromMilliseconds(10);

      m_seekTimer.Tick += (o, e1) => {
        UpdatePosition();
        m_seekTimer.Stop();
      };

      m_lbBookmarks.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", System.ComponentModel.ListSortDirection.Ascending));

      m_slider.AddHandler(Slider.PreviewMouseDownEvent, new MouseButtonEventHandler(m_slider_PreviewMouseDown), true);
      m_slider.AddHandler(Slider.PreviewMouseUpEvent, new MouseButtonEventHandler(m_slider_PreviewMouseUp), true);
      m_slider.AddHandler(Slider.MouseMoveEvent, new MouseEventHandler(m_slider_PreviewMouseMove), true);
    }

    System.Windows.Threading.DispatcherTimer m_seekTimer = new System.Windows.Threading.DispatcherTimer();
    System.Windows.Threading.DispatcherTimer m_progressTimer = new System.Windows.Threading.DispatcherTimer();

    PlayerState m_state = PlayerState.Stop;
    PlayerState m_proviouseState = PlayerState.Stop;
    private enum PlayerState
    {
      Play,
      Pause,
      Stop,
      Seek
    }

    public ObservableCollection<TimeSpan> Bookmarks { get; set; } = new ObservableCollection<TimeSpan>();

    public IOrderedEnumerable<TimeSpan> BookmarksOrdered {
      get { 
        return Bookmarks.OrderBy(o => o.Ticks);
      }
    }

    private void SaveState()
    {
      m_proviouseState = m_state;
    }
    private void ResumeState()
    {
      SetState(m_proviouseState);
    }
    private void SetState(PlayerState a_state)
    {
      SaveState();
      m_state = a_state;

      switch (m_state)
      {
        case PlayerState.Play:
          {

            if(m_player.Position.Ticks != 0 && (
              m_player.NaturalDuration == Duration.Automatic || m_player.Position == m_player.NaturalDuration.TimeSpan)
            ) {
              m_player.Position = TimeSpan.Zero;
              System.Threading.Thread.Sleep(10);
            }

            m_player.Play();
            m_progressTimer.Start();
            m_btnPlayPause.Content = "Pause";
          }
          break;
        case PlayerState.Pause:
          {
            m_player.Pause();
            m_progressTimer.Stop();
            m_btnPlayPause.Content = "Play";
          }
          break;
        case PlayerState.Stop:
          {
            m_progressTimer.Stop();
            m_player.Position = TimeSpan.Zero;
            System.Threading.Thread.Sleep(10);
            m_player.Stop();
            m_btnPlayPause.Content = "Play";
            UpdateProgressBar();
          }
          break;
        case PlayerState.Seek:
          {
            m_player.Pause();
            m_progressTimer.Stop();
          }
          break;
      }
    }

    private void m_btnPlayPause_Click(object sender, RoutedEventArgs e)
    {
      SetState(m_state == PlayerState.Play ? PlayerState.Pause : PlayerState.Play);
    }

    private void m_btnStop_Click(object sender, RoutedEventArgs e)
    {
      SetState(PlayerState.Stop);
    }

    private void m_btnBegin_Click(object sender, RoutedEventArgs e)
    {
      m_player.Position = new TimeSpan();
      UpdateProgressBar();
    }

    private void m_btnEnd_Click(object sender, RoutedEventArgs e)
    {
      m_player.Position = m_player.NaturalDuration.TimeSpan;
      UpdateProgressBar();
    }

    private void UpdateProgressBar()
    {
      m_slider.Value = m_player.Position.TotalMilliseconds;
    }

    private String GetProgressText()
    {
      String res = @"00:00:00";
      if (m_player.NaturalDuration.HasTimeSpan) {
        res = String.Format(
          "{0}/{1}",
          m_player.Position.ToString(@"hh\:mm\:ss"),
          m_player.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss")
        );
      }
      return res;
    }
    private void m_player_MediaOpened(object sender, RoutedEventArgs e)
    {
      m_progress.Content = GetProgressText();
      m_slider.Maximum = m_player.NaturalDuration.TimeSpan.TotalMilliseconds;
      m_progressTimer.Interval = TimeSpan.FromMilliseconds(10);
      m_progressTimer.Start();
      m_progressTimer.Tick += (o, e1) => {
        UpdateProgressBar();
      };

      IsFileOpened = m_player.HasVideo;
    }

    private bool m_isFileOpened = false;
    public bool IsFileOpened
    {
      get
      {
        return m_isFileOpened;
      }
      private set
      {
        m_isFileOpened = value;
        OnPropertyChanged("IsFileOpened");
      }
    }

    public bool HasBookmarks
    {
      get
      {
        return Bookmarks.Count > 0;
      }
    }

    private void UpdatePosition()
    {
      m_player.Position = new TimeSpan((long)m_slider.Value * 10000);
    }

    private void AddBookMark(TimeSpan a_timeSpan)
    {
      Bookmarks.Add(a_timeSpan);
      m_lbBookmarks.SelectedItem = a_timeSpan;
      OnPropertyChanged("HasBookmarks");
    }

    private void RemoveBookmark()
    {
      if (m_lbBookmarks.SelectedItem == null)
      {
        return;
      }

      int index = m_lbBookmarks.SelectedIndex;
      Bookmarks.Remove((TimeSpan)m_lbBookmarks.SelectedItem);
      if (m_lbBookmarks.Items.Count > 0)
      {
        m_lbBookmarks.SelectedIndex = index < m_lbBookmarks.Items.Count ? index : index - 1;
      }
      OnPropertyChanged("HasBookmarks");
    }

    private void m_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      m_progress.Content = GetProgressText();

      if (m_state == PlayerState.Play)
      {
        return;
      }

      m_seekTimer.Start();
    }

    private void m_slider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if(m_state == PlayerState.Stop)
      {
        m_state = PlayerState.Pause;
      }

      SaveState();
      m_seekTimer.Stop();
      SetState(PlayerState.Seek);
      UpdatePosition();
      m_slider.CaptureMouse();
    }

    private void m_slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
      ResumeState();

      m_slider.ReleaseMouseCapture();
    }

    private void m_player_MediaEnded(object sender, RoutedEventArgs e)
    {
      SetState(PlayerState.Pause);
    }

    private void m_slider_PreviewMouseMove(object sender, MouseEventArgs e)
    {
      if (e.LeftButton == MouseButtonState.Pressed)
      {
        Point position = e.GetPosition(m_slider);
        double d = 1.0d / m_slider.ActualWidth * position.X;
        var p = m_slider.Maximum * d;
        m_slider.Value = p;
      }
    }

    private void m_btnBookMarks_Click(object sender, RoutedEventArgs e)
    {
      if (Grid1.ColumnDefinitions[1].ActualWidth == 0) {
        m_bookMarkPanel.Width = new GridLength(200);
        m_btnBookMarks.Content = "Bookmarks <<";
      } else {
        m_bookMarkPanel.Width = new GridLength(0);
        m_btnBookMarks.Content = "Bookmarks >>";
      }
    }

    private void m_btnNextBookmark_Click(object sender, RoutedEventArgs e)
    {
      if(Bookmarks.Count == 0)
      {
        return;
      }

      var bookmark = BookmarksOrdered.FirstOrDefault(o=> o > m_player.Position);
      if(bookmark == TimeSpan.Zero)
      {
        bookmark = BookmarksOrdered.First();
      }

      GoToBookmark(bookmark);
    }

    private void m_btnPreviousBookmark_Click(object sender, RoutedEventArgs e)
    {
      if (Bookmarks.Count == 0)
      {
        return;
      }

      var bookmark = BookmarksOrdered.LastOrDefault(o => o < m_player.Position);
      if (bookmark == TimeSpan.Zero)
      {
        bookmark = BookmarksOrdered.Last();
      }

      GoToBookmark(bookmark);
    }

    void GoToBookmark(TimeSpan a_bookMark)
    {
      m_player.Position = a_bookMark;
      m_slider.Value = a_bookMark.TotalMilliseconds;
    }

    private void m_lbBookmarks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      GoToBookmark((TimeSpan)m_lbBookmarks.SelectedItem);
    }

    private void m_btnRemoveBookMark_Click(object sender, RoutedEventArgs e)
    {
      RemoveBookmark();
    }

    private void m_btnAddBookmark_Click(object sender, RoutedEventArgs e)
    {
      AddBookMark(new TimeSpan(m_player.Position.Hours, m_player.Position.Minutes, m_player.Position.Seconds));
    }

    private void MenuItem_CloseClick(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private void MenuItem_OpenClick(object sender, RoutedEventArgs e)
    {
      OpenFileDialog openFileDialog = new OpenFileDialog();
      openFileDialog.Filter = "mp4 video files|*.mp4";
      if (openFileDialog.ShowDialog() == true)
      {
        m_player.Close();
        m_player.Source = new Uri(openFileDialog.FileName);
        Bookmarks.Clear();
        SetState(PlayerState.Play);
      }
    }

    private void MenuItem_AboutClick(object sender, RoutedEventArgs e)
    {
      MessageBox.Show("Test Task for Forcheck Company.", "My Video Player");
    }

    private void RemoveBookmarkItem_Click(object sender, RoutedEventArgs e)
    {
      RemoveBookmark();
    }

    private void AddBookmarkItem_Click(object sender, RoutedEventArgs e)
    {
      var range = m_slider.Maximum - m_slider.Minimum;
      double percentage = ((double)m_sliderPoint.X / m_slider.Width) * 100;
      var res = ((percentage / 100) * range) + m_slider.Minimum;
      TimeSpan timeSpan = new TimeSpan((long)res * 10000);
      AddBookMark(new TimeSpan(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds));
    }

    Point m_sliderPoint;

    private void m_slider_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
      m_sliderPoint = e.GetPosition(m_slider);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged(string a_name)
    {
      if (PropertyChanged != null) {
        PropertyChanged(this, new PropertyChangedEventArgs(a_name));
      }
    }

    private void m_volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      if (IsFileOpened)
      {
        m_player.Volume = m_volume.Value / 100;
        m_lblVolume.Content = String.Format("{0}%", Math.Round(m_volume.Value));
      }
    }

  }
}
