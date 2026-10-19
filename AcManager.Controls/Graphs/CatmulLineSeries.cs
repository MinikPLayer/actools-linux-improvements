using System;
using System.Collections.Generic;
using AcManager.Tools.Helpers;
using OxyPlot;
using OxyPlot.Series;

namespace AcManager.Controls.Graphs {
    public class CatmulLineSeries : LineSeries {
        private readonly double _smoothessMultiplier;

        public bool Smooth { get; set; }

        public CatmulLineSeries(double smoothessMultiplier = 0.1) {
            _smoothessMultiplier = smoothessMultiplier;

            // TrackerFormatString = FormatString;
            Smooth = SettingsHolder.Content.SmoothCurves;
            CanTrackerInterpolatePoints = SettingsHolder.Content.SmoothCurves;
        }

        protected override void RenderLineAndMarkers(IRenderContext rc, IList<ScreenPoint> pointsToRender) {
            if (Smooth) {
                pointsToRender = CatmullRomSplineHelper.CreateSpline(ScreenPointHelper.ResamplePoints(pointsToRender, MinimumSegmentLength), 0.5,
                        0.25 / _smoothessMultiplier);
            }

            if (StrokeThickness > 0.0 && ActualLineStyle != LineStyle.None) {
                RenderLine(rc, pointsToRender);
            }

            if (MarkerType != MarkerType.None) {
                var binOffset = MarkerResolution > 0 ? Transform(new DataPoint(MinX, MinY)) : new ScreenPoint();
                rc.DrawMarkers(pointsToRender, MarkerType, MarkerOutline, new[] { MarkerSize }, ActualMarkerFill, MarkerStroke,
                        MarkerStrokeThickness, EdgeRenderingMode, MarkerResolution, binOffset);
            }
        }

        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate) {
            return Points.Count < 2 ? null : base.GetNearestPoint(point, interpolate);
        }

        protected override HitTestResult HitTestOverride(HitTestArguments args) {
            return Points.Count < 2 ? null : base.HitTestOverride(args);
        }

    }
}