using Microsoft.Win32;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.Linq;
using System.Printing;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TagLib;

namespace AudioPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
   
    public partial class MainWindow : Window
    {
        private MediaPlayer songPlayer = new MediaPlayer();
        private bool isPlaying = false;
        private bool seekHeld = false;
        private string filepath = string.Empty;
        private string tempFilepath = string.Empty;
        private DispatcherTimer posTimer;
        private bool mediaOpened = false; // Used to prevent a specific crash.

        public MainWindow()
        {
            InitializeComponent();
            posTimer = new DispatcherTimer();
            posTimer.Tick += new EventHandler(posTimer_Tick);
            posTimer.Interval = TimeSpan.FromSeconds(1);
            posTimer.IsEnabled = false; // Should only be enabled when a song is loaded.
            posTimer.Start();
        }

        private void MenuItem_Edit_Click(object sender, RoutedEventArgs e)
        {
            if (NowPlaying == null || NowPlaying.Children.Count == 0)
            {
                return;
            }
            AudioControl currentSong = NowPlaying.Children.OfType<AudioControl>().FirstOrDefault();
            TagEdit tagEdit = new TagEdit(currentSong); // Takes the whole AudioControl so it can both access the tag file and call a function in the AudioControl
                                                        // to reflect the tag changes.
            tagEdit.Show();
        }

        private void Slider_Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (NowPlaying == null || NowPlaying.Children.Count == 0) // Must check for NowPlaying == null here or it breaks; does initializing the slider
                // count as changing the value, triggering this before NowPlaying exists? Weird, but this fixes it.
            {
                return;
            }
            songPlayer.Volume = (double)e.NewValue / 100; // mediaPlayer.Volume is a double 0-1, integer 1-100 is more intuitive for the slider.
        }

        private void Button_Mute_Click(object sender, RoutedEventArgs e)
        {
            if (songPlayer.IsMuted == true)
            {
                ImgMute.Source = (ImageSource)this.FindResource("Icon_Unmuted");
                songPlayer.IsMuted = false;
            } else
            {
                ImgMute.Source = (ImageSource)this.FindResource("Icon_Muted");
                songPlayer.IsMuted = true;
            }
        }


        private void TogglePlayPause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = songPlayer.HasAudio;
        }

        private void TogglePlayPause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!isPlaying)
            {
                songPlayer.Play();
                isPlaying = true;
                Img_PlayPause.Source = (ImageSource)this.FindResource("Icon_Pause");
            }
            else
            {
                songPlayer.Pause();
                isPlaying = false;
                Img_PlayPause.Source = (ImageSource)this.FindResource("Icon_Play");
                updatePosition(); // Stops the position from changing on a timer tick while paused, making it look like the song is still playing for a second.
            }
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = songPlayer.HasAudio;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            songPlayer.Stop();
            isPlaying = false;
            Img_PlayPause.Source = (ImageSource)this.FindResource("Icon_Play");
            updatePosition();
        }

        private void Slider_Seek_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            seekHeld = true;
        }

        private void Slider_Seek_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            double newPos = (double)Slider_Seek.Value / 100;
            try
            {
                songPlayer.Position = songPlayer.NaturalDuration.TimeSpan * newPos;
                TB_SeekPosition.Text = songPlayer.Position.ToString(@"hh\:mm\:ss");
            } catch (System.InvalidOperationException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            seekHeld = false;
            updatePosition();
        }


        private void posTimer_Tick(object? sender, EventArgs e)
        {
            if (!songPlayer.HasAudio)
            {
                return;
            }
            updatePosition();
        }

        private void MediaPlayer_MediaOpened(object? sender, EventArgs e)
        {
            // MediaOpened needs to run before NaturalDuration can be accessed; putting this in the open file method breaks.
            // Even putting it after .Play() breaks. It can play the song, but doesn't know how long it is because it hasn't been opened yet? What???
            // Based this solution on https://stackoverflow.com/questions/43803539/how-to-properly-wait-for-mediaplayer-to-open
            TB_Duration.Text = songPlayer.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss");
            mediaOpened = true;
            posTimer.IsEnabled = true;
        }

        private void MediaPlayer_MediaEnded(object? sender, EventArgs e)
        {
            // Return the position to 0 and restore the play button.
            songPlayer.Stop();
            isPlaying = false;
            Img_PlayPause.Source = (ImageSource)this.FindResource("Icon_Play");
            updatePosition();
        }

        private void updatePosition()
        {
            // Sets the position text box and the seek bar value to the current position.
            // Called every second by the timer, but also by other controls so it responds immediately.
            TB_SeekPosition.Text = songPlayer.Position.ToString(@"hh\:mm\:ss");

            if (seekHeld)
            {
                return;
            }

            try
            {
                // Precaution. Accessing NaturalDuration can cause a crash if the file hasn't been fully opened yet.
                // This will happen if the timer calls this after loading a new file.
                double posDouble = songPlayer.Position / songPlayer.NaturalDuration.TimeSpan;
                Slider_Seek.Value = posDouble * 100;
            } catch (System.InvalidOperationException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void clearCurrentSong()
        {   
            posTimer.IsEnabled = false;
            songPlayer.Stop();
            songPlayer.Close();
            isPlaying = false;
            mediaOpened = false;
            NowPlaying.Children.Clear();
            try
            {
                System.IO.File.Delete(tempFilepath); // Delete the temporary file so as not to flood the user's temp folder with MP3s.
            }
            catch
            {
                // Don't need to break here though; if the file can't be deleted for some reason, oh well.
            }
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnExit(object sender, EventArgs e)
        {
            System.IO.File.Delete(tempFilepath);
        }

        private void OpenFile_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenFile_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            if (openFile.ShowDialog() != true)
            {
                return;
            }

            if (songPlayer.HasAudio)
            {
                clearCurrentSong();
            }

            try
            {
                createTempMP3(openFile.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
                return;
            }

            AudioControl openAudio = new AudioControl(filepath); // This reads from the source file, but doesn't lock it.
            NowPlaying.Children.Add(openAudio);

            songPlayer.MediaOpened += MediaPlayer_MediaOpened;
            songPlayer.MediaEnded += MediaPlayer_MediaEnded;
            songPlayer.Open(new Uri(tempFilepath));
            songPlayer.Volume = (double)Slider_Volume.Value / 100;

            try
            {
                // Loads the album cover into the main window's background. If there's no cover, or if reading the tag data fails,
                // just returns the background to white.
                ImageBrush albumCover = new ImageBrush(openAudio.getAlbumCover());
                Grid_Main.Background = albumCover;
                Grid_Main.Background.Opacity = 0.2;
            }
            catch (System.IndexOutOfRangeException ex)
            {
                Grid_Main.Background = Brushes.White;
            }

            // Start playing the song upon loading.
            songPlayer.Play();
            isPlaying = true;
            Img_PlayPause.Source = (ImageSource)this.FindResource("Icon_Pause");
        }

        private void createTempMP3(string filepath)
        {
            //Creates a copy of the source file in the temp folder for the MediaPlayer to load without locking the source file.
            string tempFilepathTMP = System.IO.Path.GetTempFileName(); // Creates an empty .tmp file in the temp folder with a unique name.
            tempFilepath = tempFilepathTMP.Replace(".tmp", ".mp3"); // Stores the filename of the previous file but with a .mp3 extension.
                                                                    // The file doesn't exist yet.
            System.IO.File.Delete(tempFilepathTMP); // Deletes the empty .tmp file.
            System.IO.File.Copy(filepath, tempFilepath); // Copies the source into the temp folder.
        }
    }
}
