using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HyperArcade.EditorTools
{
    /// <summary>
    /// Genera sprites y sonidos de marcador de posición (formas cartoon y "bips" sintetizados)
    /// para que el proyecto sea jugable sin arte. Se reemplazan después con el skin del cliente.
    /// </summary>
    public static class PlaceholderArt
    {
        // ================================================================ Lienzo

        public class Canvas
        {
            public readonly int W, H;
            public readonly Color32[] Px;

            public Canvas(int w, int h)
            {
                W = w; H = h;
                Px = new Color32[w * h];
            }

            public void Set(int x, int y, Color c)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return;
                Color dst = Px[y * W + x];
                float a = c.a + dst.a * (1f - c.a);
                Color o = a <= 0f ? Color.clear : new Color(
                    (c.r * c.a + dst.r * dst.a * (1f - c.a)) / a,
                    (c.g * c.a + dst.g * dst.a * (1f - c.a)) / a,
                    (c.b * c.a + dst.b * dst.a * (1f - c.a)) / a, a);
                Px[y * W + x] = o;
            }

            /// <summary>Rellena con antialias usando una función de distancia con signo (negativa = dentro).</summary>
            public void Fill(Func<float, float, float> sdf, Color c)
            {
                for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float d = sdf(x + 0.5f, y + 0.5f);
                    float cov = Mathf.Clamp01(0.5f - d);
                    if (cov > 0f) Set(x, y, new Color(c.r, c.g, c.b, c.a * cov));
                }
            }

            public void Circle(float cx, float cy, float r, Color c) =>
                Fill((x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, c);

            public void Ellipse(float cx, float cy, float rx, float ry, Color c) =>
                Fill((x, y) =>
                {
                    float nx = (x - cx) / rx, ny = (y - cy) / ry;
                    return (Mathf.Sqrt(nx * nx + ny * ny) - 1f) * Mathf.Min(rx, ry);
                }, c);

            public void Ring(float cx, float cy, float r, float thickness, Color c) =>
                Fill((x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - thickness * 0.5f, c);

            public void RoundRect(float x0, float y0, float x1, float y1, float radius, Color c) =>
                Fill((x, y) =>
                {
                    float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
                    float hx = (x1 - x0) * 0.5f - radius, hy = (y1 - y0) * 0.5f - radius;
                    float qx = Mathf.Abs(x - cx) - hx, qy = Mathf.Abs(y - cy) - hy;
                    float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                    return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                }, c);

            public void Polygon(Vector2[] pts, Color c) =>
                Fill((x, y) =>
                {
                    float d = float.MaxValue;
                    bool inside = false;
                    for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                    {
                        Vector2 a = pts[j], b = pts[i], p = new Vector2(x, y);
                        Vector2 e = b - a, w = p - a;
                        float t = Mathf.Clamp01(Vector2.Dot(w, e) / e.sqrMagnitude);
                        d = Mathf.Min(d, (w - e * t).magnitude);
                        if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                    }
                    return inside ? -d : d;
                }, c);

            public void VerticalGradient(Color bottom, Color top)
            {
                for (int y = 0; y < H; y++)
                {
                    Color c = Color.Lerp(bottom, top, (float)y / (H - 1));
                    for (int x = 0; x < W; x++) Px[y * W + x] = c;
                }
            }

            public void Hills(float baseY, float amplitude, float frequency, float phase, Color c) =>
                Fill((x, y) => y - (baseY + Mathf.Sin(x / W * Mathf.PI * 2f * frequency + phase) * amplitude
                                         + Mathf.Sin(x / W * Mathf.PI * 2f * frequency * 2.3f + phase * 1.7f) * amplitude * 0.35f), c);
        }

        public static Vector2[] Star(float cx, float cy, float outer, float inner)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? outer : inner;
                pts[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            return pts;
        }

        // ================================================================ Guardado de sprites

        public enum Pivot { Center, BottomCenter }

        public static Sprite SaveSprite(Canvas canvas, string path, int pixelsPerUnit, Pivot pivot = Pivot.Center,
            Vector4 border = default, bool fullRect = false)
        {
            var tex = new Texture2D(canvas.W, canvas.H, TextureFormat.RGBA32, false);
            tex.SetPixels32(canvas.Px);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (border != default) importer.spriteBorder = border;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)(pivot == Pivot.BottomCenter ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            settings.spriteMeshType = fullRect || border != default ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ================================================================ Personajes / enemigos

        /// <summary>Personaje cartoon: cuerpo redondeado, ojos grandes, cinturón. "bob" desplaza para animar.</summary>
        public static Canvas Character(Color body, Color accent, int bob, bool angry = false, float widthScale = 1f)
        {
            var c = new Canvas(96, 128);
            float w = 30f * widthScale;
            float x0 = 48 - w, x1 = 48 + w;
            float top = 112 + bob, bottom = 6;
            c.Ellipse(48, 4, w * 0.9f, 4, new Color(0, 0, 0, 0.25f));                  // sombra
            c.RoundRect(x0 - 3, bottom - 3, x1 + 3, top + 3, 24, new Color(0.08f, 0.08f, 0.12f)); // contorno
            c.RoundRect(x0, bottom, x1, top, 22, body);
            c.RoundRect(x0, 40 + bob * 0.5f, x1, 52 + bob * 0.5f, 4, accent);           // cinturón
            float eyeY = 84 + bob;
            c.Circle(36, eyeY, 10, Color.white);
            c.Circle(62, eyeY, 10, Color.white);
            c.Circle(39, eyeY - 1, 5, new Color(0.1f, 0.1f, 0.15f));
            c.Circle(65, eyeY - 1, 5, new Color(0.1f, 0.1f, 0.15f));
            if (angry)
            {
                c.Polygon(new[] { new Vector2(24, eyeY + 16), new Vector2(46, eyeY + 6), new Vector2(46, eyeY + 11), new Vector2(26, eyeY + 20) }, new Color(0.1f, 0.1f, 0.15f));
                c.Polygon(new[] { new Vector2(72, eyeY + 16), new Vector2(50, eyeY + 6), new Vector2(50, eyeY + 11), new Vector2(70, eyeY + 20) }, new Color(0.1f, 0.1f, 0.15f));
            }
            else
            {
                c.RoundRect(26, top - 22, 70, top - 14, 4, accent);                    // cinta en la frente
            }
            return c;
        }

        public static Canvas Bat(int flap)
        {
            var c = new Canvas(112, 80);
            Color body = new Color(0.55f, 0.3f, 0.75f), wing = new Color(0.38f, 0.18f, 0.55f);
            float wy = flap == 0 ? 60 : 24;
            c.Polygon(new[] { new Vector2(56, 44), new Vector2(4, wy), new Vector2(26, 30), new Vector2(40, 36) }, wing);
            c.Polygon(new[] { new Vector2(56, 44), new Vector2(108, wy), new Vector2(86, 30), new Vector2(72, 36) }, wing);
            c.Circle(56, 40, 22, new Color(0.08f, 0.08f, 0.12f));
            c.Circle(56, 40, 19, body);
            c.Circle(48, 44, 6, Color.white); c.Circle(64, 44, 6, Color.white);
            c.Circle(49, 43, 3, Color.red); c.Circle(65, 43, 3, Color.red);
            return c;
        }

        public static Canvas Boss(int bob)
        {
            var c = Character(new Color(0.55f, 0.1f, 0.12f), new Color(1f, 0.8f, 0.1f), bob, true, 1.4f);
            Color horn = new Color(0.95f, 0.9f, 0.8f);
            c.Polygon(new[] { new Vector2(18, 100 + bob), new Vector2(8, 127), new Vector2(32, 108 + bob) }, horn);
            c.Polygon(new[] { new Vector2(78, 100 + bob), new Vector2(88, 127), new Vector2(64, 108 + bob) }, horn);
            return c;
        }

        public static Canvas Gunner(int bob)
        {
            var c = PlaceholderArt.Character(new Color(0.25f, 0.65f, 0.3f), new Color(0.15f, 0.3f, 0.15f), bob, true, 0.9f);
            c.RoundRect(56, 56 + bob, 94, 66 + bob, 3, new Color(0.2f, 0.2f, 0.25f)); // arma
            return c;
        }

        // ================================================================ Objetos

        public static Canvas Projectile(Color core, Color glow, int w = 48, int h = 24)
        {
            var c = new Canvas(w, h);
            c.Ellipse(w / 2f, h / 2f, w / 2f - 1, h / 2f - 1, new Color(glow.r, glow.g, glow.b, 0.45f));
            c.Ellipse(w / 2f + 4, h / 2f, w / 3f, h / 4f, core);
            return c;
        }

        public static Canvas Orb(Color core, Color glow, int size = 40)
        {
            var c = new Canvas(size, size);
            c.Circle(size / 2f, size / 2f, size / 2f - 1, new Color(glow.r, glow.g, glow.b, 0.45f));
            c.Circle(size / 2f, size / 2f, size / 3.2f, core);
            return c;
        }

        public static Canvas Coin(float squash = 1f)
        {
            var c = new Canvas(48, 48);
            c.Ellipse(24, 24, 22 * squash, 22, new Color(0.75f, 0.5f, 0.05f));
            c.Ellipse(24, 24, 18 * squash, 18, new Color(1f, 0.82f, 0.15f));
            c.Ellipse(24, 24, 9 * squash, 12, new Color(1f, 0.95f, 0.55f));
            return c;
        }

        public static Canvas Diamond()
        {
            var c = new Canvas(48, 48);
            c.Polygon(new[] { new Vector2(24, 46), new Vector2(46, 26), new Vector2(24, 2), new Vector2(2, 26) }, new Color(0.1f, 0.45f, 0.8f));
            c.Polygon(new[] { new Vector2(24, 42), new Vector2(41, 26), new Vector2(24, 7), new Vector2(7, 26) }, new Color(0.35f, 0.85f, 1f));
            c.Polygon(new[] { new Vector2(24, 42), new Vector2(32, 30), new Vector2(16, 30) }, new Color(0.85f, 0.97f, 1f));
            return c;
        }

        public static Canvas StarIcon(Color fill, Color outline)
        {
            var c = new Canvas(64, 64);
            c.Polygon(Star(32, 31, 31, 14), outline);
            c.Polygon(Star(32, 31, 25, 11), fill);
            return c;
        }

        public static Canvas ShieldBubble()
        {
            var c = new Canvas(160, 160);
            c.Circle(80, 80, 78, new Color(0.4f, 0.8f, 1f, 0.25f));
            c.Ring(80, 80, 74, 6, new Color(0.6f, 0.95f, 1f, 0.8f));
            return c;
        }

        public static Canvas Lock()
        {
            var c = new Canvas(64, 64);
            c.Ring(32, 38, 14, 7, new Color(0.75f, 0.75f, 0.8f));
            c.RoundRect(10, 4, 54, 38, 6, new Color(0.95f, 0.75f, 0.2f));
            c.Circle(32, 22, 5, new Color(0.3f, 0.2f, 0.05f));
            return c;
        }

        public static Canvas Icon(Color bg, Action<Canvas> drawSymbol)
        {
            var c = new Canvas(96, 96);
            c.Circle(48, 48, 46, new Color(0.08f, 0.08f, 0.12f));
            c.Circle(48, 48, 42, bg);
            drawSymbol(c);
            return c;
        }

        public static Canvas Panel(Color color, int size = 96, int radius = 28)
        {
            var c = new Canvas(size, size);
            c.RoundRect(0, 0, size, size, radius, color);
            return c;
        }

        public static Canvas Gradient(int w, int h, Color bottom, Color top)
        {
            var c = new Canvas(w, h);
            c.VerticalGradient(bottom, top);
            return c;
        }

        public static Canvas HillsLayer(int w, int h, float baseY, float amp, float freq, float phase, Color color)
        {
            var c = new Canvas(w, h);
            c.Hills(baseY, amp, freq, phase, color);
            return c;
        }

        public static Canvas GroundTile()
        {
            var c = new Canvas(64, 64);
            c.Fill((x, y) => -1f, new Color(0.55f, 0.35f, 0.2f));
            for (int i = 0; i < 6; i++) c.Circle(8 + i * 11, 14 + (i % 2) * 14, 3, new Color(0.45f, 0.28f, 0.15f));
            c.RoundRect(-4, 48, 68, 64, 4, new Color(0.35f, 0.75f, 0.3f));
            c.RoundRect(-4, 44, 68, 50, 2, new Color(0.25f, 0.55f, 0.22f));
            return c;
        }

        public static Canvas Logo()
        {
            var c = new Canvas(512, 256);
            c.RoundRect(8, 40, 504, 216, 60, new Color(0.08f, 0.08f, 0.12f));
            c.RoundRect(16, 48, 496, 208, 54, new Color(1f, 0.55f, 0.1f));
            c.Polygon(Star(90, 128, 60, 26), new Color(1f, 0.9f, 0.2f));
            c.Polygon(Star(422, 128, 60, 26), new Color(1f, 0.9f, 0.2f));
            return c;
        }

        // ================================================================ Audio sintetizado

        public static AudioClip SaveTone(string path, float duration, Func<float, float> frequencyOverTime,
            float volume = 0.5f, bool square = false, float noise = 0f)
        {
            const int rate = 22050;
            int n = Mathf.CeilToInt(rate * duration);
            var samples = new float[n];
            float phase = 0f;
            var rng = new System.Random(path.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                phase += frequencyOverTime(t) / rate;
                float s = square ? (Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f) : Mathf.Sin(phase * Mathf.PI * 2f);
                if (noise > 0f) s = Mathf.Lerp(s, (float)(rng.NextDouble() * 2 - 1), noise);
                float env = Mathf.Min(1f, t * 40f) * (1f - t) * (1f - t);
                samples[i] = s * env * volume;
            }
            WriteWav(path, samples, rate);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        /// <summary>Melodía simple (lista de notas en Hz) en bucle, para música de prueba.</summary>
        public static AudioClip SaveMelody(string path, float[] notes, float noteDuration, float volume = 0.18f)
        {
            const int rate = 22050;
            int perNote = Mathf.CeilToInt(rate * noteDuration);
            var samples = new float[perNote * notes.Length];
            for (int k = 0; k < notes.Length; k++)
            {
                float f = notes[k];
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / perNote;
                    float env = Mathf.Min(1f, t * 30f) * Mathf.Lerp(1f, 0.3f, t);
                    float s = f <= 0f ? 0f : (Mathf.Sin(2f * Mathf.PI * f * i / rate) * 0.7f
                                               + (Mathf.Repeat(f * 0.5f * i / rate, 1f) < 0.5f ? 0.3f : -0.3f));
                    samples[k * perNote + i] = s * env * volume;
                }
            }
            WriteWav(path, samples, rate);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void WriteWav(string path, float[] samples, int rate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int dataSize = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + dataSize);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(dataSize);
                foreach (var s in samples) w.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
            }
        }
    }
}
