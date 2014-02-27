using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;

namespace SocketTestClient
{
    class PatternGenerator
    {
        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);

        static readonly int canvasWid = 1280;
        static readonly int canvasHei = 1024;

        Bitmap patternBmp;
        Canvas parent;
        System.Windows.Controls.Image img;
        MainWindow mainWindow;

        public PatternGenerator(MainWindow m)
        {
            mainWindow = m;
            patternBmp = new Bitmap(canvasWid, canvasHei, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            img = new System.Windows.Controls.Image();
        }

        public void AddToParent(Canvas c)
        {
            parent = c;
            Action workAction = delegate
            {
                c.Children.Add(img);
                img.Width = canvasWid;
                img.Height = canvasHei;
                Canvas.SetTop(img, 0);
                Canvas.SetLeft(img, 0);
            };
            mainWindow.Dispatcher.BeginInvoke(workAction, System.Windows.Threading.DispatcherPriority.Normal);
        }

        Bitmap Generator(double complexity)
        {
            System.Drawing.Rectangle rectToDraw = new System.Drawing.Rectangle(0, 0, canvasWid, canvasHei);
            BitmapData bmpData = patternBmp.LockBits(rectToDraw, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            IntPtr startPtr = bmpData.Scan0;
            int dataLength = canvasWid * canvasHei * 4;

            byte[] bmpDataBuffer = new byte[dataLength];
            Marshal.Copy(startPtr, bmpDataBuffer, 0, dataLength);

            int minSize = 10; //minimum square size is 10 pixel
            int squareSize = (int)((canvasWid - minSize) * (1 - complexity) + minSize); //the acutual square size given a complexity level is computed as:
            //size = (max - min) * (1 - complexity) + min
            for (int i = 0; i < canvasWid; i++)
                for (int j = 0; j < canvasHei; j++)
                {
                    int xPos = (i / squareSize % 2);
                    int yPos = (j / squareSize % 2);
                    int bOrW = xPos ^ yPos;
                    bmpDataBuffer[(j * canvasWid + i) * 4] = (byte)(bOrW * 255);
                    bmpDataBuffer[(j * canvasWid + i) * 4 + 1] = (byte)(bOrW * 255);
                    bmpDataBuffer[(j * canvasWid + i) * 4 + 2] = (byte)(bOrW * 255);
                    bmpDataBuffer[(j * canvasWid + i) * 4 + 3] = 0;
                }
            Marshal.Copy(bmpDataBuffer, 0, startPtr, dataLength);
            patternBmp.UnlockBits(bmpData);
            return patternBmp;
        }

        public void showImage(ConfigStatus.BckgrdSts backgroundLevel)
        {
            BitmapImage bitmapImage = new BitmapImage();
            double level;
            switch (backgroundLevel)
            {
                case ConfigStatus.BckgrdSts.No:
                    level = 0;
                    break;
                case ConfigStatus.BckgrdSts.Level1:
                    level = 0.9;
                    break;
                case ConfigStatus.BckgrdSts.Level2:
                    level = 1;
                    break;
                default:
                    level = 0;
                    break;
            }
            Bitmap bmp = Generator(level);
            //bmp.MakeTransparent(System.Drawing.Color.White);
            //System.Drawing.Image img = (System.Drawing.Image)bmp;
            //Console.WriteLine(bmp.RawFormat.ToString());
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {

                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);

                bitmapImage.BeginInit();

                bitmapImage.StreamSource = ms;

                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

                bitmapImage.EndInit();

                bitmapImage.Freeze();

            }
            Action workAction = delegate
            {
                img.Source = bitmapImage;
            };
            mainWindow.Dispatcher.BeginInvoke(workAction, System.Windows.Threading.DispatcherPriority.Normal);
            Console.Write("GENERATED!");
        }
    }
}
