using System;
using System.Collections.Generic;
using System.IO.Ports;
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
using Microsoft.Surface;
using Microsoft.Surface.Core;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Input;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;


namespace ActiveReading
{
    /// <summary>
    /// Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    public partial class SurfaceWindow1 : SurfaceWindow
    {
        private TouchTarget touchTarget;
        private CircleF[] contourCircles;
        private IntPtr hwnd;
        private ImageMetrics normalizedMetrics;
        private TimeSpan diffTime;
        private DateTime currentTime;
        private byte[] normalizedImage;
        private bool imageAvailable;
        private PenTrack.Tracking trackLED;
        private int mode;
        private bool mode1, mode2, mode3;
        private bool highlight;
        private SerialPort sp;
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SurfaceWindow1()
        {
            trackLED = new PenTrack.Tracking();
            try
            {
                sp = new SerialPort("COM5", 19200);
                sp.Open();
                sp.ReadTimeout = 100;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex);
            }
            InitializeComponent();
            mode = 0;
            highlight = false;
            writeBoard.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.Yellow;
            writeBoard.DefaultDrawingAttributes.Height = 22;
            writeBoard.DefaultDrawingAttributes.Width = 22;
            InitializeSurfaceInput();
            // Add handlers for window availability events
            AddWindowAvailabilityHandlers();
        }

        private void InitializeSurfaceInput()
        {
            // Release all inputs
            writeBoard.ReleaseAllCaptures();
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
            if (sp.IsOpen)
            {
                if (sp.BytesToRead != 0)
                {
                    try
                    {
                        Console.Write(sp.ReadLine());
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
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
                imageAvailable = e.UpdateRawImage(ImageType.Normalized, normalizedImage,
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
        }

        /// <summary>
        /// This is called when the application's window is not visible or interactive.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowUnavailable(object sender, EventArgs e)
        {
            //TODO: disable audio, animations here
        }

        private void onModeClick(object s, RoutedEventArgs e)
        {
            switch (mode)
            {
                case 0:
                    {
                        mode = 1;
                        modeButton.Content = "Mode 2";
                        highlightButton.Visibility = System.Windows.Visibility.Hidden;
                        highlightButton.Background = Brushes.Silver;
                        writeBoard.EditingMode = SurfaceInkEditingMode.None;
                        break;
                    }
                case 1:
                    {
                        mode = 2;
                        modeButton.Content = "Mode 3";
                        break;
                    }
                case 2:
                    {
                        mode = 0;
                        modeButton.Content = "Mode 1";
                        highlightButton.Visibility = System.Windows.Visibility.Visible;
                        break;
                    }
            }
        }

        private void onHlClick(object s, RoutedEventArgs e)
        {
            if (!highlight)
            {
                highlight = true;
                writeBoard.EditingMode = SurfaceInkEditingMode.Ink;
                highlightButton.Background = Brushes.Yellow;
            }
            else
            {
                highlight = false;
                writeBoard.EditingMode = SurfaceInkEditingMode.None;
                highlightButton.Background = Brushes.Silver;
            }
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
                            (System.Math.Abs(((int)e.TouchDevice.GetCenterPosition(this).Y - (int)(circle.Center.Y * 2 - 15))) < 15) &&
                            ( mode == 0 || (mode2 && mode == 1) || (mode3 && mode == 2)))
                        {
                            e.Handled = false;
                            writeBoard.DefaultDrawingAttributes.FitToCurve = false;
                        }
                    }
                }
            }
        }
    }
}