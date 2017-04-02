﻿using System.IO;
using Microsoft.Xna.Framework.Graphics;
using StbSharp;

namespace Myra.Assets
{
	internal class Texture2DLoader : IAssetLoader<Texture2D>
	{
		private static byte ApplyAlpha(byte color, byte alpha)
		{
			var fc = color/255.0f;
			var fa = alpha/255.0f;

			var fr = (int) (255.0f*fc*fa);

			if (fr < 0)
			{
				fr = 0;
			}

			if (fr > 255)
			{
				fr = 255;
			}

			return (byte) fr;
		}

		public Texture2D Load(AssetManager assetManager, Stream stream, object p)
		{
			var parameters = (Texture2DLoadingParameters) p;
			var reader = new ImageReader();

			var image = reader.Read(stream, parameters.RequiredComponents ?? Stb.STBI_default);

			var data = image.Data;
			if (parameters.PremultiplyAlpha)
			{
				// Premultiply alpha
				for (var i = 0; i < image.Width*image.Height; ++i)
				{
					var a = data[i*4 + 3];

					data[i*4] = ApplyAlpha(data[i*4], a);
					data[i*4 + 1] = ApplyAlpha(data[i*4 + 1], a);
					data[i*4 + 2] = ApplyAlpha(data[i*4 + 2], a);
				}
			}

			var texture = new Texture2D(MyraEnvironment.GraphicsDevice, image.Width, image.Height, false, SurfaceFormat.Color);
			texture.SetData(data);

			return texture;
		}
	}
}
