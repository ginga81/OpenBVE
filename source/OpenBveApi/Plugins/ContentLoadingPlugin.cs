﻿namespace OpenBveApi
{
	/// <summary>Represents a plugin for loading content.</summary>
	public class ContentLoadingPlugin
	{
		/// <summary>The plugin file.</summary>
		public readonly string File;

		/// <summary>The plugin title.</summary>
		public readonly string Title;

		/// <summary>The interface to load textures as exposed by the plugin, or a null reference.</summary>
		public Textures.TextureInterface Texture;

		/// <summary>The interface to load sounds as exposed by the plugin, or a null reference.</summary>
		public Sounds.SoundInterface Sound;

		/// <summary>The interface to load objects as exposed by the plugin, or a null reference.</summary>
		public Objects.ObjectInterface Object;

		/// <summary>Creates a new instance of this class.</summary>
		/// <param name="file">The plugin file.</param>
		public ContentLoadingPlugin(string file)
		{
			this.File = file;
			this.Title = System.IO.Path.GetFileName(file);
			this.Texture = null;
			this.Sound = null;
		}

		/// <summary>Loads all interfaces this plugin supports.</summary>
		public void Load(Hosts.HostInterface Host, FileSystem.FileSystem FileSystem, BaseOptions Options)
		{
			if (this.Texture != null)
			{
				this.Texture.Load(Host);
			}

			if (this.Sound != null)
			{
				this.Sound.Load(Host);
			}

			if (this.Object != null)
			{
				this.Object.Load(Host, FileSystem);
				this.Object.SetObjectParser(Options.CurrentXParser);
				this.Object.SetObjectParser(Options.CurrentObjParser);
			}
		}

		/// <summary>Unloads all interfaces this plugin supports.</summary>
		public void Unload()
		{
			if (this.Texture != null)
			{
				this.Texture.Unload();
			}

			if (this.Sound != null)
			{
				this.Sound.Unload();
			}

			if (this.Object != null)
			{
				this.Object.Unload();
			}
		}
	}
}
