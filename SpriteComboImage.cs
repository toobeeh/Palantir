using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Palantir
{
    public static class SpriteComboImage
    {
        public static string GenerateImage(string[] spriteSources, string savePath)
        {
            savePath = savePath + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() + ".png";
            string svgst = @"<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" viewBox = ""0 0 80 80"" >";
            foreach (string sprite in spriteSources)
            {
                byte[] imageArray = System.IO.File.ReadAllBytes(sprite);
                string base64ImageRepresentation = Convert.ToBase64String(imageArray);
                svgst += @"<image width=""80"" height=""80"" xlink:href=""data:image/gif;base64," + base64ImageRepresentation + @"""/>";
            }
            svgst += "</svg>";

            var svg = new SKSvg(new SKSize(80, 80));
            System.IO.Stream svgstream = new System.IO.MemoryStream(System.Text.Encoding.ASCII.GetBytes(svgst));
            svg.Load(svgstream);
            var bitmap = new SKBitmap((int)svg.CanvasSize.Width, (int)svg.CanvasSize.Height);
            var canvas = new SKCanvas(bitmap);
            canvas.DrawPicture(svg.Picture);
            
            canvas.Flush();
            canvas.Save();

            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 80))
            {
                // save the data to a stream
                using (var stream = System.IO.File.OpenWrite(savePath))
                {
                    data.SaveTo(stream);
                }
            }
            return savePath;
        }

        public static string SVGtoPNG(string svgst, string savePath)
        {
            var svg = new SKSvg(new SKSize(80, 80));
            System.IO.Stream svgstream = new System.IO.MemoryStream(System.Text.Encoding.ASCII.GetBytes(svgst));
            svg.Load(svgstream);
            var bitmap = new SKBitmap((int)svg.CanvasSize.Width, (int)svg.CanvasSize.Height);
            var canvas = new SKCanvas(bitmap);
            canvas.DrawPicture(svg.Picture);

            canvas.Flush();
            canvas.Save();

            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 80))
            {
                // save the data to a stream
                using (var stream = System.IO.File.OpenWrite(savePath))
                {
                    data.SaveTo(stream);
                }
            }
            return savePath;
        }

        public static string[] GetSpriteSources(int[] sprites)
        {
            List<string> sources = new List<string>();
            foreach (int id in sprites)
            {
                Sprite spt;
                try
                {
                    spt = BubbleWallet.GetSpriteByID(id);
                }
                catch { continue; }
                if (spt.URL.Contains("https://tobeh.host/"))
                {
                    sources.Add(spt.URL.Replace("https://tobeh.host/", "/home/pi/Webroot/"));
                }
                else
                {
                    // download sprite
                    System.Net.WebClient client = new System.Net.WebClient();
                    string path = "/home/pi/Webroot/files/combos/" + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() + ".gif";
                    client.DownloadFile(spt.URL,path);
                    sources.Add(path);
                }
            }
            return sources.ToArray();
        }
    }
}
