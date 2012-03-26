#region Things to do better
/*
 * - create class for login.
 * - Create some sort of class for all this variables
 * - Maybe create a class for each task?!
 * - Create several methods that handle all this changes of state
 * - Create several classes that handle all this methods
 */
#endregion


using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Surface;
using Microsoft.Surface.Core;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Input;
using NLog;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using PenTrack;

namespace FinalVersion
{
    /// <summary>
    /// Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    public partial class SurfaceWindow1 : SurfaceWindow
    {
        private static Logger logger;
        private TouchDevice rectangleControlTouchDevice;
        private static int finger = 150;
        private static int pen = 254;
        private static int penSpot = 30;
        private static int fingerSpot = 100;
        private TouchTarget touchTarget;
        private CircleF[] contourCircles;
        private IntPtr hwnd;
        private ImageMetrics normalizedMetrics;
        private TimeSpan diffTime;
        private DateTime currentTime;
        private DateTime startLog;
        private byte[] normalizedImage;
        private bool imageAvailable;
        private Tracking trackLED;
        private System.Windows.Point lastPoint;
        private int technique, task, difficulty;
        private int done;
        private bool buttonTechnique, tiltTechnique;
        private bool highlight;
        private bool draw;
        private bool drag;
        private bool hlShort, hlMedium, hlLong;
        private bool isStarted;
        private SerialPort sp;
        private String[] split;
        private bool isInside;
        float[] rwAcc;
        float[] rwGyro;
        int button;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SurfaceWindow1()
        {
            logger = LogManager.GetCurrentClassLogger();
            done = 0;
            buttonTechnique = false;
            tiltTechnique = false;
            hlMedium = false;
            hlShort = false;
            hlLong = false;
            split = null;
            isStarted = false;
            rwAcc = new float[3];
            rwGyro = new float[3];
            trackLED = new Tracking();
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
            technique = 0;
            difficulty = 0;
            task = 0;
            highlight = false;
            highlightBoard.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.Yellow;
            drawBoard.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.White;
            InitializeSurfaceInput();
            // Add handlers for window availability events
            AddWindowAvailabilityHandlers();
        }

        private void InitializeSurfaceInput()
        {
            // Release all inputs
            myCanvas.ReleaseAllCaptures();
            dragRectangle.ReleaseAllCaptures();
            theBox.ReleaseAllCaptures();
            highlightBoard.ReleaseAllCaptures();
            drawBoard.ReleaseAllCaptures();
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
                        String str = sp.ReadLine();
                        split = str.Split(';');
                        Console.WriteLine(str);
                        switch (technique)
                        {
                            case 1:
                                {
                                    button = int.Parse(split[0]);
                                    break;
                                }
                            case 2:
                                {
                                    rwAcc[0] = float.Parse(split[1]) / 100;
                                    rwAcc[1] = float.Parse(split[2]) / 100;
                                    rwAcc[2] = float.Parse(split[3]) / 100;
                                    break;
                                }
                            //case 3:
                            //    {
                            //        rwGyro[0] = float.Parse(split[4]) / 100;
                            //        rwGyro[1] = float.Parse(split[5]) / 100;
                            //        rwGyro[2] = float.Parse(split[6]) / 100;
                            //        break;
                            //    }
                        }
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
                if (technique != 3)
                    contourCircles = trackLED.TrackContours(normalizedMetrics, normalizedImage, pen, penSpot);
                else
                    contourCircles = trackLED.TrackContours(normalizedMetrics, normalizedImage, finger, fingerSpot);
                currentTime = DateTime.Now;
            }

            if (split != null)
            {
                switch (technique)
                {
                    case 1:
                        {
                            if (button == 1)
                            {
                                buttonTechnique = true;
                                switch (task)
                                {
                                    case 0:
                                        {
                                            highlightBoard.EditingMode = SurfaceInkEditingMode.Ink;
                                            break;
                                        }
                                    case 2:
                                        {
                                            drawBoard.EditingMode = SurfaceInkEditingMode.Ink;
                                            break;
                                        }
                                }
                            }
                            else
                            {
                                buttonTechnique = false;
                                switch (task)
                                {
                                    case 0:
                                        {
                                            highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                                            break;
                                        }
                                    case 2:
                                        {
                                            drawBoard.EditingMode = SurfaceInkEditingMode.None;
                                            break;
                                        }
                                }
                            }
                            break;
                        }
                    case 2:
                        {
                            if (((rwAcc[0] <= 0.75) && (rwAcc[1] >= 0.65)) || ((rwAcc[0] <= 0.6) && (rwAcc[1] <= -0.6))
                                || ((rwAcc[0] <= 0.7) && (rwAcc[2] <= -0.4 || rwAcc[2] >= 0.7)))
                            {
                                tiltTechnique = true;
                                switch (task)
                                {
                                    case 0:
                                        {
                                            highlightBoard.EditingMode = SurfaceInkEditingMode.Ink;
                                            break;
                                        }
                                    case 2:
                                        {
                                            drawBoard.EditingMode = SurfaceInkEditingMode.Ink;
                                            break;
                                        }
                                }
                            }
                            else
                            {
                                tiltTechnique = false;
                                switch (task)
                                {
                                    case 0:
                                        {
                                            highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                                            break;
                                        }
                                    case 2:
                                        {
                                            drawBoard.EditingMode = SurfaceInkEditingMode.None;
                                            break;
                                        }
                                }
                            }
                            break;
                        }
                }
            }
            imageAvailable = false;

            if (task == 0)
            {
                if (highlightBoard.Strokes.Count() != 0)
                {
                    switch (difficulty)
                    {
                        case 0:
                            {
                                foreach (System.Windows.Ink.Stroke strk in highlightBoard.Strokes)
                                {
                                    if (!hlShort &&
                                        Math.Abs(Canvas.GetTop(shortRect) - (strk.GetBounds().Top + Canvas.GetTop(highlightBoard))) < 50 &&
                                        Math.Abs(Canvas.GetLeft(shortRect) - (strk.GetBounds().Left + Canvas.GetLeft(highlightBoard))) < 50 &&
                                        Math.Abs((Canvas.GetLeft(shortRect) - Canvas.GetLeft(highlightBoard) + shortRect.Width) -
                                            (strk.GetBounds().Left + strk.GetBounds().Width)) < 50 &&
                                        Math.Abs((Canvas.GetTop(shortRect) - Canvas.GetTop(highlightBoard) + shortRect.Height) -
                                            (strk.GetBounds().Top + strk.GetBounds().Height)) < 50)
                                    {
                                        hlShort = true;
                                        Console.WriteLine("YES - 1");
                                        //send log
                                        logger.Info("{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ;", startLog, "test", task, technique, difficulty, DateTime.Now, 0);
                                        // wait 5 seconds then show alert/dialogs
                                    }
                                }
                                break;
                            }
                        case 1:
                            {
                                foreach (System.Windows.Ink.Stroke strk in highlightBoard.Strokes)
                                {
                                    if (!hlMedium &&
                                        Math.Abs(Canvas.GetTop(mediumRect) - (strk.GetBounds().Top + Canvas.GetTop(highlightBoard))) < 50 &&
                                        Math.Abs(Canvas.GetLeft(mediumRect) - (strk.GetBounds().Left + Canvas.GetLeft(highlightBoard))) < 50 &&
                                        Math.Abs((Canvas.GetLeft(mediumRect) - Canvas.GetLeft(highlightBoard) + mediumRect.Width) -
                                            (strk.GetBounds().Left + strk.GetBounds().Width)) < 50 &&
                                        Math.Abs((Canvas.GetTop(mediumRect) - Canvas.GetTop(highlightBoard) + mediumRect.Height) -
                                            (strk.GetBounds().Top + strk.GetBounds().Height)) < 50)
                                    {
                                        hlMedium = true;
                                        Console.WriteLine("YES - 2");
                                        // wait 5 seconds then show alert/dialogs
                                        //send log
                                        logger.Info("{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ;", startLog, "test", task, technique, difficulty, DateTime.Now, 0);
                                    }
                                }
                                break;
                            }
                        case 2:
                            {
                                foreach (System.Windows.Ink.Stroke strk in highlightBoard.Strokes)
                                {
                                    if (!hlLong &&
                                        Math.Abs(Canvas.GetTop(longRect) - (strk.GetBounds().Top + Canvas.GetTop(highlightBoard))) < 50 &&
                                        Math.Abs(Canvas.GetLeft(longRect) - (strk.GetBounds().Left + Canvas.GetLeft(highlightBoard))) < 50 &&
                                        Math.Abs((Canvas.GetLeft(longRect) - Canvas.GetLeft(highlightBoard) + longRect.Width) -
                                            (strk.GetBounds().Left + strk.GetBounds().Width)) < 50 &&
                                        Math.Abs((Canvas.GetTop(longRect) - Canvas.GetTop(highlightBoard) + longRect.Height) -
                                            (strk.GetBounds().Top + strk.GetBounds().Height)) < 50)
                                        {
                                            hlLong = true;
                                            Console.WriteLine("YES - 3");
                                            //send log
                                            // wait 5 seconds then show alert/dialogs
                                            logger.Info("{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ;", startLog, "test", task, technique, difficulty, DateTime.Now, 0);
                                        }
                                }
                                break;
                            }
                    }    
                }
            }
            if (task == 1)
            {
                if (rectangleControlTouchDevice == null)
                {
                    if (Canvas.GetTop(this.dragRectangle) > Canvas.GetTop(this.theBox) && 
                        (Canvas.GetTop(this.dragRectangle) + dragRectangle.Height < Canvas.GetTop(theBox) + theBox.Height) &&
                        Canvas.GetLeft(this.dragRectangle) > Canvas.GetLeft(this.theBox) &&
                        (Canvas.GetLeft(this.dragRectangle) + dragRectangle.Width < Canvas.GetLeft(theBox) + theBox.Width))
                    {
                        if (!isInside)
                        {
                            Console.WriteLine("YAY");
                            //send log
                            isInside = true;
                            // wait 5 seconds then show alert/dialogs
                            logger.Info("{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ;", startLog, "test", task, technique, difficulty, DateTime.Now, 0);
                        }
                    }
                    else isInside = false;
                }
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

        private void onTechniqueClick(object s, RoutedEventArgs e)
        {
            switch (technique)
            {
                case 0:
                    {
                        isStarted = false;
                        technique = 1;
                        modeButton.Content = "Pen Mode 2";
                        switch (task)
                        {
                            case 0:
                                {
                                    highlightButton.Visibility = System.Windows.Visibility.Hidden;
                                    highlightButton.Background = Brushes.Silver;
                                    highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                                    highlightBoard.Strokes.Clear();
                                    break;
                                }
                            case 1:
                                {
                                    selectButton.Visibility = System.Windows.Visibility.Hidden;
                                    selectButton.Background = Brushes.Silver;
                                    drag = false;
                                    switch (difficulty)
                                    {
                                        case 0:
                                            {
                                                Canvas.SetTop(dragRectangle, 380);
                                                Canvas.SetLeft(dragRectangle, 430);
                                                dragRectangle.Width = 300;
                                                dragRectangle.Height = 300;
                                                Canvas.SetTop(theBox, 264);
                                                Canvas.SetLeft(theBox, 1077);
                                                theBox.Width = 600;
                                                theBox.Height = 600;
                                                break;
                                            }
                                        case 1:
                                            {
                                                Canvas.SetTop(dragRectangle, 400);
                                                Canvas.SetLeft(dragRectangle, 150);
                                                dragRectangle.Width = 250;
                                                dragRectangle.Height = 250;
                                                Canvas.SetTop(theBox, 350);
                                                Canvas.SetLeft(theBox, 1400);
                                                theBox.Width = 400;
                                                theBox.Height = 400;
                                                break;
                                            }
                                        case 2:
                                            {
                                                Canvas.SetTop(dragRectangle, 425);
                                                Canvas.SetLeft(dragRectangle, 50);
                                                dragRectangle.Width = 125;
                                                dragRectangle.Height = 125;
                                                Canvas.SetTop(theBox, 410);
                                                Canvas.SetLeft(theBox, 1700);
                                                theBox.Width = 150;
                                                theBox.Height = 150;
                                                break;
                                            }
                                    }
                                    break;
                                }
                            case 2:
                                {
                                    drawButton.Visibility = System.Windows.Visibility.Hidden;
                                    drawButton.Background = Brushes.Silver;
                                    doneButton.Visibility = System.Windows.Visibility.Hidden;
                                    doneButton.Background = Brushes.Silver;
                                    drawBoard.EditingMode = SurfaceInkEditingMode.None;
                                    drawBoard.Strokes.Clear();
                                    break;
                                }
                        }
                        break;
                    }
                case 1:
                    {
                        isStarted = false;
                        technique = 2;
                        modeButton.Content = "Pen Mode 3";
                        switch (task)
                        {
                            case 0:
                                {
                                    highlightBoard.Strokes.Clear();
                                    break;
                                }
                            case 1:
                                {
                                    switch (difficulty)
                                    {
                                        case 0:
                                            {
                                                Canvas.SetTop(dragRectangle, 380);
                                                Canvas.SetLeft(dragRectangle, 430);
                                                dragRectangle.Width = 300;
                                                dragRectangle.Height = 300;
                                                Canvas.SetTop(theBox, 264);
                                                Canvas.SetLeft(theBox, 1077);
                                                theBox.Width = 600;
                                                theBox.Height = 600;
                                                break;
                                            }
                                        case 1:
                                            {
                                                Canvas.SetTop(dragRectangle, 400);
                                                Canvas.SetLeft(dragRectangle, 150);
                                                dragRectangle.Width = 250;
                                                dragRectangle.Height = 250;
                                                Canvas.SetTop(theBox, 350);
                                                Canvas.SetLeft(theBox, 1400);
                                                theBox.Width = 400;
                                                theBox.Height = 400;
                                                break;
                                            }
                                        case 2:
                                            {
                                                Canvas.SetTop(dragRectangle, 425);
                                                Canvas.SetLeft(dragRectangle, 50);
                                                dragRectangle.Width = 125;
                                                dragRectangle.Height = 125;
                                                Canvas.SetTop(theBox, 410);
                                                Canvas.SetLeft(theBox, 1700);
                                                theBox.Width = 150;
                                                theBox.Height = 150;
                                                break;
                                            }
                                    }
                                    break;
                                }
                            case 2:
                                {
                                    drawBoard.Strokes.Clear();
                                    break;
                                }
                        }
                        break;
                    }
                case 2:
                    {
                        isStarted = false;
                        technique = 3;
                        modeButton.Content = "Finger";
                        switch (task)
                        {
                            case 0:
                                {
                                    highlightButton.Visibility = System.Windows.Visibility.Visible;
                                    highlightButton.Background = Brushes.Silver;
                                    highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                                    highlightBoard.Strokes.Clear();
                                    break;
                                }
                            case 1:
                                {
                                    selectButton.Visibility = System.Windows.Visibility.Visible;
                                    selectButton.Background = Brushes.Silver;
                                    drag = false;
                                    switch (difficulty)
                                    {
                                        case 0:
                                            {
                                                Canvas.SetTop(dragRectangle, 380);
                                                Canvas.SetLeft(dragRectangle, 430);
                                                dragRectangle.Width = 300;
                                                dragRectangle.Height = 300;
                                                Canvas.SetTop(theBox, 264);
                                                Canvas.SetLeft(theBox, 1077);
                                                theBox.Width = 600;
                                                theBox.Height = 600;
                                                break;
                                            }
                                        case 1:
                                            {
                                                Canvas.SetTop(dragRectangle, 400);
                                                Canvas.SetLeft(dragRectangle, 150);
                                                dragRectangle.Width = 250;
                                                dragRectangle.Height = 250;
                                                Canvas.SetTop(theBox, 350);
                                                Canvas.SetLeft(theBox, 1400);
                                                theBox.Width = 400;
                                                theBox.Height = 400;
                                                break;
                                            }
                                        case 2:
                                            {
                                                Canvas.SetTop(dragRectangle, 425);
                                                Canvas.SetLeft(dragRectangle, 50);
                                                dragRectangle.Width = 125;
                                                dragRectangle.Height = 125;
                                                Canvas.SetTop(theBox, 410);
                                                Canvas.SetLeft(theBox, 1700);
                                                theBox.Width = 150;
                                                theBox.Height = 150;
                                                break;
                                            }
                                    }
                                    break;
                                }
                            case 2:
                                {
                                    drawButton.Visibility = System.Windows.Visibility.Visible;
                                    drawButton.Background = Brushes.Silver;
                                    doneButton.Visibility = System.Windows.Visibility.Visible;
                                    doneButton.Background = Brushes.Silver;
                                    drawBoard.Strokes.Clear();
                                    break;
                                }
                        }
                        break;
                    }
                case 3:
                    {
                        isStarted = false;
                        technique = 0;
                        modeButton.Content = "Pen Mode 1";
                        switch (task)
                        {
                            case 0:
                                {
                                    highlightButton.Visibility = System.Windows.Visibility.Visible;
                                    highlightButton.Background = Brushes.Silver;
                                    highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                                    highlightBoard.Strokes.Clear();
                                    break;
                                }
                            case 1:
                                {
                                    selectButton.Visibility = System.Windows.Visibility.Visible;
                                    selectButton.Background = Brushes.Silver;
                                    drag = false;
                                    switch (difficulty)
                                    {
                                        case 0:
                                            {
                                                Canvas.SetTop(dragRectangle, 380);
                                                Canvas.SetLeft(dragRectangle, 430);
                                                dragRectangle.Width = 300;
                                                dragRectangle.Height = 300;
                                                Canvas.SetTop(theBox, 264);
                                                Canvas.SetLeft(theBox, 1077);
                                                theBox.Width = 600;
                                                theBox.Height = 600;
                                                break;
                                            }
                                        case 1:
                                            {
                                                Canvas.SetTop(dragRectangle, 400);
                                                Canvas.SetLeft(dragRectangle, 150);
                                                dragRectangle.Width = 250;
                                                dragRectangle.Height = 250;
                                                Canvas.SetTop(theBox, 350);
                                                Canvas.SetLeft(theBox, 1400);
                                                theBox.Width = 400;
                                                theBox.Height = 400;
                                                break;
                                            }
                                        case 2:
                                            {
                                                Canvas.SetTop(dragRectangle, 425);
                                                Canvas.SetLeft(dragRectangle, 50);
                                                dragRectangle.Width = 125;
                                                dragRectangle.Height = 125;
                                                Canvas.SetTop(theBox, 410);
                                                Canvas.SetLeft(theBox, 1700);
                                                theBox.Width = 150;
                                                theBox.Height = 150;
                                                break;
                                            }
                                    }
                                    break;
                                }
                            case 2:
                                {
                                    drawButton.Visibility = System.Windows.Visibility.Visible;
                                    drawButton.Background = Brushes.Silver;
                                    doneButton.Visibility = System.Windows.Visibility.Visible;
                                    doneButton.Background = Brushes.Silver;
                                    drawBoard.Strokes.Clear();
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        private void onHlClick(object s, RoutedEventArgs e)
        {
            if (!highlight)
            {
                highlight = true;
                highlightBoard.EditingMode = SurfaceInkEditingMode.Ink;
                highlightButton.Background = Brushes.Yellow;
            }
            else
            {
                highlight = false;
                highlightBoard.EditingMode = SurfaceInkEditingMode.None;
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
                            (System.Math.Abs(((int)e.TouchDevice.GetCenterPosition(this).Y - (int)(circle.Center.Y * 2 - 15))) < 15))
                        {
                            switch (task)
                            {
                                case 0:
                                    {
                                        if (technique == 0 || (buttonTechnique && technique == 1) || (tiltTechnique && technique == 2) || technique == 3)
                                        {
                                            if (!isStarted)
                                            {
                                                startLog = DateTime.Now;
                                                isStarted = true;
                                            }
                                            e.Handled = false;
                                            highlightBoard.DefaultDrawingAttributes.Height = circle.Radius * 2;
                                            highlightBoard.DefaultDrawingAttributes.Width = circle.Radius * 2;
                                            highlightBoard.DefaultDrawingAttributes.FitToCurve = false;
                                        }
                                        break;
                                    }
                                case 1:
                                    {
                                        if (technique == 0 || (buttonTechnique && technique == 1) || (tiltTechnique && technique == 2) || (technique == 3 && drag))
                                        {
                                            if (!isStarted)
                                            {
                                                startLog = DateTime.Now;
                                                isStarted = true;
                                            }
                                            e.Handled = false;
                                            e.TouchDevice.Capture(this.dragRectangle);
                                            if (rectangleControlTouchDevice == null)
                                            {
                                                rectangleControlTouchDevice = e.TouchDevice;

                                                // Remember where this contact took place.  
                                                lastPoint = rectangleControlTouchDevice.GetTouchPoint(this).Position;
                                            }
                                        }
                                        break;
                                    }
                                case 2:
                                    {
                                        if (technique == 0 || (buttonTechnique && technique == 1) || (tiltTechnique && technique == 2) || technique == 3)
                                        {
                                            if (!isStarted)
                                            {
                                                startLog = DateTime.Now;
                                                isStarted = true;
                                            }
                                            e.Handled = false;
                                            drawBoard.DefaultDrawingAttributes.Height = circle.Radius * 2;
                                            drawBoard.DefaultDrawingAttributes.Width = circle.Radius * 2;
                                            drawBoard.DefaultDrawingAttributes.FitToCurve = false;
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
        }

        private void onTaskClick(object s, RoutedEventArgs e)
        {
            switch (task)
            {
                case 0:
                    {
                        isStarted = false;
                        task = 1;
                        taskButton.Content = "Task2 - DnD";
                        highlightLabel.Visibility = System.Windows.Visibility.Hidden;
                        highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                        highlightBoard.Visibility = System.Windows.Visibility.Hidden;
                        highlightBoard.Strokes.Clear();
                        dragRectangle.Visibility = System.Windows.Visibility.Visible;
                        theBox.Visibility = System.Windows.Visibility.Visible;
                        textBoard.Visibility = System.Windows.Visibility.Hidden;
                        if (technique == 0 || technique == 3)
                        {
                            highlightButton.Visibility = System.Windows.Visibility.Hidden;
                            highlightButton.Background = Brushes.Silver;
                            selectButton.Visibility = System.Windows.Visibility.Visible;
                            selectButton.Background = Brushes.Silver;
                        }
                        switch (difficulty)
                        {
                            case 0:
                                {
                                    Canvas.SetTop(dragRectangle, 380);
                                    Canvas.SetLeft(dragRectangle, 430);
                                    dragRectangle.Width = 300;
                                    dragRectangle.Height = 300;
                                    Canvas.SetTop(theBox, 264);
                                    Canvas.SetLeft(theBox, 1077);
                                    theBox.Width = 600;
                                    theBox.Height = 600;
                                    break;
                                }
                            case 1:
                                {
                                    Canvas.SetTop(dragRectangle, 400);
                                    Canvas.SetLeft(dragRectangle, 150);
                                    dragRectangle.Width = 250;
                                    dragRectangle.Height = 250;
                                    Canvas.SetTop(theBox, 350);
                                    Canvas.SetLeft(theBox, 1400);
                                    theBox.Width = 400;
                                    theBox.Height = 400;
                                    break;
                                }
                            case 2:
                                {
                                    Canvas.SetTop(dragRectangle, 425);
                                    Canvas.SetLeft(dragRectangle, 50);
                                    dragRectangle.Width = 125;
                                    dragRectangle.Height = 125;
                                    Canvas.SetTop(theBox, 410);
                                    Canvas.SetLeft(theBox, 1700);
                                    theBox.Width = 150;
                                    theBox.Height = 150;
                                    break;
                                }
                        }
                        break;
                    }
                case 1:
                    {
                        isStarted = false;
                        task = 2;
                        taskButton.Content = "Task3 - INK";
                        dragRectangle.Visibility = System.Windows.Visibility.Hidden;
                        theBox.Visibility = System.Windows.Visibility.Hidden;
                        drawLable.Visibility = System.Windows.Visibility.Visible;
                        drawBoard.Visibility = System.Windows.Visibility.Visible;
                        wordDrawLabel.Visibility = System.Windows.Visibility.Visible;
                        if (technique == 0 || technique == 3)
                        {
                            selectButton.Visibility = System.Windows.Visibility.Hidden;
                            selectButton.Background = Brushes.Silver;
                            drawButton.Background = Brushes.Silver;
                            drawButton.Visibility = System.Windows.Visibility.Visible;
                            doneButton.Visibility = System.Windows.Visibility.Visible;
                        }
                        switch (difficulty)
                        {
                            case 0:
                                {
                                    wordDrawLabel.Content = "PALERMO";
                                    wordDrawLabel.FontSize = 200;
                                    wordDrawLabel.FontFamily = new FontFamily("Segoe360");
                                    drawBoard.Strokes.Clear();
                                    break;
                                }
                            case 1:
                                {
                                    wordDrawLabel.Content = "Forza Palermo";
                                    wordDrawLabel.FontSize = 180;
                                    wordDrawLabel.FontFamily = new FontFamily("Gabriola");
                                    drawBoard.Strokes.Clear();
                                    break;
                                }
                            case 2:
                                {
                                    wordDrawLabel.Content = "Andrea's ITU Internship";
                                    wordDrawLabel.FontSize = 72;
                                    wordDrawLabel.FontFamily = new FontFamily("Segoe Script");
                                    drawBoard.Strokes.Clear();
                                    break;
                                }
                        }
                        break;
                    }
                case 2:
                    {
                        isStarted = false;
                        task = 0;
                        taskButton.Content = "Task1 - HL";
                        drawLable.Visibility = System.Windows.Visibility.Hidden;
                        highlightLabel.Visibility = System.Windows.Visibility.Visible;
                        highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                        drawBoard.EditingMode = SurfaceInkEditingMode.None;
                        highlightBoard.Visibility = System.Windows.Visibility.Visible;
                        textBoard.Visibility = System.Windows.Visibility.Visible;
                        drawBoard.Visibility = System.Windows.Visibility.Hidden;
                        wordDrawLabel.Visibility = System.Windows.Visibility.Hidden;
                        drawBoard.Strokes.Clear();
                        if (technique == 0 || technique == 3)
                        {
                            drawButton.Background = Brushes.Silver;
                            drawButton.Visibility = System.Windows.Visibility.Hidden;
                            doneButton.Visibility = System.Windows.Visibility.Hidden;
                            highlightButton.Visibility = System.Windows.Visibility.Visible;
                            highlightButton.Background = Brushes.Silver;
                        }
                        switch (difficulty)
                        {
                            case 0:
                                {
                                    highlightLabel.Content = "Please highlight the word 'inputs'";
                                    break;
                                }
                            case 1:
                                {
                                    highlightLabel.Content = "Please highlight the word 'immediately'";
                                    break;
                                }
                            case 2:
                                {
                                    highlightLabel.Content = "Please highlight the word 'PixelSense technology'";
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        private void onDrawEraseClick(object s, RoutedEventArgs e)
        {
            if (!draw)
            {
                draw = true;
                drawBoard.EditingMode = SurfaceInkEditingMode.Ink;
                drawButton.Background = Brushes.MediumAquamarine;
            }
            else
            {
                draw = false;
                drawBoard.EditingMode = SurfaceInkEditingMode.None;
                drawButton.Background = Brushes.Silver;
            }
        }

        private void onDoneClick(object s, RoutedEventArgs e)
        {
            DateTime stopLog;
            if (done == 0)
            {
                stopLog = DateTime.Now;
                voteLabel.Visibility = System.Windows.Visibility.Visible;
                radio1.Visibility = System.Windows.Visibility.Visible;
                radio2.Visibility = System.Windows.Visibility.Visible;
                radio3.Visibility = System.Windows.Visibility.Visible;
                radio4.Visibility = System.Windows.Visibility.Visible;
                radio5.Visibility = System.Windows.Visibility.Visible;
                done = 1;
            }
            else
            {
                doneButton.Content = "Vote";
                
                if (radio1.IsChecked.Value || radio2.IsChecked.Value || radio3.IsChecked.Value ||
                    radio4.IsChecked.Value || radio5.IsChecked.Value)
                {
                    done = 0;
                    if (radio1.IsChecked.Value)
                    {
                        //send log-1
                        logger.Info("{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ;", startLog, "test", task, technique, difficulty, DateTime.Now, 1);
                        // find a way to have screenshot
                    }
                    if (radio2.IsChecked.Value)
                    {
                        //send log-2
                        logger.Info("{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ;", startLog, "test", task, technique, difficulty, DateTime.Now, 2);
                        // find a way to have screenshot
                    }
                    if (radio3.IsChecked.Value)
                    {
                        //send log-3
                        logger.Info("{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ;", startLog, "test", task, technique, difficulty, DateTime.Now, 3);
                        // find a way to have screenshot
                    }
                    if (radio4.IsChecked.Value)
                    {
                        //send log-4
                        logger.Info("{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ;", startLog, "test", task, technique, difficulty, DateTime.Now, 4);
                        // find a way to have screenshot
                    }
                    if (radio5.IsChecked.Value)
                    {
                        //send log-5
                        logger.Info("{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ;", startLog, "test", task, technique, difficulty, DateTime.Now, 5);
                        // find a way to have screenshot
                    }
                    voteLabel.Visibility = System.Windows.Visibility.Hidden;
                    radio1.Visibility = System.Windows.Visibility.Collapsed;
                    radio2.Visibility = System.Windows.Visibility.Collapsed;
                    radio3.Visibility = System.Windows.Visibility.Collapsed;
                    radio4.Visibility = System.Windows.Visibility.Collapsed;
                    radio5.Visibility = System.Windows.Visibility.Collapsed;
                    doneButton.Content = "Done";
                    //show alert!
                }
                else
                {
                    //alert, need a vote!!
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

        private void onDragClick(object s, RoutedEventArgs e)
        {
            if (!drag)
            {
                drag = true;
                selectButton.Background = Brushes.Green;
            }
            else
            {
                drag = false;
                selectButton.Background = Brushes.Silver;
            }
        }

        private void onDifficultyClick(object s, RoutedEventArgs e)
        {
            switch (difficulty)
            {
                case 0:
                    {
                        isStarted = false;
                        difficulty = 1;
                        difficultyButton.Content = "Medium";
                        switch (task)
                        {
                            case 0:
                                {
                                    highlightLabel.Content = "Please highlight the word 'immediately'";
                                    highlightBoard.Strokes.Clear();
                                    break;
                                }
                            case 1:
                                {
                                    Canvas.SetTop(dragRectangle, 400);
                                    Canvas.SetLeft(dragRectangle, 150);
                                    dragRectangle.Width = 250;
                                    dragRectangle.Height = 250;
                                    Canvas.SetTop(theBox, 350);
                                    Canvas.SetLeft(theBox, 1400);
                                    theBox.Width = 400;
                                    theBox.Height = 400;
                                    drag = false;
                                    selectButton.Background = Brushes.Silver;
                                    break;
                                }
                            case 2:
                                {
                                    wordDrawLabel.Content = "Forza Palermo";
                                    wordDrawLabel.FontSize = 180;
                                    wordDrawLabel.FontFamily = new FontFamily("Gabriola");
                                    drawBoard.Strokes.Clear();
                                    break;
                                }
                        }
                        break;
                    }
                case 1:
                    {
                        isStarted = false;
                        difficulty = 2;
                        difficultyButton.Content = "Hard";
                        switch (task)
                        {
                            case 0:
                                {
                                    highlightLabel.Content = "Please highlight the word 'PixelSense technology'";
                                    highlightBoard.Strokes.Clear();
                                    break;
                                }
                            case 1:
                                {
                                    Canvas.SetTop(dragRectangle, 425);
                                    Canvas.SetLeft(dragRectangle, 50);
                                    dragRectangle.Width = 125;
                                    dragRectangle.Height = 125;
                                    Canvas.SetTop(theBox, 410);
                                    Canvas.SetLeft(theBox, 1700);
                                    theBox.Width = 150;
                                    theBox.Height = 150;
                                    drag = false;
                                    selectButton.Background = Brushes.Silver;
                                    break;
                                }
                            case 2:
                                {
                                    wordDrawLabel.Content = "Andrea's ITU Internship";
                                    wordDrawLabel.FontSize = 72;
                                    wordDrawLabel.FontFamily = new FontFamily("Segoe Script");
                                    drawBoard.Strokes.Clear();
                                    break;
                                }
                        }
                        break;
                    }
                case 2:
                    {
                        isStarted = false;
                        difficulty = 0;
                        difficultyButton.Content = "Easy";
                        switch (task)
                        {
                            case 0:
                                {
                                    highlightLabel.Content = "Please highlight the word 'inputs'";
                                    highlightBoard.Strokes.Clear();
                                    break;
                                }
                            case 1:
                                {
                                    Canvas.SetTop(dragRectangle, 380);
                                    Canvas.SetLeft(dragRectangle, 430);
                                    dragRectangle.Width = 300;
                                    dragRectangle.Height = 300;
                                    Canvas.SetTop(theBox, 264);
                                    Canvas.SetLeft(theBox, 1077);
                                    theBox.Width = 600;
                                    theBox.Height = 600;
                                    drag = false;
                                    selectButton.Background = Brushes.Silver;
                                    break;
                                }
                            case 2:
                                {
                                    wordDrawLabel.Content = "PALERMO";
                                    wordDrawLabel.FontSize = 200;
                                    wordDrawLabel.FontFamily = new FontFamily("Segoe360");
                                    drawBoard.Strokes.Clear();
                                    break;
                                }
                        }
                        break;
                    }
            }
        }
    }
}