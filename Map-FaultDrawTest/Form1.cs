using Ichihai1415.GeoJSON;
using System.Drawing.Drawing2D;
using System.Text.Json;

namespace Map_FaultDrawTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        internal static GeoJSONScheme.GeoJSON_JMA_Map? map = null;
        internal static GeoJSONScheme.GeoJSON_JMA_FaultDL? fault = null;
        internal static GeoJSONScheme.GeoJSON_Base? trench = null;


        private void Form1_Load(object sender, EventArgs e)
        {
            var mapRaw = File.ReadAllText(@"D:\Ichihai1415\data\map\JMA\geojson\AreaForecastLocalE_GIS_20240520_01.geojson");
            var faultRaw = File.ReadAllText(@"D:\Ichihai1415\data\jma\webapi\faultDL.geojson");//https://www.jma.go.jp/bosai/hypo/const/faultDL.geojson
            var trenchRaw = File.ReadAllText(@"D:\Ichihai1415\data\jma\webapi\trench.geojson");//https://www.jma.go.jp/bosai/hypo/const/trench.geojson

            map = JsonSerializer.Deserialize<GeoJSONScheme.GeoJSON_JMA_Map>(mapRaw, GeoJSONHelper.ORIGINAL_GEOMETRY_SERIALIZER_OPTIONS_SAMPLE);
            fault = JsonSerializer.Deserialize<GeoJSONScheme.GeoJSON_JMA_FaultDL>(faultRaw, GeoJSONHelper.ORIGINAL_GEOMETRY_SERIALIZER_OPTIONS_SAMPLE);
            trench = JsonSerializer.Deserialize<GeoJSONScheme.GeoJSON_Base>(trenchRaw, GeoJSONHelper.ORIGINAL_GEOMETRY_SERIALIZER_OPTIONS_SAMPLE);

            var latSta = 20.0f;
            var latEnd = 50.0f;
            var lonSta = 120.0f;
            var lonEnd = 150.0f;
            var zoom = 1080f / (latEnd - latSta);

            var img = DrawMap(latSta, latEnd, lonSta, lonEnd);
            using var g = Graphics.FromImage(img);



            foreach (var feature in fault.Features)
            {
                foreach (var singleObject in feature.Geometry.Coordinates.Objects)
                {
                    var points = singleObject.MainPoints.Select(coordinate => new PointF((coordinate.Lon - lonSta) * zoom, (latEnd - coordinate.Lat) * zoom));
                    if (points.Count() > 2)
                    {
                        g.DrawLines(Pens.Red, points.ToArray());
                    }
                }
            }

            foreach (var feature in trench.Features)
            {
                foreach (var singleObject in feature.Geometry.Coordinates.Objects)
                {
                    var points = singleObject.MainPoints.Select(coordinate => new PointF((coordinate.Lon - lonSta) * zoom, (latEnd - coordinate.Lat) * zoom));
                    if (points.Count() > 2)
                    {
                        g.DrawLines(Pens.Yellow, points.ToArray());
                    }
                }
            }

            BackgroundImage = img;


        }

        public static Bitmap DrawMap(float latSta, float latEnd, float lonSta, float lonEnd)
        {

            var zoom = 1080f / (latEnd - latSta);

            var bitmap = new Bitmap(1920, 1080);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.FromArgb(20, 40, 60));
            using var gp = new GraphicsPath();





            foreach (var feature in map.Features)
            {
                if (feature.Geometry == null)
                    continue;
                if (feature.Geometry.Type == "Polygon")
                {
                    gp.StartFigure();
                    var points = feature.Geometry.Coordinates.Objects[0].MainPoints.Select(coordinate => new PointF((coordinate.Lon - lonSta) * zoom, (latEnd - coordinate.Lat) * zoom));
                    if (points.Count() > 2)
                    {
                        gp.AddPolygon(points.ToArray());
                        g.FillPolygon(new SolidBrush(Color.FromArgb(100, 100, 150)), points.ToArray());
                    }
                }
                else
                {

                    foreach (var singleObject in feature.Geometry.Coordinates.Objects)
                    {
                        gp.StartFigure();
                        var points = singleObject.MainPoints.Select(coordinate => new PointF((coordinate.Lon - lonSta) * zoom, (latEnd - coordinate.Lat) * zoom));
                        if (points.Count() > 2)
                        {
                            gp.AddPolygon(points.ToArray());
                            g.FillPolygon(new SolidBrush(Color.FromArgb(100, 100, 150)), points.ToArray());
                        }
                    }
                }
            }
            var lineWidth = Math.Max(1f, zoom / 216f);
            //g.FillPath(new SolidBrush(Color.FromArgb(100, 100, 150)), gp);
            g.DrawPath(new Pen(Color.FromArgb(255, 200, 200, 200), lineWidth) { LineJoin = LineJoin.Round }, gp);//zoom > 200 ? 2 : 1
            return bitmap;
        }

    }



}
