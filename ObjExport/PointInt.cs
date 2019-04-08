using System;
using Autodesk.Revit.DB;

namespace ObjExport
{
    /// <summary>
    /// An integer-based 3D point class.
    /// </summary>
    public class PointInt : IComparable<PointInt>
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        /// <summary>
        /// Consider a Revit length zero 
        /// if is smaller than this.
        /// 考虑一个Revit长度为零，如果小于这个长度。
        /// </summary>
        const double _eps = 1.0e-9;

        /// <summary>
        /// Conversion factor from feet to millimetres.
        /// 从英尺到毫米的转换系数。
        /// </summary>
        const double _feet_to_mm = 25.4 * 12;

        /// <summary>
        /// Conversion a given length value 
        /// from feet to millimetre.
        /// 将给定的长度值从英尺转换为毫米。
        /// </summary>
        static int ConvertFeetToMillimetres(double d)
        {
            if (0 < d)
            {
                return _eps > d
                  ? 0
                  : (int)(_feet_to_mm * d + 0.5);

            }
            else
            {
                return _eps > -d
                  ? 0
                  : (int)(_feet_to_mm * d - 0.5);

            }
        }

        public PointInt(XYZ p)
        {
            X = ConvertFeetToMillimetres(p.X);
            Y = ConvertFeetToMillimetres(p.Y);
            Z = ConvertFeetToMillimetres(p.Z);

            //if (switch_coordinates)
            if (true)
            {
                //X = X;
                //var tmp = Y;
                //Y = Z;
                //Z = tmp;
                var tmp = Y;
                Y = Z;
                Z = tmp;
                Z = -Z;
            }
        }

        public int CompareTo(PointInt a)
        {
            int d = X - a.X;

            if (0 == d)
            {
                d = Y - a.Y;

                if (0 == d)
                {
                    d = Z - a.Z;
                }
            }
            return (0 == d) ? 0 : ((0 < d) ? 1 : -1);
        }
    }

}
