using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;

using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace SFR
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Capture capture; //zmienna potrzebna do uzyskania kanału live z kamerki
        private CascadeClassifier haarCascade; //zmienna detektora twarzy (przeszkolony na tysiacach ludzkich twarzy)
        DispatcherTimer timer;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                capture = new Capture();
            }
            catch(NullReferenceException exception)
            {
                MessageBox.Show(exception.Message);
            }
            
            string link = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            haarCascade = new CascadeClassifier(link + "/haarcascade_frontalface_default.xml"); //zestaw danych generowany z pliku
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1); //interwał 1 ms
                
        }

        void timer_Tick(object sender, EventArgs e)
        {
            using (var currentFrame = capture.QueryFrame().ToImage<Bgr, Byte>())
            {
                if (currentFrame != null)
                {
                    Image<Gray, Byte> grayFrame = currentFrame.Convert<Gray, byte>(); //konwertowanie do klatki w odcieniach szarości
                    //Parametry metody DetectMultiScale: 
                    //- grayscale image (grayFrame) - aktualny obrazek, z ktorego chcemy wykryc twarz.  
                    //- scale factor (współczynnik skali) - musi być większy niż 1.0. Im bliżej do 1.0, tym więcej czasu
                    //  zajmuje wykrycie twarzy, ale istnieje większa szansa, że znajdziemy twarz
                    //- minimum number of nearest neighbors - im większa liczba, tym mniej otrzymamy fałszywych pozytywów
                    //- max size (px) - pozostawic pusty 

                    var faces = haarCascade.DetectMultiScale(grayFrame, 1.1, 10, System.Drawing.Size.Empty); //aktualna detekcja twarzy

                    System.Drawing.Rectangle[] facesTab = haarCascade.DetectMultiScale(grayFrame, 1.1, 10, System.Drawing.Size.Empty); //tablica z wykrytymi twarzami
                    
                    foreach (var face in faces)
                    {
                        currentFrame.Draw(face, new Bgr(System.Drawing.Color.DarkBlue), 3); //podswietlenie twarzy za pomocą box'a rysowanego dookoła niej
                    }
                    countFacesLabel.Content = facesTab.Length.ToString(); //zliczanie twarzy
                }
                image.Source = Emgu.CV.WPF.BitmapSourceConvert.ToBitmapSource(currentFrame); //przekazanie obrazu na komponent Image
            }
            
        }

        private void captureButton_Click(object sender, RoutedEventArgs e)
        { 
            timer.Start();
        }

        private void stopCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            image.Source = null;
        }
    }
}
