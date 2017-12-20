using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;

namespace Volumetric
{
	[ExecuteInEditMode]
	public class Slicer : MonoBehaviour
	{
		[SerializeField] int resolution;
		[SerializeField] Shader normalShader;
		[SerializeField] Shader sliceShader;
		[SerializeField] Mesh mesh;
		[SerializeField, Range (0, 0.2f)] float margin;
		[SerializeField, Range(0, 1)] float slicePosition;
		[SerializeField] bool drawGUI;
		[SerializeField] bool drawGizmos;
		[SerializeField] Color gizmosColor;
		[SerializeField, Range(0, 1)] float step;

		[SerializeField] bool renderOnOnRenderObject;
		[SerializeField] bool renderOnUpdate;
		[SerializeField, Range(0.1f, 8)] float guiScale;

		Material sliceMaterial;
		Material normalMaterial;
		Vector3 meshScale;
		Vector3 meshPosition;
		RenderTexture sliceTex;		
		RenderTexture normalTex;
		Bounds bounds;

		void OnEnable ()
		{
			Reset ();
		}

		void OnValidate ()
		{
			Reset ();
		}

		void OnDisable ()
		{
			if (sliceTex != null)
			{
				sliceTex.Release ();
				DestroyImmediate (sliceTex);
			}
			if (normalTex != null)
			{
				normalTex.Release ();
				DestroyImmediate (normalTex);
			}
		}

		void OnGUI()
		{
			if (drawGUI)
			{
				GUI.matrix = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, Vector3.one * guiScale);
				GUI.DrawTexture (new Rect(0, 0, resolution, resolution), normalTex);
				GUI.DrawTexture (new Rect(resolution, 0, resolution, resolution), sliceTex);
			}
		}

		void OnDrawGizmos ()
		{
			if (drawGizmos)
			{
				Gizmos.color = gizmosColor;
				Gizmos.DrawWireCube (Vector3.one * 0.5f, Vector3.one);
				Gizmos.DrawWireMesh (mesh, meshPosition, Quaternion.identity, meshScale);
			}
		}

		void OnRenderObject()
		{
			if (renderOnOnRenderObject) RenderSlice ();
		}

		void Update()
		{
			if (renderOnUpdate) RenderSlice ();
		}

		void Reset()
		{
			CheckRenderTexture (ref normalTex);
			CheckRenderTexture (ref sliceTex);
			CheckMaterial (ref normalMaterial, normalShader);
			CheckMaterial (ref sliceMaterial, sliceShader);
			UpdateMeshMatrix ();
		}

		void CheckRenderTexture (ref RenderTexture tex)
		{
			if (tex == null || tex.width != resolution || tex.height != resolution)
			{
				if (tex != null)
				{
					tex.Release ();
					DestroyImmediate (tex);
				}
				tex = new RenderTexture (resolution, resolution, 24);
				tex.filterMode = FilterMode.Point;
				tex.hideFlags = HideFlags.DontSave;
			}
		}

		void UpdateMeshMatrix ()
		{
			if (mesh != null)
			{
				bounds = Util.PointCloudBounds (mesh.vertices);
				var size = bounds.extents * 2;
				meshScale = Vector3.one * (1.0f - margin) / Mathf.Max (size.x, Mathf.Max (size.y, size.z));
				meshPosition = Vector3.one * 0.5f - bounds.center * meshScale.x;
			}
		}

		void CheckMaterial(ref Material mat, Shader shader)
		{
			if (shader != null && (mat == null || mat.shader != shader))
			{
				if (mat != null)
				{
					Material.DestroyImmediate (mat);
				}
				mat = new Material (shader);
				mat.hideFlags = HideFlags.DontSave;
			}
		}

		void RenderSlice ()
		{
			RenderTexture toBeRestored = RenderTexture.active;

			var meshTransform = Matrix4x4.TRS (
				meshPosition + Vector3.forward * slicePosition, Quaternion.identity, meshScale);

			Graphics.SetRenderTarget (normalTex);

			GL.PushMatrix ();
			GL.Viewport (new Rect (0, 0, resolution, resolution));
			GL.LoadOrtho ();
			GL.LoadIdentity ();

			GL.Clear (true, true, Color.black);
			normalMaterial.SetPass (0);
			Graphics.DrawMeshNow (mesh, meshTransform);

			GL.PopMatrix ();
			Graphics.SetRenderTarget (null);

			sliceMaterial.SetFloat ("_Step", step);

			Graphics.Blit (normalTex, sliceTex, sliceMaterial);

			RenderTexture.active = toBeRestored;
		}

		[ContextMenu("ExportTexture3D")]
		void ExportTexture3D ()
		{
			Reset ();

			var voxels = new Color[resolution * resolution * resolution];
			var tex = new Texture2D (resolution, resolution);
			var rect = new Rect (0, 0, resolution, resolution);

			for (var z = 0; z < resolution; z++)
			{
				slicePosition = z / (resolution - 1.0f);
				RenderSlice ();
				RenderTexture toBeRestored = RenderTexture.active;
				RenderTexture.active = sliceTex;
				tex.ReadPixels (rect, 0, 0);
				RenderTexture.active = toBeRestored;
				var pixels = tex.GetPixels ();
				System.Array.Copy (pixels, 0, voxels, z * resolution * resolution, resolution * resolution);
			}

			Texture3D tex3D = new Texture3D (resolution, resolution, resolution, TextureFormat.RGB24, false);
			tex3D.SetPixels (voxels);
			tex3D.Apply ();

			string path = EditorUtility.SaveFilePanelInProject ("Export", "texture", "asset", "Choose Export Location");
			AssetDatabase.CreateAsset (tex3D, path);
		}
	}
}