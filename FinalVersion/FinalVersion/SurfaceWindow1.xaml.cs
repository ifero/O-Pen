#region Things to do better
/*
 * - Create several methods that handle all this changes of state
 * - Create several classes that handle all this methods
 * - Insert the words to remark for each difficulty
 * - create the log and integrate it with log4net.
 * - Create some sort of class for all this variables
 * - Maybe create a class for each task?!
 * 
 */
#endregion


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
        private int technique, task, difficulty;
        private bool buttonTechnique, tiltTechnique;
        private bool highlight;
        private bool draw;
        private bool drag;
        private bool hlShort, hlMedium, hlLong;
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
            buttonTechnique = false;
            tiltTechnique = false;
            hlMedium = false;
            hlShort = false;
            hlLong = false;
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
                                            drawBoard.EditingMode = SurfaceInkEditingMode.None;
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
                                        Console.WriteLine("YES - 2");
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
                                        Console.WriteLine("YES - 1");
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
                                        }
                                }
                                break;
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

        private void onTechniqueClick(object s, RoutedEventArgs e)
        {
            switch (technique)
            {
                case 0:
                    {
                        technique = 1;
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
                                    selectButton.Visibility = System.Windows.Visibility.Hidden;
                                    selectButton.Background = Brushes.Silver;
                                    drag = false;
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
                        technique = 2;
                        modeButton.Content = "Pen Mode 3";
                        break;
                    }
                case 2:
                    {
                        technique = 3;
                        modeButton.Content = "Finger";
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
                                    selectButton.Visibility = System.Windows.Visibility.Visible;
                                    selectButton.Background = Brushes.Silver;
                                    drag = false;
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
                case 3:
                    {
                        technique = 0;
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
                                    selectButton.Visibility = System.Windows.Visibility.Visible;
                                    selectButton.Background = Brushes.Silver;
                                    drag = false;
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
                                        if (technique == 0 || (buttonTechnique && technique == 1) || (tiltTechnique && technique == 2) || technique == 3)
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
                                        if (technique == 0 || (buttonTechnique && technique == 1) || (tiltTechnique && technique == 2) || (technique == 3 && drag))
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
                                        if (technique == 0 || (buttonTechnique && technique == 1) || (tiltTechnique && technique == 2) || technique == 3)
                                        {
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
                        task = 1;
                        taskButton.Content = "Task2 - DnD";
                        highlightLabel.Visibility = System.Windows.Visibility.Hidden;
                        highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                        highlightBoard.Visibility = System.Windows.Visibility.Hidden;
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
                        if (technique == 0 || technique == 3)
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
                        if (technique == 0 || technique == 3)
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
                        difficulty = 1;
                        difficultyButton.Content = "Medium";
                        switch (task)
                        {
                            case 0:
                                {
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
                                    break;
                                }
                            case 2:
                                {
                                    break;
                                }
                        }
                        break;
                    }
                case 1:
                    {
                        difficulty = 2;
                        difficultyButton.Content = "Hard";
                        switch (task)
                        {
                            case 0:
                                {
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
                                    break;
                                }
                            case 2:
                                {
                                    break;
                                }
                        }
                        break;
                    }
                case 2:
                    {
                        difficulty = 0;
                        difficultyButton.Content = "Easy";
                        switch (task)
                        {
                            case 0:
                                {
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
                                    break;
                                }
                            case 2:
                                {
                                    break;
                                }
                        }
                        break;
                    }
            }
        }
    }
}