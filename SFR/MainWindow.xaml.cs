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
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using System.Windows.Threading;
using System.Runtime.InteropServices;



namespace SFR
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Capture capture;
        private CascadeClassifier haarCascade;
        DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            capture = new Capture();
            string link = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            haarCascade = new CascadeClassifier(link + "/haarcascade_frontalface_default.xml");
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            using (var currentFrame = capture.QueryFrame().ToImage<Bgr, Byte>())
            {
                if (currentFrame != null)
                {
                    var grayFrame = currentFrame.Convert<Gray, byte>();
                    var faces = haarCascade.DetectMultiScale(grayFrame, 1.1, 10, System.Drawing.Size.Empty);
                    foreach (var face in faces)
                        currentFrame.Draw(face, new Bgr(System.Drawing.Color.DarkBlue), 3);
                }
                image.Source = Emgu.CV.WPF.BitmapSourceConvert.ToBitmapSource(currentFrame);
            }
        }


    }
}
