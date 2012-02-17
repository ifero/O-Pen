using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Surface;
using Microsoft.Surface.Core;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Input;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.IO;

namespace InkOnThat
{
    /// <summary>
    /// Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    public partial class SurfaceWindow1 : SurfaceWindow
    {
        Microsoft.Surface.Core.TouchTarget touchTarget;
        IntPtr hwnd;
        private byte[] normalizedImage;
        private Microsoft.Surface.Core.ImageMetrics normalizedMetrics;
        System.Drawing.Imaging.ColorPalette pal;
        bool imageAvailable;
        private CircleF[] contourCircles;
        bool isPen;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SurfaceWindow1()
        {
            isPen = false;
            InitializeComponent();
            AutoOrientsOnStartup = false;
            // Add handlers for window availability events
            AddWindowAvailabilityHandlers();

            InitializeSurfaceInput();
        }

        private void InitializeSurfaceInput()
        {
            // Get the hWnd for the SurfaceWindow object after it has been loaded.
            hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            touchTarget = new Microsoft.Surface.Core.TouchTarget(hwnd);
            // Set up the TouchTarget object for the entire SurfaceWindow object.
            touchTarget.EnableInput();
            EnableRawImage();
            // Attach an event handler for the FrameReceived event.
            touchTarget.FrameReceived += new EventHandler<FrameReceivedEventArgs>(OnTouchTargetFrameReceived);
        }

        private void OnTouchTargetFrameReceived(object sender, Microsoft.Surface.Core.FrameReceivedEventArgs e)
        {
            imageAvailable = false;
            int paddingLeft,
                  paddingRight;
            if (normalizedImage == null)
            {
                imageAvailable = e.TryGetRawImage(Microsoft.Surface.Core.ImageType.Normalized,
                    Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Left,
                    Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Top,
                    Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Width,
                    Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Height,
                    out normalizedImage,
                    out normalizedMetrics,
                    out paddingLeft,
                    out paddingRight);
            }
            else
            {
                imageAvailable = e.UpdateRawImage(Microsoft.Surface.Core.ImageType.Normalized,
                     normalizedImage,
                     Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Left,
                     Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Top,
                     Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Width,
                     Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Height);
            }

            if (!imageAvailable)
                return;

            // Surface image (byte array) to EmguCV image
            Image<Gray, Byte> imageFrame = new Image<Gray, byte>(normalizedMetrics.Width, normalizedMetrics.Height) { Bytes = normalizedImage };

            //process the frame for tracking the blob
            imageFrame = processFrame(imageFrame);

            iCapturedFrame.Source = Bitmap2BitmapImage(imageFrame.ToBitmap());
            //imageFrame.Dispose();
            imageAvailable = false;


        }

        /// <summary>
        /// Occurs when the window is about to close. 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Remove handlers for window availability events
            RemoveWindowAvailabilityHandlers();
        }

        /// <summary>
        /// Adds handlers for window availability events.
        /// </summary>
        private void AddWindowAvailabilityHandlers()
        {
            // Subscribe to surface window availability events
            ApplicationServices.WindowInteractive += OnWindowInteractive;
            ApplicationServices.WindowNoninteractive += OnWindowNoninteractive;
            ApplicationServices.WindowUnavailable += OnWindowUnavailable;
        }

        /// <summary>
        /// Removes handlers for window availability events.
        /// </summary>
        private void RemoveWindowAvailabilityHandlers()
        {
            // Unsubscribe from surface window availability events
            ApplicationServices.WindowInteractive -= OnWindowInteractive;
            ApplicationServices.WindowNoninteractive -= OnWindowNoninteractive;
            ApplicationServices.WindowUnavailable -= OnWindowUnavailable;
        }

        /// <summary>
        /// This is called when the user can interact with the application's window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowInteractive(object sender, EventArgs e)
        {
            //TODO: enable audio, animations here
            // Enable a normalized image to be obtained.
            touchTarget.EnableImage(Microsoft.Surface.Core.ImageType.Normalized);
        }

        /// <summary>
        /// This is called when the user can see but not interact with the application's window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowNoninteractive(object sender, EventArgs e)
        {
            //TODO: Disable audio here if it is enabled

            //TODO: optionally enable animations here

            // If the application is deactivated before a raw image is
            // captured, make sure to disable core raw images for performance reasons.
            touchTarget.DisableImage(Microsoft.Surface.Core.ImageType.Normalized);
        }

        /// <summary>
        /// This is called when the application's window is not visible or interactive.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowUnavailable(object sender, EventArgs e)
        {
            //TODO: disable audio, animations here
            // If the application is deactivated before a raw image is
            // captured, make sure to disable core raw images for performance reasons.
            touchTarget.DisableImage(Microsoft.Surface.Core.ImageType.Normalized);
        }


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

        private BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage bImg = new System.Windows.Media.Imaging.BitmapImage();
            ms.Position = 0;
            bImg.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            bImg.StreamSource = ms;
            bImg.EndInit();
            return bImg;
        }


        private void EnableRawImage()
        {
            touchTarget.EnableImage(Microsoft.Surface.Core.ImageType.Normalized);
            touchTarget.FrameReceived += new EventHandler<FrameReceivedEventArgs>(OnTouchTargetFrameReceived);
        }

        private void DisableRawImage()
        {
            touchTarget.DisableImage(Microsoft.Surface.Core.ImageType.Normalized);
            touchTarget.FrameReceived -= OnTouchTargetFrameReceived;
        }

        private Image<Gray, byte> processFrame(Image<Gray, byte> image)
        {
            image = image.ThresholdBinary(new Gray(254), new Gray(255)); //Show just the very bright things

            Contour<System.Drawing.Point> contours = image.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
            Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST);
            contourCircles = FindPossibleCircles(contours);

            //ContactEventArgs cea = new Microsoft.Surface.Presentation.ContactEventArgs();
            //onContactDown(this, new Microsoft.Surface.Presentation.ContactEventArgs());

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
                if (contours.Area >= 1 && contours.Area <= 50)
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

        private void onContactDown(object s, System.Windows.Input.TouchEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            e.Handled = true;
            if (isPen)
            {
                if (contourCircles != null)
                {
                    sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "Captured X:{0} Y:{1}", (int)e.TouchDevice.GetPosition(this).X, (int)e.TouchDevice.GetPosition(this).Y));
                    foreach (CircleF circle in contourCircles)
                    {
                        sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "Image     X:{0} Y:{1}", (int)(circle.Center.X * 2), (int)(circle.Center.Y * 2 - 15)));
                        if ((System.Math.Abs(((int)e.TouchDevice.GetCenterPosition(this).X - (int)(circle.Center.X * 2))) < 10) &&
                            (System.Math.Abs(((int)e.TouchDevice.GetCenterPosition(this).Y - (int)(circle.Center.Y * 2 - 15))) < 10))
                        {
                            e.Handled = false;
                            inkBoard.DefaultDrawingAttributes.Height = circle.Radius;
                            inkBoard.DefaultDrawingAttributes.Width = circle.Radius;
                            inkBoard.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.WhiteSmoke;
                            inkBoard.DefaultDrawingAttributes.FitToCurve = false;
                        }
                        info.Text = sb.ToString();
                    }
                }
            }
        }

        private void onContactUp(object s, System.Windows.Input.TouchEventArgs e)
        {
            e.Handled = true;
            isPen = false;
        }
    }
}