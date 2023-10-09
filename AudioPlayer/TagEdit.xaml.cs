using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Shapes;

namespace AudioPlayer
{
    /// <summary>
    /// Interaction logic for TagEdit.xaml
    /// </summary>
    public partial class TagEdit : Window
    {

        AudioControl currentSong;
        public TagEdit()
        {
            InitializeComponent();
        }

        public TagEdit(AudioControl ac)
        {
            InitializeComponent();
            // This seems like a lot of repetition, but these are formatted separately, i.e. Performers is an array and Year is a uint.
            currentSong = ac;
            TagLib.File tagFile = currentSong.getTagFile();
            TB_Title.Text = tagFile.Tag.Title;
            TB_Artist.Text = tagFile.Tag.FirstPerformer;
            TB_Album.Text = tagFile.Tag.Album;
            TB_Year.Text = tagFile.Tag.Year.ToString();
            TB_Genre.Text = tagFile.Tag.FirstGenre;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TagLib.File tagFile = currentSong.getTagFile();
                tagFile.Tag.Title = TB_Title.Text;
                tagFile.Tag.Performers = new string[1] { TB_Artist.Text }; // Setting Performers[0] by itself does not work.
                tagFile.Tag.Album = TB_Album.Text;
                uint yearInt;
                if (UInt32.TryParse(TB_Year.Text, out yearInt))
                {
                    tagFile.Tag.Year = yearInt;
                }
                tagFile.Tag.Genres = new string[1] { TB_Genre.Text };
                tagFile.Save();
                currentSong.setTagText();
                this.Close();

            } catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Unable to save tags. Access denied.\n" + ex, "Error");
            } catch (FileNotFoundException ex)
            {
                MessageBox.Show("Unable to save tags. File not found.\n" + ex, "Error");
            } catch (Exception ex)
            {
                MessageBox.Show("Unable to save tags.\n" + ex, "Error");
            }
            
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
