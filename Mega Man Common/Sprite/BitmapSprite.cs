using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;

namespace MegaMan.Common
{
    public class BitmapSprite : Sprite
    {
        private List<Bitmap> paletteSwaps;

        public BitmapSprite(int width, int height) : base(width, height) { }
        public BitmapSprite(BitmapSprite copy) : base(copy) { }

        /// <summary>
        /// Draws the sprite on the specified Graphics surface at the specified position. Remember that the HotSpot is used as a position offset.
        /// </summary>
        /// <param name="graphics">The graphics surface on which to draw the sprite.</param>
        /// <param name="posX">The x-coordinate at which to draw the sprite.</param>
        /// <param name="posY">The y-coordinate at which to draw the sprite.</param>
        public void Draw(Graphics graphics, float positionX, float positionY)
        {
            Draw(graphics, positionX, positionY, (img) => { return img; });
        }

        public void Draw(Graphics graphics, float positionX, float positionY, Func<Image, Image> transform)
        {
            if (!Visible || frames.Count == 0) return;
            if (this.frames[currentFrame].Image == null)
            {
                graphics.FillRectangle(Brushes.Black, positionX, positionY, this.Width, this.Height);
                return;
            }

            bool horiz = this.HorizontalFlip;
            if (this.Reversed)
            {
                horiz = !horiz;
            }

            var sourceRect = this.frames[currentFrame].SheetLocation;

            float destX, destY, destH, destW;

            if (HorizontalFlip ^ this.Reversed)
            {
                destX = positionX + this.HotSpot.X;
                destW = -this.Width;
            }
            else
            {
                destX = positionX - this.HotSpot.X;
                destW = this.Width;
            }

            if (VerticalFlip)
            {
                destY = positionY + this.HotSpot.Y;
                destH = -this.Height;
            }
            else
            {
                destY = positionY - this.HotSpot.Y;
                destH = this.Height;
            }

            var destRect = new RectangleF(destX, destY, destW, destH);

            graphics.DrawImage(this.sheet, destRect, sourceRect, GraphicsUnit.Pixel);
        }

        protected override void VerifyPaletteSwaps()
        {
            if (PaletteName != null && this.palette == null)
            {
                this.palette = Palette.Get(PaletteName);
            }

            if (this.palette != null && this.paletteSwaps.Count == 0)
            {
                this.paletteSwaps = this.palette.GenerateSwappedBitmaps((Bitmap)this.sheet);
            }
        }

        public static Sprite FromXml(XElement element, Image tilesheet)
        {
            int width = element.GetInteger("width");
            int height = element.GetInteger("height");

            var sprite = new BitmapSprite(width, height);

            LoadXml(sprite, element, tilesheet);

            return sprite;
        }

        public static Sprite FromXml(XElement element, string basePath)
        {
            XAttribute tileattr = element.RequireAttribute("tilesheet");
            Sprite sprite;

            string sheetPath = Path.Combine(basePath, tileattr.Value);
            Image tilesheet = Image.FromFile(sheetPath);
            sprite = FromXml(element, tilesheet);
            sprite.SheetPath = FilePath.FromRelative(tileattr.Value, basePath);
            return sprite;
        }
    }
}
