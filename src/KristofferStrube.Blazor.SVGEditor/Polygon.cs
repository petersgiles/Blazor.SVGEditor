﻿using AngleSharp.Dom;
using Microsoft.AspNetCore.Components.Web;

namespace KristofferStrube.Blazor.SVGEditor
{
    public class Polygon : Shape
    {
        public Polygon(IElement element, SVG svg) : base(element, svg)
        {
            Points = StringToPoints(Element.GetAttribute("points") ?? string.Empty);
        }

        public override Type Editor => typeof(PolygonEditor);

        public List<(double x, double y)> Points { get; set; }

        private void UpdatePoints()
        {
            Element.SetAttribute("points", PointsToString(Points));
            Changed.Invoke(this);
        }

        public string PointsToString(List<(double x, double y)> points)
        {
            return string.Join(" ", points.Select(point => $"{point.x.AsString()},{point.y.AsString()}"));
        }
        public List<(double x, double y)> StringToPoints(string points)
        {
            if (points == string.Empty)
            {
                return new();
            }
            return points.Split(" ").Select(p => (x: p.Split(",")[0].ParseAsDouble(), y: p.Split(",")[1].ParseAsDouble())).ToList();
        }

        public override void HandleMouseMove(MouseEventArgs eventArgs)
        {
            var pos = SVG.LocalDetransform((eventArgs.OffsetX, eventArgs.OffsetY));
            switch (SVG.EditMode)
            {
                case EditMode.MoveAnchor:
                    if (SVG.CurrentAnchor == null)
                    {
                        SVG.CurrentAnchor = 0;
                    }
                    Points[(int)SVG.CurrentAnchor] = (pos.x, pos.y);
                    UpdatePoints();
                    break;
                case EditMode.Move:
                    var diff = (x: pos.x - SVG.MovePanner.x, y: pos.y - SVG.MovePanner.y);
                    Points = Points.Select(point => { point.x += diff.x; point.y += diff.y; return point; }).ToList();
                    UpdatePoints();
                    break;
                case EditMode.Add:
                    if (Points.Count == 0)
                    {
                        var startPos = SVG.LocalDetransform((SVG.LastRightClick.x, SVG.LastRightClick.y));
                        Points.Add((startPos.x, startPos.y));
                        Points.Add(pos);
                    }
                    Points[^1] = pos;
                    UpdatePoints();
                    break;
            }
        }

        public override void HandleMouseUp(MouseEventArgs eventArgs)
        {
            var pos = SVG.LocalDetransform((eventArgs.OffsetX, eventArgs.OffsetY));
            switch (SVG.EditMode)
            {
                case EditMode.MoveAnchor:
                    if (pos.x < 50 && pos.y < 50)
                    {
                        Points.RemoveAt((int)SVG.CurrentAnchor);
                        UpdatePoints();
                    }
                    SVG.CurrentAnchor = null;
                    SVG.EditMode = EditMode.None;
                    if (Points.Count() == 0)
                    {
                        SVG.Elements.Remove(this);
                        SVG.SelectedElements.Clear();
                        Changed.Invoke(this);
                    }
                    break;
                case EditMode.Move:
                    SVG.EditMode = EditMode.None;
                    break;
                case EditMode.Add:
                    Points.Add(pos);
                    break;
            }
        }

        public override void HandleMouseOut(MouseEventArgs eventArgs)
        {
        }

        public static void AddNew(SVG SVG)
        {
            var element = SVG.Document.CreateElement("POLYGON");

            var polygon = new Polygon(element, SVG);
            polygon.Changed = SVG.UpdateInput;
            polygon.Stroke = "black";
            polygon.StrokeWidth = "1";
            polygon.Fill = "lightgrey";
            SVG.EditMode = EditMode.Add;

            SVG.SelectedElements.Clear();
            SVG.SelectedElements.Add(polygon);
            SVG.AddElement(polygon);
        }

        public override void Complete()
        {
            Points.RemoveAt(Points.Count - 1);
            UpdatePoints();
        }
    }
}
