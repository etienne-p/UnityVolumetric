using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Volumetric
{
	public class Renderer : MonoBehaviour
	{
		enum Resolution
		{
			RES_64,
			RES_128,
			RES_256,
			RES_512
		}

		public Texture3D volumetricData;

		[SerializeField] Transform volumeTransform;
		[SerializeField] Mesh cubeMesh;
		[SerializeField] Shader boundsShader;
		[SerializeField] Shader raymarchingShader;
		[SerializeField] Shader stencilShader;
		[SerializeField, Range (.0f, 0.1f)] float alphaMul;
		[SerializeField] int positionBufferWidth;
		[SerializeField] int positionBufferHeight;
		[SerializeField] float volumeCameraIntersectionOffset;
		[SerializeField] Resolution resolution;
		[SerializeField] bool useDepth;
		[SerializeField] bool debug;

		Vector3[] intersectionVertices;
		RenderTexture frontPositionsTexture;
		RenderTexture backPositionsTexture;
		Mesh frontBounds;
		Mesh backBounds;
		Mesh intersectionMesh;
		Material rayMarchingMaterial;
		Material stencilMaterial;
		Material boundsMaterial;
		Plane[] camFrustumPlanes;
		bool isVisible;

		void OnDisable ()
		{
			if (rayMarchingMaterial != null)
			{
				DestroyImmediate (rayMarchingMaterial);
			}
			ReleaseTextures ();
		}

		void OnEnable ()
		{
			// we wiĺl need the camera to generate a depth texture so that we can handle depth while raymarching
			Camera.main.depthTextureMode = DepthTextureMode.Depth;
			isVisible = false;
			CheckBounds ();
			CheckTextures (positionBufferWidth, positionBufferHeight);
			enabled = CheckMaterials (); // deactivate in case shader is not supported
			if (enabled)
			{
				UpdateKeyWords ();
			}
		}

		void OnValidate ()
		{
			CheckTextures (positionBufferWidth, positionBufferHeight);
			bool checkMaterials = CheckMaterials ();
			if (CheckMaterials ())
			{
				UpdateKeyWords ();
			} else
			{
				enabled = false; // deactivate in case shader is not supported
			}
		}

		void Update ()
		{
			UpdateIntersection ();
			// TODO check for volume transform change
			if (camFrustumPlanes == null || Camera.main.transform.hasChanged)
			{
				camFrustumPlanes = GeometryUtility.CalculateFrustumPlanes (Camera.main);
				// convert planes to local volume coordinates as Bounds are axis aligned
				for (int i = 0; i != camFrustumPlanes.Length; ++i)
				{
					camFrustumPlanes [i].SetNormalAndPosition (
						volumeTransform.InverseTransformDirection (camFrustumPlanes [i].normal),
						volumeTransform.InverseTransformPoint (camFrustumPlanes [i].ClosestPointOnPlane (Vector3.zero))
					);
				}
			}
			// Remember that camFrustumPlanes have been converted to volume local coordinates
			// hence the static unit volume bounds
			isVisible = GeometryUtility.TestPlanesAABB (camFrustumPlanes, new Bounds (Vector3.zero, Vector3.one));
		}

		void OnGUI ()
		{
			if (debug)
			{
				int size = 256;
				GUI.DrawTexture (new Rect (10, 10, size, size), frontPositionsTexture);
				GUI.DrawTexture (new Rect (10 * 2 + size, 10, size, size), backPositionsTexture);
				GUI.Label (new Rect (0, size + 10, 300, 80), "volume visible [" + isVisible + "]");
			}
		}

		void OnDrawGizmos ()
		{
			BoxIntersection.OnDrawGizmos (volumeTransform, intersectionVertices);
		}

		void OnRenderImage (RenderTexture source, RenderTexture destination)
		{
			if (rayMarchingMaterial == null)
			{
				Graphics.Blit (source, destination);
				return;
			}
			CheckTextures (source.width, source.height);
			Render (source, destination);
		}

		void Render (RenderTexture source, RenderTexture destination)
		{
			// clear destination stencil buffer
			if (isVisible)
			{
				Graphics.SetRenderTarget (destination);
				GL.Clear (true, false, Color.black);
				Graphics.SetRenderTarget (null);
			}
			Graphics.Blit (source, destination);
			if (isVisible)
			{
				UpdateUniforms ();
				RenderPositionAndUpdateStencil (destination);
				Graphics.Blit (source, destination, rayMarchingMaterial);
			}
		}

		void CheckBounds ()
		{
			if (intersectionMesh == null)
			{
				intersectionMesh = new Mesh ();
				frontBounds = Instantiate (cubeMesh);
				backBounds = Util.ReverseMeshNormals (Instantiate (cubeMesh));
			}
		}

		bool CheckMaterials ()
		{
			if (rayMarchingMaterial == null || rayMarchingMaterial.shader != raymarchingShader)
			{
				if (raymarchingShader.isSupported)
				{
					rayMarchingMaterial = new Material (raymarchingShader);
					rayMarchingMaterial.hideFlags = HideFlags.DontSave;
				} else
				{
					rayMarchingMaterial = null;
					Debug.LogError ("Volumetric renderings shader not supported by this platform.");
					return false;
				}
			}
			if (stencilMaterial == null || stencilMaterial.shader != stencilShader)
			{
				if (stencilShader.isSupported)
				{
					stencilMaterial = new Material (stencilShader);
					stencilMaterial.hideFlags = HideFlags.DontSave;
				} else
				{
					stencilMaterial = null;
					Debug.LogError ("Stencil shader not supported by this platform.");
					return false;
				}
			}
			if (boundsMaterial == null || boundsMaterial.shader != boundsShader)
			{
				if (boundsShader.isSupported)
				{
					boundsMaterial = new Material (boundsShader);
					boundsMaterial.hideFlags = HideFlags.DontSave;
				} else
				{
					boundsMaterial = null;
					Debug.LogError ("Bounds shader not supported by this platform.");
					return false;
				}
			}
			return true;
		}

		void UpdateKeyWords ()
		{
			if (useDepth)
			{
				rayMarchingMaterial.EnableKeyword ("DEPTH_ON");
			} else
			{
				rayMarchingMaterial.DisableKeyword ("DEPTH_ON");
			}
			foreach (Resolution res in Resolutions)
			{
				if (res != resolution)
				{
					rayMarchingMaterial.DisableKeyword (ResolutionToPreProcessorSymbol (res));
				}
			}
			rayMarchingMaterial.EnableKeyword (ResolutionToPreProcessorSymbol (resolution));
		}

		void UpdateUniforms ()
		{
			rayMarchingMaterial.SetTexture ("_VolumeTex", volumetricData);
			rayMarchingMaterial.SetTexture ("_FrontBoundsTex", frontPositionsTexture);
			rayMarchingMaterial.SetTexture ("_BackBoundsTex", backPositionsTexture);
			rayMarchingMaterial.SetFloat ("_AlphaMul", alphaMul);
			// these matrices are needed for depth sorting
			rayMarchingMaterial.SetMatrix ("_VolumeModelMatrix", volumeTransform.localToWorldMatrix);
			Matrix4x4 VP = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix;
			rayMarchingMaterial.SetMatrix ("_CamViewProjectionMatrix", VP);
		}

		void UpdateIntersection ()
		{
			intersectionVertices = BoxIntersection.UpdateIntersectionVertices (volumeTransform, Camera.main, volumeCameraIntersectionOffset);
			BoxIntersection.UpdateMesh (intersectionMesh, intersectionVertices);
		}

		void RenderPositionAndUpdateStencil (RenderTexture dest)
		{
			RenderTexture toBeRestored = RenderTexture.active;

			boundsMaterial.SetPass (0);

			GL.PushMatrix ();
			GL.Viewport (new Rect (0, 0, positionBufferWidth, positionBufferHeight));
			GL.LoadProjectionMatrix (Camera.main.projectionMatrix);
			GL.LoadIdentity ();
			GL.MultMatrix (Camera.main.worldToCameraMatrix);

			// Render front
			Graphics.SetRenderTarget (frontPositionsTexture);
			GL.Clear (true, true, Color.black);
			Graphics.DrawMeshNow (frontBounds, volumeTransform.localToWorldMatrix);
			Graphics.DrawMeshNow (intersectionMesh, volumeTransform.localToWorldMatrix);

			// Render back
			Graphics.SetRenderTarget (backPositionsTexture);
			GL.Clear (true, true, Color.black);
			Graphics.DrawMeshNow (backBounds, volumeTransform.localToWorldMatrix);

			// Update stencil buffer
			stencilMaterial.SetPass (0);
			Graphics.SetRenderTarget (dest);
			Graphics.DrawMeshNow (backBounds, volumeTransform.localToWorldMatrix);

			GL.PopMatrix ();

			Graphics.SetRenderTarget (null);
			RenderTexture.active = toBeRestored;
		}

		void CheckTextures (int width, int height)
		{
			if (frontPositionsTexture == null ||
			    frontPositionsTexture.width != width ||
			    frontPositionsTexture.height != height)
			{
				if (frontPositionsTexture != null)
				{
					ReleaseTextures ();
				}
				frontPositionsTexture = new RenderTexture (width, height, 0, RenderTextureFormat.ARGB32);
				backPositionsTexture = new RenderTexture (width, height, 0, RenderTextureFormat.ARGB32);
			}
		}

		void ReleaseTextures ()
		{
			if (frontPositionsTexture == null)
				return;

			frontPositionsTexture.Release ();
			backPositionsTexture.Release ();
			DestroyImmediate (frontPositionsTexture);
			DestroyImmediate (backPositionsTexture);
		}

		static void CopyTransform (Transform from, Transform to)
		{
			if (to.parent != from.parent)
			{
				to.SetParent (from.parent);
			}
			to.position = from.position;
			to.rotation = from.rotation;
			to.localScale = from.localScale;
		}

		static readonly Resolution[] Resolutions = new Resolution[] {
			Resolution.RES_64, Resolution.RES_128, Resolution.RES_256, Resolution.RES_512
		};

		static string ResolutionToPreProcessorSymbol (Resolution res)
		{
			switch (res)
			{
				case Resolution.RES_64:
					return "RES_64";
				case Resolution.RES_128:
					return "RES_128";
				case Resolution.RES_256:
					return "RES_256";
				case Resolution.RES_512:
					return "RES_512";
			}
			return null;
		}
	}
}