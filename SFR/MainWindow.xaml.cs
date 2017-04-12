﻿using System;
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

using DirectShowLib;

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
        Video_Device[] webCams;
        int cameraDevice = 0;
        int brightnessStore = 0;
        int contrastStore = 0;
        int sharpnessStore = 0;

        public MainWindow()
        {
            InitializeComponent();
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
            cameraSettings();
        }

        private void captureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                capture = new Capture();
            }
            catch (NullReferenceException exception)
            {
                MessageBox.Show(exception.Message);
            }

            string link = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            haarCascade = new CascadeClassifier(link + "/haarcascade_frontalface_default.xml"); //zestaw danych generowany z pliku
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1); //interwał 1 ms
            timer.Start();
            cameraInformation();
            capture.FlipHorizontal = !capture.FlipHorizontal; //obrot widoku kamery w poziomie
        }

        private void stopCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            countFacesLabel.Content = "";
            richTextBox.Document.Blocks.Clear();
            Reset_Cam_Settings_Click(null,null);
            timer.Stop();
            image.Source = null;
            capture.Dispose();
        }

        //Get camera information
        private void cameraInformation()
        {
            DsDevice[] systemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            webCams = new Video_Device[systemCameras.Length];
            for (int i = 0; i < systemCameras.Length; i++)
            {
                webCams[i] = new Video_Device(i, systemCameras[i].Name, systemCameras[i].ClassID); //fill web cam array
            }
            richTextBox.AppendText("Camera name: "+webCams[cameraDevice].Device_Name +"\n\n");
        }

        private void cameraSettings()
        {
            brightnessLabel.Content = ((int)brightnessSlider.Value).ToString();
            contrastLabel.Content = ((int)contrastSlider.Value).ToString();
            sharpnessLabel.Content = ((int)sharpnessSlider.Value).ToString();

            brightnessSlider.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Brightness);
            contrastSlider.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Contrast);
            sharpnessSlider.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Sharpness);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (capture != null)
            {
                Reset_Cam_Settings_Click(null, null);
            }
        }

        private void brightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (capture != null) capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Brightness, brightnessSlider.Value);
        }

        private void contrastSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (capture != null) capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Contrast, contrastSlider.Value);
        }

        private void sharpnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (capture != null) capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Sharpness, sharpnessSlider.Value);
        }

        private void RetrieveCaptureInformation()
        {           
           
            brightnessSlider.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Brightness);  //Set the slider value
            brightnessLabel.Content = brightnessSlider.Value.ToString(); //set the slider text
       
            contrastSlider.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Contrast);  //Set the slider value
            contrastLabel.Content = contrastSlider.Value.ToString(); //set the slider text
      
            sharpnessSlider.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Sharpness);  //Set the slider value
            sharpnessLabel.Content = sharpnessSlider.Value.ToString(); //set the slider text

        }

        private void Reset_Cam_Settings_Click(object sender, EventArgs e)
        {
            if (capture != null)
            {
                capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Brightness, brightnessStore);
                capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Contrast, contrastStore);
                capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Sharpness, sharpnessStore);
                RetrieveCaptureInformation();
            }
        }
    }
}
