﻿using System;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Myra.Assets;
using Myra.Graphics2D.UI;
using Menu = Myra.Graphics2D.UI.Menu;

namespace Myra.Samples
{
	public class Notepad: Game
	{
		private readonly GraphicsDeviceManager graphics;

		private readonly AssetManager _assetManager = new AssetManager(new ResourceAssetResolver(typeof(CustomUIStylesheetSample).GetTypeInfo().Assembly, "Myra.Samples.Resources."));
		private string _filePath;
		private bool _dirty = true;
		private Desktop _host;
		private TextField _textField;

		public string FilePath
		{
			get
			{
				return _filePath;
			}

			set
			{
				if (value == _filePath)
				{
					return;
				}

				_filePath = value;

				UpdateTitle();
			}
		}

		public bool Dirty
		{
			get
			{
				return _dirty;
			}

			set
			{
				if (value == _dirty)
				{
					return;
				}

				_dirty = value;
				UpdateTitle();
			}
		}

		public Func<string> OpenFileHandler { get; set; }
		public Func<string> SaveFileHandler { get; set; }

		public Notepad()
		{
			graphics = new GraphicsDeviceManager(this);
			IsMouseVisible = true;
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			MyraEnvironment.Game = this;

			UpdateTitle();

			_host = new Desktop();

			// Load UI
			var ui = _assetManager.Load<Project>("notepad.ui");

			var mainMenu = (Menu) ui.Root.FindWidgetById("mainMenu");
			var newItem = mainMenu.FindMenuItemById("menuItemNew");
			newItem.Selected += NewItemOnDown;

			// File/Open
			var openItem = mainMenu.FindMenuItemById("menuItemOpen");
			openItem.Selected += OpenItemOnDown;

			// File/Save
			var saveItem = mainMenu.FindMenuItemById("menuItemSave");
			saveItem.Selected += SaveItemOnDown;

			// File/Save As...
			var saveAsItem = mainMenu.FindMenuItemById("menuItemSaveAs");
			saveAsItem.Selected += SaveAsItemOnDown;

			// File/Quit
			var quitItem = mainMenu.FindMenuItemById("menuItemQuit");
			quitItem.Selected += QuitItemOnDown;

			var aboutItem = mainMenu.FindMenuItemById("menuItemAbout");
			aboutItem.Selected += AboutItemOnDown;

			_textField = (TextField)ui.Root.FindWidgetById("textArea");

			_textField.Text =
				"Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum!";

			_textField.TextChanged += TextFieldOnTextChanged;

			_host.Widgets.Add(ui.Root);
		}

		private void UpdateTitle()
		{
			Window.Title = CalculateTitle();
		}

		private string CalculateTitle()
		{
			if (string.IsNullOrEmpty(_filePath))
			{
				return "Notepad";
			}

			if (!Dirty)
			{
				return _filePath;
			}

			return _filePath + " *";
		}

		private void TextFieldOnTextChanged(object sender, EventArgs eventArgs)
		{
			Dirty = true;
		}

		private void Save(bool setFileName)
		{
			if (string.IsNullOrEmpty(FilePath) || setFileName)
			{
				var h = SaveFileHandler;
				if (h == null)
				{
					return;
				}

				var f = h();
				if (string.IsNullOrEmpty(f))
				{
					return;
				}

				FilePath = f;
			}

			using (var writer = new StreamWriter(_filePath))
			{
				writer.Write(_textField.Text);
			}

			Dirty = false;
		}

		private void AboutItemOnDown(object sender, EventArgs eventArgs)
		{
			var messageBox = Dialog.CreateMessageBox("Notepad", "Myra Notepad Sample " + MyraEnvironment.Version);
			messageBox.ShowModal(_host);
		}

		private void SaveAsItemOnDown(object sender, EventArgs eventArgs)
		{
			Save(true);
		}

		private void SaveItemOnDown(object sender, EventArgs eventArgs)
		{
			Save(false);
		}

		private void OpenItemOnDown(object sender, EventArgs eventArgs)
		{
			var h = OpenFileHandler;
			if (h == null)
			{
				return;
			}

			var filePath = h();
			if (string.IsNullOrEmpty(filePath))
			{
				return;
			}

			using (var reader = new StreamReader(filePath))
			{
				_textField.Text = reader.ReadToEnd();
			}

			FilePath = _filePath;
			Dirty = false;
		}

		private void NewItemOnDown(object sender, EventArgs eventArgs)
		{
			FilePath = string.Empty;
			_textField.Text = string.Empty;
		}

		private void QuitItemOnDown(object sender, EventArgs genericEventArgs)
		{
			Exit();
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

			GraphicsDevice.Clear(Color.Black);

			_host.Bounds = new Rectangle(0, 0, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
			_host.Render();
		}
	}
}
