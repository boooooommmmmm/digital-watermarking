﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LBSWatermark;
using Microsoft.Win32;

namespace LBSWatermarkingWithUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _imageLocation;
        private string _grayScaleImageLocation;
        private string _watermarkImageLocation;
        private string _recoveredWatermarkLocation;
        private List<int> grayscaleListY = new List<int>();
        private List<int> grayscaleListU = new List<int>();
        private List<int> grayscaleListV = new List<int>();

        private Watermark _watermark;

        public MainWindow()
        {
            InitializeComponent();

            _imageLocation = AppDomain.CurrentDomain.BaseDirectory + "original.jpg";
            _grayScaleImageLocation = AppDomain.CurrentDomain.BaseDirectory + "grayScaleImage.jpg";
            _watermarkImageLocation = AppDomain.CurrentDomain.BaseDirectory + "embeddedwatermark.jpg";
            _recoveredWatermarkLocation = AppDomain.CurrentDomain.BaseDirectory + "recoveredwatermark.jpg";

            var fileBytes = File.ReadAllBytes(_imageLocation);
            RenderImageBytes(OriginalImage, fileBytes);

            _watermark = new Watermark(true);
        }

        private void BtnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image|*.jpg;*.png;*.gif;*.bmp";
            if (ofd.ShowDialog() == true)
            {
                _imageLocation = ofd.FileName;

                var fileBytes = File.ReadAllBytes(_imageLocation);
                RenderImageBytes(OriginalImage, fileBytes);
            }
        }

        private void BtnLoadWatermarkedImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image|*.jpg;*.png;*.gif;*.bmp";
            if (ofd.ShowDialog() == true)
            {
                _watermarkImageLocation = ofd.FileName;
                var fileBytes = File.ReadAllBytes(_watermarkImageLocation);
                RenderImageBytes(WatermarkedImage, fileBytes);
            }
        }

        private void BtnSaveWatermarkedImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Image|*.jpg;*.png;*.gif;*.bmp";
            if (sfd.ShowDialog() == true)
            {
                File.Copy(_watermarkImageLocation, sfd.FileName);
            }
        }

        /// <summary>
        /// Test function, to generate the gray level picture
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnGetGrayLevel_Click(object sender, RoutedEventArgs e)
        {
            var fileBytes = File.ReadAllBytes(_imageLocation);        
            grayscaleListY.Clear();
            grayscaleListU.Clear();
            grayscaleListV.Clear();
            
            var sw = Stopwatch.StartNew();//for count time used

            Bitmap grayScaleBitMap = new Bitmap(_imageLocation);
            for (int x = 0; x < grayScaleBitMap.Width; x++)
            {
                for (int y = 0; y < grayScaleBitMap.Height; y++)
                {
                    System.Drawing.Color pixelColor = grayScaleBitMap.GetPixel(x, y);
                    int grayscaleY = (int)(ColorSpaceConversion.RgbToY(pixelColor.R, pixelColor.G, pixelColor.B));//convert color to gray
                    int grayscaleU = (int)(ColorSpaceConversion.RgbToU(pixelColor.R, pixelColor.G, pixelColor.B));
                    int grayscaleV = (int)(ColorSpaceConversion.RgbToV(pixelColor.R, pixelColor.G, pixelColor.B));
                    grayscaleListY.Add(grayscaleY);
                    grayscaleListU.Add(grayscaleU);
                    grayscaleListV.Add(grayscaleV);
                    System.Drawing.Color newColor = System.Drawing.Color.FromArgb(pixelColor.A, grayscaleY, grayscaleY, grayscaleY);
                    grayScaleBitMap.SetPixel(x, y, newColor);
                }
            }
            BitmapToImageSource(GrayScaleImage, grayScaleBitMap); //render gray image

            sw.Stop();
            grayScaleBitMap.Save(AppDomain.CurrentDomain.BaseDirectory + "grayScaleImage.jpg");//save to LBSWatermarkingWithUI/bin/debug
            EmbedTime.Text = String.Format("{0}ms", sw.ElapsedMilliseconds);
        }

        //core function
        private void BtnEmbedWatermark_Click(object sender, RoutedEventArgs e)
        {
            var fileBytes = File.ReadAllBytes(_grayScaleImageLocation);

            var sw = Stopwatch.StartNew();//for count time used
            var embeddedBytes = _watermark.EmbedWatermark(fileBytes);
            //var embeddedBytes = _watermark.RetrieveAndEmbedWatermark(fileBytes).WatermarkedImage;
            sw.Stop();

            EmbedTime.Text = String.Format("{0}ms", sw.ElapsedMilliseconds);
            _watermarkImageLocation = AppDomain.CurrentDomain.BaseDirectory + "embeddedwatermark.jpg";

            File.WriteAllBytes(_watermarkImageLocation, embeddedBytes);

            RenderImageBytes(GrayScaleImage, embeddedBytes);
            //RenderImageBytes(WatermarkedImage, embeddedBytes);
        }
        //private void BtnEmbedWatermark_Click(object sender, RoutedEventArgs e)
        //{
        //    var fileBytes = File.ReadAllBytes(_imageLocation);        

        //    var sw = Stopwatch.StartNew();//for count time used
        //    var embeddedBytes = _watermark.EmbedWatermark(fileBytes);
        //    //var embeddedBytes = _watermark.RetrieveAndEmbedWatermark(fileBytes).WatermarkedImage;
        //    sw.Stop();

        //    EmbedTime.Text = String.Format("{0}ms", sw.ElapsedMilliseconds);
        //    _watermarkImageLocation = AppDomain.CurrentDomain.BaseDirectory + "embeddedwatermark.jpg";

        //    File.WriteAllBytes(_watermarkImageLocation, embeddedBytes);
        //    RenderImageBytes(WatermarkedImage, embeddedBytes);
        //}

        private void BtnRetrieveWatermark_Click(object sender, RoutedEventArgs e)
        {
            var fileBytes = File.ReadAllBytes(_watermarkImageLocation);

            var sw = Stopwatch.StartNew();
            var result = _watermark.RetrieveWatermark(fileBytes);
            sw.Stop();

            RetrieveTime.Text = String.Format("{0}ms", sw.ElapsedMilliseconds);
            SimilarityText.Text = String.Format("Similarity: {0}%", result.Similarity);

            if (result.WatermarkDetected)
            {
                SuccessImage.Visibility = Visibility.Visible;
                FailureImage.Visibility = Visibility.Collapsed;
            }
            else
            {
                SuccessImage.Visibility = Visibility.Collapsed;
                FailureImage.Visibility = Visibility.Visible;
            }

            File.WriteAllBytes(_recoveredWatermarkLocation, result.RecoveredWatermark);
            RenderImageBytes(RetrievedWatermark, result.RecoveredWatermark);
        }

        //render image using bytes
        private void RenderImageBytes(System.Windows.Controls.Image control, byte[] bytes)
        {
            MemoryStream byteStream = new MemoryStream(bytes);
            BitmapImage imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.StreamSource = byteStream;
            imageSource.EndInit();

            control.Source = imageSource;
        }

        private void BitmapToImageSource(System.Windows.Controls.Image control, Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                control.Source = bitmapimage;
            }
        }
    }
}