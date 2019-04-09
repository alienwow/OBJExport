using System.Collections.Generic;
using System.Text;

namespace ObjExport
{
    public class ObjModel
    {
        public string UniqueId { get; set; }

        /// <summary>
        /// 点数据
        /// 已废弃，将所有数据点优化到一个字典里，
        /// </summary>
        public List<PointInt> Vertexs { get; set; }
        public List<object> vt { get; set; }
        public string Mtl { get; set; } = "";

        /// <summary>
        /// 三角面数据
        /// </summary>
        public List<VFace> Faces { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            // o [name]
            sb.AppendLine($"o {UniqueId}");
            //// v {0} {1} {2}
            //foreach (var item in Vertexs)
            //{
            //    sb.AppendLine($"v {item.X.ToString("0.##")} {item.Y.ToString("0.##")} {item.Z.ToString("0.##")}");
            //}
            //// vt {0} {1} {2}
            //foreach (var item in Vertexs)
            //{
            //    sb.AppendLine($"vt {item.X.ToString("0.##")} {item.Y.ToString("0.##")} {item.X.ToString("0.##")}");
            //}
            // usemtl [melId]
            if (string.IsNullOrEmpty(Mtl))
                sb.AppendLine($"usemtl");
            else
                sb.AppendLine($"usemtl {Mtl}");

            // f {0} {1} {2}
            foreach (var item in Faces)
            {
                sb.AppendLine($"f {item.Point1} {item.Point2} {item.Point3}");
                //sb.AppendLine($"f {item.Point1}/{item.Point1} {item.Point2}/{item.Point2} {item.Point3}/{item.Point3}");
            }
            return sb.ToString().TrimEnd('\n').TrimEnd('\r');
        }
    }

    public struct VFace
    {
        public long Point1 { get; set; }
        public long Point2 { get; set; }
        public long Point3 { get; set; }
    }
}
