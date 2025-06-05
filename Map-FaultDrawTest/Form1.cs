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
            var size = 1080f * 1;
            var zoom = size / (latEnd - latSta);

            var img = DrawMap(latSta, latEnd, lonSta, lonEnd, size);
            using var g = Graphics.FromImage(img);

            //var faultCsv = new StringBuilder("id,name,name1,active,size,rank\n");

            foreach (var feature in fault.Features)
            {
                // 活断層における今後30年以内の地震発生確率が3%以上を「Ｓランク」、0.1～3％未満を「Ａランク」、0.1%未満を「Ｚランク」、不明（すぐに地震が
                // 起きることが否定できない）を「Ｘランク」と表記している。地震後経過率（注2）が0.7以上である活断層については、ランクに「＊」を付記してい
                // る。Ｚランクでも、活断層が存在すること自体、当該地域で大きな地震が発生する可能性を示す。
                /*
                faultCsv.Append(feature.Properties.Id);
                faultCsv.Append(',');
                faultCsv.Append(feature.Properties.Name);
                faultCsv.Append(',');
                faultCsv.Append(feature.Properties.Name1);
                faultCsv.Append(',');
                faultCsv.Append(feature.Properties.Active);
                faultCsv.Append(',');
                faultCsv.Append(feature.Properties.Size);
                faultCsv.Append(',');
                faultCsv.Append(feature.Properties.Rank);
                faultCsv.AppendLine();
                */
                var rankSt = feature.Properties.Rank;
                var rankId = -1;
                switch (rankSt.Split("として").Last().Replace('（', '(').Split('(')[0])//中南部としてA*ランク　くらいしかないけど　複数ケースは1を利用 なぜか全角半角かっこあるので統一
                {
                    //複数ケース例: https://www.jishin.go.jp/regional_seismicity/rs_katsudanso/f103_muikamachi/　https://www.jishin.go.jp/regional_seismicity/rs_katsudanso/shinji/
                    case "Sランク":
                    case "S*ランク":
                        rankId = 1;
                        break;
                    case "Aランク":
                    case "A*ランク":
                        rankId = 2;
                        break;
                    case "Zランク":
                        rankId = 3;
                        break;
                    case "Xランク":
                        rankId = 4;
                        break;
                    case "-":
                    case "単独で震源断層となることはないと推定":
                        rankId = 5;
                        break;
                }
                if (rankId == -1) Console.WriteLine("vvvvvvvvvv不明ランク検知vvvvvvvvvv");
                Console.WriteLine(feature.Properties.Name + " : " + rankSt + " = " + rankSt.Split("として").Last().Replace('（', '(').Split('(')[0] + " => " + rankId);

                foreach (var singleObject in feature.Geometry.Coordinates.Objects)
                {
                    var points = singleObject.MainPoints.Select(coordinate => new PointF((coordinate.Lon - lonSta) * zoom, (latEnd - coordinate.Lat) * zoom));
                    if (points.Count() > 2)
                        switch (rankId)
                        {
                            case 1:
                                g.DrawLines(Pens.Red, points.ToArray());
                                break;
                            case 2:
                                g.DrawLines(Pens.Yellow, points.ToArray());
                                break;
                            case 3:
                                g.DrawLines(Pens.Wheat, points.ToArray());
                                break;
                            case 4:
                                g.DrawLines(Pens.Gray, points.ToArray());
                                break;
                            case 5:
                                g.DrawLines(Pens.LightGray, points.ToArray());
                                break;
                            default:
                                g.DrawLines(Pens.White, points.ToArray());
                                break;

                        }
                }
            }

            //Console.WriteLine(faultCsv.ToString());
            //File.WriteAllText(@"faultDL-features.csv", faultCsv.ToString());

            foreach (var feature in trench.Features)
            {
                foreach (var singleObject in feature.Geometry.Coordinates.Objects)
                {
                    var points = singleObject.MainPoints.Select(coordinate => new PointF((coordinate.Lon - lonSta) * zoom, (latEnd - coordinate.Lat) * zoom));
                    if (points.Count() > 2)
                    {
                        g.DrawLines(Pens.DarkRed, points.ToArray());
                    }
                }
            }

            BackgroundImage = img;
            img.Save("output.png", System.Drawing.Imaging.ImageFormat.Png);

        }

        public static Bitmap DrawMap(float latSta, float latEnd, float lonSta, float lonEnd, float size = 1080f)
        {

            var zoom = size / (latEnd - latSta);

            var bitmap = new Bitmap((int)(size * 16 / 9), (int)size);
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
