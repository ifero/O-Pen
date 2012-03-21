using System;
using System.Collections.Generic;
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
        private TouchDevice rectangleControlTouchDevice;
        private static int finger = 150;
        private static int pen = 254;
        private TouchTarget touchTarget;
        private CircleF[] contourCircles;
        private IntPtr hwnd;
        private ImageMetrics normalizedMetrics;
        private TimeSpan diffTime;
        private DateTime currentTime;
        private byte[] normalizedImage;
        private bool imageAvailable;
        private Tracking trackLED;
        private System.Windows.Point lastPoint;
        private int mode, task;
        private bool mode2, mode3;
        private bool highlight, annotate;
        private bool draw;
        private bool hlWord1, hlWord2, hlWord3;
        private bool hlDone, annotateDone, drawDone;
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
            mode2 = false;
            mode3 = false;
            hlWord1 = false;
            hlWord2 = false;
            hlWord3 = false;
            hlDone = false;
            annotateDone = false;
            split = null;
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
            mode = 0;
            highlight = false;
            annotate = false;
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
                        switch (mode)
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
                if(mode != 3)
                    contourCircles = trackLED.TrackContours(normalizedMetrics, normalizedImage, pen);
                else
                    contourCircles = trackLED.TrackContours(normalizedMetrics, normalizedImage, finger);
                currentTime = DateTime.Now;
            }

            if (task == 1)
            {
                if (rectangleControlTouchDevice == null)
                {
                    if ((Canvas.GetTop(this.dragRectangle) > Canvas.GetTop(this.theBox) && (Canvas.GetTop(this.dragRectangle) - Canvas.GetTop(this.theBox)) < 50)
                     && (Canvas.GetLeft(this.dragRectangle) > Canvas.GetLeft(this.theBox) && (Canvas.GetLeft(this.dragRectangle) - Canvas.GetLeft(this.theBox)) < 50))
                    {
                        if (!isInside)
                        {
                            isInside = true;
                        }
                    }
                    else isInside = false;
                }
            }

            if (split != null)
            {
                switch (mode)
                {
                    case 1:
                        {
                            if (button == 1)
                            {
                                mode2 = true;
                                switch (task)
                                {
                                    case 0:
                                        {
                                            highlightBoard.EditingMode = SurfaceInkEditingMode.Ink;
                                            break;
                                        }
                                    //case 1:
                                    //    {
                                    //        //annotateBoard.EditingMode = SurfaceInkEditingMode.Ink; <-- Change with Drag 'n Drop
                                    //        break;
                                    //    }
                                    case 2:
                                        {
                                            drawBoard.EditingMode = SurfaceInkEditingMode.Ink;
                                            break;
                                        }
                                }
                            }
                            else
                            {
                                mode2 = false;
                                switch (task)
                                {
                                    case 0:
                                        {
                                            highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                                            break;
                                        }
                                    //case 1:
                                    //    {
                                    //        //annotateBoard.EditingMode = SurfaceInkEditingMode.None; <-- Change with Drag 'n Drop
                                    //        break;
                                    //    }
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
                                mode3 = true;
                                switch (task)
                                {
                                    case 0:
                                        {
                                            highlightBoard.EditingMode = SurfaceInkEditingMode.Ink;
                                            break;
                                        }
                                    //case 1:
                                    //    {
                                    //        //annotateBoard.EditingMode = SurfaceInkEditingMode.Ink; <-- Change with Drag 'n Drop
                                    //        break;
                                    //    }
                                    case 2:
                                        {
                                            drawBoard.EditingMode = SurfaceInkEditingMode.None;
                                            break;
                                        }
                                }
                            }
                            else
                            {
                                mode3 = false;
                                switch (task)
                                {
                                    case 0:
                                        {
                                            highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                                            break;
                                        }
                                    //case 1:
                                    //    {
                                    //        //annotateBoard.EditingMode = SurfaceInkEditingMode.None; <-- Change with Drag 'n Drop
                                    //        break;
                                    //    }
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

            if (highlightBoard.Strokes.Count() != 0)
            {
                foreach (System.Windows.Ink.Stroke strk in highlightBoard.Strokes)
                {
                    if (!hlWord1 &&
                        Math.Abs(Canvas.GetTop(mediumRect) - (strk.GetBounds().Top + Canvas.GetTop(highlightBoard))) < 50 &&
                        Math.Abs(Canvas.GetLeft(mediumRect) - (strk.GetBounds().Left + Canvas.GetLeft(highlightBoard))) < 50 &&
                        Math.Abs((Canvas.GetLeft(mediumRect) - Canvas.GetLeft(highlightBoard) + mediumRect.Width) -
                                (strk.GetBounds().Left + strk.GetBounds().Width)) < 50 &&
                        Math.Abs((Canvas.GetTop(mediumRect) - Canvas.GetTop(highlightBoard) + mediumRect.Height) -
                                (strk.GetBounds().Top + strk.GetBounds().Height)) < 50)
                    {
                        hlWord1 = true;
                        Console.WriteLine("YES - 1");
                    }
                    if (!hlWord2 &&
                        Math.Abs(Canvas.GetTop(shortRect) - (strk.GetBounds().Top + Canvas.GetTop(highlightBoard))) < 50 &&
                        Math.Abs(Canvas.GetLeft(shortRect) - (strk.GetBounds().Left + Canvas.GetLeft(highlightBoard))) < 50 &&
                        Math.Abs((Canvas.GetLeft(shortRect) - Canvas.GetLeft(highlightBoard) + shortRect.Width) -
                                (strk.GetBounds().Left + strk.GetBounds().Width)) < 50 &&
                        Math.Abs((Canvas.GetTop(shortRect) - Canvas.GetTop(highlightBoard) + shortRect.Height) -
                                (strk.GetBounds().Top + strk.GetBounds().Height)) < 50)
                    {
                        hlWord2 = true;
                        Console.WriteLine("YES - 2");
                    }
                    if (!hlWord3 &&
                        Math.Abs(Canvas.GetTop(longRect) - (strk.GetBounds().Top + Canvas.GetTop(highlightBoard))) < 50 &&
                        Math.Abs(Canvas.GetLeft(longRect) - (strk.GetBounds().Left + Canvas.GetLeft(highlightBoard))) < 50 &&
                        Math.Abs((Canvas.GetLeft(longRect) - Canvas.GetLeft(highlightBoard) + longRect.Width) -
                                (strk.GetBounds().Left + strk.GetBounds().Width)) < 50 &&
                        Math.Abs((Canvas.GetTop(longRect) - Canvas.GetTop(highlightBoard) + longRect.Height) -
                                (strk.GetBounds().Top + strk.GetBounds().Height)) < 50)
                    {
                        hlWord3 = true;
                        Console.WriteLine("YES - 3");
                    }

                }
                // To Be changed! 
                //if (hlWord1 && hlWord2 && hlWord3 && !hlDone)
                //{
                //    hlDone = true;
                //    //stop timer, send log
                //}
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

        private void onModeClick(object s, RoutedEventArgs e)
        {
            switch (mode)
            {
                case 0:
                    {
                        mode = 1;
                        modeButton.Content = "Pen Mode 2";
                        switch (task)
                        {
                            case 0:
                                {
                                    highlightButton.Visibility = System.Windows.Visibility.Hidden;
                                    highlightButton.Background = Brushes.Silver;
                                    highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                                    break;
                                }
                            case 1:
                                {
                                    //annotateButton.Visibility = System.Windows.Visibility.Hidden;
                                    //annotateButton.Background = Brushes.Silver;                  <-- replace with DnD
                                    //annotateBoard.EditingMode = SurfaceInkEditingMode.None;
                                    break;
                                }
                            case 2:
                                {
                                    drawButton.Visibility = System.Windows.Visibility.Hidden;
                                    drawButton.Background = Brushes.Silver;
                                    clearButton.Visibility = System.Windows.Visibility.Hidden;
                                    clearButton.Background = Brushes.Silver;
                                    drawBoard.EditingMode = SurfaceInkEditingMode.None;
                                    break;
                                }
                        }
                        break;
                    }
                case 1:
                    {
                        mode = 2;
                        modeButton.Content = "Pen Mode 3";
                        break;
                    }
                case 2:
                    {
                        mode = 0;
                        modeButton.Content = "Pen Mode 1";
                        switch (task)
                        {
                            case 0:
                                {
                                    highlightButton.Visibility = System.Windows.Visibility.Visible;
                                    highlightButton.Background = Brushes.Silver;
                                    highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                                    break;
                                }
                            case 1:
                                {
                                    //annotateButton.Visibility = System.Windows.Visibility.Visible;
                                    //annotateButton.Background = Brushes.Silver;
                                    //annotateBoard.EditingMode = SurfaceInkEditingMode.None;
                                    break;
                                }
                            case 2:
                                {
                                    drawButton.Visibility = System.Windows.Visibility.Visible;
                                    drawButton.Background = Brushes.Silver;
                                    clearButton.Visibility = System.Windows.Visibility.Visible;
                                    clearButton.Background = Brushes.Silver;
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
                                        if (mode == 0 || (mode2 && mode == 1) || (mode3 && mode == 2))
                                        {
                                            e.Handled = false;
                                            highlightBoard.DefaultDrawingAttributes.Height = circle.Radius * 2;
                                            highlightBoard.DefaultDrawingAttributes.Width = circle.Radius * 2;
                                            highlightBoard.DefaultDrawingAttributes.FitToCurve = false;
                                        }
                                        break;
                                    }
                                case 1:
                                    {
                                        if (mode == 0 || (mode2 && mode == 1) || (mode3 && mode == 2))
                                        {
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
                                        if (mode == 0 || (mode2 && mode == 1) || (mode3 && mode == 2))
                                        {
                                            e.Handled = false;
                                            drawBoard.DefaultDrawingAttributes.Height = circle.Radius * 2;
                                            drawBoard.DefaultDrawingAttributes.Width = circle.Radius * 2;
                                            drawBoard.DefaultDrawingAttributes.FitToCurve = false;
                                        }
                                        break;
                                    }
                                case 3:
                                    {
                                        //finger recognition
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
                        task = 1;
                        taskButton.Content = "Task2 - DnD";
                        highlightLabel.Visibility = System.Windows.Visibility.Hidden;
                        highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                        highlightBoard.Visibility = System.Windows.Visibility.Hidden;
                        dragRectangle.Visibility = System.Windows.Visibility.Visible;
                        theBox.Visibility = System.Windows.Visibility.Visible;
                        textBoard.Visibility = System.Windows.Visibility.Hidden;
                        if (mode == 0)
                        {
                            highlightButton.Visibility = System.Windows.Visibility.Hidden;
                            highlightButton.Background = Brushes.Silver;
                            selectButton.Visibility = System.Windows.Visibility.Visible;
                            selectButton.Background = Brushes.Silver;
                        }
                        break;
                    }
                case 1:
                    {
                        task = 2;
                        taskButton.Content = "Task3 - INK";
                        dragRectangle.Visibility = System.Windows.Visibility.Hidden;
                        theBox.Visibility = System.Windows.Visibility.Hidden;
                        drawLable.Visibility = System.Windows.Visibility.Visible;
                        drawBoard.Visibility = System.Windows.Visibility.Visible;
                        if (mode == 0)
                        {
                            selectButton.Visibility = System.Windows.Visibility.Hidden;
                            selectButton.Background = Brushes.Silver;
                            drawButton.Background = Brushes.Silver;
                            drawButton.Visibility = System.Windows.Visibility.Visible;
                            clearButton.Visibility = System.Windows.Visibility.Visible;
                        }
                        break;
                    }
                case 2:
                    {
                        task = 0;
                        taskButton.Content = "Task1 - HL";
                        drawLable.Visibility = System.Windows.Visibility.Hidden;
                        highlightLabel.Visibility = System.Windows.Visibility.Visible;
                        highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                        drawBoard.EditingMode = SurfaceInkEditingMode.None;
                        highlightBoard.Visibility = System.Windows.Visibility.Visible;
                        textBoard.Visibility = System.Windows.Visibility.Visible;
                        drawBoard.Visibility = System.Windows.Visibility.Hidden;
                        if (mode == 0)
                        {
                            drawButton.Background = Brushes.Silver;
                            drawButton.Visibility = System.Windows.Visibility.Hidden;
                            clearButton.Visibility = System.Windows.Visibility.Hidden;
                            highlightButton.Visibility = System.Windows.Visibility.Visible;
                            highlightButton.Background = Brushes.Silver;
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
                drawButton.Background = Brushes.Blue;
            }
            else
            {
                draw = false;
                drawBoard.EditingMode = SurfaceInkEditingMode.None;
                drawButton.Background = Brushes.Silver;
            }
        }

        private void onClearClick(object s, RoutedEventArgs e)
        {
            drawBoard.Strokes.Clear();
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
    }
}