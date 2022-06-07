﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tools;

namespace Qualia.Controls
{
    public class QPresenter : Panel
    {
        private readonly VisualCollection _visuals;

        public Func<double, double> Scale = Render.Scale;

        public QPresenter()
        {
            _visuals = new VisualCollection(this);
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
        }

        public void Clear()
        {
            _visuals.Clear();
        }

        private DrawingContext G()
        {
            var dv = new DrawingVisual();
            _visuals.Add(dv);

            return dv.RenderOpen();
        }

        public void AddVisual(Visual visual)
        {
            _visuals.Add(visual);
        }

        protected override int VisualChildrenCount
        {
            get => _visuals.Count;
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _visuals.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _visuals[index];
        }

        public void Update()
        {
            //
        }

        public void DrawRectangle(Brush brush, Pen pen, Rect rect)
        {
            using (var g = G())
            {
                rect.X = Scale(rect.X);
                rect.Y = Scale(rect.Y);
                rect.Width = Scale(rect.Width);
                rect.Height = Scale(rect.Height);
                
                g.DrawRectangle(brush, pen, rect);
            }
        }

        public void DrawText(FormattedText text, Point point, double angle = 0)
        {
            using (var g = G())
            {
                point.X = Scale(point.X);
                point.Y = Scale(point.Y);

                if (angle != 0)
                {
                    var rt = new RotateTransform
                    {
                        Angle = angle
                    };
                    g.PushTransform(rt);
                }

                g.DrawText(text, point);

                if (angle != 0)
                {
                    g.Pop();
                }
            }
        }

        public void DrawLine(Pen pen, Point point0, Point point1)
        {
            using (var g = G())
            {
                point0.X = Scale(point0.X);
                point0.Y = Scale(point0.Y);
                point1.X = Scale(point1.X);
                point1.Y = Scale(point1.Y);

                g.DrawLine(pen, point0, point1);
            }
        }

        public void DrawEllipse(Brush brush, Pen pen, Point center, double radiusX, double radiusY)
        {
            using (var g = G())
            {
                center.X = Scale(center.X);
                center.Y = Scale(center.Y);
                radiusX = Scale(radiusX);
                radiusY = Scale(radiusY);

                g.DrawEllipse(brush, pen, center, radiusX, radiusY);
            }
        }
    }
}
