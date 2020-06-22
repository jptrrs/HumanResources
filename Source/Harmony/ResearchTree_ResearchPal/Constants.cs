// Constants.cs
// Copyright Karel Kroeze, 2018-2020

using UnityEngine;

namespace HumanResources
{
    public static class Constants
    {
        public const double Epsilon = 1e-4;
        public const float Margin = 6f;
        public const float QueueLabelSize = 30f;
        public static readonly Vector2 IconSize = new Vector2(18f, 18f);
        public static readonly Vector2 NodeMargins = new Vector2(50f, 10f);
        public static bool showCompact = false;
        private static float defaultNodeSizeY = 50f;
        public static Vector2 NodeSize => new Vector2(200f, compactSizeY);
        private static float compactSizeY => showCompact ? defaultNodeSizeY / 2 : defaultNodeSizeY;
    }
}