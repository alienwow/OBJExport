using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace ObjExport
{
    #region ColorTransparencyLookup
    /// <summary>
    /// A colour and transparency lookup class to 
    /// eliminate duplicate material definitions.
    /// </summary>
    public class ColorTransparencyLookup : Dictionary<int, int>
    {
        int _current;

        public ColorTransparencyLookup()
        {
            _current = Util.ColorTransparencyToInt(
              Command.DefaultColor, 0);
        }

        /// <summary>
        /// Add a new entry for the given colour,
        /// if needed. Return true if the given 
        /// colour differs from the current colour,
        /// and update the current colour.
        /// </summary>
        public bool AddColorTransparency(Color color, int transparency)
        {
            int trgb = Util.ColorTransparencyToInt(color, transparency);

            if (!ContainsKey(trgb))
            {
                this[trgb] = Count;
            }

            bool rc = !_current.Equals(trgb);
            _current = trgb;

            return rc;
        }
    }
    #endregion // ColorTransparencyLookup

}
