using UnityEngine;

namespace HumanResources
{
    public static class ResearchTree_Constants
    {
#pragma warning disable 0649
        private static object instance;
#pragma warning restore
        public static double Epsilon => (double)ResearchTree_Patches.EpsilonInfo.GetValue(instance);
        public static float DetailedModeZoomLevelCutoff => (float)ResearchTree_Patches.DetailedModeZoomLevelCutoffInfo.GetValue(instance);
        public static float Margin => (float)ResearchTree_Patches.MarginInfo.GetValue(instance);
        public static float QueueLabelSize => (float)ResearchTree_Patches.QueueLabelSizeInfo.GetValue(instance);
        public static Vector2 IconSize => (Vector2)ResearchTree_Patches.IconSizeInfo.GetValue(instance);
        public static Vector2 NodeMargins => (Vector2)ResearchTree_Patches.NodeMarginsInfo.GetValue(instance);
        public static Vector2 NodeSize => (Vector2)ResearchTree_Patches.NodeSizeInfo.GetValue(instance);
        public static float TopBarHeight => (float)ResearchTree_Patches.TopBarHeightInfo.GetValue(instance);
        public static Vector2 push => new Vector2(NodeSize.y * 0.618f, 0);
    }
}