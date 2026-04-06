using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Inovance.Utils
{
    internal class ByteHelper
    {
      internal static short[] ConvertToShortArray(object data)
        {
            if (data is short[] shorts) return shorts;
            if (data is object[] objArray)
                return objArray.Select(o => Convert.ToInt16(o)).ToArray();
            if (data is IEnumerable<object> enumerable)
                return enumerable.Select(o => Convert.ToInt16(o)).ToArray();

            return null;
        }

        internal static int[] ConvertToIntArray(object data)
        {
            if (data is int[] ints) return ints;
            if (data is object[] objArray)
                return objArray.Select(o => Convert.ToInt32(o)).ToArray();
            if (data is IEnumerable<object> enumerable)
                return enumerable.Select(o => Convert.ToInt32(o)).ToArray();

            return null;
        }

        internal static float[] ConvertToFloatArray(object data)
        {
            if (data is float[] floats) return floats;
            if (data is object[] objArray)
                return objArray.Select(o => Convert.ToSingle(o)).ToArray();
            if (data is IEnumerable<object> enumerable)
                return enumerable.Select(o => Convert.ToSingle(o)).ToArray();

            return null;
        }

        internal static double[] ConvertToDoubleArray(object data)
        {
            if (data is double[] doubles) return doubles;
            if (data is object[] objArray)
                return objArray.Select(o => Convert.ToDouble(o)).ToArray();
            if (data is IEnumerable<object> enumerable)
                return enumerable.Select(o => Convert.ToDouble(o)).ToArray();

            return null;
        }
    }
}
