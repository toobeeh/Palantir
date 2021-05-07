using System;
using System.Collections.Generic;
using System.Text;
using Svg;
using System.Drawing;

namespace Palantir
{
    public static class SpriteComboImage
    {
        public static string GenerateImage(string[] spriteSources, string savePath)
        {
            string svgst = @"<svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" viewBox = ""0 0 80 80"" >";
            foreach (string sprite in spriteSources)
            {
                byte[] imageArray = System.IO.File.ReadAllBytes(sprite);
                string base64ImageRepresentation = Convert.ToBase64String(imageArray);
                svgst += @"<image width=""80"" height=""80"" xlink:href=""data:image/gif;base64," + base64ImageRepresentation + @"""/>";
            }
            svgst += "</svg>";
            Console.WriteLine(svgst);

            SvgDocument doc = Svg.SvgDocument.FromSvg<SvgDocument>(svgst);
            doc.ShapeRendering = SvgShapeRendering.Auto;
            Bitmap mp = doc.Draw();
            savePath = savePath + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() + ".png";
            mp.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            return savePath;
        }
    }
}
