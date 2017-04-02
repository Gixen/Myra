﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.MultiCompileEffects;
using Myra.Edit;
using Myra.Graphics3D.Materials;
using Newtonsoft.Json;
using DirectionalLight = Myra.Graphics3D.Environment.DirectionalLight;

namespace Myra.Graphics3D
{
	public class ModelObject : BaseObject
	{
		private readonly MultiCompileEffect _effect;

		private BasicEffect _basicEffect;

		private readonly VertexPositionNormalTexture[] _vertices;
		private Vector3 _scale = new Vector3(1, 1, 1);
		private Vector3 _translate;
		private Vector3 _rotate;
		private readonly Mesh _mesh;
		private Mesh _normalsMesh;
		private Mesh _boundingBoxMesh;
		private bool _transformDirty = true;
		private Matrix _transform, _transformWithoutScale;
		private BoundingBox _boundingBox;

		public Mesh Mesh
		{
			get { return _mesh; }
		}

		public Vector3 Scale
		{
			get
			{
				return _scale;
			}

			set
			{
				if (value == _scale)
				{
					return;
				}

				_scale = value;
				_normalsMesh = null;
				InvalidateTransform();
			}
		}

		public Vector3 Translate
		{
			get
			{
				return _translate;
			}

			set
			{
				if (value == _translate)
				{
					return;
				}

				_translate = value;
				InvalidateTransform();
			}
		}

		public Vector3 Rotate
		{
			get
			{
				return _rotate;
			}

			set
			{
				if (value == _rotate)
				{
					return;
				}

				_rotate = value;
				InvalidateTransform();
			}
		}

		[HiddenInEditor]
		[JsonIgnore]
		public Matrix Transform
		{
			get
			{
				Update();
				return _transform;
			}
		}


		[HiddenInEditor]
		[JsonIgnore]
		public Matrix TransformWithoutScale
		{
			get
			{
				Update();
				return _transformWithoutScale;
			}
		}

		[HiddenInEditor]
		public BaseMaterial Material { get; set; }

		public override BoundingBox BoundingBox
		{
			get
			{
				return new BoundingBox(Mesh.MinimumBound*Scale + Translate,
					Mesh.MaximumBound*Scale + Translate);
			}
		}

		public ModelObject(VertexPositionNormalTexture[] vertices, int[] indices,
			PrimitiveType primitiveType = PrimitiveType.TriangleList)
		{
			_vertices = vertices;
			_mesh = new Mesh(vertices, indices, primitiveType);
			_effect = Graphics3DAssets.DefaultEffect;
		}

		private void Update()
		{
			if (!_transformDirty)
			{
				return;
			}

			_transform = Matrix.CreateRotationY(MathHelper.ToRadians(Rotate.X))*
			             Matrix.CreateRotationX(MathHelper.ToRadians(Rotate.Y))*
			             Matrix.CreateRotationZ(MathHelper.ToRadians(Rotate.Z))*
			             Matrix.CreateScale(Scale)*
			             Matrix.CreateTranslation(Translate);

			_transformWithoutScale = Matrix.CreateRotationY(MathHelper.ToRadians(Rotate.X))*
			                         Matrix.CreateRotationX(MathHelper.ToRadians(Rotate.Y))*
			                         Matrix.CreateRotationZ(MathHelper.ToRadians(Rotate.Z))*
			                         Matrix.CreateTranslation(Translate);

			_boundingBox = new BoundingBox(Vector3.Transform(Mesh.MinimumBound, _transform),
					Vector3.Transform(Mesh.MaximumBound, _transform));

			_transformDirty = false;
		}

		private void InvalidateTransform()
		{
			_transformDirty = true;
		}

		public Effect GetEffect(bool hasLight, bool hasTexture)
		{
			var defines = new List<string>();

			if (hasLight)
			{
				defines.Add("LIGHTNING");
			}

			if (hasTexture)
			{
				defines.Add("TEXTURE");
			}

			return _effect.GetEffect(defines.ToArray());
		}

		public override void Render(RenderContext context)
		{
			if (Material == null)
			{
				return;
			}

			var device = MyraEnvironment.GraphicsDevice;

			var viewProjection = context.Camera.ViewProjection;

			var hasLight = Material.HasLight && context.Lights != null && context.Lights.Length > 0;

			var effect = GetEffect(hasLight, Material.Texture != null);

			var mesh = Mesh;
			device.SetVertexBuffer(mesh.VertexBuffer);
			device.Indices = mesh.IndexBuffer;

			var worldViewProj = Transform*viewProjection;

			effect.Parameters["_worldViewProj"].SetValue(worldViewProj);
			effect.Parameters["_diffuseColor"].SetValue(Material.DiffuseColor.ToVector4());

			if (Material.Texture != null)
			{
				effect.Parameters["_texture"].SetValue(Material.Texture);
			}

			if (hasLight)
			{
				var worldInverseTranspose = Matrix.Transpose(Matrix.Invert(Transform));
				effect.Parameters["_world"].SetValue(Transform);
				effect.Parameters["_eyePosition"].SetValue(context.Camera.Position);
				effect.Parameters["_worldInverseTranspose"].SetValue(worldInverseTranspose);

				device.BlendState = BlendState.AlphaBlend;
				for (var i = 0; i < context.Lights.Length; ++i)
				{
					if (i == 1)
					{
						device.BlendState = BlendState.Additive;
					}

					var dl = context.Lights[i];

					effect.Parameters["_lightDir"].SetValue(dl.NormalizedDirection);
					effect.Parameters["_lightColor"].SetValue(dl.Color.ToVector3());
					effect.Parameters["_specularPower"].SetValue(16.0f);

					foreach (var pass in effect.CurrentTechnique.Passes)
					{
						pass.Apply();
						device.DrawIndexedPrimitives(mesh.PrimitiveType, 0, 0, 0, 0, mesh.PrimitiveCount);
					}
				}

				if (context.Lights.Length > 1)
				{
					device.BlendState = BlendState.AlphaBlend;
				}
			}
			else
			{
				foreach (var pass in effect.CurrentTechnique.Passes)
				{
					pass.Apply();

					device.DrawIndexedPrimitives(mesh.PrimitiveType, 0, 0, 0, 0, mesh.PrimitiveCount);
				}
			}

			if ((context.Flags & RenderFlags.DrawNormals) == RenderFlags.None &&
			    (context.Flags & RenderFlags.DrawBoundingBoxes) == RenderFlags.None) return;

			effect = Graphics3DAssets.DefaultEffect.GetDefaultEffect();

			if ((context.Flags & RenderFlags.DrawNormals) == RenderFlags.DrawNormals)
			{
				if (_normalsMesh == null)
				{
					// Apply scale
					var scaledVertices = new VertexPositionNormalTexture[_vertices.Length];
					for (var i = 0; i < scaledVertices.Length; ++i)
					{
						scaledVertices[i] = new VertexPositionNormalTexture(_vertices[i].Position*Scale, _vertices[i].Normal,
							_vertices[i].TextureCoordinate);
					}

					_normalsMesh = CreateNormalsMesh(scaledVertices, 1.0f);
				}

				device.SetVertexBuffer(_normalsMesh.VertexBuffer);
				device.Indices = _normalsMesh.IndexBuffer;

				worldViewProj = TransformWithoutScale*viewProjection;
				effect.Parameters["_worldViewProj"].SetValue(worldViewProj);
				effect.Parameters["_diffuseColor"].SetValue(NormalsColor);

				foreach (var pass in effect.CurrentTechnique.Passes)
				{
					pass.Apply();

					device.DrawIndexedPrimitives(_normalsMesh.PrimitiveType, 0, 0, 0, 0, _normalsMesh.PrimitiveCount);
				}
			}

			if ((context.Flags & RenderFlags.DrawBoundingBoxes) == RenderFlags.DrawBoundingBoxes)
			{
				if (_boundingBoxMesh == null)
				{
					_boundingBoxMesh = CreateBoundingBoxMesh(new BoundingBox(Mesh.MinimumBound, Mesh.MaximumBound));
				}

				device.SetVertexBuffer(_boundingBoxMesh.VertexBuffer);
				device.Indices = _boundingBoxMesh.IndexBuffer;

				worldViewProj = Transform*viewProjection;
				effect.Parameters["_worldViewProj"].SetValue(worldViewProj);
				effect.Parameters["_diffuseColor"].SetValue(BoundingBoxesColor);

				foreach (var pass in effect.CurrentTechnique.Passes)
				{
					pass.Apply();

					device.DrawIndexedPrimitives(_boundingBoxMesh.PrimitiveType, 0, 0, 0, 0, _boundingBoxMesh.PrimitiveCount);
				}
			}
		}

		private static void SetMonoGameDirectionalLight(Microsoft.Xna.Framework.Graphics.DirectionalLight mgLight,
			DirectionalLight light)
		{
			mgLight.DiffuseColor = light.Color.ToVector3();
			mgLight.SpecularColor = mgLight.DiffuseColor;
			mgLight.Direction = light.Direction;
			mgLight.Enabled = true;
		}

		public void Render2(RenderContext context)
		{
			var device = MyraEnvironment.GraphicsDevice;

			if (_basicEffect == null)
			{
				_basicEffect = new BasicEffect(device);
			}

			// Set the View matrix which defines the camera and what it's looking at
			context.Camera.Viewport = new Vector2(device.Viewport.Width, device.Viewport.Height);

			_basicEffect.View = context.Camera.View;
			_basicEffect.Projection = context.Camera.Projection;

			if (Material.HasLight && context.Lights != null && context.Lights.Length > 0)
			{
				_basicEffect.LightingEnabled = true;

				if (context.Lights.Length > 0)
				{
					SetMonoGameDirectionalLight(_basicEffect.DirectionalLight0, context.Lights[0]);
				}
				else
				{
					_basicEffect.DirectionalLight0.Enabled = false;
				}

				if (context.Lights.Length > 1)
				{
					SetMonoGameDirectionalLight(_basicEffect.DirectionalLight1, context.Lights[1]);
				}
				else
				{
					_basicEffect.DirectionalLight1.Enabled = false;
				}

				if (context.Lights.Length > 2)
				{
					SetMonoGameDirectionalLight(_basicEffect.DirectionalLight2, context.Lights[2]);
				}
				else
				{
					_basicEffect.DirectionalLight2.Enabled = false;
				}
			}
			else
			{
				_basicEffect.LightingEnabled = false;
			}

			if (Material.Texture != null)
			{
				_basicEffect.Texture = Material.Texture;
				_basicEffect.TextureEnabled = true;
			}
			else
			{
				_basicEffect.TextureEnabled = false;
			}

			_basicEffect.DiffuseColor = Material.DiffuseColor.ToVector3();
			_basicEffect.World = Transform;
			var mesh = Mesh;
			device.SetVertexBuffer(mesh.VertexBuffer);
			device.Indices = mesh.IndexBuffer;
			foreach (var pass in _basicEffect.CurrentTechnique.Passes)
			{
				pass.Apply();

				device.DrawIndexedPrimitives(mesh.PrimitiveType, 0, 0, mesh.VertexBuffer.VertexCount, 0,
					mesh.PrimitiveCount);
			}
		}
	}
}