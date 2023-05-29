﻿using OpenTS2.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace OpenTS2.UI
{
    public class UITextElement : UIElement
    {
        public TextAnchor Alignment = TextAnchor.MiddleCenter;
        public Color32 ForeColor = Color.black;
        public override void ParseProperties(UIProperties properties)
        {
            base.ParseProperties(properties);
            if (properties.HasProperty("captionres"))
            {
                var captionRes = properties.GetStringSetProperty("captionres");
                Caption = captionRes.GetLocalizedString();
            }
            ForeColor = properties.GetColorProperty("forecolor");
            var align = properties.GetProperty("align");
            if (align != null)
            {
                switch(align)
                {
                    case "lefttop":
                        Alignment = TextAnchor.UpperLeft;
                        break;
                    case "centertop":
                        Alignment = TextAnchor.UpperCenter;
                        break;
                    case "righttop":
                        Alignment = TextAnchor.UpperRight;
                        break;
                    case "leftcenter":
                        Alignment = TextAnchor.MiddleLeft;
                        break;
                    case "center":
                        Alignment = TextAnchor.MiddleCenter;
                        break;
                    case "rightcenter":
                        Alignment = TextAnchor.MiddleRight;
                        break;
                    case "leftbottom":
                        Alignment = TextAnchor.LowerLeft;
                        break;
                    case "centerbottom":
                        Alignment = TextAnchor.LowerCenter;
                        break;
                    case "rightbottom":
                        Alignment = TextAnchor.LowerRight;
                        break;
                }
            }
        }
        public override UIComponent Instantiate(Transform parent)
        {
            var uiComponent = base.Instantiate(parent);
            var text = uiComponent.gameObject.AddComponent<Text>();
            text.color = ForeColor;
            text.text = Caption;
            text.font = AssetController.DefaultFont;
            text.fontSize = 14;
            text.alignment = Alignment;
            return uiComponent;
        }
    }
}