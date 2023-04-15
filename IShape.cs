using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Cyotek.Windows.Forms;

namespace howto_polygon_editor2
{
    public interface IShape
    {
        List<Point> Points { get; set; }

        void AddNewImagePoint(Point e);
        void Reset();
        void Draw(Image img);
        void DrawRubberLine(Control ctr, Point currentLocation);
        void Draw(Image image, Color color);
    }
}
