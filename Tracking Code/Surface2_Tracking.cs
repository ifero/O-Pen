using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using Microsoft.Surface.Core;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;
using NLog;

namespace Pen
{
    class Surface2_Tracking
    {
        private bool isPen;
        private Image<Gray, Byte> image;
        private const int size = 30;
        private const int threshold = 254;

        public Surface2_Tracking()
        {
            isPen = false;
            image = null;
        }
        
        #region findContours

        public CircleF[] TrackContours(ImageMetrics normalizedMetrics, byte[] normalizedImage)
        {
            image = new Image<Gray,byte>(normalizedMetrics.Width, normalizedMetrics.Height) { Bytes = normalizedImage };
            image = image.ThresholdBinary(new Gray(threshold), new Gray(255)); //Show just the very bright things
            //detecy Contours from Thresholded image.
            Contour<System.Drawing.Point> contours = image.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
            Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST);
            return FindPossibleCircles(contours);
        }

        private CircleF[] FindPossibleCircles(Contour<System.Drawing.Point> contours)
        {
            if (contours == null)
            {
                isPen = false;
                return null;
            }

            ResetContoursNavigation(ref contours);
            isPen = false;
            IList<CircleF> circles = new List<CircleF>();
            for (; contours.HNext != null; contours = contours.HNext)
            {
                if (contours.Area >= 1 && contours.Area <= size)
                {
                    circles.Add(new CircleF(
                      new PointF(contours.BoundingRectangle.Left + (contours.BoundingRectangle.Width / 2),
                        contours.BoundingRectangle.Top + (contours.BoundingRectangle.Height / 2)),
                        contours.BoundingRectangle.Width / 2));
                    isPen = true;

                }

            }

            if (contours.Area >= 1 && contours.Area <= size)
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
        #endregion
        
        public bool isAPen()
        {
            return isPen;
        }
        
    }

}
