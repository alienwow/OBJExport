using System;
using Autodesk.Revit.DB;

namespace ObjExport
{
    /// <summary>
    /// An integer-based 3D point class.
    /// </summary>
    public class PointInt : IComparable<PointInt>
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

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
        static double ConvertFeetToMillimetres(double d)
        {
            if (0 < d)
            {
                return _eps > d
                  ? 0
                  : (double)(_feet_to_mm * d + 0.5);

            }
            else
            {
                return _eps > -d
                  ? 0
                  : (double)(_feet_to_mm * d - 0.5);

            }
        }

        public PointInt(XYZ p)
        {
            X = ConvertFeetToMillimetres(p.X) / 1000.0;
            Y = ConvertFeetToMillimetres(p.Y) / 1000.0;
            Z = ConvertFeetToMillimetres(p.Z) / 1000.0;

            if (true)
            {
                Y = -Y;
                var tmp = Y;
                Y = Z;
                Z = tmp;
            }
        }

        public int CompareTo(PointInt a)
        {
            double d = X - a.X;

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
