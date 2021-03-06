using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using LibRender2;
using LibRender2.Objects;
using LibRender2.Viewports;
using OpenBveApi;
using OpenBveApi.Colors;
using OpenBveApi.Graphics;
using OpenBveApi.Hosts;
using OpenBveApi.Interface;
using OpenBveApi.Math;
using OpenBveApi.Objects;
using OpenBveApi.World;
using OpenTK.Graphics.OpenGL;

namespace OpenBve
{
	internal class NewRenderer : BaseRenderer
	{
		// options
		internal bool OptionCoordinateSystem = false;
		internal bool OptionInterface = true;

		// background color
		internal int BackgroundColor = 0;
		internal Color128 TextColor = Color128.White;
		internal const int MaxBackgroundColor = 4;

		private VertexArrayObject redAxisVAO;
		private VertexArrayObject greenAxisVAO;
		private VertexArrayObject blueAxisVAO;

		public override void Initialize(HostInterface CurrentHost, BaseOptions CurrentOptions)
		{
			base.Initialize(CurrentHost, CurrentOptions);

			if (!ForceLegacyOpenGL)
			{
				redAxisVAO = RegisterBox(Color128.Red);
				greenAxisVAO = RegisterBox(Color128.Green);
				blueAxisVAO = RegisterBox(Color128.Blue);
			}
		}

		internal string GetBackgroundColorName()
		{
			switch (BackgroundColor)
			{
				case 0: return "Light Gray";
				case 1: return "White";
				case 2: return "Black";
				case 3: return "Dark Gray";
				default: return "Custom";
			}
		}

		internal void ApplyBackgroundColor()
		{
			switch (BackgroundColor)
			{
				case 0:
					GL.ClearColor(0.67f, 0.67f, 0.67f, 1.0f);
					TextColor = Color128.White;
					break;
				case 1:
					GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
					TextColor = Color128.Black;
					break;
				case 2:
					GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
					TextColor = Color128.White;
					break;
				case 3:
					GL.ClearColor(0.33f, 0.33f, 0.33f, 1.0f);
					TextColor = Color128.White;
					break;
			}
		}

		internal void ApplyBackgroundColor(byte red, byte green, byte blue)
		{
			GL.ClearColor(red / 255.0f, green / 255.0f, blue / 255.0f, 1.0f);
		}

		internal void CreateObject(UnifiedObject Prototype, Vector3 Position, Transformation BaseTransformation, Transformation AuxTransformation, bool AccurateObjectDisposal, double StartingDistance, double EndingDistance, double BlockLength, double TrackPosition)
		{
			if (Prototype != null)
			{
				CreateObject(Prototype, Position, BaseTransformation, AuxTransformation, -1, AccurateObjectDisposal, StartingDistance, EndingDistance, BlockLength, TrackPosition, 1.0, false);
			}
		}

		internal void CreateObject(UnifiedObject Prototype, Vector3 Position, Transformation BaseTransformation, Transformation AuxTransformation, int SectionIndex, bool AccurateObjectDisposal, double StartingDistance, double EndingDistance, double BlockLength, double TrackPosition, double Brightness, bool DuplicateMaterials)
		{
			if (Prototype is StaticObject)
			{
				StaticObject s = (StaticObject)Prototype;
				CreateStaticObject(s, Position, BaseTransformation, AuxTransformation, AccurateObjectDisposal, 0.0, StartingDistance, EndingDistance, BlockLength, TrackPosition, Brightness);
			}
			else if (Prototype is AnimatedObjectCollection)
			{
				AnimatedObjectCollection a = (AnimatedObjectCollection)Prototype;
				a.CreateObject(Position, BaseTransformation, AuxTransformation, SectionIndex, AccurateObjectDisposal, StartingDistance, EndingDistance, BlockLength, TrackPosition, Brightness, DuplicateMaterials);
			}
		}

		internal int CreateStaticObject(UnifiedObject Prototype, Vector3 Position, Transformation BaseTransformation, Transformation AuxTransformation, bool AccurateObjectDisposal, double AccurateObjectDisposalZOffset, double StartingDistance, double EndingDistance, double BlockLength, double TrackPosition, double Brightness)
		{
			StaticObject obj = Prototype as StaticObject;

			if (obj == null)
			{
				Interface.AddMessage(MessageType.Error, false, "Attempted to use an animated object where only static objects are allowed.");
				return -1;
			}

			return base.CreateStaticObject(obj, Position, BaseTransformation, AuxTransformation, AccurateObjectDisposal, AccurateObjectDisposalZOffset, StartingDistance, EndingDistance, BlockLength, TrackPosition, Brightness);
		}

		private VertexArrayObject RegisterBox(Color128 Color)
		{
			LibRenderVertex[] vertexData = new LibRenderVertex[8];
			vertexData[0].Position = new Vector3f(1.0f, 1.0f, 1.0f);
			vertexData[1].Position = new Vector3f(1.0f, -1.0f, 1.0f);
			vertexData[2].Position = new Vector3f(-1.0f, -1.0f, 1.0f);
			vertexData[3].Position = new Vector3f(-1.0f, 1.0f, 1.0f);
			vertexData[4].Position = new Vector3f(1.0f, 1.0f, -1.0f);
			vertexData[5].Position = new Vector3f(1.0f, -1.0f, -1.0f);
			vertexData[6].Position = new Vector3f(-1.0f, -1.0f, -1.0f);
			vertexData[7].Position = new Vector3f(-1.0f, 1.0f, -1.0f);

			for (int i = 0; i < vertexData.Length; i++)
			{
				vertexData[i].Color = Color;
			}

			ushort[] indexData =
			{
				0, 1, 2, 3,
				0, 4, 5, 1,
				0, 3, 7, 4,
				6, 5, 4, 7,
				6, 7, 3, 2,
				6, 2, 1, 5
			};

			VertexArrayObject vao = new VertexArrayObject();
			vao.Bind();
			vao.SetVBO(new VertexBufferObject(vertexData, BufferUsageHint.StaticDraw));
			vao.SetIBO(new IndexBufferObjectU(indexData, BufferUsageHint.StaticDraw));
			vao.SetAttributes(DefaultShader.VertexLayout);
			vao.UnBind();

			return vao;
		}

		// render scene
		internal void RenderScene()
		{
			// initialize
			ResetOpenGlState();

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			UpdateViewport(ViewportChangeMode.ChangeToScenery);
			CurrentViewMatrix = Matrix4D.LookAt(Vector3.Zero, new Vector3(Camera.AbsoluteDirection.X, Camera.AbsoluteDirection.Y, -Camera.AbsoluteDirection.Z), new Vector3(Camera.AbsoluteUp.X, Camera.AbsoluteUp.Y, -Camera.AbsoluteUp.Z));
			if (!AvailableNewRenderer)
			{
				GL.Light(LightName.Light0, LightParameter.Position, new[] { (float)Lighting.OptionLightPosition.X, (float)Lighting.OptionLightPosition.Y, (float)-Lighting.OptionLightPosition.Z, 0.0f });
			}

			Lighting.OptionLightingResultingAmount = (Lighting.OptionAmbientColor.R + Lighting.OptionAmbientColor.G + Lighting.OptionAmbientColor.B) / 480.0f;

			if (Lighting.OptionLightingResultingAmount > 1.0f)
			{
				Lighting.OptionLightingResultingAmount = 1.0f;
			}

			OptionFog = false;

			if (OptionCoordinateSystem)
			{
				UnsetAlphaFunc();

				if (AvailableNewRenderer)
				{
					Cube.DrawRetained(redAxisVAO, Vector3.Zero, Vector3.Forward, Vector3.Down, Vector3.Right, new Vector3(100.0, 0.01, 0.01), Camera.AbsolutePosition, null);
					Cube.DrawRetained(greenAxisVAO, Vector3.Zero, Vector3.Forward, Vector3.Down, Vector3.Right, new Vector3(0.01, 100.0, 0.01), Camera.AbsolutePosition, null);
					Cube.DrawRetained(blueAxisVAO, Vector3.Zero, Vector3.Forward, Vector3.Down, Vector3.Right, new Vector3(0.01, 0.01, 100.0), Camera.AbsolutePosition, null);
				}
				else
				{
					GL.Color4(1.0, 0.0, 0.0, 0.2);
					Cube.DrawImmediate(Vector3.Zero, Vector3.Forward, Vector3.Down, Vector3.Right, new Vector3(100.0, 0.01, 0.01), Camera.AbsolutePosition, null);
					GL.Color4(0.0, 1.0, 0.0, 0.2);
					Cube.DrawImmediate(Vector3.Zero, Vector3.Forward, Vector3.Down, Vector3.Right, new Vector3(0.01, 100.0, 0.01), Camera.AbsolutePosition, null);
					GL.Color4(0.0, 0.0, 1.0, 0.2);
					Cube.DrawImmediate(Vector3.Zero, Vector3.Forward, Vector3.Down, Vector3.Right, new Vector3(0.01, 0.01, 100.0), Camera.AbsolutePosition, null);
				}
			}
			GL.Disable(EnableCap.DepthTest);
			// opaque face
			if (AvailableNewRenderer)
			{
				//Setup the shader for rendering the scene
				DefaultShader.Activate();
				if (OptionLighting)
				{
					DefaultShader.SetIsLight(true);
					TransformedLightPosition = new Vector3(Lighting.OptionLightPosition.X, Lighting.OptionLightPosition.Y, -Lighting.OptionLightPosition.Z);
					DefaultShader.SetLightPosition(TransformedLightPosition);
					DefaultShader.SetLightAmbient(Lighting.OptionAmbientColor);
					DefaultShader.SetLightDiffuse(Lighting.OptionDiffuseColor);
					DefaultShader.SetLightSpecular(Lighting.OptionSpecularColor);
					DefaultShader.SetLightModel(Lighting.LightModel);
				}
				DefaultShader.SetTexture(0);
				DefaultShader.SetCurrentProjectionMatrix(CurrentProjectionMatrix);
			}
			ResetOpenGlState();
			foreach (FaceState face in VisibleObjects.OpaqueFaces)
			{
				face.Draw();
			}

			// alpha face
			ResetOpenGlState();
			VisibleObjects.SortPolygonsInAlphaFaces();

			if (Interface.CurrentOptions.TransparencyMode == TransparencyMode.Performance)
			{
				SetBlendFunc();
				SetAlphaFunc(AlphaFunction.Greater, 0.0f);
				GL.DepthMask(false);

				foreach (FaceState face in VisibleObjects.AlphaFaces)
				{
					face.Draw();
				}
			}
			else
			{
				UnsetBlendFunc();
				SetAlphaFunc(AlphaFunction.Equal, 1.0f);
				GL.DepthMask(true);

				foreach (FaceState face in VisibleObjects.AlphaFaces)
				{
					if (face.Object.Prototype.Mesh.Materials[face.Face.Material].BlendMode == MeshMaterialBlendMode.Normal && face.Object.Prototype.Mesh.Materials[face.Face.Material].GlowAttenuationData == 0)
					{
						if (face.Object.Prototype.Mesh.Materials[face.Face.Material].Color.A == 255)
						{
							face.Draw();
						}
					}
				}

				SetBlendFunc();
				SetAlphaFunc(AlphaFunction.Less, 1.0f);
				GL.DepthMask(false);
				bool additive = false;

				foreach (FaceState face in VisibleObjects.AlphaFaces)
				{
					if (face.Object.Prototype.Mesh.Materials[face.Face.Material].BlendMode == MeshMaterialBlendMode.Additive)
					{
						if (!additive)
						{
							UnsetAlphaFunc();
							additive = true;
						}

						face.Draw();
					}
					else
					{
						if (additive)
						{
							SetAlphaFunc();
							additive = false;
						}

						face.Draw();
					}
				}
			}

			if (AvailableNewRenderer)
			{
				DefaultShader.Deactivate();
				lastVAO = -1;
			}

			// render overlays
			ResetOpenGlState();
			OptionLighting = false;
			UnsetAlphaFunc();
			GL.Disable(EnableCap.DepthTest);
			SetBlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha); //FIXME: Remove when text switches between two renderer types
			RenderOverlays();
			OptionLighting = true;
		}

		private void RenderOverlays()
		{
			//Initialize openGL
			SetBlendFunc();
			PushMatrix(MatrixMode.Projection);
			Matrix4D.CreateOrthographicOffCenter(0.0f, Screen.Width, Screen.Height, 0.0f, -1.0f, 1.0f, out CurrentProjectionMatrix);
			PushMatrix(MatrixMode.Modelview);
			CurrentViewMatrix = Matrix4D.Identity;

			CultureInfo culture = CultureInfo.InvariantCulture;

			if (OptionInterface)
			{
				string[][] keys;

				if (VisibleObjects.Objects.Count == 0 && ObjectManager.AnimatedWorldObjectsUsed == 0)
				{
					keys = new[] { new[] { "F7" }, new[] { "F8" }, new[] { "F10" } };
					Keys.Render(4, 4, 20, Fonts.SmallFont, keys);
					OpenGlString.Draw(Fonts.SmallFont, "Open one or more objects", new Point(32, 4), TextAlignment.TopLeft, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, "Display the options window", new Point(32, 24), TextAlignment.TopLeft, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, "Display the train settings window", new Point(32, 44), TextAlignment.TopLeft, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, $"v{Application.ProductVersion}", new Point(Screen.Width - 8, Screen.Height - 20), TextAlignment.TopLeft, TextColor);
				}
				else
				{
					OpenGlString.Draw(Fonts.SmallFont, $"Position: {Camera.AbsolutePosition.X.ToString("0.00", culture)}, {Camera.AbsolutePosition.Y.ToString("0.00", culture)}, {Camera.AbsolutePosition.Z.ToString("0.00", culture)}", new Point((int)(0.5 * Screen.Width - 88), 4), TextAlignment.TopLeft, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, $"Renderer: {(AvailableNewRenderer ? "New (GL 3.0)" : "Old (GL 1.2)")}", new Point((int)(0.5 * Screen.Width - 88), 24), TextAlignment.TopLeft, TextColor);
					keys = new[] { new[] { "F5" }, new[] { "F7" }, new[] { "del" }, new[] { "F8" }, new[] { "F10" } };
					Keys.Render(4, 4, 24, Fonts.SmallFont, keys);
					OpenGlString.Draw(Fonts.SmallFont, "Reload the currently open objects", new Point(32, 4), TextAlignment.TopLeft, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, "Open additional objects", new Point(32, 24), TextAlignment.TopLeft, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, "Clear currently open objects", new Point(32, 44), TextAlignment.TopLeft, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, "Display the options window", new Point(32, 64), TextAlignment.TopLeft, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, "Display the train settings window", new Point(32, 84), TextAlignment.TopLeft, TextColor);

					keys = new[] { new[] { "F" }, new[] { "N" }, new[] { "L" }, new[] { "G" }, new[] { "B" }, new[] { "I" }, new[] { "R" } };
					Keys.Render(Screen.Width - 20, 4, 16, Fonts.SmallFont, keys);
					OpenGlString.Draw(Fonts.SmallFont, $"WireFrame: {(OptionWireFrame ? "on" : "off")}", new Point(Screen.Width - 28, 4), TextAlignment.TopRight, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, $"Normals: {(OptionNormals ? "on" : "off")}", new Point(Screen.Width - 28, 24), TextAlignment.TopRight, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, $"Lighting: {(Program.LightingTarget == 0 ? "night" : "day")}", new Point(Screen.Width - 28, 44), TextAlignment.TopRight, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, $"Grid: {(OptionCoordinateSystem ? "on" : "off")}", new Point(Screen.Width - 28, 64), TextAlignment.TopRight, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, $"Background: {GetBackgroundColorName()}", new Point(Screen.Width - 28, 84), TextAlignment.TopRight, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, $"Hide interface:", new Point(Screen.Width - 28, 104), TextAlignment.TopRight, TextColor);
					OpenGlString.Draw(Fonts.SmallFont, $"Switch renderer type:", new Point(Screen.Width - 28, 124), TextAlignment.TopRight, TextColor);

					keys = new[] { new[] { null, "W", null }, new[] { "A", "S", "D" } };
					Keys.Render(4, Screen.Height - 40, 16, Fonts.SmallFont, keys);

					keys = new[] { new[] { null, "↑", null }, new[] { "←", "↓", "→" } };
					Keys.Render((int)(0.5 * Screen.Width - 28), Screen.Height - 40, 16, Fonts.SmallFont, keys);

					keys = new[] { new[] { null, "8", "9" }, new[] { "4", "5", "6" }, new[] { null, "2", "3" } };
					Keys.Render(Screen.Width - 60, Screen.Height - 60, 16, Fonts.SmallFont, keys);

					if (Interface.MessageCount == 1)
					{
						Keys.Render(4, 112, 20, Fonts.SmallFont, new[] { new[] { "F9" } });

						if (Interface.LogMessages[0].Type != MessageType.Information)
						{
							OpenGlString.Draw(Fonts.SmallFont, "Display the 1 error message recently generated.", new Point(32, 112), TextAlignment.TopLeft, new Color128(1.0f, 0.5f, 0.5f));
						}
						else
						{
							//If all of our messages are information, then print the message text in grey
							OpenGlString.Draw(Fonts.SmallFont, "Display the 1 message recently generated.", new Point(32, 112), TextAlignment.TopLeft, TextColor);
						}
					}
					else if (Interface.MessageCount > 1)
					{
						Keys.Render(4, 112, 20, Fonts.SmallFont, new[] { new[] { "F9" } });
						bool error = Interface.LogMessages.Any(x => x.Type != MessageType.Information);

						if (error)
						{
							OpenGlString.Draw(Fonts.SmallFont, $"Display the {Interface.MessageCount.ToString(culture)} error messages recently generated.", new Point(32, 112), TextAlignment.TopLeft, new Color128(1.0f, 0.5f, 0.5f));
						}
						else
						{
							OpenGlString.Draw(Fonts.SmallFont, $"Display the {Interface.MessageCount.ToString(culture)} messages recently generated.", new Point(32, 112), TextAlignment.TopLeft, TextColor);
						}
					}
				}
			}

			// finalize
			PopMatrix(MatrixMode.Projection);
			PopMatrix(MatrixMode.Modelview);
		}
	}
}
