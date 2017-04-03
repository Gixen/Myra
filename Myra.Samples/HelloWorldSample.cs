﻿using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.Text;
using Myra.Utility;

namespace Myra.Samples
{
	public class HelloWorldSample : Game
	{
		private readonly GraphicsDeviceManager graphics;
		private SpriteBatch _batch;
		private BitmapFont _font;

		public HelloWorldSample()
		{
			graphics = new GraphicsDeviceManager(this);
			IsMouseVisible = true;
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			MyraEnvironment.Game = this;

			_batch = new SpriteBatch(GraphicsDevice);

			using (var stream = File.OpenRead("Assets/mistral_0.png"))
			{
				var texture = GraphicsExtension.PremultipliedTextureFromStream(stream);
				var region = new TextureRegion(texture, new Rectangle(0, 0, texture.Width, texture.Height));

				var fontData = File.ReadAllText("Assets/mistral.fnt");
				// _font = BitmapFont.CreateFromFNT(fontData, region);
			}
		}

		protected override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			if (graphics.PreferredBackBufferWidth != Window.ClientBounds.Width ||
				graphics.PreferredBackBufferHeight != Window.ClientBounds.Height)
			{
				graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
				graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
				graphics.ApplyChanges();
			}

			var device = GraphicsDevice;
			device.Clear(Color.Black);

			_batch.Begin();

			_font.Draw(_batch, "Hello, World!", Point.Zero, Color.White);

			_batch.End();
		}
	}
}