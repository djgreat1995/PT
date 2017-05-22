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

using DirectShowLib;
using System.IO;
using Microsoft.Win32;
using System.Threading;
using Emgu.CV.Face;
using System.Windows.Interop;

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
        bool isCapture = false;
        int ContTrain, NumLabels;
        CascadeClassifier face;
        Image<Gray, byte> TrainedFace = null;
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        List<string> labels = new List<string>();
        List<string> NamePersons = new List<string>();
        string link = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        //FaceRecognizer _faceRecognizer = new EigenFaceRecognizer(80,3000);
        // FaceRecognizer _faceRecognizer = new FisherFaceRecognizer(0, 2000);
        FaceRecognizer _faceRecognizer = new LBPHFaceRecognizer(1, 8, 8, 8,100 );
        string _recognizerFilePath = "TrainedFaces/plik.yml";
        Image<Gray, byte>[] faceImages;
        int[] faceLabels;
        Image<Bgr, Byte> currentFrame;
        List<Person> people;
        public string face_label ="";


        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer();


            timer.Interval = TimeSpan.FromMilliseconds(1); //interwał 1 ms
        
            people = new List<Person>();
            face = new CascadeClassifier("haarcascade_frontalface_default.xml");
            try
            {
                people = Person.personsFromJson(File.ReadAllText("TrainedFaces/people.txt"));

                //Load of previus trainned faces and labels for each image
                string Labelsinfo = File.ReadAllText(link + "/TrainedFaces/TrainedLabels.txt");
                string[] Labels = Labelsinfo.Split('%');
                NumLabels = Convert.ToInt16(Labels[0]);
                ContTrain = NumLabels;
                string LoadFaces;
                faceImages = new Image<Gray, byte>[NumLabels];
                faceLabels = new int[NumLabels];
                for (int tf = 1; tf < NumLabels + 1; tf++)
                {
                    LoadFaces = "face" + tf + ".bmp";
                    trainingImages.Add(new Image<Gray, byte>(link + "/TrainedFaces/" + LoadFaces));
                    labels.Add(Labels[tf]);
                }

            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString());
                MessageBox.Show("Exception " + e, "Triained faces load", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

        }

        void timer_Tick(object sender, EventArgs e)
        {

            cameraSettings();
        }

        private void captureButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            isCapture = true;
            try
            {
                capture = new Capture();
            }
            catch (NullReferenceException exception)
            {
                MessageBox.Show(exception.Message);
            }
            timer.Tick += new EventHandler(timer_Tick);
            timer.Tick += new EventHandler(FrameGrabber);
            haarCascade = new CascadeClassifier(link + "/haarcascade_frontalface_default.xml"); //zestaw danych generowany z pliku

            cameraInformation();
            capture.FlipHorizontal = !capture.FlipHorizontal; //obrot widoku kamery w poziomie
            for (int i = 0; i < NumLabels; i++)
            {
                faceImages[i] = trainingImages[i];
                faceLabels[i] = Convert.ToInt32(labels[i]);
            }
            _faceRecognizer.Train(faceImages, faceLabels);
            _faceRecognizer.Save(_recognizerFilePath);

        }

        private void stopCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            richTextBox.Document.Blocks.Clear();
            Reset_Cam_Settings_Click(null, null);
            timer.Stop();
            image.Source = null;
            capture.Dispose();
            isCapture = false;
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
            richTextBox.AppendText("Camera name: " + webCams[cameraDevice].Device_Name + "\n\n");
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
                imageBox.Source = null;
                capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Brightness, brightnessStore);
                capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Contrast, contrastStore);
                capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Sharpness, sharpnessStore);
                RetrieveCaptureInformation();
            }
        }

        //Pobranie klatki
        private void takePhotoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isCapture == true)
            {
                using (var currentFrame = capture.QueryFrame().ToImage<Bgr, Byte>())
                {
                    imageBox.Source = Emgu.CV.WPF.BitmapSourceConvert.ToBitmapSource(currentFrame);
                }
            }
        }

        private void savePhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Trained face counter
                ContTrain = ContTrain + 1;

                //Get a gray frame from capture device

                UMat grayFrame = new UMat();
                currentFrame = capture.QueryFrame().ToImage<Bgr, Byte>();
                CvInvoke.CvtColor(currentFrame, grayFrame, ColorConversion.Bgr2Gray);
                imageBox.Source = Emgu.CV.WPF.BitmapSourceConvert.ToBitmapSource(grayFrame);

                //Face Detector
                System.Drawing.Rectangle[] facesDetected = face.DetectMultiScale(
                grayFrame,
                1.2,
                10,
                new System.Drawing.Size(20, 20));

                //Action for each element detected
                foreach (System.Drawing.Rectangle f in facesDetected)
                {
                    TrainedFace = currentFrame.Copy(f).Convert<Gray, byte>();
                    break;
                }
                imageBox.Source = Emgu.CV.WPF.BitmapSourceConvert.ToBitmapSource(TrainedFace);
                //resize face detected image for force to compare the same size with the 
                //test image with cubic interpolation type method
                TrainedFace = TrainedFace.Resize(100, 100, Emgu.CV.CvEnum.Inter.Cubic);

                trainingImages.Add(TrainedFace);

                int _id = Person.findPersonID(people, nameTextBox.Text);
                if (_id != -1)
                {
                    people.Add(new Person(nameTextBox.Text, _id));
                    labels.Add(_id.ToString());
                }
                else
                {
                    int __id = Person.setID(people);
                    people.Add(new Person(nameTextBox.Text, __id));
                    labels.Add(__id.ToString());
                }

                File.WriteAllText("TrainedFaces/people.txt", Person.personsToJson(people));
                //Show face added in gray scale
                string exePath = Environment.GetCommandLineArgs()[0];
                string startupPath = System.IO.Path.GetDirectoryName(exePath);
                //Write the number of triained faces in a file text for further load
                File.WriteAllText(startupPath + "/TrainedFaces/TrainedLabels.txt", trainingImages.ToArray().Length.ToString() + "%");

                //Write the labels of triained faces in a file text for further load
                for (int i = 1; i < trainingImages.ToArray().Length + 1; i++)
                {
                    trainingImages.ToArray()[i - 1].Save(startupPath + "/TrainedFaces/face" + i + ".bmp");
                    File.AppendAllText(startupPath + "/TrainedFaces/TrainedLabels.txt", labels.ToArray()[i - 1] + "%");
                }
                MessageBox.Show(nameTextBox.Text + "´s face detected and added!", "Training OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception a)
            {
                MessageBox.Show(a.ToString());
                MessageBox.Show("Enable the face detection first", "Training Fail", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        void FrameGrabber(object sender, EventArgs e)
        {
            string name;
            if (actualFace() != null)
            {
                var result = _faceRecognizer.Predict(actualFace());
                if (result.Distance < 88)
                {
                    name = Person.findNameByID(people, result.Label);
                    face_label = name;
                    LabelName.Content = "PERSON: " + name;
                    labelDistance.Foreground = new SolidColorBrush(Colors.Green);
                    labelDistance.Content = "Distance: " + result.Distance;
                }
                else
                {
                    face_label = "Person undetected";
                    LabelName.Content = "Person undetected";
                }
                
            }
        }


        private Image<Gray, byte> actualFace()
        {

            currentFrame = capture.QueryFrame().ToImage<Bgr, Byte>().Resize(300,250, Emgu.CV.CvEnum.Inter.Cubic);
            Image<Gray, Byte> grayFrame = currentFrame.Convert<Gray, byte>(); //konwertowanie do klatki w odcieniach szarości
            //var faces = haarCascade.DetectMultiScale(grayFrame, 1.1, 10, System.Drawing.Size.Empty); //aktualna detekcja twarzy

            //Face Detector
            System.Drawing.Rectangle[] facesDetected = face.DetectMultiScale(
            grayFrame,
            1.2,
            10,
            new System.Drawing.Size(20, 20));

            //TrainedFace = null;
            //Action for each element detected
            foreach (System.Drawing.Rectangle f in facesDetected)
            {
                TrainedFace = currentFrame.Copy(f).Convert<Gray, byte>();
                currentFrame.Draw(f, new Bgr(System.Drawing.Color.DarkBlue), 3); //podswietlenie twarzy za pomocą box'a rysowanego dookoła niej
                CvInvoke.PutText(currentFrame, face_label, new System.Drawing.Point(f.Location.X + 10, f.Location.Y - 10), Emgu.CV.CvEnum.FontFace.HersheyComplex, 0.5, new Bgr(0, 255, 0).MCvScalar);
                break;
            }

            image.Source = Emgu.CV.WPF.BitmapSourceConvert.ToBitmapSource(currentFrame); //przekazanie obrazu na komponent Image
            System.Drawing.Rectangle[] facesTab = haarCascade.DetectMultiScale(grayFrame, 1.1, 10, System.Drawing.Size.Empty); //tablica z wykrytymi twarzami


            if (TrainedFace != null)
                return TrainedFace.Resize(100, 100, Emgu.CV.CvEnum.Inter.Cubic);
            else
                return null;
        }

    }
}
