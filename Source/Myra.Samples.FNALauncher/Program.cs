﻿using System;
using Microsoft.Xna.Framework;
using Myra.Samples.WinForms;

namespace Myra.Samples.FNALauncher
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Game sample;
			using (var form = new SampleForm())
			{
				foreach (var t in Samples2D.AllSampleTypes)
				{
					form.AddSampleType(t);
				}

				form.Launcher += sampleType =>
				{
					var result = Activator.CreateInstance(sampleType);
					using (sample = (Game)result)
					{
						sample.Window.AllowUserResizing = true;
						sample.Run();
					}
				};

				form.ShowDialog();
			}
		}
	}
}
