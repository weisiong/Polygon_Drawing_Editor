using System;
using System.Windows.Forms;

namespace howto_polygon_editor2
{
    public partial class Form1 : Form
    {
        private int DrawMode = 1;
        private Polygon polygon;
        private PolyRectangle normRectangle;
        private IPolyShape polyShape;
        public Form1()
        {
            InitializeComponent();
        }

        // Create the add point cursor.
        private void Form1_Load(object sender, EventArgs e)
        {
            polygon = new Polygon(picCanvas);
            normRectangle = new PolyRectangle(picCanvas);
            normRectangle.lblMsg = lblMsg;
            normRectangle.LoadImage(@"C:\\SourceCodes\\My_Data\\pic01.jpg");
        }

        // Start or continue drawing a new polygon,
        // or start moving a corner or polygon.
        private void PicCanvas1_MouseDown(object sender, MouseEventArgs e)
        {
            if (DrawMode == 1)
                polygon.MouseDown(sender, e);
            if (DrawMode == 0)
                normRectangle.MouseDown(sender, e);
        }

        // See if we're over a polygon or corner point.
        private void PicCanvas1_MouseMove_NotDrawing(object sender, MouseEventArgs e)
        {
            if (DrawMode == 1)
                polygon.MouseMove_NotDrawing(sender, e);
            if (DrawMode == 0)
                normRectangle.MouseMove_NotDrawing(sender, e);
        }

        // Redraw old polygons in blue. Draw the new polygon in green.
        // Draw the final segment dashed.
        private void PicCanvas1_Paint(object sender, PaintEventArgs e)
        {
            if (DrawMode == 1)
                polygon.Paint(sender, e);
            if (DrawMode == 0)
                normRectangle.Paint(sender, e);
        }

        private void rbRect_CheckedChanged(object sender, EventArgs e)
        {
            DrawMode = 0;
        }

        private void rbPolygon_CheckedChanged(object sender, EventArgs e)
        {
            DrawMode = 1;
        }

    }
}
