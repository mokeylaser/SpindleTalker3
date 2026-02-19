using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SpindleTalker2
{
    public class AnalogMeterControl : Control
    {
        private double _value;
        private double _minValue;
        private double _maxValue = 100;
        private int _scaleDivisions = 6;
        private int _scaleSubDivisions = 3;
        private Color _needleColor = Color.DarkRed;
        private Color _bodyColor = SystemColors.Control;
        private Color _scaleColor = Color.White;

        // Arc spans from 225° (min) to -45° (max) = 270° sweep
        private const float ArcStartAngle = 225f;
        private const float ArcSweepAngle = -270f;

        public AnalogMeterControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw, true);
        }

        public double Value
        {
            get => _value;
            set { _value = value; Invalidate(); }
        }

        public double MinValue
        {
            get => _minValue;
            set { _minValue = value; Invalidate(); }
        }

        public double MaxValue
        {
            get => _maxValue;
            set { _maxValue = value; Invalidate(); }
        }

        public int ScaleDivisions
        {
            get => _scaleDivisions;
            set { _scaleDivisions = Math.Max(1, value); Invalidate(); }
        }

        public int ScaleSubDivisions
        {
            get => _scaleSubDivisions;
            set { _scaleSubDivisions = Math.Max(1, value); Invalidate(); }
        }

        public Color NeedleColor
        {
            get => _needleColor;
            set { _needleColor = value; Invalidate(); }
        }

        public Color BodyColor
        {
            get => _bodyColor;
            set { _bodyColor = value; Invalidate(); }
        }

        public Color ScaleColor
        {
            get => _scaleColor;
            set { _scaleColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            int side = Math.Min(Width, Height);
            if (side < 10) return;

            float cx = Width / 2f;
            float cy = Height / 2f;
            float radius = side / 2f - 4f;

            // Body circle
            using (var bodyBrush = new SolidBrush(_bodyColor))
                g.FillEllipse(bodyBrush, cx - radius, cy - radius, radius * 2, radius * 2);
            using (var borderPen = new Pen(Color.DimGray, 1.5f))
                g.DrawEllipse(borderPen, cx - radius, cy - radius, radius * 2, radius * 2);

            // Inner face
            float innerRadius = radius * 0.88f;
            using (var faceBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                g.FillEllipse(faceBrush, cx - innerRadius, cy - innerRadius, innerRadius * 2, innerRadius * 2);

            // Draw scale
            DrawScale(g, cx, cy, innerRadius);

            // Draw needle
            DrawNeedle(g, cx, cy, innerRadius * 0.78f);

            // Center cap
            float capRadius = radius * 0.06f;
            using (var capBrush = new SolidBrush(_needleColor))
                g.FillEllipse(capBrush, cx - capRadius, cy - capRadius, capRadius * 2, capRadius * 2);
        }

        private void DrawScale(Graphics g, float cx, float cy, float radius)
        {
            int totalTicks = _scaleDivisions * _scaleSubDivisions;
            float majorTickLen = radius * 0.15f;
            float minorTickLen = radius * 0.08f;

            using var majorPen = new Pen(Color.Black, 1.5f);
            using var minorPen = new Pen(Color.Gray, 0.8f);

            float fontSize = Math.Max(6f, radius * 0.13f);
            using var font = new Font("Segoe UI", fontSize, FontStyle.Regular);
            using var textBrush = new SolidBrush(Color.Black);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            for (int i = 0; i <= totalTicks; i++)
            {
                float fraction = (float)i / totalTicks;
                float angleDeg = ArcStartAngle + fraction * ArcSweepAngle;
                float angleRad = angleDeg * (float)Math.PI / 180f;

                float outerX = cx + radius * (float)Math.Cos(angleRad);
                float outerY = cy - radius * (float)Math.Sin(angleRad);

                bool isMajor = (i % _scaleSubDivisions == 0);
                float tickLen = isMajor ? majorTickLen : minorTickLen;
                var pen = isMajor ? majorPen : minorPen;

                float innerX = cx + (radius - tickLen) * (float)Math.Cos(angleRad);
                float innerY = cy - (radius - tickLen) * (float)Math.Sin(angleRad);

                g.DrawLine(pen, outerX, outerY, innerX, innerY);

                // Labels on major ticks
                if (isMajor)
                {
                    double labelValue = _minValue + fraction * (_maxValue - _minValue);
                    string label = FormatLabel(labelValue);
                    float labelR = radius - majorTickLen - fontSize * 0.9f;
                    float lx = cx + labelR * (float)Math.Cos(angleRad);
                    float ly = cy - labelR * (float)Math.Sin(angleRad);
                    g.DrawString(label, font, textBrush, lx, ly, sf);
                }
            }
        }

        private void DrawNeedle(Graphics g, float cx, float cy, float needleLen)
        {
            double range = _maxValue - _minValue;
            double clampedValue = Math.Max(_minValue, Math.Min(_maxValue, _value));
            float fraction = range > 0 ? (float)((clampedValue - _minValue) / range) : 0f;
            float angleDeg = ArcStartAngle + fraction * ArcSweepAngle;
            float angleRad = angleDeg * (float)Math.PI / 180f;

            float tipX = cx + needleLen * (float)Math.Cos(angleRad);
            float tipY = cy - needleLen * (float)Math.Sin(angleRad);

            // Tail (opposite side, short)
            float tailLen = needleLen * 0.18f;
            float tailX = cx - tailLen * (float)Math.Cos(angleRad);
            float tailY = cy + tailLen * (float)Math.Sin(angleRad);

            using var needlePen = new Pen(_needleColor, 2f);
            g.DrawLine(needlePen, tailX, tailY, tipX, tipY);
        }

        private static string FormatLabel(double value)
        {
            if (Math.Abs(value) >= 1000) return value.ToString("F0");
            if (Math.Abs(value) >= 100) return value.ToString("F0");
            if (Math.Abs(value) >= 10) return value.ToString("F0");
            if (Math.Abs(value) >= 1) return value.ToString("F1");
            return value.ToString("F2");
        }
    }
}
