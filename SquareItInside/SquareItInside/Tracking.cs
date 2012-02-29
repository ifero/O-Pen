﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Surface.Core;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;

namespace PenTrack
{
    class Tracking
    {
        private bool isPen;
        private Image<Gray, Byte> image;

        public Tracking()
        {
            isPen = false;
            image = null;
        }

        public CircleF[] TrackContours(ImageMetrics normalizedMetrics, byte[] normalizedImage)
        {
            image = new Image<Gray,byte>(normalizedMetrics.Width, normalizedMetrics.Height) { Bytes = normalizedImage };
            image = image.ThresholdBinary(new Gray(254), new Gray(255)); //Show just the very bright things

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
                if (contours.Area >= 1 && contours.Area <= 40)
                {
                    circles.Add(new CircleF(
                      new PointF(contours.BoundingRectangle.Left + (contours.BoundingRectangle.Width / 2),
                        contours.BoundingRectangle.Top + (contours.BoundingRectangle.Height / 2)),
                        contours.BoundingRectangle.Width / 2));
                    isPen = true;

                }

            }

            if (contours.Area >= 1 && contours.Area <= 40)
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

        public bool isAPen()
        {
            return isPen;
        }
    }
}
