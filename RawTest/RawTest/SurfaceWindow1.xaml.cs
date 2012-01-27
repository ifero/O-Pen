using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Core;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using System.Diagnostics;

namespace InkCanvasTest
{
    /// <summary>
    /// Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    public partial class SurfaceWindow1 : SurfaceWindow
    {
        private ContactTarget contactTarget;
        private IntPtr hwnd;
        private byte[] normalizedImage;
        private ImageMetrics imageMetrics;
        private bool imageAvailable;
        private ColorPalette pal;
        private Bitmap frame;
        private static double scaleValue = 1.333333333;
        CircleF[] contourCircles;
        bool isPen;
        //private int i;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SurfaceWindow1()
        {
            //i = 0;
            isPen = false;
            InitializeComponent();
            InitializeSurfaceInput();
            // Add handlers for Application activation events
            AddActivationHandlers();
        }


        private void InitializeSurfaceInput()
        {
            hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            contactTarget = new Microsoft.Surface.Core.ContactTarget(hwnd);
            contactTarget.EnableInput();
            EnableRawImage();
        }

        /// <summary>
        /// Occurs when the window is about to close. 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Remove handlers for Application activation events
            RemoveActivationHandlers();
        }

        /// <summary>
        /// Adds handlers for Application activation events.
        /// </summary>
        private void AddActivationHandlers()
        {
            // Subscribe to surface application activation events
            ApplicationLauncher.ApplicationActivated += OnApplicationActivated;
            ApplicationLauncher.ApplicationPreviewed += OnApplicationPreviewed;
            ApplicationLauncher.ApplicationDeactivated += OnApplicationDeactivated;
        }

        /// <summary>
        /// Removes handlers for Application activation events.
        /// </summary>
        private void RemoveActivationHandlers()
        {
            // Unsubscribe from surface application activation events
            ApplicationLauncher.ApplicationActivated -= OnApplicationActivated;
            ApplicationLauncher.ApplicationPreviewed -= OnApplicationPreviewed;
            ApplicationLauncher.ApplicationDeactivated -= OnApplicationDeactivated;
        }

        /// <summary>
        /// This is called when application has been activated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplicationActivated(object sender, EventArgs e)
        {
            //TODO: enable audio, animations here
        }

        /// <summary>
        /// This is called when application is in preview mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplicationPreviewed(object sender, EventArgs e)
        {
            //TODO: Disable audio here if it is enabled

            //TODO: optionally enable animations here
        }

        /// <summary>
        ///  This is called when application has been deactivated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplicationDeactivated(object sender, EventArgs e)
        {
            //TODO: disable audio, animations here
        }

        private void EnableRawImage()
        {
            contactTarget.EnableImage(ImageType.Normalized);
            contactTarget.FrameReceived += new EventHandler<FrameReceivedEventArgs>(target_FrameReceived);
        }

        private void DisableRawImage()
        {
            contactTarget.DisableImage(ImageType.Normalized);
            contactTarget.FrameReceived -= new EventHandler<FrameReceivedEventArgs>(target_FrameReceived);
        }

        void target_FrameReceived(object sender, FrameReceivedEventArgs e)
        {
            imageAvailable = false;
            int paddingLeft, paddingRight;
            if (normalizedImage == null)
            {
                imageAvailable = e.TryGetRawImage(ImageType.Normalized,
                  Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Left,
                  Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Top,
                  Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Width,
                  Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Height,
                  out normalizedImage, out imageMetrics, out paddingLeft, out paddingRight);
            }
            else
            {
                imageAvailable = e.UpdateRawImage(ImageType.Normalized, normalizedImage,
                  Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Left,
                  Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Top,
                  Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Width,
                  Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Height);
            }

            if (!imageAvailable)
                return;

            DisableRawImage();

            GCHandle h = GCHandle.Alloc(normalizedImage, GCHandleType.Pinned);
            IntPtr ptr = h.AddrOfPinnedObject();
            frame = new Bitmap(imageMetrics.Width,
                                  imageMetrics.Height,
                                  imageMetrics.Stride,
                                  System.Drawing.Imaging.PixelFormat.Format8bppIndexed,
                                  ptr);

            Convert8bppBMPToGrayscale(frame);
            
            //convert the bitmap into an EmguCV image <Gray,byte>
            Image<Gray, byte> imageFrame = new Image<Gray, byte>(frame);
            //process the frame for tracking the blob
            imageFrame = processFrame(imageFrame); 
            
            iCapturedFrame.Source = Bitmap2BitmapImage(imageFrame.ToBitmap());
            
            /* save the first 40 images captured
             * 
             * if (i < 40) 
             * {               
             * flipper.Save("capture-" + i + ".bmp"); 
             * i++;
             * }  
             * 
             */
            
            imageAvailable = false;
            EnableRawImage();
        }

        private void onContactDown(object s, Microsoft.Surface.Presentation.ContactEventArgs e)
        {
            e.Handled = true;
            if (isPen)
            {
                if (contourCircles != null)
                {
                    Console.WriteLine("touch: x:{0} y:{1}", (int)e.Contact.GetPosition(this).X, (int)e.Contact.GetPosition(this).Y);
                    foreach (CircleF circle in contourCircles)
                    {
                        Console.WriteLine("pen  : x:{0} y:{1}", (int)(circle.Center.X * scaleValue), (int)(circle.Center.Y * scaleValue));
                        if ((System.Math.Abs(((int)e.Contact.GetCenterPosition(this).X - (int)(circle.Center.X * scaleValue))) < 10) &&
                            (System.Math.Abs(((int)e.Contact.GetCenterPosition(this).Y - (int)(circle.Center.Y * scaleValue))) < 10))
                        {
                            e.Handled = false;
                            Console.WriteLine("xxx");
                        }
                    }
                }
            }
        }

        private void onContactUp(object s, Microsoft.Surface.Presentation.ContactEventArgs e)
        {
            e.Handled = true;
            isPen = false;
        }

        private Image<Gray, byte> processFrame(Image<Gray, byte> image)
        {
            

            // image._Flip(Emgu.CV.CvEnum.FLIP.VERTICAL);
            
            image = image.ThresholdBinary(new Gray(250), new Gray(255)); //Show just the very bright things

            Contour<System.Drawing.Point> contours = image.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
            Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST);
            contourCircles = FindPossibleCircles(contours);

            /*Testing blob detection.
             * 
             *if (contourCircles != null)
             *{
             *    foreach (CircleF circle in contourCircles)
             *    {
             *        image.Draw(circle, new Gray(100), 1);
             *    }
             *}
             *
             */

            return image;
        }

        private CircleF[] FindPossibleCircles(Contour<System.Drawing.Point> contours)
        {
            if (contours == null)
            {
                isPen = false;
                return null;
            }

            ResetContoursNavigation(ref contours);

            IList<CircleF> circles = new List<CircleF>();
            for (; contours.HNext != null; contours = contours.HNext)
            {
                if (contours.Area >= 5 && contours.Area <= 50)
                {
                    circles.Add(new CircleF(
                      new PointF(contours.BoundingRectangle.Left + (contours.BoundingRectangle.Width / 2),
                        contours.BoundingRectangle.Top + (contours.BoundingRectangle.Height / 2)),
                        contours.BoundingRectangle.Width / 2));
                    isPen = true;
                }

            }

            if (contours.Area >= 10 && contours.Area <= 50)
            {
                circles.Add(new CircleF(
                  new PointF(contours.BoundingRectangle.Left + contours.BoundingRectangle.Width / 2,
                    contours.BoundingRectangle.Top + contours.BoundingRectangle.Height / 2),
                    contours.BoundingRectangle.Width / 2));
                isPen = true;
            }
            return circles.ToArray();
        }

        private static void ResetContoursNavigation(ref Contour<System.Drawing.Point> contours)
        {
            if (contours == null)
                return;

            //go back to the begining
            while (contours.HPrev != null)
                contours = contours.HPrev;
        }

        /// <summary>
        /// Convert RGB Bitmap to a GrayScale Bitmap
        /// </summary>
        /// <param name="bmp"></param>
        private void Convert8bppBMPToGrayscale(Bitmap bmp)
        {
            if (pal == null) // pal is defined at module level as --- ColorPalette pal;
            {
                pal = bmp.Palette;
                for (int i = 0; i < 256; i++)
                {
                    pal.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                }
            }
            bmp.Palette = pal;
        }

        /// <summary>
        /// Convert from Bitmap to BitmapImage
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        private BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage bImg = new System.Windows.Media.Imaging.BitmapImage();
            bImg.BeginInit();
            bImg.StreamSource = new MemoryStream(ms.ToArray());
            bImg.EndInit();
            return bImg;
        }

    }
        
}