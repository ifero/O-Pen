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
        private int mode, task;
        private bool mode2, mode3;
        private bool highlight, annotate;
        private bool draw;
        private bool hlWord1,hlWord2,hlWord3;
        private SerialPort sp;
        private String[] split;
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
            split = null;
            rwAcc = new float[3];
            rwGyro = new float[3];
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
            annotate = false;
            highlightBoard.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.Yellow;
            annotateBoard.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.Black;
            drawBoard.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.White;
            InitializeSurfaceInput();
            // Add handlers for window availability events
            AddWindowAvailabilityHandlers();
        }

        private void InitializeSurfaceInput()
        {
            // Release all inputs
            highlightBoard.ReleaseAllCaptures();
            annotateBoard.ReleaseAllCaptures();
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
                            case 3:
                                {
                                    rwGyro[0] = float.Parse(split[4]) / 100;
                                    rwGyro[1] = float.Parse(split[5]) / 100;
                                    rwGyro[2] = float.Parse(split[6]) / 100;
                                    break;
                                }
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
                contourCircles = trackLED.TrackContours(normalizedMetrics, normalizedImage);
                currentTime = DateTime.Now;
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
                                    case 1:
                                        {
                                            annotateBoard.EditingMode = SurfaceInkEditingMode.Ink;
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
                                mode2 = false;
                                switch (task)
                                {
                                    case 0:
                                        {
                                            highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                                            break;
                                        }
                                    case 1:
                                        {
                                            annotateBoard.EditingMode = SurfaceInkEditingMode.None;
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
                                mode3 = true;
                                switch (task)
                                {
                                    case 0:
                                        {
                                            highlightBoard.EditingMode = SurfaceInkEditingMode.Ink;
                                            break;
                                        }
                                    case 1:
                                        {
                                            annotateBoard.EditingMode = SurfaceInkEditingMode.Ink;
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
                                mode3 = false;
                                switch (task)
                                {
                                    case 0:
                                        {
                                            highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                                            break;
                                        }
                                    case 1:
                                        {
                                            annotateBoard.EditingMode = SurfaceInkEditingMode.None;
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
                                    annotateButton.Visibility = System.Windows.Visibility.Hidden;
                                    annotateButton.Background = Brushes.Silver;
                                    annotateBoard.EditingMode = SurfaceInkEditingMode.None;
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
                                    annotateButton.Visibility = System.Windows.Visibility.Visible;
                                    annotateButton.Background = Brushes.Silver;
                                    annotateBoard.EditingMode = SurfaceInkEditingMode.None;
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
                if (highlightBoard.Strokes.Count() != 0)
                {
                    foreach (System.Windows.Ink.Stroke strk in highlightBoard.Strokes)
                    {
                        Console.WriteLine("{0} {1} {2}", Canvas.GetTop(immediatelyRect) - Canvas.GetTop(highlightBoard),strk.GetBounds().Top,
                            Math.Abs(Canvas.GetTop(immediatelyRect) - Canvas.GetTop(highlightBoard) - strk.GetBounds().Top));
                        Console.WriteLine("{0} {1} {2}", Canvas.GetLeft(immediatelyRect) - Canvas.GetLeft(highlightBoard), strk.GetBounds().Left,
                           Math.Abs(Canvas.GetLeft(immediatelyRect) - Canvas.GetLeft(highlightBoard) - strk.GetBounds().Left));
                        Console.WriteLine("{0} {1} {2}", Canvas.GetLeft(immediatelyRect) - Canvas.GetLeft(highlightBoard) + immediatelyRect.Width,
                           strk.GetBounds().Left + strk.GetBounds().Width,
                           Math.Abs(Canvas.GetLeft(immediatelyRect) - Canvas.GetLeft(highlightBoard) + immediatelyRect.Width - 
                                    strk.GetBounds().Left + strk.GetBounds().Width));
                        Console.WriteLine("{0} {1} {2}", Canvas.GetTop(immediatelyRect) - Canvas.GetTop(highlightBoard) + immediatelyRect.Height,
                           strk.GetBounds().Top + strk.GetBounds().Height,
                           Math.Abs(Canvas.GetTop(immediatelyRect) - Canvas.GetTop(highlightBoard) + immediatelyRect.Height - 
                                    strk.GetBounds().Top + strk.GetBounds().Height));
                        if (Math.Abs(Canvas.GetTop(immediatelyRect) - (strk.GetBounds().Top + Canvas.GetTop(highlightBoard))) < 20 &&
                            Math.Abs(Canvas.GetLeft(immediatelyRect) - (strk.GetBounds().Left + Canvas.GetLeft(highlightBoard))) < 20 &&
                            Math.Abs((Canvas.GetLeft(highlightBoard) + strk.GetBounds().Right) - Canvas.GetRight(immediatelyRect)) < 20 &&
                            Math.Abs((Canvas.GetLeft(highlightBoard) + strk.GetBounds().Bottom) - Canvas.GetBottom(immediatelyRect)) < 20)
                        {
                            hlWord1 = true;
                            Console.WriteLine("YES");
                        }

                    }
                }
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
                                            annotateBoard.DefaultDrawingAttributes.Height = circle.Radius * 2;
                                            annotateBoard.DefaultDrawingAttributes.Width = circle.Radius * 2;
                                            annotateBoard.DefaultDrawingAttributes.FitToCurve = false;
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
                        taskButton.Content = "Task2 - E/A";
                        highlightLabel.Visibility = System.Windows.Visibility.Hidden;
                        Panel.SetZIndex(annotateBoard, 2);
                        Panel.SetZIndex(highlightLabel, 0);
                        annotateLabel.Visibility = Visibility.Visible;
                        highlightBoard.EditingMode = SurfaceInkEditingMode.None;
                        annotateBoard.EditingMode = SurfaceInkEditingMode.None;
                        if (mode == 0)
                        {
                            highlightButton.Visibility = System.Windows.Visibility.Hidden;
                            highlightButton.Background = Brushes.Silver;
                            annotateButton.Background = Brushes.Silver;
                            annotateButton.Visibility = System.Windows.Visibility.Visible;
                        }
                        break;
                    }
                case 1:
                    {
                        task = 2;
                        taskButton.Content = "Task3 - INK";
                        annotateLabel.Visibility = System.Windows.Visibility.Hidden;
                        drawLable.Visibility = System.Windows.Visibility.Visible;
                        annotateBoard.EditingMode = SurfaceInkEditingMode.None;
                        annotateBoard.Visibility = System.Windows.Visibility.Hidden;
                        highlightBoard.Visibility = System.Windows.Visibility.Hidden;
                        drawBoard.Visibility = System.Windows.Visibility.Visible;
                        textBoard.Visibility = System.Windows.Visibility.Hidden;
                        if (mode == 0)
                        {
                            annotateButton.Background = Brushes.Silver;
                            annotateButton.Visibility = System.Windows.Visibility.Hidden;
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
                        annotateBoard.Visibility = System.Windows.Visibility.Visible;
                        textBoard.Visibility = System.Windows.Visibility.Visible;
                        drawBoard.Visibility = System.Windows.Visibility.Hidden;
                        Panel.SetZIndex(highlightBoard, 1);
                        Panel.SetZIndex(annotateBoard, 0);
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

        private void onAnnotateClick(object s, RoutedEventArgs e)
        {
            if (!annotate)
            {
                annotate = true;
                annotateBoard.EditingMode = SurfaceInkEditingMode.Ink;
                annotateButton.Background = Brushes.Green;
            }
            else
            {
                annotate = false;
                annotateBoard.EditingMode = SurfaceInkEditingMode.None;
                annotateButton.Background = Brushes.Silver;
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
    }
}