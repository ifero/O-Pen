using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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


namespace SquareItInside
{
    
    /// <summary>
    /// Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    public partial class SurfaceWindow1 : SurfaceWindow
    {
        private TouchDevice rectangleControlTouchDevice;
        private TouchTarget touchTarget;
        private CircleF[] contourCircles;
        private IntPtr hwnd;
        private ImageMetrics normalizedMetrics;
        private TimeSpan diffTime;
        private DateTime currentTime;
        private byte[] normalizedImage;
        private bool imageAvailable;
        private bool isPen;
        private bool isInside;
        private System.Windows.Point lastPoint;
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
            // Initialize Surface Inputs
            InitializeSurfaceInput();
        }

        private void InitializeSurfaceInput()
        {
            // Release all inputs
            myCanvas.ReleaseAllCaptures();
            dragRectangle.ReleaseAllCaptures();
            theBox.ReleaseAllCaptures();
            // Set current date time
            currentTime = DateTime.Now;
            // Get the hWnd for the SurfaceWindow object after it has been loaded.
            hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            touchTarget = new TouchTarget(hwnd);
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
                imageAvailable = e.TryGetRawImage(ImageType.Normalized,
                    Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Left,
                    Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Top,
                    Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Width,
                    Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Height,
                    out normalizedImage,
                    out normalizedMetrics);
            }
            else
            {
                imageAvailable = e.UpdateRawImage(ImageType.Normalized,normalizedImage,
                     Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Left,
                     Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Top,
                     Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Width,
                     Microsoft.Surface.Core.InteractiveSurface.PrimarySurfaceDevice.Height);
            }

            if (!imageAvailable)
                return;

            // Reduce from 60fpps to 30fpps (frame processed per second) 
            diffTime = DateTime.Now - currentTime;
            if(diffTime.Milliseconds > 30)
            {
                // Process the frame to detect the LED blob
                contourCircles = trackLED.TrackContours(normalizedMetrics, normalizedImage);
                currentTime = DateTime.Now;
            }
            if (rectangleControlTouchDevice == null)
            {
                if ((Canvas.GetTop(this.dragRectangle) > Canvas.GetTop(this.theBox) && (Canvas.GetTop(this.dragRectangle) - Canvas.GetTop(this.theBox)) < 50)
                 && (Canvas.GetLeft(this.dragRectangle) > Canvas.GetLeft(this.theBox) && (Canvas.GetLeft(this.dragRectangle) - Canvas.GetLeft(this.theBox)) < 50))
                {
                    if(!isInside)
                    {
                        aButton.Visibility = Visibility.Visible;
                        isInside = true;
                    }
                }
                else isInside = false;
            }

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
                            e.TouchDevice.Capture(this.dragRectangle);
                            if (rectangleControlTouchDevice == null)
                            {
                                rectangleControlTouchDevice = e.TouchDevice;

                                // Remember where this contact took place.  
                                lastPoint = rectangleControlTouchDevice.GetTouchPoint(this).Position;
                            }
                        }
                    }
                }
            }
        }

        private void onTouchMove(object s, System.Windows.Input.TouchEventArgs e)
        {
            e.Handled = true;

            if (e.TouchDevice == rectangleControlTouchDevice)
            {
                // Get the current position of the contact.  
                System.Windows.Point currentTouchPoint = rectangleControlTouchDevice.GetCenterPosition(this);

                // Get the change between the controlling contact point and
                // the changed contact point.  
                double deltaX = currentTouchPoint.X - lastPoint.X;
                double deltaY = currentTouchPoint.Y - lastPoint.Y;

                // Get and then set a new top position and a new left position for the ellipse.  
                double newTop = Canvas.GetTop(this.dragRectangle) + deltaY;
                double newLeft = Canvas.GetLeft(this.dragRectangle) + deltaX;

                Canvas.SetTop(this.dragRectangle, newTop);
                Canvas.SetLeft(this.dragRectangle, newLeft);

                // Forget the old contact point, and remember the new contact point.  
                lastPoint = currentTouchPoint;
            }
        }

        private void onTouchLeave(object s, System.Windows.Input.TouchEventArgs e)
        {
            // If this contact is the one that was remembered  
            if (e.TouchDevice == rectangleControlTouchDevice)
            {
                // Forget about this contact.
                rectangleControlTouchDevice = null;
            }

            // Mark this event as handled.  
            e.Handled = true;
        }

        private void onButtonClick(object s, RoutedEventArgs e)
        {
            Canvas.SetTop(this.dragRectangle, 400);
            Canvas.SetLeft(this.dragRectangle, 100);
            aButton.Visibility = Visibility.Hidden;
        }
    }
}