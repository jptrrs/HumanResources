using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace HumanResources
{
    [HarmonyPatch(typeof(GenText), "Truncate", new Type[] { typeof(string), typeof(float), typeof(Dictionary<string, string>) })]
    public static class GenText_Truncate
    {
        public static bool Prefix(string str, float width, ref string __result)
        {
			if (Text.CalcSize(str).x <= width)
			{
				__result = str;
				return false;
			}
			string value = str;
			do
			{
				value = value.Substring(0, value.Length - 1);
			}
			while (value.Length > 0 && Text.CalcSize(value + "...").x > width);
			value += "...";
			__result = value;
			return false;
        }
    }
}
