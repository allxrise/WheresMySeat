using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace WheresMySeat
{
    public static class ProceduralSprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static TMP_FontAsset _cachedGameFont;
        private static bool _triedLoadFont;

        // Restyled APK Theme Colors
        public static readonly Color DarkOutlineColor = new Color(0.16f, 0.09f, 0.21f, 1f); // Deep plum-espresso (#291736)
        public static readonly Color WarmBrownText = new Color(0.22f, 0.12f, 0.06f, 1f);    // Warm dark roast (#381F0F)
        public static readonly Color CreamBgColor = new Color(1f, 0.98f, 0.92f, 1f);       // Warm ivory (#FFF9EB)

        public static TMP_FontAsset GetGameFont()
        {
            if (_cachedGameFont != null) return _cachedGameFont;
            if (_triedLoadFont) return TMP_Settings.defaultFontAsset;
            _triedLoadFont = true;

            Font ttf = Resources.Load<Font>("Fonts/Mali-Regular");
            if (ttf == null) ttf = Resources.Load<Font>("Fonts/Jua-Regular");
            if (ttf == null) ttf = Resources.Load<Font>("Fonts/Roboto-Bold");

            if (ttf != null)
            {
                _cachedGameFont = TMP_FontAsset.CreateFontAsset(ttf);
                if (_cachedGameFont != null)
                {
                    _cachedGameFont.name = ttf.name + "_TMP";
                    return _cachedGameFont;
                }
            }
            return TMP_Settings.defaultFontAsset;
        }

        public static void ApplyGameFont(TMP_Text tmp)
        {
            if (tmp == null) return;
            var f = GetGameFont();
            if (f != null) tmp.font = f;
        }

        /// <summary>
        /// Loads a restyled sprite from Assets/Resources/APKUI/{relativePath}.png
        /// </summary>
        public static Sprite LoadAPKSprite(string relativePath, float pixelsPerUnit = 100f, Vector4? border = null, Vector2? pivot = null)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            Vector4 b = border ?? Vector4.zero;
            Vector2 p = pivot ?? new Vector2(0.5f, 0.5f);
            string key = $"apk_{relativePath}_{pixelsPerUnit}_{b}_{p}";
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            string fullPath = "APKUI/" + relativePath;
            // First try direct Sprite load (if Unity imported it as Sprite)
            if (b == Vector4.zero && p == new Vector2(0.5f, 0.5f) && Mathf.Approximately(pixelsPerUnit, 100f))
            {
                Sprite direct = Resources.Load<Sprite>(fullPath);
                if (direct != null)
                {
                    Cache[key] = direct;
                    return direct;
                }
            }

            Texture2D tex = Resources.Load<Texture2D>(fullPath);
            if (tex == null)
            {
                Sprite sp = Resources.Load<Sprite>(fullPath);
                if (sp != null) tex = sp.texture;
            }
            if (tex == null)
            {
                return null;
            }

            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Sprite created = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                p,
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                b
            );
            created.name = relativePath;
            Cache[key] = created;
            return created;
        }

        public static Sprite GetRoundedRect(int w, int h, float radius, float borderThickness = 0f)
        {
            string key = $"rrect_{w}_{h}_{radius}_{borderThickness}";
            if (Cache.TryGetValue(key, out Sprite s) && s != null) return s;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[w * h];
            float halfW = w * 0.5f;
            float halfH = h * 0.5f;
            float r = Mathf.Min(radius, Mathf.Min(halfW, halfH) - 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - halfW) - (halfW - r - 1f);
                    float py = Mathf.Abs(y + 0.5f - halfH) - (halfH - r - 1f);
                    float dist = Vector2.Max(new Vector2(px, py), Vector2.zero).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - r;

                    float alpha = Mathf.Clamp01(0.5f - dist);
                    if (borderThickness > 0.01f)
                    {
                        float innerAlpha = Mathf.Clamp01(0.5f - (dist + borderThickness));
                        float lum = Mathf.Lerp(1f, 0.90f, innerAlpha);
                        pixels[y * w + x] = new Color(lum, lum, lum, alpha);
                    }
                    else
                    {
                        pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            Cache[key] = s;
            return s;
        }

        public static Sprite GetCircle(int size, float ringThickness = 0f)
        {
            string key = $"circle_{size}_{ringThickness}";
            if (Cache.TryGetValue(key, out Sprite s) && s != null) return s;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            float center = size * 0.5f;
            float outerR = center - 1.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    float a = Mathf.Clamp01(outerR - d + 0.5f);
                    if (ringThickness > 0.01f)
                    {
                        float innerR = outerR - ringThickness;
                        float innerA = Mathf.Clamp01(d - innerR + 0.5f);
                        a *= innerA;
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            Cache[key] = s;
            return s;
        }

        public static Sprite GetBlobBody(int size = 128)
        {
            Sprite apkBlob = LoadAPKSprite("Blob/Blob_1_body1", 160f);
            if (apkBlob != null) return apkBlob;
            return GetCircle(size);
        }

        public static Sprite GetMouthSprite(PersonFaceAnimation face, int w = 64, int h = 36)
        {
            string lipName = "Blob/Normal_lip";
            if (face == PersonFaceAnimation.Face_happy || face == PersonFaceAnimation.Face_Sing_happy)
                lipName = "Blob/Happy_lip";
            else if (face == PersonFaceAnimation.Face_angry || face == PersonFaceAnimation.Face_sad)
                lipName = "Blob/Angry_lip";
            else if (face == PersonFaceAnimation.Face_drag || face == PersonFaceAnimation.Face_wow)
                lipName = "Blob/Wow_lip";

            Sprite apkLip = LoadAPKSprite(lipName, 160f);
            if (apkLip != null) return apkLip;
            return GetCircle(32);
        }
    }
}
