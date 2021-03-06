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
using Pen;
using ScreenShotDemo;
using NLog.Targets;

namespace FinalVersion
{
    /// <summary>
    /// Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    public partial class SurfaceWindow1 : SurfaceWindow
    {
        private String groupName;
        private static Logger logger;
        private TouchDevice rectangleControlTouchDevice;
        private TouchTarget touchTarget;
        private CircleF[] contourCircles;
        private IntPtr hwnd;
        private ImageMetrics normalizedMetrics;
        private TimeSpan diffTime;
        private DateTime currentTime;
        private DateTime startLog;
        private DateTime stopLog;
        private long epochStart, epochStop;
        private byte[] normalizedImage;
        private bool imageAvailable;
        private Tracking trackLED;
        private System.Windows.Point lastPoint;
        private int technique, task, difficulty;
        private bool buttonTechnique, tiltTechnique;
        private bool highlight, draw, drag;
        private bool hlShort, hlMedium, hlLong;
        private bool isStarted;
        private String userName;
        private SerialPort sp;
        private String[] split;
        private bool isInside;
        private bool trainingMode;
        private bool thankyou;
        private bool demoMode;
        private Random random;
        float[] rwAcc;
        private ScreenCapture sc;
        //float[] rwGyro;
        int button;
        private int errors;
        private int loop;
        private int numberOfStrokes;
        private int lastDifficulty;
        private int numDifficulty;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SurfaceWindow1()
        {
            
            // suppress, if there is, the virtual keyboard of the System.
            Microsoft.Surface.SurfaceKeyboard.SuppressTextInputPanel(hwnd);

            sc = new ScreenCapture();
            trackLED = new Tracking();

            // link logger to the related Nlog logger.
            logger = LogManager.GetCurrentClassLogger();

            demoMode = false;
            thankyou = false;
            groupName = null;

            highlight = false;
            drag = false;
            draw = false;
            buttonTechnique = false;
            tiltTechnique = false;

            hlMedium = false;
            hlShort = false;
            hlLong = false;
            
            split = null;
            
            isStarted = false;
            trainingMode = false;

            button = 0;
            errors = 0;
            rwAcc = new float[3];
            random = new Random();
            //rwGyro = new float[3];

            technique = 0;
            difficulty = 0;
            task = 0;
            lastDifficulty = -1;
            numDifficulty = 0;
            

            // Trying to get serial communication with port: COM6
            try
            {
                sp = new SerialPort("COM6", 19200);
                sp.Open();
                sp.ReadTimeout = 100;
            }
            catch (System.Exception ex)
            {
                logger.Warn(ex);
            }
            
            // Set current date time
            currentTime = DateTime.Now;
            
            InitializeComponent();

            // Set the color of the strokes of the Ink Boards    
            highlightBoard.UsesTouchShape = false;
            highlightBoard.DefaultDrawingAttributes.FitToCurve = false;
            highlightBoard.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.Yellow;
            drawBoard.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.Green;
            
            // Initialize inputs of Surface
            InitializeSurfaceInput();

            // Add handlers for window availability events
            AddWindowAvailabilityHandlers();
        }

        /// <summary>
        /// Initialize Surface Input.
        /// </summary> 
        private void InitializeSurfaceInput()
        {
            // Release all inputs
            myCanvas.ReleaseAllCaptures();
            dragRectangle.ReleaseAllCaptures();
            theBox.ReleaseAllCaptures();
            highlightBoard.ReleaseAllCaptures();
            drawBoard.ReleaseAllCaptures();

            // Get the hWnd for the SurfaceWindow object after it has been loaded.
            hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            // Initialize the touchTarget for the hWnd loaded
            touchTarget = new TouchTarget(hwnd);
            // Set up the TouchTarget object for the entire SurfaceWindow object.
            touchTarget.EnableInput();
            touchTarget.EnableImage(ImageType.Normalized);
            // Attach an event handler for the FrameReceived event.
            touchTarget.FrameReceived += new EventHandler<FrameReceivedEventArgs>(OnTouchTargetFrameReceived);

        }

        /// <summary>
        /// Occurs on every frame that touchTarget receive (60 frame per second). 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTouchTargetFrameReceived(object sender, Microsoft.Surface.Core.FrameReceivedEventArgs e)
        {
            // Check if the serial connection is opened
            if (sp.IsOpen)
            {
                // Check if there is some Byte to read
                if (sp.BytesToRead != 0)
                {
                    // Trying to receive the Bytes and save it all in a String.
                    try
                    {
                        String str = sp.ReadLine();
                        // Split the message received on every ';' that occurs
                        split = str.Split(';');
                        // If it is on Tester mode show the message received on Console.
                        if (groupName == "T")
                        {
                            Console.WriteLine(str);   
                        }
                        // Based on the technique in use get information 
                        // from the message received from the pen.
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
                        logger.Trace(ex);
                    }
                }
            }

            imageAvailable = false;

            // Get the RAW Byte captured from the surface
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
                    contourCircles = trackLED.TrackContours(normalizedMetrics, normalizedImage);
                //else
                //    contourCircles = trackLED.TrackContours(normalizedMetrics, normalizedImage, fingerThreshold, fingerSpot);
                //currentTime = DateTime.Now;
            }

            // If the pen is working then check actions 
            // and enable interaction based on the task in use.    
            if (split != null)
            {
                TechniqueEnabler();
            }
            imageAvailable = false;
            if (!demoMode)
            {
                if (!trainingMode)
                {
                    if (task == 0)
                    {
                        // ---> WRITE HERE!!
                        if (highlightBoard.Strokes.Count() != 0)
                        {
                            switch (difficulty)
                            {
                                case 0:
                                    {
                                        foreach (System.Windows.Ink.Stroke strk in highlightBoard.Strokes)
                                        {
                                            if (!hlShort &&
                                                Math.Abs(Canvas.GetTop(shortRect) - (strk.GetBounds().Top + Canvas.GetTop(highlightBoard))) < 22 &&
                                                Math.Abs(Canvas.GetLeft(shortRect) - (strk.GetBounds().Left + Canvas.GetLeft(highlightBoard))) < 22 &&
                                                Math.Abs((Canvas.GetLeft(shortRect) - Canvas.GetLeft(highlightBoard) + shortRect.Width) -
                                                    (strk.GetBounds().Left + strk.GetBounds().Width)) < 22 &&
                                                Math.Abs((Canvas.GetTop(shortRect) - Canvas.GetTop(highlightBoard) + shortRect.Height) -
                                                    (strk.GetBounds().Top + strk.GetBounds().Height)) < 22)
                                            {
                                                hlShort = true;
                                                Console.WriteLine("YES - 1");
                                                stopLog = DateTime.Now;
                                                epochStart = (long)(startLog - new DateTime(1970, 1, 1)).TotalMilliseconds;
                                                epochStop = (long)(stopLog - new DateTime(1970, 1, 1)).TotalMilliseconds;
                                                logger.Info("; {0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ; {7}", epochStart, userName, groupName, task, technique, difficulty, errors, epochStop);
                                                if (groupName != "T")
                                                {
                                                    ShowDoneTask();
                                                }
                                            }
                                            else
                                            {
                                                if (numberOfStrokes != highlightBoard.Strokes.Count())
                                                {
                                                    errors++;
                                                    numberOfStrokes = highlightBoard.Strokes.Count();
                                                }
                                            }
                                        }
                                        break;
                                    }
                                case 1:
                                    {
                                        foreach (System.Windows.Ink.Stroke strk in highlightBoard.Strokes)
                                        {
                                            if (!hlMedium &&
                                                Math.Abs(Canvas.GetTop(mediumRect) - (strk.GetBounds().Top + Canvas.GetTop(highlightBoard))) < 10 &&
                                                Math.Abs(Canvas.GetLeft(mediumRect) - (strk.GetBounds().Left + Canvas.GetLeft(highlightBoard))) < 10 &&
                                                Math.Abs((Canvas.GetLeft(mediumRect) - Canvas.GetLeft(highlightBoard) + mediumRect.Width) -
                                                    (strk.GetBounds().Left + strk.GetBounds().Width)) < 10 &&
                                                Math.Abs((Canvas.GetTop(mediumRect) - Canvas.GetTop(highlightBoard) + mediumRect.Height) -
                                                    (strk.GetBounds().Top + strk.GetBounds().Height)) < 10)
                                            {
                                                hlMedium = true;
                                                Console.WriteLine("YES - 2");
                                                //send log
                                                stopLog = DateTime.Now;
                                                epochStart = (long)(startLog - new DateTime(1970, 1, 1)).TotalMilliseconds;
                                                epochStop = (long)(stopLog - new DateTime(1970, 1, 1)).TotalMilliseconds;
                                                logger.Info("; {0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ; {7}", epochStart, userName, groupName, task, technique, difficulty, errors, epochStop);
                                                if (groupName != "T")
                                                {
                                                    ShowDoneTask();
                                                }
                                            }
                                            else
                                            {
                                                if (numberOfStrokes != highlightBoard.Strokes.Count())
                                                {
                                                    errors++;
                                                    numberOfStrokes = highlightBoard.Strokes.Count();
                                                }
                                            }
                                        }
                                        break;
                                    }
                                case 2:
                                    {
                                        foreach (System.Windows.Ink.Stroke strk in highlightBoard.Strokes)
                                        {
                                            if (!hlLong &&
                                                Math.Abs(Canvas.GetTop(longRect) - (strk.GetBounds().Top + Canvas.GetTop(highlightBoard))) < 3 &&
                                                Math.Abs(Canvas.GetLeft(longRect) - (strk.GetBounds().Left + Canvas.GetLeft(highlightBoard))) < 5 &&
                                                Math.Abs((Canvas.GetLeft(longRect) - Canvas.GetLeft(highlightBoard) + longRect.Width) -
                                                    (strk.GetBounds().Left + strk.GetBounds().Width)) < 5 &&
                                                Math.Abs((Canvas.GetTop(longRect) - Canvas.GetTop(highlightBoard) + longRect.Height) -
                                                    (strk.GetBounds().Top + strk.GetBounds().Height)) < 3)
                                            {
                                                hlLong = true;
                                                Console.WriteLine("YES - 3");
                                                //send log
                                                stopLog = DateTime.Now;
                                                epochStart = (long)(startLog - new DateTime(1970, 1, 1)).TotalMilliseconds;
                                                epochStop = (long)(stopLog - new DateTime(1970, 1, 1)).TotalMilliseconds;
                                                logger.Info("; {0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ; {7}", epochStart, userName, groupName, task, technique, difficulty, errors, epochStop);
                                                if (groupName != "T")
                                                {
                                                    ShowDoneTask();
                                                }
                                            }
                                            else
                                            {
                                                if (numberOfStrokes != highlightBoard.Strokes.Count())
                                                {
                                                    errors++;
                                                    numberOfStrokes = highlightBoard.Strokes.Count();
                                                }
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
                                    stopLog = DateTime.Now;
                                    epochStart = (long)(startLog - new DateTime(1970, 1, 1)).TotalMilliseconds;
                                    epochStop = (long)(stopLog - new DateTime(1970, 1, 1)).TotalMilliseconds;
                                    logger.Info("; {0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ; {7}", epochStart, userName, groupName, task, technique, difficulty, errors, epochStop);
                                    if (groupName != "T")
                                    {
                                        ShowDoneTask();
                                    }
                                }
                            }
                            else
                            {
                                isInside = false;
                            }
                        }
                    }
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

        /// <summary>
        /// Occurs when technique button is pressed.
        /// It change the technique in use.
        /// Showed only on Tester Mode
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnTechniqueClick(object s, RoutedEventArgs e)
        {
            switch (technique)
            {
                case 0:
                    {
                        HideContent();
                        technique = 1;
                        ShowContent();
                        break;
                    }
                case 1:
                    {
                        HideContent();
                        technique = 2;
                        ShowContent();
                        break;
                    }
                case 2:
                    {
                        HideContent();
                        technique = 3;
                        ShowContent();
                        break;
                    }
                case 3:
                    {
                        HideContent();
                        technique = 0;
                        ShowContent();
                        break;
                    }
            }
        }

        /// <summary>
        /// Occurs when highlight enabler button is pressed.
        /// It allow users to highlight on finger and always-on-pen Mode
        /// Showed only on finger and always-on-pen Mode.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnHlClick(object s, RoutedEventArgs e)
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

        /// <summary>
        /// Occurs when TouchDown Event inside a specific object is called.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnTouchDown(object s, System.Windows.Input.TouchEventArgs e)
        {
            e.Handled = true;
            // Check if the TouchEvent is caused by a finger or a Pen.
            if (trackLED.isAPen())
            {
                if (contourCircles != null)
                {
                    foreach (CircleF circle in contourCircles)
                    {
                        // Check if the TouchDevice is the pen, of finger, found processing the RAW image.
                        if ((System.Math.Abs(((int)e.TouchDevice.GetCenterPosition(this).X - (int)(circle.Center.X * 2))) < 15) &&
                            (System.Math.Abs(((int)e.TouchDevice.GetCenterPosition(this).Y - (int)(circle.Center.Y * 2))) < 15))
                        {
                            // Based on the task that is running and if actions are performed it allow to write/highlight/drag
                            switch (task)
                            {
                                case 0:
                                    {
                                        if ((technique == 0 && highlight) || (buttonTechnique && technique == 1) || (tiltTechnique && technique == 2))
                                        {
                                            // Save the time in which user start touching the surface to complete a task
                                            if (!isStarted)
                                            {
                                                startLog = DateTime.Now;
                                                isStarted = true;
                                            }
                                            e.Handled = false;
                                            highlightBoard.DefaultDrawingAttributes.Height = 16;
                                            highlightBoard.DefaultDrawingAttributes.Width = 1;
                                        }
                                        break;
                                    }
                                case 1:
                                    {
                                        if ((technique == 0 && drag) || (buttonTechnique && technique == 1) || (tiltTechnique && technique == 2))
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
                                        if ((technique == 0 && draw) || (buttonTechnique && technique == 1) || (tiltTechnique && technique == 2))
                                        {
                                            if (!isStarted)
                                            {
                                                startLog = DateTime.Now;
                                                isStarted = true;
                                            }
                                            e.Handled = false;
                                            drawBoard.UsesTouchShape = false;
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
            else
            {
                if (technique == 3)
                {
                    switch (task)
                    {
                        case 0:
                            {
                                if (highlight)
                                {
                                    if (!isStarted)
                                    {
                                        // Save the time in which user start touching the surface to complete a task
                                        startLog = DateTime.Now;
                                        isStarted = true;
                                    }
                                    e.Handled = false;
                                    highlightBoard.DefaultDrawingAttributes.Height = 8;
                                    highlightBoard.DefaultDrawingAttributes.Width = 1;
                                }
                                break;
                            }
                        case 1:
                            {
                                if (drag)
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
                                if (draw)
                                {
                                    if (!isStarted)
                                    {
                                        startLog = DateTime.Now;
                                        isStarted = true;
                                    }
                                    e.Handled = false;
                                    drawBoard.UsesTouchShape = false;
                                    drawBoard.DefaultDrawingAttributes.Height = 4;
                                    drawBoard.DefaultDrawingAttributes.Width = 4;
                                    drawBoard.DefaultDrawingAttributes.FitToCurve = false;
                                }
                                break;
                            }
                    }
                }
            }
        }

        /// <summary>
        /// Occurs when task button is pressed.
        /// It change the task in use.
        /// Showed only on Tester Mode
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnTaskClick(object s, RoutedEventArgs e)
        {
            switch (task)
            {
                case 0:
                    {
                        HideContent();
                        task = 1;
                        ShowContent();
                        break;
                    }
                case 1:
                    {
                        HideContent();
                        task = 2;
                        ShowContent();
                        break;
                    }
                case 2:
                    {
                        HideContent();
                        task = 0;
                        ShowContent();
                        break;
                    }
            }
        }

        /// <summary>
        /// Occurs when draw enabler button is pressed.
        /// It allow users to draw on finger and always-on-pen Mode
        /// Showed only on finger and always-on-pen Mode.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnDrawClick(object s, RoutedEventArgs e)
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

        /// <summary>
        /// Occurs on draw task when done button is pressed.
        /// It will be pressed when users are done drawing.
        /// It also send the log of the specific task and take a snapshot
        /// of the image drawed.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnDoneClick(object s, RoutedEventArgs e)
        {
            stopLog = DateTime.Now;
            epochStart = (long)(startLog - new DateTime(1970, 1, 1)).TotalMilliseconds;
            epochStop = (long)(stopLog - new DateTime(1970, 1, 1)).TotalMilliseconds;
            logger.Info("; {0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ; {7}", epochStart, userName, groupName, task, technique, difficulty, errors, epochStop);
            //take a snapshot
            string tmp = "C:\\AndreaInternship\\FinalVersion\\Logs\\" + userName + "-" + technique + "-" + difficulty + ".jpeg";
            sc.CaptureScreenToFile(tmp, System.Drawing.Imaging.ImageFormat.Jpeg);
            //show alert.
            if (groupName != "T")
            {
                ShowDoneTask();
                doneButton.Visibility = System.Windows.Visibility.Hidden;
            };
        }

        /// <summary>
        /// Occurs when TouchMove Event on Drag 'n Drop task is called.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnTouchMove(object s, System.Windows.Input.TouchEventArgs e)
        {
            e.Handled = true;

            if (e.TouchDevice == rectangleControlTouchDevice && drag)
            {
                // Get the current position of the contact.  
                System.Windows.Point currentTouchPoint = rectangleControlTouchDevice.GetCenterPosition(this);

                // Get the change between the controlling contact point and
                // the changed contact point.  
                double deltaX = currentTouchPoint.X - lastPoint.X;
                double deltaY = currentTouchPoint.Y - lastPoint.Y;

                // Get and then set a new top position and a new left position for the square.  
                double newTop = Canvas.GetTop(this.dragRectangle) + deltaY;
                double newLeft = Canvas.GetLeft(this.dragRectangle) + deltaX;

                // Update top and left position of the square
                Canvas.SetTop(this.dragRectangle, newTop);
                Canvas.SetLeft(this.dragRectangle, newLeft);

                // Forget the old contact point, and remember the new contact point.  
                lastPoint = currentTouchPoint;
            }
        }

        /// <summary>
        /// Occurs when TouchLeave Event on Drag 'n Drop task is called.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnTouchLeave(object s, System.Windows.Input.TouchEventArgs e)
        {
            // If this contact is the one that was remembered  
            if (e.TouchDevice == rectangleControlTouchDevice)
            {
                // Forget about this contact.
                rectangleControlTouchDevice = null;
                if (!isInside)
                    errors++;
            }
            // Mark this event as handled.  
            e.Handled = true;
        }

        /// <summary>
        /// Occurs when drag enabler button is pressed.
        /// It allow users to drag on finger and always-on-pen Mode
        /// Showed only on finger and always-on-pen Mode.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnDragClick(object s, RoutedEventArgs e)
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

        /// <summary>
        /// Occurs when difficulty button is pressed.
        /// It change the difficulty in use.
        /// Showed only on Tester Mode
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnDifficultyClick(object s, RoutedEventArgs e)
        {
            switch (difficulty)
            {
                case 0:
                    {
                        HideContent();
                        difficulty = 1;
                        ShowContent();
                        break;
                    }
                case 1:
                    {
                        HideContent();
                        difficulty = 2;
                        ShowContent();
                        break;
                    }
                case 2:
                    {
                        HideContent();
                        difficulty = 0;
                        ShowContent();
                        break;
                    }
            }
        }

        /// <summary>
        /// Ocurs when login button is pressed.
        /// It register users and group of appartenience --->(??!! is it a right word ??!!)
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnUserNameClick(object s, RoutedEventArgs e) // Change Method name on "OnLoginClick"
        {
            if (userTB.Text.Length != 0 && 
                (groupTB.Text == "A" || groupTB.Text == "B" || 
                 groupTB.Text == "C" || groupTB.Text == "D" ||
                 groupTB.Text == "T" || groupTB.Text == "P"))
            {
                userName = userTB.Text;
                groupName = groupTB.Text;
                HideLogin();
                if ((techniqueTB.Text == "0" || techniqueTB.Text == "1" ||
                    techniqueTB.Text == "2" || techniqueTB.Text == "3") &&
                    (taskTB.Text == "0" || taskTB.Text == "1" || taskTB.Text == "2"))
                {
                    technique = int.Parse(techniqueTB.Text);
                    task = int.Parse(taskTB.Text);
                    trainingMode = true;
                    WhichDifficulty();
                    ShowContent();
                    UserInformations();
                }
                else
                {
                    StartWith();
                }
                // Create a single-user Log file
                if (groupName != "P")
                {
                    FileTarget target = LogManager.Configuration.FindTargetByName("logFile") as FileTarget;
                    String logfile = "C:\\AndreaInternship\\FinalVersion\\Logs\\" + userName + "_Log.txt";
                    target.FileName = logfile;
                }
            }
        }

        /// <summary>
        /// Show content based on technique in use, task to do and difficulty to perform
        /// </summary>
        private void ShowContent()
        {
            isStarted = false;
            errors = 0;
            // Check if all combinations are done, if they are it show final content.
            if (!thankyou)
            {
                switch (technique)
                {
                    case 0:
                        {
                            modeButton.Content = "Always-On";
                            ShowTaskAndDifficulty();
                            break;
                        }
                    case 1:
                        {
                            modeButton.Content = "Button";
                            ShowTaskAndDifficulty();
                            break;
                        }
                    case 2:
                        {
                            modeButton.Content = "Tilt";
                            ShowTaskAndDifficulty();
                            break;
                        }
                    case 3:
                        {
                            modeButton.Content = "Finger";
                            ShowTaskAndDifficulty();
                            break;
                        }
                }
            }
            else
            {
                finishLabel.Visibility = System.Windows.Visibility.Visible;
            }
        }

        /// <summary>
        /// Show content. It is called by ShowContent method.
        /// </summary>
        private void ShowTaskAndDifficulty()
        {
            switch (task)
            {
                case 0:
                    {
                        taskButton.Content = "Highlight";
                        highlightBoard.Visibility = System.Windows.Visibility.Visible;
                        textBoard.Visibility = System.Windows.Visibility.Visible;
                        ClearButton.Visibility = System.Windows.Visibility.Visible;
                        // If technique is finger or always-on-pen Mode show relative button
                        if (technique == 0 || technique == 3)
                        {
                            highlightButton.Visibility = System.Windows.Visibility.Visible;
                        }
                        if (!trainingMode && !demoMode)
                        {
                            highlightLabel.Visibility = System.Windows.Visibility.Visible;
                            switch (difficulty)
                            {
                                case 0:
                                    {
                                        difficultyButton.Content = "Easy";
                                        shortRect.Visibility = System.Windows.Visibility.Visible;
                                        break;
                                    }
                                case 1:
                                    {
                                        difficultyButton.Content = "Medium";
                                        mediumRect.Visibility = System.Windows.Visibility.Visible;
                                        break;
                                    }
                                case 2:
                                    {
                                        difficultyButton.Content = "Hard";
                                        longRect.Visibility = System.Windows.Visibility.Visible;
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            ShowTrainingLabel();
                            trainingLabel.Visibility = System.Windows.Visibility.Visible;
                            if(!demoMode)
                                trainigButton.Visibility = System.Windows.Visibility.Visible;
                        }
                        break;
                    }
                case 1:
                    {
                        taskButton.Content = "Drag 'n Drop";
                        dragRectangle.Visibility = System.Windows.Visibility.Visible;
                        if (technique == 0 || technique == 3)
                        {
                            selectButton.Visibility = System.Windows.Visibility.Visible; 
                        }
                        if (!trainingMode && !demoMode)
                        {
                            DragLabel.Visibility = System.Windows.Visibility.Visible;
                            theBox.Visibility = System.Windows.Visibility.Visible;
                            switch (difficulty)
                            {
                                case 0:
                                    {
                                        difficultyButton.Content = "Easy";
                                        Canvas.SetTop(dragRectangle, 400);
                                        Canvas.SetLeft(dragRectangle, 550);
                                        theBox.Width = 182;
                                        theBox.Height = 182;
                                        break;
                                    }
                                case 1:
                                    {
                                        difficultyButton.Content = "Medium";
                                        Canvas.SetTop(dragRectangle, 400);
                                        Canvas.SetLeft(dragRectangle, 550);
                                        theBox.Width = 166;
                                        theBox.Height = 166;
                                        break;
                                    }
                                case 2:
                                    {
                                        difficultyButton.Content = "Hard";
                                        Canvas.SetTop(dragRectangle, 400);
                                        Canvas.SetLeft(dragRectangle, 550);
                                        theBox.Width = 158;
                                        theBox.Height = 158;
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            ShowTrainingLabel();
                            Canvas.SetTop(dragRectangle, 400);
                            Canvas.SetLeft(dragRectangle, 550);
                            trainingLabel.Visibility = System.Windows.Visibility.Visible;
                            if (!demoMode)
                                trainigButton.Visibility = System.Windows.Visibility.Visible;
                        }
                        break;
                    }
                case 2:
                    {
                        taskButton.Content = "Write";
                        ClearButton.Visibility = System.Windows.Visibility.Visible;
                        drawBoard.Visibility = System.Windows.Visibility.Visible;
                        drawBoard.Background = System.Windows.Media.Brushes.Black;
                        if (technique == 0 || technique == 3)
                        {
                            drawButton.Visibility = System.Windows.Visibility.Visible;
                        }
                        if (!trainingMode && !demoMode)
                        {
                            drawBoard.Background = System.Windows.Media.Brushes.Transparent;
                            doneButton.Visibility = System.Windows.Visibility.Visible;
                            wordDrawLabel.Visibility = System.Windows.Visibility.Visible;
                            drawLable.Visibility = System.Windows.Visibility.Visible;
                            
                            switch (difficulty)
                            {
                                case 0:
                                    {
                                        difficultyButton.Content = "Easy";
                                        wordDrawLabel.Content = "pIT lab";
                                        wordDrawLabel.FontSize = 200;
                                        wordDrawLabel.FontFamily = new FontFamily("Segoe360");
                                        break;
                                    }
                                case 1:
                                    {
                                        difficultyButton.Content = "Medium";
                                        wordDrawLabel.Content = "Forza Palermo";
                                        wordDrawLabel.FontSize = 180;
                                        wordDrawLabel.FontFamily = new FontFamily("Gabriola");
                                        break;
                                    }
                                case 2:
                                    {
                                        difficultyButton.Content = "Hard";
                                        wordDrawLabel.Content = "Andrea's ITU Internship";
                                        wordDrawLabel.FontSize = 72;
                                        wordDrawLabel.FontFamily = new FontFamily("Segoe Script");
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            ShowTrainingLabel();
                            trainingLabel.Visibility = System.Windows.Visibility.Visible;
                            if(!demoMode)
                                trainigButton.Visibility = System.Windows.Visibility.Visible;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Hide content based on technique in use, task to do and difficulty to perform
        /// </summary>
        private void HideContent()
        {
            switch (task)
            {
                case 0:
                    {   
                        if (!trainingMode && !demoMode)
                        {
                            highlightLabel.Visibility = System.Windows.Visibility.Hidden;
                        }
                        else
                        {
                            trainingLabel.Visibility = System.Windows.Visibility.Hidden;
                            trainigButton.Visibility = System.Windows.Visibility.Hidden;
                        }
                        highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                        highlightBoard.Visibility = System.Windows.Visibility.Hidden;
                        highlightBoard.Strokes.Clear();
                        ClearButton.Visibility = System.Windows.Visibility.Hidden;
                        hlLong = hlShort = hlMedium = false;

                        textBoard.Visibility = System.Windows.Visibility.Hidden;

                        if (technique == 0 || technique == 3)
                        {
                            highlightButton.Visibility = System.Windows.Visibility.Hidden;
                            highlightButton.Background = Brushes.Silver;

                        }
                        mediumRect.Visibility = System.Windows.Visibility.Hidden;
                        shortRect.Visibility = System.Windows.Visibility.Hidden;
                        longRect.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    }
                case 1:
                    {
                        drag = false;
                        if (!trainingMode && !demoMode)
                        {
                            DragLabel.Visibility = System.Windows.Visibility.Hidden;
                        }
                        else
                        {
                            trainingLabel.Visibility = System.Windows.Visibility.Hidden;
                            trainigButton.Visibility = System.Windows.Visibility.Hidden;
                        }
                        dragRectangle.Visibility = System.Windows.Visibility.Hidden;
                        theBox.Visibility = System.Windows.Visibility.Hidden;

                        if (technique == 0 || technique == 3)
                        {
                            selectButton.Visibility = System.Windows.Visibility.Hidden;
                            selectButton.Background = Brushes.Silver;
                        }
                        break;
                    }
                case 2:
                    {
                        drawBoard.EditingMode = SurfaceInkEditingMode.None;
                        drawBoard.Visibility = System.Windows.Visibility.Hidden;
                        drawBoard.Strokes.Clear();
                        ClearButton.Visibility = System.Windows.Visibility.Hidden;
                        if (!trainingMode && !demoMode)
                        {
                            drawLable.Visibility = System.Windows.Visibility.Hidden;
                        }
                        else
                        {
                            trainingLabel.Visibility = System.Windows.Visibility.Hidden;
                            trainigButton.Visibility = System.Windows.Visibility.Hidden;
                        }
                        doneButton.Visibility = System.Windows.Visibility.Hidden;

                        wordDrawLabel.Visibility = System.Windows.Visibility.Hidden;

                        if (technique == 0 || technique == 3)
                        {
                            drawButton.Background = Brushes.Silver;
                            drawButton.Visibility = System.Windows.Visibility.Hidden;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Show user name and appartenience group
        /// </summary>
        private void UserInformations()
        {
            showNameLabel.Content = "Name: " + userName;
            showGroupLabel.Content = "Group: " + groupName;
            showNameLabel.Visibility = System.Windows.Visibility.Visible;
            showGroupLabel.Visibility = System.Windows.Visibility.Visible;
            showTechnique.Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// Occurs when next task button is pressed.
        /// Based on group, technique, task and difficulty it change contents.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void OnNextClick(object s, RoutedEventArgs e)
        {
            errors = 0;
            HideDoneTask();
            HideContent();
            if (groupName != "T")
            {
                if (numDifficulty != 2)
                {
                    numDifficulty++;
                    WhichDifficulty();
                }
                else
                {
                    numDifficulty = 0;
                    WhichDifficulty();
                    if (loop != 2 && task != 2)
                    {
                        loop++;
                    }
                    else
                    {
                        loop = 0;
                        trainingMode = true;
                        if (task != 2)
                        {
                            task++;
                        }
                        else
                        {
                            ChangeTechnique();
                            task = 0;
                        }
                    }
                }
                ShowContent();
            }
        }

        /// <summary>
        /// It change technique in use, based on group type.
        /// It is called by OnNextClick method.
        /// </summary>
        private void ChangeTechnique()
        {
            switch (groupName)
            {
                // technique: 0-1-3-2
                case "A":
                    {
                        switch (technique)
                        {
                            case 0:
                                {
                                    showTechnique.Content = "Button Pen";
                                    technique = 1;
                                    break;
                                }
                            case 1:
                                {
                                    showTechnique.Content = "Finger";
                                    technique = 3;
                                    break;
                                }
                            case 2:
                                {
                                    thankyou = true;
                                    break;
                                }
                            case 3:
                                {
                                    showTechnique.Content = "Tilt Pen";
                                    technique = 2;
                                    break;
                                }
                        }
                        break;
                    }
                // technique: 1-2-0-3
                case "B":
                    {
                        switch (technique)
                        {
                            case 0:
                                {
                                    showTechnique.Content = "Finger";
                                    technique = 3;
                                    break;
                                }
                            case 1:
                                {
                                    showTechnique.Content = "Tilt Pen";
                                    technique = 2;
                                    break;
                                }
                            case 2:
                                {
                                    showTechnique.Content = "Always-On Pen";
                                    technique = 0;
                                    break;
                                }
                            case 3:
                                {
                                    thankyou = true;
                                    break;
                                }
                        }
                        break;
                    }
                // technique: 2-3-1-0
                case "C":
                    {
                        switch (technique)
                        {
                            case 0:
                                {
                                    thankyou = true;
                                    break;
                                }
                            case 1:
                                {
                                    showTechnique.Content = "Always-On Pen";
                                    technique = 0;
                                    break;
                                }
                            case 2:
                                {
                                    showTechnique.Content = "Finger";
                                    technique = 3;
                                    break;
                                }
                            case 3:
                                {
                                    showTechnique.Content = "Button Pen";
                                    technique = 1;
                                    break;
                                }
                        }
                        break;
                    }
                // technique: 3-0-2-1
                case "D":
                    {
                        switch (technique)
                        {
                            case 0:
                                {
                                    showTechnique.Content = "Tilt Pen";
                                    technique = 2;
                                    break;
                                }
                            case 1:
                                {
                                    thankyou = true;
                                    break;
                                }
                            case 2:
                                {
                                    showTechnique.Content = "Button Pen";
                                    technique = 1;
                                    break;
                                }
                            case 3:
                                {
                                    showTechnique.Content = "Always-On Pen";
                                    technique = 0;
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// based on group name, decide with which technique start experiment.
        /// </summary>
        private void StartWith()
        {
            switch (groupName)
            {
                case "A":
                    {
                        // technique: 0-1-3-2
                        technique = 0;
                        showTechnique.Content = "Always-On Pen";
                        trainingMode = true;
                        WhichDifficulty();
                        ShowContent();
                        UserInformations();
                        break;
                    }
                case "B":
                    {
                        // technique: 1-2-0-3
                        technique = 1;
                        showTechnique.Content = "Button Pen";
                        trainingMode = true;
                        WhichDifficulty();
                        ShowContent();
                        UserInformations();
                        break;
                    }
                case "C":
                    {
                        // technique: 2-3-1-0
                        technique = 2;
                        showTechnique.Content = "Tilt Pen";
                        trainingMode = true;
                        WhichDifficulty();
                        ShowContent();
                        UserInformations();
                        break;
                    }
                case "D":
                    {
                        // technique: 3-0-2-1
                        technique = 3;
                        showTechnique.Content = "Finger";
                        trainingMode = true;
                        WhichDifficulty();
                        ShowContent();
                        UserInformations();
                        break;
                    }
                case "T":
                    {
                        // test mode
                        trainingMode = false;
                        difficultyButton.Visibility = System.Windows.Visibility.Visible;
                        taskButton.Visibility = System.Windows.Visibility.Visible;
                        modeButton.Visibility = System.Windows.Visibility.Visible;
                        ShowContent();
                        break;
                    }
                case "P":
                    {
                        trainingMode = false;
                        demoMode = true;
                        taskButton.Visibility = System.Windows.Visibility.Visible;
                        modeButton.Visibility = System.Windows.Visibility.Visible;
                        ShowContent();
                        break;
                    }
            } 
        }

        /// <summary>
        /// Show labels and the button for every task that is done.
        /// </summary>
        private void ShowDoneTask()
        {
            // Show labels and the button for Done Tasks
            nextLabel1.Visibility = System.Windows.Visibility.Visible;
            nextLabel2.Visibility = System.Windows.Visibility.Visible;
            nextButton.Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// Hide labels and the button for every task that is done.
        /// </summary>
        private void HideDoneTask()
        {
            // Hide labels and the button for Done Tasks
            nextLabel1.Visibility = System.Windows.Visibility.Hidden;
            nextLabel2.Visibility = System.Windows.Visibility.Hidden;
            nextButton.Visibility = System.Windows.Visibility.Hidden;
        }

        /// <summary>
        /// Occurs when training button is pressed.
        /// It stop training mode and start the experiment.
        /// </summary>
        private void OnTrainingClick(object s, RoutedEventArgs e)
        {
            HideContent();
            trainingMode = false;
            ShowContent();
        }

        /// <summary>
        /// It hide all login contents
        /// </summary>
        private void HideLogin()
        {
            userNameButton.Visibility = System.Windows.Visibility.Hidden;
            userNameLabel.Visibility = System.Windows.Visibility.Hidden;
            userTB.Visibility = System.Windows.Visibility.Hidden;
            groupTB.Visibility = System.Windows.Visibility.Hidden;
            groupLabel.Visibility = System.Windows.Visibility.Hidden;
            taskTB.Visibility = System.Windows.Visibility.Hidden;
            techniqueTB.Visibility = System.Windows.Visibility.Hidden;
        }

        /// <summary>
        /// It check, depending on technique in use, if all condictions are verified
        /// and enable TouchEvent.
        /// </summary>
        private void TechniqueEnabler()
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
                                    case 1:
                                        {
                                            drag = true;
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
                                    case 1:
                                        {
                                            drag = false;
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
                            if ((rwAcc[0] <= 0.85))
                            {
                                tiltTechnique = true;
                                switch (task)
                                {
                                    case 0:
                                        {
                                            highlightBoard.EditingMode = SurfaceInkEditingMode.Ink;
                                            break;
                                        }
                                    case 1:
                                        {
                                            drag = true;
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
                                    case 1:
                                        {
                                            drag = false;
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

        private void ShowTrainingLabel()
        {
            switch (technique)
            {
                case 0:
                    {
                        switch (task)
                        {
                            case 0:
                                {
                                    trainingLabel.Content = "Try to highlight some words, with the pen."+
                                        " Click on the button on your bottom left (also with your finger) to activate highlight.";
                                    break;
                                }
                            case 1:
                                {
                                    trainingLabel.Content = "Try to drag and drop the red square around, with the pen." +
                                        " Click on the button on your bottom left (also with your finger) to activate drag 'n drop.";
                                    break;
                                }
                            case 2:
                                {
                                    trainingLabel.Content = "Try to draw some words, with the pen." +
                                        " Click on the button on your bottom left (also with your finger) to activate ink.";
                                    break;
                                }
                        }
                        break;
                    }
                case 1:
                    {
                        switch (task)
                        {
                            case 0:
                                {
                                    trainingLabel.Content = "Try to highlight some words, with the pen." +
                                        " Hold the button inside the pen to activate highlight.";
                                    break;
                                }
                            case 1:
                                {
                                    trainingLabel.Content = "Try to drag and drop the red square around, with the pen." +
                                        " Hold the button inside the pen to activate drag 'n drop.";
                                    break;
                                }
                            case 2:
                                {
                                    trainingLabel.Content = "Try to draw some words, with the pen." +
                                        " Hold the button inside the pen to activate ink.";
                                    break;
                                }
                        }
                        break;
                    }
                case 2:
                    {
                        switch (task)
                        {
                            case 0:
                                {
                                    trainingLabel.Content = "Try to highlight some words, with the pen." +
                                        " Tilt the pen to activate highlight.";
                                    break;
                                }
                            case 1:
                                {
                                    trainingLabel.Content = "Try to drag and drop the red square around, with the pen." +
                                        " Tilt the pen to activate drag 'n drop.";
                                    break;
                                }
                            case 2:
                                {
                                    trainingLabel.Content = "Try to draw some words, with the pen." +
                                        " Tilt the pen to activate ink.";
                                    break;
                                }
                        }
                        break;
                    }
                case 3:
                    {
                        switch (task)
                        {
                            case 0:
                                {
                                    trainingLabel.Content = "Try to highlight some words, with the finger." +
                                        " Click on the button on your bottom left to activate highlight.";
                                    break;
                                }
                            case 1:
                                {
                                    trainingLabel.Content = "Try to drag and drop the red square around, with the finger." +
                                        " Click on the button on your bottom left to activate drag 'n drop.";
                                    break;
                                }
                            case 2:
                                {
                                    trainingLabel.Content = "Try to draw some words, with the finger." +
                                        " Click on the button on your bottom left to activate ink.";
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        private void WhichDifficulty()
        {
            switch (numDifficulty)
            {
                case 0:
                    {
                        difficulty = random.Next(0, 2);
                        lastDifficulty = difficulty;
                        break;
                    }
                case 1:
                    {
                        switch (difficulty)
                        {
                            case 0:
                                {
                                    difficulty = random.Next(1, 2);
                                    break;
                                }
                            case 1:
                                {
                                    while (difficulty == 1)
                                    {
                                        difficulty = random.Next(0, 2);
                                    }
                                    break;
                                }
                            case 2:
                                {
                                    difficulty = random.Next(0, 1);
                                    break;
                                }
                        }
                        break;
                    }
                case 2:
                    {
                        switch (difficulty)
                        {
                            case 0:
                                {
                                    switch (lastDifficulty)
                                    {
                                        case 1:
                                            {
                                                difficulty = 2;
                                                break;
                                            }
                                        case 2:
                                            {
                                                difficulty = 1;
                                                break;
                                            }
                                    }
                                    break;
                                }
                            case 1:
                                {
                                    switch (lastDifficulty)
                                    {
                                        case 0:
                                            {
                                                difficulty = 2;
                                                break;
                                            }
                                        case 2:
                                            {
                                                difficulty = 0;
                                                break;
                                            }
                                    }
                                    break;
                                }
                            case 2:
                                {
                                    switch (lastDifficulty)
                                    {
                                        case 0:
                                            {
                                                difficulty = 1;
                                                break;
                                            }
                                        case 1:
                                            {
                                                difficulty = 0;
                                                break;
                                            }
                                    }
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        private void OnClearClick(object s, RoutedEventArgs e)
        {
            if (highlightBoard.Strokes.Count() != 0)
            {
                highlightBoard.Strokes.Clear();
                numberOfStrokes = 0;
            }
            if (drawBoard.Strokes.Count() != 0)
            {
                drawBoard.Strokes.Clear();
            }
        }
    }
}