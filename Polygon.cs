using Cyotek.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
 
namespace howto_polygon_editor2
{
    public class Polygon : IPolyShape
    {
        // The "size" of an object for mouse over purposes.
        private const int object_radius = 4;

        // We're over an object if the distance squared
        // between the mouse and the object is less than this.
        private const int over_dist_squared = object_radius * object_radius;

        // Each polygon is represented by a List<Point>.
        private readonly List<List<Point>> Polygons = new List<List<Point>>();

        // Points for the new polygon.
        private List<Point> NewPolygon = null;

        // The current mouse position while drawing a new polygon.
        private Point NewPoint;

        // The polygon and index of the corner we are moving.
        private List<Point> MovingPolygon = null;
        private int MovingPoint = -1;
        private int OffsetX, OffsetY;

        // The add point cursor.
        private readonly Cursor AddPointCursor;

        private readonly ImageBox picCanvas;

        private int activePolygonIndex = -1;

        public Polygon(ImageBox PicCanvas)
        {
            AddPointCursor = new Cursor(Properties.Resources.add_point.GetHicon());
            picCanvas = PicCanvas;
        }

        public List<Point> GetSelectedPolygon()
        {
            if (activePolygonIndex >= 0)
                return Polygons[activePolygonIndex];
            else
                return null;
        }

        // Start or continue drawing a new polygon,
        // or start moving a corner or polygon.
        public void MouseDown(object sender, MouseEventArgs e)
        {
            // See what we're over.
            Point mouse_pt = picCanvas.PointToImage(e.Location);
            
            if (e.Button == MouseButtons.Left)
            {
                int hit_point, hit_point2;
                List<Point> hit_polygon;
                Point closest_point;
                if (NewPolygon != null)
                {
                    // Add a point to this polygon.
                    if (NewPolygon[NewPolygon.Count - 1] != mouse_pt)
                    {
                        NewPolygon.Add(mouse_pt);
                    }
                }
                else if (MouseIsOverCornerPoint(mouse_pt, out hit_polygon, out hit_point))
                {
                    // Start dragging this corner.
                    picCanvas.MouseMove -= MouseMove_NotDrawing;
                    picCanvas.MouseMove += MouseMove_MovingCorner;
                    picCanvas.MouseUp += picCanvas_MouseUp_MovingCorner;

                    // Remember the polygon and point number.
                    MovingPolygon = hit_polygon;
                    MovingPoint = hit_point;

                    // Remember the offset from the mouse to the point.
                    OffsetX = hit_polygon[hit_point].X - e.X;
                    OffsetY = hit_polygon[hit_point].Y - e.Y;

                    // Set the active polygon to the one that was clicked on
                    activePolygonIndex = Polygons.IndexOf(hit_polygon);

                }
                else if (MouseIsOverEdge(mouse_pt, out hit_polygon,
                    out hit_point, out hit_point2, out closest_point))
                {
                    // Add a point.
                    hit_polygon.Insert(hit_point + 1, closest_point);

                    // Set the active polygon to the one that was clicked on
                    activePolygonIndex = Polygons.IndexOf(hit_polygon);
                }
                else if (MouseIsOverPolygon(mouse_pt, out hit_polygon))
                {
                    // Start moving this polygon.
                    picCanvas.MouseMove -= MouseMove_NotDrawing;
                    picCanvas.MouseMove += MouseMove_MovingPolygon;
                    picCanvas.MouseUp += MouseUp_MovingPolygon;

                    // Remember the polygon.
                    MovingPolygon = hit_polygon;

                    // Remember the offset from the mouse to the segment's first point.
                    OffsetX = hit_polygon[0].X - e.X;
                    OffsetY = hit_polygon[0].Y - e.Y;

                    // Set the active polygon to the one that was clicked on
                    activePolygonIndex = Polygons.IndexOf(hit_polygon);
                }
                else
                {
                    // Start a new polygon.
                    NewPolygon = new List<Point>();
                    NewPoint = ImageToControl(mouse_pt);
                    NewPolygon.Add(mouse_pt);

                    // Get ready to work on the new polygon.
                    picCanvas.MouseMove -= MouseMove_NotDrawing;
                    picCanvas.MouseMove += MouseMove_Drawing;
                }

            }
            else if (e.Button == MouseButtons.Right)
            {
                // We are already drawing a polygon.
                // If it's the right mouse button, finish this polygon.
                // Finish this polygon.
                if (NewPolygon.Count > 2) Polygons.Add(NewPolygon);

                activePolygonIndex = -1;
                if (NewPolygon != null)
                {
                    NewPolygon = null;
                    picCanvas.MouseMove += MouseMove_NotDrawing;
                    picCanvas.MouseMove -= MouseMove_Drawing;
                }
            }

            // Redraw.
            picCanvas.Invalidate();
        }

        // Move the next point in the new polygon.
        public void MouseMove_Drawing(object sender, MouseEventArgs e)
        {
            //NewPoint = picCanvas.PointToImage(e.Location);
            NewPoint = e.Location;
            picCanvas.Invalidate();
        }

        // Move the selected corner.
        public void MouseMove_MovingCorner(object sender, MouseEventArgs e)
        {
            // Move the point.
            MovingPolygon[MovingPoint] = new Point(e.X + OffsetX, e.Y + OffsetY);

            // Redraw.
            picCanvas.Invalidate();
        }

        // Finish moving the selected corner.
        public void picCanvas_MouseUp_MovingCorner(object sender, MouseEventArgs e)
        {
            picCanvas.MouseMove += MouseMove_NotDrawing;
            picCanvas.MouseMove -= MouseMove_MovingCorner;
            picCanvas.MouseUp -= picCanvas_MouseUp_MovingCorner;
        }

        // Move the selected polygon.
        public void MouseMove_MovingPolygon(object sender, MouseEventArgs e)
        {
            // See how far the first point will move.
            int new_x1 = e.X + OffsetX;
            int new_y1 = e.Y + OffsetY;

            int dx = new_x1 - MovingPolygon[0].X;
            int dy = new_y1 - MovingPolygon[0].Y;

            if (dx == 0 && dy == 0) return;

            // Move the polygon.
            for (int i = 0; i < MovingPolygon.Count; i++)
            {
                MovingPolygon[i] = new Point(
                    MovingPolygon[i].X + dx,
                    MovingPolygon[i].Y + dy);
            }

            // Redraw.
            picCanvas.Invalidate();
        }

        // Finish moving the selected polygon.
        public void MouseUp_MovingPolygon(object sender, MouseEventArgs e)
        {
            picCanvas.MouseMove += MouseMove_NotDrawing;
            picCanvas.MouseMove -= MouseMove_MovingPolygon;
            picCanvas.MouseUp -= MouseUp_MovingPolygon;
        }

        // See if we're over a polygon or corner point.
        public void MouseMove_NotDrawing(object sender, MouseEventArgs e)
        {
            Cursor new_cursor = Cursors.Cross;

            // See what we're over.
            Point mouse_pt = picCanvas.PointToImage(e.Location);  //e.Location;
            
            List<Point> hit_polygon;
            int hit_point, hit_point2;
            Point closest_point;

            if (MouseIsOverCornerPoint(mouse_pt, out hit_polygon, out hit_point))
            {
                new_cursor = Cursors.Arrow;
            }
            else if (MouseIsOverEdge(mouse_pt, out hit_polygon, out hit_point, out hit_point2, out closest_point))
            {
                new_cursor = AddPointCursor;
            }
            else if (MouseIsOverPolygon(mouse_pt, out hit_polygon))
            {
                new_cursor = Cursors.Hand;
            }

            // Set the new cursor.
            if (picCanvas.Cursor != new_cursor)
            {
                picCanvas.Cursor = new_cursor;
            }
        }

        // Redraw old polygons in blue. Draw the new polygon in green.
        // Draw the final segment dashed.
        public void Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            //e.Graphics.Clear(picCanvas.BackColor);

            // Calculate the zoomed object radius.
            int zoomed_radius = (int)Math.Ceiling(object_radius * picCanvas.ZoomFactor);

            // Draw the old polygons.
            for (int i = 0; i < Polygons.Count; i++)
            {
                List<Point> polygon = Polygons[i];
                // Draw the polygon.
                PointF[] zoomed_points = GetZoomedPoints(polygon);
                e.Graphics.DrawPolygon(new Pen(Color.Aqua, 3), zoomed_points);

                if(i == activePolygonIndex)
                {
                    // Draw the corners.
                    for (int j = 0; j < zoomed_points.Length; j++)
                    {
                        PointF corner = zoomed_points[j];
                        RectangleF rect = new RectangleF(corner.X - zoomed_radius, corner.Y - zoomed_radius, 2 * zoomed_radius + 1, 2 * zoomed_radius + 1);
                        e.Graphics.FillEllipse(Brushes.White, rect);
                        e.Graphics.DrawEllipse(Pens.Black, rect);
                    }
                }
                
            }

            // Draw the new polygon.
            if (NewPolygon != null)
            {
                // Draw the new polygon.
                if (NewPolygon.Count > 1)
                {
                    PointF[] zoomed_points = GetZoomedPoints(NewPolygon);
                    e.Graphics.DrawLines(new Pen(Color.Yellow, 3), zoomed_points);
                }

                // Draw the newest edge.
                if (NewPolygon.Count > 0)
                {
                    var pt1 = ImageToControl(NewPolygon[NewPolygon.Count - 1]);
                    var pt2 = NewPoint;
                    e.Graphics.DrawLine(new Pen(Color.Yellow, 2) { DashPattern = new float[] { 3, 3 } }, pt1, pt2);
                }
            }
        }

        // See if the mouse is over a corner point.
        public bool MouseIsOverCornerPoint(Point mouse_pt, out List<Point> hit_polygon, out int hit_pt)
        {
            // See if we're over a corner point.
            foreach (List<Point> polygon in Polygons)
            {
                // See if we're over one of the polygon's corner points.
                for (int i = 0; i < polygon.Count; i++)
                {
                    // See if we're over this point.
                    if (FindDistanceToPointSquared(polygon[i], mouse_pt) < over_dist_squared)
                    {
                        // We're over this point.
                        hit_polygon = polygon;
                        hit_pt = i;
                        return true;
                    }
                }
            }

            hit_polygon = null;
            hit_pt = -1;
            return false;
        }

        // See if the mouse is over a polygon's edge.
        public bool MouseIsOverEdge(Point mouse_pt, out List<Point> hit_polygon, out int hit_pt1, out int hit_pt2, out Point closest_point)
        {
            // Examine each polygon.
            // Examine them in reverse order to check the ones on top first.
            for (int pgon = Polygons.Count - 1; pgon >= 0; pgon--)
            {
                List<Point> polygon = Polygons[pgon];

                // See if we're over one of the polygon's segments.
                for (int p1 = 0; p1 < polygon.Count; p1++)
                {
                    // Get the index of the polygon's next point.
                    int p2 = (p1 + 1) % polygon.Count;

                    // See if we're over the segment between these points.
                    PointF closest;
                    if (FindDistanceToSegmentSquared(mouse_pt,
                        polygon[p1], polygon[p2], out closest) < over_dist_squared)
                    {
                        // We're over this segment.
                        hit_polygon = polygon;
                        hit_pt1 = p1;
                        hit_pt2 = p2;
                        closest_point = Point.Round(closest);
                        return true;
                    }
                }
            }

            hit_polygon = null;
            hit_pt1 = -1;
            hit_pt2 = -1;
            closest_point = new Point(0, 0);
            return false;
        }

        // See if the mouse is over a polygon's body.
        public bool MouseIsOverPolygon(Point mouse_pt, out List<Point> hit_polygon)
        {
            // Examine each polygon.
            // Examine them in reverse order to check the ones on top first.
            for (int i = Polygons.Count - 1; i >= 0; i--)
            {
                // Make a GraphicsPath representing the polygon.
                GraphicsPath path = new GraphicsPath();
                path.AddPolygon(Polygons[i].ToArray());

                // See if the point is inside the GraphicsPath.
                if (path.IsVisible(mouse_pt))
                {
                    hit_polygon = Polygons[i];
                    return true;
                }
            }

            hit_polygon = null;
            return false;
        }

        #region DistanceFunctions

        // Calculate the distance squared between two points.
        private int FindDistanceToPointSquared(Point pt1, Point pt2)
        {
            int dx = pt1.X - pt2.X;
            int dy = pt1.Y - pt2.Y;
            return dx * dx + dy * dy;
        }

        // Calculate the distance squared between
        // point pt and the segment p1 --> p2.
        private double FindDistanceToSegmentSquared(PointF pt, PointF p1, PointF p2, out PointF closest)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            if ((dx == 0) && (dy == 0))
            {
                // It's a point not a line segment.
                closest = p1;
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            float t = ((pt.X - p1.X) * dx + (pt.Y - p1.Y) * dy) / (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                closest = new PointF(p1.X, p1.Y);
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
            }
            else if (t > 1)
            {
                closest = new PointF(p2.X, p2.Y);
                dx = pt.X - p2.X;
                dy = pt.Y - p2.Y;
            }
            else
            {
                closest = new PointF(p1.X + t * dx, p1.Y + t * dy);
                dx = pt.X - closest.X;
                dy = pt.Y - closest.Y;
            }

            // return Math.Sqrt(dx * dx + dy * dy);
            return dx * dx + dy * dy;
        }


        private Point ControlToImage(Point controlPoint)
        {
            var imgVwPt = picCanvas.GetImageViewPort();
            float factor = (float)picCanvas.ZoomFactor;
            int offsetX = (int)((controlPoint.X + picCanvas.HorizontalScroll.Value - imgVwPt.X) / factor);
            int offsetY = (int)((controlPoint.Y + picCanvas.VerticalScroll.Value - imgVwPt.Y) / factor);
            return new Point(offsetX, offsetY);
        }

        private Point ImageToControl(Point imagePoint)
        {
            var imgVwPt = picCanvas.GetImageViewPort();
            float factor = (float)picCanvas.ZoomFactor;
            int offsetX = (int)(imagePoint.X * factor + imgVwPt.X - picCanvas.HorizontalScroll.Value);
            int offsetY = (int)(imagePoint.Y * factor + imgVwPt.Y - picCanvas.VerticalScroll.Value);
            return new Point(offsetX, offsetY);
        }


        // Return the given points scaled by the current zoom factor.
        private PointF[] GetZoomedPoints(List<Point> points)
        {
            var imgVwPt = picCanvas.GetImageViewPort();
            float factor = (float)picCanvas.ZoomFactor;
            var pts = new PointF[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                var pt = points[i];
                var offsetX = (pt.X * factor) + imgVwPt.X - picCanvas.HorizontalScroll.Value;
                var offsetY = (pt.Y * factor) + imgVwPt.Y - picCanvas.VerticalScroll.Value;
                pts[i] = new PointF(offsetX, offsetY);
            }
            return pts;
        }


        #endregion DistanceFunctions


    }
}
