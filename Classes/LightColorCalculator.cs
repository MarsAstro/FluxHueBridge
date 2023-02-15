using FluxHueBridge.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluxHueBridge
{
    public static class LightColorCalculator
    {
        public static LightColor GetLightColorFromKelvin(int kelvin)
        {
            var miredShift = MiredShift.GetMiredShift();

            var mireds = (1000000f / kelvin) + miredShift;

            if (mireds < 153f) mireds = 153f;

            if (mireds >= 153f && mireds <= 500)
                return new LightColor() { IsMirek = true, Mirek = (int)mireds };
            else
                return GetCIEObject(mireds);
        }

        private static LightColor GetCIEObject(float mireds)
        {
            var lightColor = new LightColor() { IsMirek = false };

            var kelvin = (int)(1000000 / mireds);
            var lineNumberInFile = kelvin - 997;
            var lines = Resources.BlackBodyLocus.Replace("\r", "").Split('\n');
            var lineItems = lines[lineNumberInFile - 1].Split('\t');

            lightColor.X = Math.Round(double.Parse(lineItems[1]), 4);
            lightColor.Y = Math.Round(double.Parse(lineItems[2]), 4);

            return lightColor;
        }
    }

    public class LightColor
    {
        public bool IsMirek { get; set; }
        public long Mirek { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }
}
