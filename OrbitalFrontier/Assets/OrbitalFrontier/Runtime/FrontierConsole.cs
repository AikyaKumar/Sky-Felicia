using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitalFrontier.Runtime
{
    sealed class FrontierConsole : IDisposable
    {
        public event Action<string> HelpRequested;
        public static readonly Color Ink = new Color(.944f, .93f, .974f);
        public static readonly Color Muted = new Color(.657f, .681f, .754f);
        public static readonly Color Subtle = new Color(.49f, .52f, .62f);
        public static readonly Color Accent = new Color(.773f, .713f, 1);
        public static readonly Color Mint = new Color(.54f, .86f, .77f);
        public static readonly Color Amber = new Color(.97f, .74f, .49f);
        public static readonly Color Border = new Color(.15f, .165f, .225f);
        public static readonly Color Panel = new Color(.061f, .069f, .102f);
        public static readonly Color Bg = new Color(.033f, .037f, .067f);
        public static readonly Color ButtonBg = new Color(.098f, .104f, .157f);
        public static readonly Color Selected = new Color(.17f, .142f, .245f);
        readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();
        Font regular, bold, mono;
        GUIStyle input;
        Texture2D skinTexture;
        string tooltip = "";

        public void ResetTooltip() { tooltip = ""; }
        public string Tooltip => tooltip;
        public void Hint(Rect rect, string value) { if (rect.Contains(Event.current.mousePosition)) tooltip = value; }

        void Prepare()
        {
            if (regular) return;
            regular = Resources.Load<Font>("Frontier/Barlow-Regular");
            bold = Resources.Load<Font>("Frontier/Barlow-Bold");
            mono = Resources.Load<Font>("Frontier/IBMPlexMono-Regular");
            if (!regular) regular = GUI.skin.font;
            if (!bold) bold = regular;
            if (!mono) mono = regular;
        }

        public void Fill(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public void Card(Rect rect)
        {
            Fill(rect, Border);
            Fill(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), Panel);
        }

        GUIStyle TextStyle(int size, bool heavy, bool fixedWidth, TextAnchor alignment, bool wrap, Color color)
        {
            Prepare();
            string key = size + "/" + heavy + "/" + fixedWidth + "/" + (int)alignment + "/" + wrap + "/" + (Color32)color;
            if (styles.TryGetValue(key, out var style)) return style;
            style = new GUIStyle(GUIStyle.none)
            {
                font = fixedWidth ? mono : heavy ? bold : regular,
                fontSize = size,
                alignment = alignment,
                wordWrap = wrap,
                clipping = TextClipping.Clip,
                richText = false
            };
            style.normal.textColor = color;
            styles.Add(key, style);
            return style;
        }

        public void Text(Rect rect, string value, int size = 15, Color? color = null, bool heavy = false,
            bool fixedWidth = false, TextAnchor align = TextAnchor.MiddleLeft, bool wrap = false)
        {
            GUI.Label(rect, value, TextStyle(size, heavy, fixedWidth, align, wrap, color ?? Ink));
        }

        public bool Button(Rect rect, string text, bool main = false, bool selected = false, bool enabled = true, int size = 14)
        {
            bool hovered = rect.Contains(Event.current.mousePosition);
            bool old = GUI.enabled;
            GUI.enabled = old && enabled;
            Color edge = main ? Accent : selected ? new Color(.43f,.36f,.63f) : hovered ? Subtle : Border;
            Color fill = main ? (hovered ? new Color(.89f,.84f,1) : Accent) : selected ? Selected : hovered ?
                new Color(.14f,.15f,.23f) : ButtonBg;
            if (!enabled || !old)
            {
                fill = Color.Lerp(fill, Bg, .56f);
                edge = Color.Lerp(edge, Bg, .55f);
            }
            Fill(rect, edge);
            Fill(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), fill);
            var style = TextStyle(size, true, false, TextAnchor.MiddleCenter, false,
                enabled && old ? main ? Bg : selected ? Accent : Ink : Subtle);
            var hit = GUI.Button(rect, text, style);
            GUI.enabled = old;
            return hit;
        }

        public void Help(Rect rect, string key)
        {
            if (string.IsNullOrEmpty(key) || !ExpeditionGlossary.TryGet(key, out _)) return;
            if (Button(rect, "?", size: 10)) HelpRequested?.Invoke(key);
        }

        public string Field(Rect rect, string name, string value)
        {
            Prepare();
            if (input == null)
            {
                skinTexture = new Texture2D(1, 1) {name = "Console input fill", hideFlags = HideFlags.DontSave};
                skinTexture.SetPixel(0, 0, ButtonBg);
                skinTexture.Apply();
                input = new GUIStyle(GUI.skin.textField)
                {
                    font = mono, fontSize = 13, alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(6, 6, 2, 2)
                };
                input.normal.background = input.hover.background = input.focused.background = skinTexture;
                input.normal.textColor = input.hover.textColor = input.focused.textColor = Ink;
            }
            Fill(rect, Border);
            GUI.SetNextControlName(name);
            return GUI.TextField(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), value, 52, input);
        }

        public void Title(Rect rect, string title, string meta = "")
        {
            string key = ExpeditionGlossary.KeyForTitle(title);
            Text(new Rect(rect.x + 11, rect.y, rect.width - (string.IsNullOrEmpty(key) ? 22 : 42), 43), title, 17, Ink, true);
            if (!string.IsNullOrEmpty(key)) Help(new Rect(rect.x + 13 + Math.Min(rect.width - 45, title.Length * 9.2f), rect.y + 13, 17, 17), key);
            Text(new Rect(rect.x + 9, rect.y, rect.width - 19, 43), meta, 10, Subtle, false, true, TextAnchor.MiddleRight);
            Fill(new Rect(rect.x + 1, rect.y + 43, rect.width - 2, 1), Border);
        }

        public void Metric(Rect rect, string title, string value, string unit = "")
        {
            Fill(rect, Border);
            Fill(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), new Color(.073f,.081f,.122f));
            string key = ExpeditionGlossary.KeyForLabel(title);
            Text(new Rect(rect.x + 9, rect.y + 4, rect.width - (string.IsNullOrEmpty(key) ? 16 : 33), 14), title, 9, Muted, false, true);
            if (!string.IsNullOrEmpty(key)) Help(new Rect(rect.xMax - 21, rect.y + 3, 15, 15), key);
            Text(new Rect(rect.x + 8, rect.y + 19, rect.width - 15, rect.height - 21), value + (unit.Length == 0 ? "" : " " + unit),
                value.Length + unit.Length > 12 ? 15 : 20, Ink, true);
        }

        public void Rule(float x, float y, float w) { Fill(new Rect(x, y, w, 1), Border); }

        public void Meter(Rect rect, double value)
        {
            Fill(rect, Border);
            Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01((float)value), rect.height), Mint);
        }

        public void Reading(float x, float y, float w, string label, string value, Color? color = null)
        {
            string key = ExpeditionGlossary.KeyForLabel(label);
            float labelWidth = w * .55f;
            Text(new Rect(x, y, labelWidth - (string.IsNullOrEmpty(key) ? 0 : 20), 23), label, 13, Muted);
            if (!string.IsNullOrEmpty(key)) Help(new Rect(x + labelWidth - 18, y + 3, 15, 15), key);
            Text(new Rect(x + w * .43f, y, w * .57f, 23), value, 11, color ?? Ink, false, true, TextAnchor.MiddleRight);
            Rule(x, y + 22, w);
        }

        public void Dispose() { if (skinTexture) UnityEngine.Object.Destroy(skinTexture); }
    }
}
