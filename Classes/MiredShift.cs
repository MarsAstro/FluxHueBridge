using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluxHueBridge
{
    public static class MiredShift
    {
        public enum MiredShiftType
        {
            QuiteABitWarmer,
            SlightlyWarmer,
            MatchScreen
        }

        public static MiredShiftType GetMiredShiftType()
        {
            return (MiredShiftType)Properties.Settings.Default.MiredShiftType;
        }

        public static void SetMiredShiftType(MiredShiftType miredShiftType)
        {
            Properties.Settings.Default.MiredShiftType = (int)miredShiftType;
            Properties.Settings.Default.Save();
        }

        public static float GetMiredShift()
        {
            switch(GetMiredShiftType())
            {
                case MiredShiftType.MatchScreen: return 0f;
                case MiredShiftType.SlightlyWarmer: return 45f;
                case MiredShiftType.QuiteABitWarmer: return 90f;
                default: return 0f;
            }
        }
    }
}
