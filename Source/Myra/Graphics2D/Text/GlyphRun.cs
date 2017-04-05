﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;

namespace Myra.Graphics2D.Text
{
	public class GlyphRun
	{
		private int _posX;
		private readonly List<GlyphRender> _glyphRenders = new List<GlyphRender>();
		private string _text;

		public string Text
		{
			get
			{
				return _text;
			}
		}

		public Point Size;

		public Rectangle? RenderedBounds { get; private set; }

		public int Count
		{
			get { return _glyphRenders.Count; }
		}

		public GlyphRender this[int index]
		{
			get { return _glyphRenders[index]; }
		}

		public List<GlyphRender> GlyphRenders
		{
			get { return _glyphRenders; }
		}

		public GlyphRun(int initialLineHeight)
		{
			Size.Y = initialLineHeight;
		}

		public void Clear()
		{
			_glyphRenders.Clear();
			Size = Point.Zero;
			_posX = 0;
			_text = string.Empty;
		}

		public void Append(BitmapFont font, CharInfo ci, Color? color)
		{
			var glyph = font.GetCharacterRegion(ci.Value);

			// Add kerning
			if (Count > 0)
			{
				var lastRender = _glyphRenders[Count - 1];

				if (lastRender.Glyph != null)
				{
					// _posX += lastRender.Glyph.GetKerning(ci.Value);
				}
			}

			GlyphRender render;
			if (glyph != null)
			{
				var dest = new Point(_posX, 0);
				render = new GlyphRender(font, ci, this, glyph, color, dest);

				_posX += glyph.XAdvance;

				Size.X = _posX;

				var height = glyph.YOffset + glyph.Height;
				if (height > Size.Y)
				{
					Size.Y = height;
				}
			}
			else
			{
				render = new GlyphRender(font, ci, this, null, null, Point.Zero);
			}

			_glyphRenders.Add(render);


			_text += ci.Value;
		}

		public void Append(BitmapFont font, IEnumerable<CharInfo> charInfos, Color? color)
		{
			foreach (var ci in charInfos)
			{
				Append(font, ci, color);
			}
		}

		public void ResetDraw()
		{
			RenderedBounds = null;
		}

		public void Draw(SpriteBatch batch, Point pos, int totalWidth, Color color, bool drawPartialLastSymbol = false)
		{
			var right = pos.X + totalWidth;
			var x = pos.X;
			foreach (var gr in _glyphRenders)
			{
				x = gr.Location.X + pos.X;

				if (x > right)
				{
					// Completely out
					break;
				}

				if (x + gr.XAdvance > right && !drawPartialLastSymbol)
				{
					break;
				}

				gr.Draw(batch, pos, color);
				x += gr.XAdvance;
			}
				
			RenderedBounds = new Rectangle(pos.X, pos.Y, x - pos.X, Size.Y);
//			if (BitmapFont.DrawFames)
			{
				batch.DrawRectangle(RenderedBounds.Value, Color.Blue);
			}
		}
	}
}