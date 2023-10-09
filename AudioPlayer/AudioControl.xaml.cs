using Microsoft.Win32;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AudioPlayer
{
    /// <summary>
    /// Interaction logic for AudioControl.xaml
    /// </summary>
    /// 

    //https://stackoverflow.com/questions/4263237/wpf-inputbinding-ctrlmwheelup-down-possible Mouse wheel for vol slider
    //https://itecnote.com/tecnote/c-loading-album-art-with-taglib-sharp-and-then-saving-it-to-same-different-file-in-c-causes-memory-error/
    public partial class AudioControl : UserControl
    {
        private TagLib.File tagFile;
        private string title;
        private string artist;
        private string album;
        private string genre;
        private uint year;
        private BitmapImage albumCover;

        public BitmapImage getAlbumCover()
        {
            return albumCover;
        }

        public TagLib.File getTagFile()
        {
            return tagFile;
        }


        public AudioControl()
        {
            InitializeComponent();
        }

        public AudioControl(string filename)
        {
            InitializeComponent();
            tagFile = TagLib.File.Create(filename);
            setTagText();
            try
            {
                albumCover = retrieveCover();
                AlbumCover.Source = albumCover;
            }
            catch
            {
                return;
            }
        }

        public void setTagText()
        {
            title = tagFile.Tag.Title;
            artist = tagFile.Tag.FirstPerformer;
            album = tagFile.Tag.Album;
            genre = tagFile.Tag.FirstGenre;
            year = tagFile.Tag.Year;

            TextBlock_Title.Text = title;
            TextBlock_Artist.Text = artist;
            TextBlock_Album.Text = album;
            TextBlock_Genre.Text = genre;
            TextBlock_Year.Text = year.ToString();
        }

        private BitmapImage retrieveCover()
        {
            // Populates the albumCover member from the tag file's picture data.
            MemoryStream memStream = new MemoryStream(tagFile.Tag.Pictures[0].Data.Data);

            BitmapImage cover = new BitmapImage();
            cover.BeginInit();
            cover.StreamSource = memStream;
            cover.EndInit();
            return cover;
        }
    }
}
