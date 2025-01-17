using System;
using UnityEngine;
using Verse;

namespace HumanResources
{
    public static class Listing_StandardExtensions
    {
        public static float ButtonTextPadding = 5f;
        public static float AfterLabelMinGap = 10f;

        public static readonly Color SelectedButtonColor = new Color(.65f, 1f, .65f);

        public static void EnumSelector<T>(this Listing_Standard listing, ref T value, string label, string valueLabelPrefix, string valueTooltipPostfix = "_tooltip", string tooltip = null) where T : Enum
        {
            string[] names = Enum.GetNames(value.GetType());

            float lineHeight = Text.LineHeight;
            float labelWidth = Text.CalcSize(label).x + AfterLabelMinGap;

            var tempWidth = listing.ColumnWidth;

            float buttonsWidth = 0f;
            foreach (var name in names)
            {
                string text = (valueLabelPrefix + name).Translate();
                float width = Text.CalcSize(text).x + ButtonTextPadding * 2f;
                if (buttonsWidth < width)
                    buttonsWidth = width;
            }

            bool fitsOnLabelRow = (((buttonsWidth * names.Length) + labelWidth) < tempWidth);
            float buttonsRectWidth = fitsOnLabelRow ?
                listing.ColumnWidth - (labelWidth) :
                listing.ColumnWidth;

            int rowNum = 0;
            int columnNum = 0;
            int maxColumnNum = 0;
            foreach (var name in names)
            {
                if ((columnNum + 1) * buttonsWidth > buttonsRectWidth)
                {
                    columnNum = 0;
                    rowNum++;
                }
                float x = (columnNum * buttonsWidth);
                float y = rowNum * lineHeight;
                columnNum++;
                if (rowNum == 0 && maxColumnNum < columnNum)
                    maxColumnNum = columnNum;
            }
            rowNum++; //label row
            if (!fitsOnLabelRow)
                rowNum++;

            Rect wholeRect = listing.GetRect((float)rowNum * lineHeight);

            if (!tooltip.NullOrEmpty())
            {
                if (Mouse.IsOver(wholeRect))
                {
                    Widgets.DrawHighlight(wholeRect);
                }
                TooltipHandler.TipRegion(wholeRect, tooltip);
            }

            Rect labelRect = wholeRect.TopPartPixels(lineHeight).LeftPartPixels(labelWidth);
            GUI.color = Color.white;
            Widgets.Label(labelRect, label);

            Rect buttonsRect = fitsOnLabelRow ?
                wholeRect.RightPartPixels(buttonsRectWidth) :
                wholeRect.BottomPartPixels(wholeRect.height - lineHeight);

            buttonsWidth = buttonsRectWidth / (float)maxColumnNum;

            rowNum = 0;
            columnNum = 0;
            foreach (var name in names)
            {
                if ((columnNum + 1) * buttonsWidth > buttonsRectWidth)
                {
                    columnNum = 0;
                    rowNum++;
                }
                float x = (columnNum * buttonsWidth);
                float y = rowNum * lineHeight;
                columnNum++;
                string buttonText = (valueLabelPrefix + name).Translate();
                var enumValue = (T)Enum.Parse(value.GetType(), name);
                GUI.color = value.Equals(enumValue) ? SelectedButtonColor : Color.white;
                var buttonRect = new Rect(buttonsRect.x + x, buttonsRect.y + y, buttonsWidth, lineHeight);
                if (valueTooltipPostfix != null)
                    TooltipHandler.TipRegion(buttonRect, (valueLabelPrefix + name + valueTooltipPostfix).Translate());
                bool clicked = Widgets.ButtonText(buttonRect, buttonText);
                if (clicked)
                    value = enumValue;
            }

            listing.Gap(listing.verticalSpacing);
            GUI.color = Color.white;
        }
    }
}
