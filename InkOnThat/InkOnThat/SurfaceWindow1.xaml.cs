using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Surface;
using Microsoft.Surface.Core;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Input;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;


namespace InkOnThat
{
    /// <summary>
    /// Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    public partial class SurfaceWindow1 : SurfaceWindow
    {
        private CircleF[] contourCircles;
        private ImageMetrics normalizedMetrics;
        private TouchTarget touchTarget;
        private IntPtr hwnd;
        private DateTime currentTime;
        private TimeSpan diffTime;
        private byte[] normalizedImage;
        private bool imageAvailable;
        private PenTrack.Tracking trackLED;


        /// <summary>
        /// Default constructor.
        /// </summary>
        public SurfaceWindow1()
        {
            trackLED = new PenTrack.Tracking();
            InitializeComponent();
            // Add handlers for window availability events
            AddWindowAvailabilityHandlers();

            InitializeSurfaceInput();
        }

        private void InitializeSurfaceInput()
        {
            // Release all input captures for SurfaceInkBoard
            inkBoard.ReleaseAllCaptures();
            // Set current date time
            currentTime = DateTime.Now;
            // Get the hWnd for the SurfaceWindow object after it has been loaded.
            hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            touchTarget = new Microsoft.Surface.Core.TouchTarget(hwnd);
            // Set up the TouchTarget object for the entire SurfaceWindow object.
            touchTarget.EnableInput();
            touchTarget.EnableImage(ImageType.Normalized);
            // Attach an event handler for the FrameReceived event.
            touchTarget.FrameReceived += new EventHandler<FrameReceivedEventArgs>(OnTouchTargetFrameReceived);
        }

        private void OnTouchTargetFrameReceived(object sender, Microsoft.Surface.Core.FrameReceivedEventArgs e)
        {
            imageAvailable = false;
            if (normalizedImage == null)
            {
                imageAvailable = e.TryGetRawImage(Microsoft.Surface.Core.ImageType.Normalized,
                    Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Left,
                    Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Top,
                    Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Width,
                    Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Height,
                    out normalizedImage,
                    out normalizedMetrics);
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

            // Reduce from 60fpps to 30fpps (frame processed per second) 
            diffTime = DateTime.Now - currentTime;
            if (diffTime.Milliseconds > 30)
            {
                // Process the frame to detect the LED blob
                contourCircles = trackLED.TrackContours(normalizedMetrics, normalizedImage);
                currentTime = DateTime.Now;
            }


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

        private void onTouchDown(object s, System.Windows.Input.TouchEventArgs e)
        {
            e.Handled = true;
            if (trackLED.isAPen())
            {
                if (contourCircles != null)
                {
                    foreach (CircleF circle in contourCircles)
                    {
                        if ((System.Math.Abs(((int)e.TouchDevice.GetCenterPosition(this).X - (int)(circle.Center.X * 2))) < 15) &&
                            (System.Math.Abs(((int)e.TouchDevice.GetCenterPosition(this).Y - (int)(circle.Center.Y * 2 - 15))) < 15))
                        {
                            e.Handled = false;
                            inkBoard.DefaultDrawingAttributes.Height = circle.Radius;
                            inkBoard.DefaultDrawingAttributes.Width = circle.Radius;
                            inkBoard.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.WhiteSmoke;
                            inkBoard.DefaultDrawingAttributes.FitToCurve = false;
                        }
                    }
                }
            }
        }

        private void onEraseClick(object s, RoutedEventArgs e)
        {
            inkBoard.EditingMode = inkBoard.EditingMode == SurfaceInkEditingMode.Ink ? SurfaceInkEditingMode.EraseByPoint : SurfaceInkEditingMode.Ink;
        }

        private void onClearClick(object s, RoutedEventArgs e)
        {
            inkBoard.Strokes.Clear();
        }
    }
}