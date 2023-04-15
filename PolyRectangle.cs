using Cyotek.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace howto_polygon_editor2
{
    public class PolyRectangle : IPolyShape
    {
        public Label lblMsg { get; set; }
        private readonly int maxPoint = 2;

        // The "size" of an object for mouse over purposes.
        private const int object_radius = 4;

        // We're over an object if the distance squared
        // between the mouse and the object is less than this.
        private const int over_dist_squared = object_radius * object_radius;

        // Each polygon is represented by a List<Point>.
        private readonly List<List<Point>> Rectangles = new List<List<Point>>();

        // Points for the new polygon.
        private List<Point> NewPoints = null;

        // The current mouse position while drawing a new polygon.
        private Point NewPoint;

        // The polygon and index of the corner we are moving.
        private List<Point> MovingRectangle = null;
        private int MovingPoint = -1;
        private int OffsetX, OffsetY;

        // The add point cursor.
        private readonly Cursor AddPointCursor;

        private readonly ImageBox picCanvas;
        public PolyRectangle(ImageBox PicCanvas)
        {
            AddPointCursor = new Cursor(Properties.Resources.add_point.GetHicon());
            picCanvas = PicCanvas;
        }

        public bool LoadImage(string FileName)
        {
            //var image = System.Drawing.Image.FromFile(FileName);
            picCanvas.Image = Image.FromFile(FileName);
            picCanvas.Zoom = 100;
            picCanvas.SizeMode = ImageBoxSizeMode.Normal;
            return true;
        }


        // Start or continue drawing a new polygon,
        // or start moving a corner or polygon.
        public void MouseDown(object sender, MouseEventArgs e)
        {
            // See what we're over.
            Point mouse_pt = picCanvas.PointToImage(e.Location); // e.Location;
            List<Point> hit_rectangle;
            int hit_point, hit_point2;
            Point closest_point;

            if (NewPoints != null)
            {
                // Add a point to this polygon.
                if (NewPoints[NewPoints.Count - 1] != mouse_pt)
                {
                    NewPoints.Add(mouse_pt);

                    // We are already drawing a polygon.
                    // If it's the right mouse button, finish this polygon.
                    if (NewPoints.Count == maxPoint)
                    {
                        Point pt1 = NewPoints[0];
                        Point pt2 = NewPoints[1];

                        // Finish this polygon.
                        if (NewPoints.Count > 1) Rectangles.Add(GetCornerPoints(pt1, pt2));
                        NewPoints = null;
                        picCanvas.SelectionRegion = new RectangleF();

                        // We no longer are drawing.
                        picCanvas.MouseMove += MouseMove_NotDrawing;
                        picCanvas.MouseMove -= MouseMove_Drawing;
                    }
                }

            }
            else if (MouseIsOverCornerPoint(mouse_pt, out hit_rectangle, out hit_point))
            {
                // Start dragging this corner.
                picCanvas.MouseMove -= MouseMove_NotDrawing;
                picCanvas.MouseMove += MouseMove_MovingCorner;
                picCanvas.MouseUp += picCanvas_MouseUp_MovingCorner;

                // Remember the polygon and point number.
                MovingRectangle = hit_rectangle;
                MovingPoint = hit_point;

                // Remember the offset from the mouse to the point.
                OffsetX = hit_rectangle[hit_point].X - e.X;
                OffsetY = hit_rectangle[hit_point].Y - e.Y;
            }
            else if (MouseIsOverEdge(mouse_pt, out hit_rectangle,
                out hit_point, out hit_point2, out closest_point))
            {
                // Add a point.
                hit_rectangle.Insert(hit_point + 1, closest_point);
            }
            else if (MouseIsOverPolygon(mouse_pt, out hit_rectangle))
            {
                // Start moving this polygon.
                picCanvas.MouseMove -= MouseMove_NotDrawing;
                picCanvas.MouseMove += MouseMove_MovingPolygon;
                picCanvas.MouseUp += MouseUp_MovingPolygon;

                // Remember the polygon.
                MovingRectangle = hit_rectangle;

                // Remember the offset from the mouse to the segment's first point.
                OffsetX = hit_rectangle[0].X - e.X;
                OffsetY = hit_rectangle[0].Y - e.Y;
            }
            else
            {
                // Start a new polygon.
                NewPoints = new List<Point>();
                NewPoint = ImageToControl(mouse_pt);
                NewPoints.Add(mouse_pt);

                // Get ready to work on the new polygon.
                picCanvas.MouseMove -= MouseMove_NotDrawing;
                picCanvas.MouseMove += MouseMove_Drawing;
            }

            // Redraw.
            picCanvas.Invalidate();
        }

        // Move the next point in the new polygon.
        public void MouseMove_Drawing(object sender, MouseEventArgs e)
        {
            NewPoint = e.Location;
            picCanvas.Invalidate();
        }

        // Move the selected corner.
        public void MouseMove_MovingCorner(object sender, MouseEventArgs e)
        {
            // Move the point.
            MovingRectangle[MovingPoint] = new Point(e.X + OffsetX, e.Y + OffsetY);

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

            int dx = new_x1 - MovingRectangle[0].X;
            int dy = new_y1 - MovingRectangle[0].Y;

            if (dx == 0 && dy == 0) return;

            // Move the polygon.
            for (int i = 0; i < MovingRectangle.Count; i++)
            {
                MovingRectangle[i] = new Point(
                    MovingRectangle[i].X + dx,
                    MovingRectangle[i].Y + dy);
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
            Point mouse_pt = picCanvas.PointToImage(e.Location);  //e.Location;
            var loc = mouse_pt; // this.PointToImage(e.Location);
            var loc2 = picCanvas.GetScaledPoint(mouse_pt);
            var loc3 = picCanvas.GetImageViewPort();
            var fac = picCanvas.ZoomFactor;
            var loc7 = picCanvas.PointToImage(mouse_pt);
            lblMsg.Text = $"Zm:{fac}, Pt2Img:{loc7.X},{loc7.Y}, picLoc:{loc.X},{loc.Y}, ScaledPt:{loc2.X},{loc2.Y}, e:{e.X},{e.Y}, srcROI:{loc3.X},{loc3.Y},{loc3.Width},{loc3.Height}";

            Cursor new_cursor = Cursors.Cross;

            // See what we're over.
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

            // Calculate the zoomed object radius.
            int zoomed_radius = (int)Math.Ceiling(object_radius * picCanvas.ZoomFactor);

            // Draw the old polygons.
            foreach (List<Point> polygon in Rectangles)
            {
                // Draw the polygon.
                PointF[] zoomed_points = GetZoomedPoints(polygon);
                e.Graphics.DrawPolygon(Pens.Aqua, zoomed_points);

                // Draw the corners.
                foreach (PointF corner in zoomed_points)
                {
                    RectangleF rect = new RectangleF(corner.X - zoomed_radius, corner.Y - zoomed_radius, 2 * zoomed_radius + 1, 2 * zoomed_radius + 1);
                    e.Graphics.FillEllipse(Brushes.White, rect);
                    e.Graphics.DrawEllipse(Pens.Black, rect);
                }
            }

            // Draw the new polygon.
            if (NewPoints != null)
            {
                // Draw the new polygon.
                if (NewPoints.Count > 0)
                {
                    var pt1 = NewPoints[NewPoints.Count - 1];
                    var pt2 = ControlToImage(NewPoint);
                    picCanvas.SelectionRegion = GetRectangle(pt1, pt2);
                }

                // Draw the newest edge.
                if (NewPoints.Count > 0)
                {
                    var pt1 = ImageToControl(NewPoints[NewPoints.Count - 1]);
                    var pt2 = NewPoint;
                    e.Graphics.DrawLine(new Pen(Color.Yellow, 2) { DashPattern = new float[] { 3, 3 } }, pt1, pt2);
                }
            }
        }

        // See if the mouse is over a corner point.
        public bool MouseIsOverCornerPoint(Point mouse_pt, out List<Point> hit_polygon, out int hit_pt)
        {
            // See if we're over a corner point.
            foreach (List<Point> polygon in Rectangles)
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
            for (int pgon = Rectangles.Count - 1; pgon >= 0; pgon--)
            {
                List<Point> polygon = Rectangles[pgon];

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
            for (int i = Rectangles.Count - 1; i >= 0; i--)
            {
                // Make a GraphicsPath representing the polygon.
                GraphicsPath path = new GraphicsPath();
                path.AddPolygon(Rectangles[i].ToArray());

                // See if the point is inside the GraphicsPath.
                if (path.IsVisible(mouse_pt))
                {
                    hit_polygon = Rectangles[i];
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

        public static List<Point> GetCornerPoints(Point startPoint, Point endPoint)
        {
            var rect = GetRectangle(startPoint, endPoint);
            List<Point> points = new List<Point>
            {
                new Point(rect.Left, rect.Top),
                new Point(rect.Right, rect.Top),
                new Point(rect.Right, rect.Bottom),
                new Point(rect.Left, rect.Bottom)
            };
            return points;
        }
        public static List<Point> GetCornerPoints(Rectangle rect)
        {
            List<Point> points = new List<Point>
            {
                new Point(rect.Left, rect.Top),
                new Point(rect.Right, rect.Top),
                new Point(rect.Right, rect.Bottom),
                new Point(rect.Left, rect.Bottom)
            };
            return points;
        }
        private static Rectangle GetRectangle(Point startPoint, Point endPoint)
        {
            return new Rectangle(
                Math.Min(startPoint.X, endPoint.X),
                Math.Min(startPoint.Y, endPoint.Y),
                Math.Abs(startPoint.X - endPoint.X),
                Math.Abs(startPoint.Y - endPoint.Y));
        }


        #endregion DistanceFunctions


    }
}
