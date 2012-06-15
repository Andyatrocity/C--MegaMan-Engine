using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using XnaRectangle = Microsoft.Xna.Framework.Rectangle;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MegaMan.Common
{
    public class XnaSprite : Sprite
    {
        private GraphicsDevice _device;

        private Texture2D _texture;
        private List<Texture2D> _paletteSwaps;

        public XnaSprite(int width, int height)
            : base(width, height)
        {
            _paletteSwaps = new List<Texture2D>();
        }

        public XnaSprite(XnaSprite copy) : base(copy)
        {
            this._texture = copy._texture;
            this._paletteSwaps = copy._paletteSwaps;
        }

        public void SetTexture(GraphicsDevice device, string sheetPath)
        {
            _device = device;

            StreamReader sr = new StreamReader(sheetPath);
            this._texture = Texture2D.FromStream(device, sr.BaseStream);

            if (this.sheet == null)
            {
                this.sheet = Image.FromFile(sheetPath);
            }

            VerifyPaletteSwaps();
        }

        protected override void VerifyPaletteSwaps()
        {
            if (PaletteName != null && this.palette == null)
            {
                this.palette = Palette.Get(PaletteName);
            }

            if (this.palette != null && this._paletteSwaps.Count == 0)
            {
                this._paletteSwaps = this.palette.GenerateSwappedTextures((Bitmap)this.sheet, _device);
            }
        }

        public void Draw(SpriteBatch batch, XnaColor color, float positionX, float positionY)
        {
            if (!Visible || frames.Count == 0 || batch == null || this._texture == null) return;

            SpriteEffects effect = SpriteEffects.None;
            if (HorizontalFlip ^ this.Reversed) effect = SpriteEffects.FlipHorizontally;
            if (VerticalFlip) effect |= SpriteEffects.FlipVertically;

            int hx = (HorizontalFlip ^ this.Reversed) ? this.Width - this.HotSpot.X : this.HotSpot.X;
            int hy = VerticalFlip ? this.Height - this.HotSpot.Y : this.HotSpot.Y;

            // check palette swap
            var drawTexture = this._texture;
            VerifyPaletteSwaps();
            if (this.palette != null && this._paletteSwaps.Count > this.palette.CurrentIndex)
            {
                drawTexture = this._paletteSwaps[this.palette.CurrentIndex];
            }

            batch.Draw(drawTexture,
                new XnaRectangle((int)(positionX),
                    (int)(positionY), this.Width, this.Height),
                new XnaRectangle(this[currentFrame].SheetLocation.X, this[currentFrame].SheetLocation.Y, this[currentFrame].SheetLocation.Width, this[currentFrame].SheetLocation.Height),
                color, 0,
                new Vector2(hx, hy), effect, 0);
        }
    }
}
