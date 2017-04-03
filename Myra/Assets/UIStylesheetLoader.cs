﻿using System;
using System.Collections.Generic;
using Myra.Graphics2D;
using Myra.Graphics2D.Text;
using Myra.Graphics2D.UI.Styles;
using Myra.Utility;
using Newtonsoft.Json.Linq;

namespace Myra.Assets
{
	public class UIStylesheetLoader : IAssetLoader<Stylesheet>
	{
		private const string FontsName = "fonts";
		private const string SpritesheetName = "spriteSheet";

		public Stylesheet Load(AssetManager assetManager, string path)
		{
			var text = assetManager.ReadAsText(path);

			var root = JObject.Parse(text);
			var fontsMap = new Dictionary<string, BitmapFont>();

			JObject fonts;
			if (root.GetStyle(FontsName, out fonts))
			{
				foreach (var props in fonts.Properties())
				{
					// Parse it
					var stringValue = props.Value.ToString();
					var font = assetManager.Load<BitmapFont>(stringValue);
					fontsMap.Add(props.Name, font);
				}
			}

			string spriteSheetName;
			if (!root.GetStyle(SpritesheetName, out spriteSheetName))
			{
				throw new Exception("UI Stylesheet lacks sprite sheet.");
			}

			var spriteSheet = assetManager.Load<SpriteSheet>(spriteSheetName);

			return Stylesheet.CreateFromSource(text, s =>
			{
				BitmapFont bf;
				if (!fontsMap.TryGetValue(s, out bf))
				{
					throw new Exception(string.Format("Unable to resolve font '{0}'", s));
				}

				return bf;
			}, spriteSheet);
		}
	}
}
