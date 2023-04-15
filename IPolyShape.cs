using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace howto_polygon_editor2
{
    public interface IPolyShape
    {
        void MouseDown(object sender, MouseEventArgs e);
        bool MouseIsOverCornerPoint(Point mouse_pt, out List<Point> hit_polygon, out int hit_pt);
        bool MouseIsOverEdge(Point mouse_pt, out List<Point> hit_polygon, out int hit_pt1, out int hit_pt2, out Point closest_point);
        bool MouseIsOverPolygon(Point mouse_pt, out List<Point> hit_polygon);
        void MouseMove_Drawing(object sender, MouseEventArgs e);
        void MouseMove_MovingCorner(object sender, MouseEventArgs e);
        void MouseMove_MovingPolygon(object sender, MouseEventArgs e);
        void MouseMove_NotDrawing(object sender, MouseEventArgs e);
        void MouseUp_MovingPolygon(object sender, MouseEventArgs e);
        void Paint(object sender, PaintEventArgs e);
        void picCanvas_MouseUp_MovingCorner(object sender, MouseEventArgs e);
    }
}