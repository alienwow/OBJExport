#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.DB;
using ObjExport.Interfaces;
#endregion // Namespaces

namespace ObjExport
{
    class ObjExporter : IJtFaceEmitter
    {
        /// <summary>
        /// Set this to support colour and transparency.
        /// </summary>
        static bool _add_color = true;

        /// <summary>
        /// Set this flag to switch everything that has
        /// even a little bit of transparency to be 
        /// completely transparent for testing purposes.
        /// </summary>
        static bool _more_transparent = false;

        #region MTL statement format strings

        //材质的阴影色（ambient color）用Ka声明。颜色用RGB定义，每条通道的值从0到1之间取。
        //Ka 1.000 1.000 1.000 # 白色
        //类似地，固有色（diffuse color）用Kd。
        //Kd 1.000 1.000 1.000 # 白色
        //高光色（specular color）用Ks。带权高光色则用高光系数Ns表示。
        //Ks 0.000 0.000 0.000 # 黑色（即关闭高光）
        //Ns 10.000 # 范围从0到1000
        //材质可以是透明的，即是说“溶解的”。与真透明度不同，其结果并不依赖于物体厚度。
        //d 0.9 # 有些用'd'实现
        //Tr 0.9 # 其他的用'Tr'
        //const string _mtl_newmtl_d
        //  = "newmtl {0}\r\n"
        //  + "Ka {1} {2} {3}\r\n"
        //  + "Kd {1} {2} {3}\r\n"
        //  + "d {4}";
        const string _mtl_newmtl_d
          = "newmtl {0}\r\n"
          + "Ka 0 0 0\r\n"
          + "Kd {1} {2} {3}\r\n"
          + "d {4}";

        const string _mtl_mtllib = "mtllib {0}";

        const string _mtl_usemtl = "usemtl {0}";

        const string _mtl_vertex = "v {0} {1} {2}";

        const string _mtl_face = "f {0} {1} {2}";

        #endregion // MTL statement format strings

        //VertexLookupXyz _vertices;
        VertexLookupInt _vertices;

        //ColorLookup _colors;
        ColorTransparencyLookup _color_transparency_lookup;

        /// <summary>
        /// List of triangles, defined as 
        /// triples of vertex indices.
        /// Colours are also stored in the 
        /// list, using a negative first index,
        /// with the three rgb values stored in the 
        /// following one, followed by a zero value
        /// to keep the values in triples.
        /// </summary>
        List<int> _triangles;

        /// <summary>
        /// Keep track of the number of faces processed.
        /// </summary>
        int _faceCount;

        /// <summary>
        /// Keep track of the number of triangles processed.
        /// Originally, we just returned _triangles.Count
        /// divided by 3, but that no longer works now that 
        /// colours may be stored as well.
        /// </summary>
        int _triangleCount;

        public ObjExporter()
        {
            _faceCount = 0;
            _triangleCount = 0;
            _vertices = new VertexLookupInt();
            _triangles = new List<int>();

            if (_add_color)
            {
                _color_transparency_lookup
                  = new ColorTransparencyLookup();
            }
        }

        /// <summary>
        /// Set a colour for the following faces.
        /// </summary>
        void StoreColorTransparency(Color color, int transparency)
        {
            _triangles.Add(-1); // color marker

            _triangles.Add(Util.ColorTransparencyToInt(
              color, transparency));

            _triangles.Add(0); // multiple of three
        }

        /// <summary>
        /// Add the vertices of the given triangle to our
        /// vertex lookup dictionary and emit a triangle.
        /// </summary>
        void StoreTriangle(MeshTriangle triangle)
        {
            for (int i = 0; i < 3; ++i)
            {
                XYZ p = triangle.get_Vertex(i);
                PointInt q = new PointInt(p);
                _triangles.Add(_vertices.AddVertex(q));
            }
        }

        /// <summary>
        /// Emit a Revit geometry Face object and 
        /// return the number of resulting triangles.
        /// </summary>
        public int EmitFace(Face face, Color color, int transparency)
        {
            Debug.Assert(0 <= transparency,
              "expected non-negative transparency");

            Debug.Assert(100 >= transparency,
              "expected transparency between 0 and 100");

            Debug.Assert(100 * Math.Pow(2, 24) == 1677721600,
              "expected shifted transparency to fit into a signed integer");

            Debug.Assert(1677721600 < int.MaxValue,
              "expected transparency to fit into a signed integer");

            ++_faceCount;

            if (_add_color && _color_transparency_lookup.AddColorTransparency(color, transparency))
            {
                StoreColorTransparency(color, transparency);
            }

            Mesh mesh = face.Triangulate(8.0 / 15.0);
            //Mesh mesh = face.Triangulate();

            int n = mesh.NumTriangles;

            Debug.Print(" {0} mesh triangles", n);

            for (int i = 0; i < n; ++i)
            {
                ++_triangleCount;

                MeshTriangle t = mesh.get_Triangle(i);

                StoreTriangle(t);
            }
            return n;
        }

        public int GetFaceCount()
        {
            return _faceCount;
        }

        /// <summary>
        /// Return the number of triangles processed.
        /// </summary>
        public int GetTriangleCount()
        {
            // Originally, we just returned _triangles.Count
            // divided by 3, but that no longer works now 
            // that colours may be stored as well.

            if (!_add_color)
            {
                int n = _triangles.Count;

                Debug.Assert(0 == n % 3,
                  "expected a multiple of 3");

                Debug.Assert(_triangleCount.Equals(n / 3),
                  "expected equal triangle count");
            }
            return _triangleCount;
        }

        /// <summary>
        /// Return the number of uhnique vertices.
        /// </summary>
        public int GetVertexCount()
        {
            return _vertices.Count;
        }

        #region ExportTo: output the OBJ file
        /// <summary>
        /// Write a new colour definition to the 
        /// material library.
        /// Revit transparency lies between 0 and 100, 
        /// where 100 is completely transparent and 0 
        /// opaque. In MTL, the transparency is written 
        /// using either a 'd' or a 'Tr' statement with
        /// values ranging from 0.0 to 1.0, where 1.0 is 
        /// opaque.
        /// </summary>
        static void EmitColorTransparency(
          StreamWriter s,
          int trgb)
        {
            // 透明度
            int transparency;

            Color color = Util.IntToColorTransparency(
              trgb, out transparency);

            string name = Util.ColorTransparencyString(
              color, transparency);

            if (_more_transparent && 0 < transparency)
            {
                transparency = 100;
            }

            s.WriteLine(_mtl_newmtl_d,
              name,
              color.Red / 255.0,
              color.Green / 255.0,
              color.Blue / 255.0,
              (100 - transparency) / 100.0);
        }

        /// <summary>
        /// Obsolete: emit an XYZ vertex.
        /// </summary>
        static void EmitVertex(StreamWriter s, XYZ p)
        {
            s.WriteLine(_mtl_vertex,
              Util.RealString(p.X),
              Util.RealString(p.Y),
              Util.RealString(p.Z));
        }

        /// <summary>
        /// Emit a vertex to OBJ. The first vertex listed 
        /// in the file has index 1, and subsequent ones
        /// are numbered sequentially.
        /// </summary>
        static void EmitVertex(StreamWriter s, PointInt p)
        {
            s.WriteLine(_mtl_vertex, p.X, p.Y, p.Z);
        }

        /// <summary>
        /// Set colour and transparency for subsequent 
        /// faces, referring to the named materials in 
        /// the material library.
        /// </summary>
        static void SetColorTransparency(StreamWriter s, int trgb)
        {
            int transparency;

            Color color = Util.IntToColorTransparency(
              trgb, out transparency);

            string name = Util.ColorTransparencyString(
              color, transparency);

            s.WriteLine(_mtl_usemtl, name);
        }

        /// <summary>
        /// Emit an OBJ triangular face.
        /// </summary>
        static void EmitFacet(StreamWriter s, int i, int j, int k)
        {
            s.WriteLine(_mtl_face, i + 1, j + 1, k + 1);
        }

        public void ExportTo(string path)
        {
            string material_library_path = null;

            if (_add_color)
            {
                material_library_path = Path.ChangeExtension(path, "mtl");

                using (StreamWriter s = new StreamWriter(material_library_path))
                {
                    foreach (int key in _color_transparency_lookup.Keys)
                    {
                        EmitColorTransparency(s, key);
                    }
                }
            }

            using (StreamWriter s = new StreamWriter(path))
            {
                if (_add_color)
                {
                    s.WriteLine(_mtl_mtllib, Path.GetFileName(material_library_path));
                }

                foreach (PointInt key in _vertices.Keys)
                {
                    EmitVertex(s, key);
                }

                int i = 0;
                int n = _triangles.Count;

                while (i < n)
                {
                    int i1 = _triangles[i++];
                    int i2 = _triangles[i++];
                    int i3 = _triangles[i++];

                    if (-1 == i1)
                    {
                        SetColorTransparency(s, i2);
                    }
                    else
                    {
                        EmitFacet(s, i1, i2, i3);
                    }
                }
            }
        }

        #endregion // ExportTo: output the OBJ file
    }
}
