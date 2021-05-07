using System;
using System.Collections.Generic;
using System.Text;
using Svg;
using System.Drawing;
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

            //SvgDocument doc = Svg.SvgDocument.FromSvg<SvgDocument>(svgst);
            //doc.ShapeRendering = SvgShapeRendering.Auto;
            //Bitmap mp = doc.Draw();
            //savePath = savePath + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() + ".png";
            //mp.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            return savePath;
        }
    }
}
