using Cyotek.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace howto_polygon_editor2
{
    public class NormRectangle
    {
        private readonly int maxPoint = 2;

        // The "size" of an object for mouse over purposes.
        private const int object_radius = 4;

        // We're over an object if the distance squared
        // between the mouse and the object is less than this.
        private const int over_dist_squared = object_radius * object_radius;

        // Each polygon is represented by a List<Point>.
        private readonly List<Rectangle> Rectangles = new List<Rectangle>();

        // Points for the new polygon.
        private List<Point> NewPoints = null;

        // The current mouse position while drawing a new polygon.
        private Point NewPoint;

        // The polygon and index of the corner we are moving.
        private Rectangle MovingRectangle= Rectangle.Empty;
        private int MovingPoint = -1;
        private int OffsetX, OffsetY;

        // The add point cursor.
        private readonly Cursor AddPointCursor;

        private readonly ImageBox picCanvas;
        public NormRectangle(ImageBox PicCanvas)
        {
            AddPointCursor = new Cursor(Properties.Resources.add_point.GetHicon());
            picCanvas = PicCanvas;
        }

        // Start or continue drawing a new polygon,
        // or start moving a corner or polygon.
        public void MouseDown(object sender, MouseEventArgs e)
        {
            // See what we're over.
            Point mouse_pt = e.Location;
            Rectangle hit_rectangle = Rectangle.Empty; 
            int hit_point, hit_point2;
            Point closest_point;

            if (NewPoints != null)
            {
                // Add a point to this polygon.
                if (NewPoints[NewPoints.Count - 1] != e.Location)
                {
                    NewPoints.Add(e.Location);

                    // We are already drawing a polygon.
                    // If it's the right mouse button, finish this polygon.
                    if (NewPoints.Count == maxPoint) //e.Button == MouseButtons.Right)
                    {
                        Point pt1 = NewPoints[0];
                        Point pt2 = NewPoints[1];

                        // Finish this polygon.
                        if (NewPoints.Count > 1) Rectangles.Add(GetRectangle(pt1, pt2));
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
                OffsetX = hit_rectangle.X - e.X;
                OffsetY = hit_rectangle.Y - e.Y;
            }
            else if (MouseIsOverEdge(mouse_pt, out hit_rectangle, out hit_point, out hit_point2, out closest_point))
            {
                // Add a point.
                //hit_rectangle.Insert(hit_point + 1, closest_point);
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
                OffsetX = hit_rectangle.X - e.X;
                OffsetY = hit_rectangle.Y - e.Y;
            }
            else
            {
                // Start a new polygon.
                NewPoints = new List<Point>();
                NewPoint = e.Location;
                NewPoints.Add(e.Location);

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
            //MovingRectangle[MovingPoint] = new Point(e.X + OffsetX, e.Y + OffsetY);
            MovingRectangle.Location = new Point(e.X + OffsetX, e.Y + OffsetY);

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

            int dx = new_x1 - MovingRectangle.X;
            int dy = new_y1 - MovingRectangle.Y;

            if (dx == 0 && dy == 0) return;

            // Move the polygon.
            MovingRectangle.Location = new Point(MovingRectangle.X + dx, MovingRectangle.Y + dy);

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
            Point mouse_pt = e.Location;
            Rectangle hit_polygon;
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

            // Draw the old polygons.
            foreach (Rectangle rectangle in Rectangles)
            {
                // Draw the polygon.
                e.Graphics.DrawRectangle(Pens.Blue, rectangle);

                // Draw the corners.
                foreach (Point corner in GetCornerPoints(rectangle))
                {
                    Rectangle rect = new Rectangle(
                        corner.X - object_radius, corner.Y - object_radius,
                        2 * object_radius + 1, 2 * object_radius + 1);
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
                    var pt1 = NewPoints[0];
                    var pt2 = NewPoint;                    
                    picCanvas.SelectionRegion = GetRectangle(pt1, pt2);
                }

                // Draw the newest edge.
                if (NewPoints.Count > 0)
                {
                    using (Pen dashed_pen = new Pen(Color.Green))
                    {
                        dashed_pen.DashPattern = new float[] { 3, 3 };
                        e.Graphics.DrawLine(dashed_pen,
                            NewPoints[NewPoints.Count - 1],
                            NewPoint);
                    }
                }
            }
        }

        // See if the mouse is over a corner point.
        public bool MouseIsOverCornerPoint(Point mouse_pt, out Rectangle hit_polygon, out int hit_pt)
        {
            // See if we're over a corner point.            
            foreach(var rectangle in Rectangles)
            {
                List<Point> polygon = GetCornerPoints(rectangle);                
                // See if we're over one of the polygon's corner points.
                for (int i = 0; i < polygon.Count; i++)
                {
                    // See if we're over this point.
                    if (FindDistanceToPointSquared(polygon[i], mouse_pt) < over_dist_squared)
                    {
                        // We're over this point.
                        hit_polygon = rectangle;
                        hit_pt = i;
                        return true;
                    }
                }                
            }

            hit_polygon = Rectangle.Empty;
            hit_pt = -1;
            return false;
        }

        // See if the mouse is over a polygon's edge.
        public bool MouseIsOverEdge(Point mouse_pt, out Rectangle hit_polygon, out int hit_pt1, out int hit_pt2, out Point closest_point)
        {
            // Examine each polygon.
            // Examine them in reverse order to check the ones on top first.
            for (int pgon = Rectangles.Count - 1; pgon >= 0; pgon--)
            {
                List<Point> polygon = GetCornerPoints(Rectangles[pgon]);

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
                        hit_polygon = Rectangles[pgon];
                        hit_pt1 = p1;
                        hit_pt2 = p2;
                        closest_point = Point.Round(closest);
                        return true;
                    }
                }
            }

            hit_polygon = Rectangle.Empty;
            hit_pt1 = -1;
            hit_pt2 = -1;
            closest_point = new Point(0, 0);
            return false;
        }

        // See if the mouse is over a polygon's body.
        public bool MouseIsOverPolygon(Point mouse_pt, out Rectangle hit_polygon)
        {
            // Examine each polygon.
            // Examine them in reverse order to check the ones on top first.
            for (int i = Rectangles.Count - 1; i >= 0; i--)
            {
                // Make a GraphicsPath representing the polygon.
                GraphicsPath path = new GraphicsPath();
                path.AddPolygon(GetCornerPoints(Rectangles[i]).ToArray());

                // See if the point is inside the GraphicsPath.
                if (path.IsVisible(mouse_pt))
                {
                    hit_polygon = Rectangles[i];
                    return true;
                }
            }

            hit_polygon = Rectangle.Empty;
            return false;
        }

        #region DistanceFunctions
        public static List<Point> GetCornerPoints(Rectangle rect)
        {
            List<Point> points = new List<Point>
            {
                new Point(rect.Left, rect.Top),         //0
                new Point(rect.Right, rect.Top),        //1
                new Point(rect.Right, rect.Bottom),     //2
                new Point(rect.Left, rect.Bottom)       //3
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

        #endregion DistanceFunctions


    }
}
