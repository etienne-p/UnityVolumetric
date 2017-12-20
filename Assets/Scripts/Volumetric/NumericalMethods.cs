using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Volumetric
{
	public class NumericalMethods
	{
		public static Texture3D Gradient (Texture3D src)
		{
			var texels = src.GetPixels ();
			var size = new Vector3Int (src.width, src.height, src.depth);
			// turn colors grid to a float 3D grid
			var values = ColorToValue (texels);
			// compute the gradient of this grid
			var gradient = CentralFiniteDifference (values, size);
			// store the gradient as color information
			var gradientAsColor = EncodeGradient (gradient);

			Texture3D dst = new Texture3D (src.width, src.height, src.depth, TextureFormat.ARGB32, false);
			dst.SetPixels (gradientAsColor);
			dst.Apply ();
			return dst;
		}

		static Color[] EncodeGradient (Vector3[] gradient)
		{
			var gradientAsColor = new Color[gradient.Length];
			for (int i = 0; i != gradient.Length; ++i)
			{
				var v = (new Vector3 (gradient [i].x, gradient [i].y, gradient [i].z) + Vector3.one) * 0.5f;
				gradientAsColor [i] = new Color (v.x, v.y, v.z);
			}
			return gradientAsColor;
		}

		static float[] ColorToValue (Color[] colors)
		{
			var values = new float[colors.Length];
			for (int i = 0; i != colors.Length; ++i)
			{
				float h, s, v;
				Color.RGBToHSV (colors [i], out h, out s, out v);	
				values [i] = v * colors [i].a;
			}
			return values;
		}

		/*static Vector3[] Diffuse (Vector3[] field, Vector3Int size, float diffusion)
		{
			var diffused = new Vector3[field.Length];
			var forward = new Vector3Int (0, 0, 1);
			var back = new Vector3Int (0, 0, -1);

			for (int x = 1; x != size.x - 1; ++x)
			{
				for (int y = 1; y != size.y - 1; ++y)
				{
					for (int z = 1; z != size.z - 1; ++z)
					{
						var i = new Vector3Int (x, y, x);
						var avgNeighbor = (
							field [Util.Index3DTo1D (i + Vector3Int.right, size)] +
							field [Util.Index3DTo1D (i + Vector3Int.left, size)] +
							field [Util.Index3DTo1D (i + Vector3Int.up, size)] +
							field [Util.Index3DTo1D (i + Vector3Int.down, size)] +
							field [Util.Index3DTo1D (i + forward, size)] +
						    field [Util.Index3DTo1D (i + back, size)]) / 6.0f;
						var vec = field [Util.Index3DTo1D (i, size)];
						// the influence of its neighbor on a vector is inversely proportional to its length
						diffused [Util.Index3DTo1D (i, size)] = vec + avgNeighbor * Mathf.Max(0, 1.0f - vec.magnitude) * diffusion;
					}
				}
			}
			return diffused;
		}*/

		static Vector3[] CentralFiniteDifference (float[] volume, Vector3Int size)
		{
			Vector3Int forward = new Vector3Int (0, 0, 1); 
			Vector3Int back = new Vector3Int (0, 0, -1); 
			var gradient = new Vector3[volume.Length];
			for (int x = 1; x != size.x - 1; ++x)
			{
				for (int y = 1; y != size.y - 1; ++y)
				{
					for (int z = 1; z != size.z - 1; ++z)
					{
						var i = new Vector3Int (x, y, z);
						var dx = 
							volume [Util.Index3DTo1D (i + Vector3Int.right, size)] -
							volume [Util.Index3DTo1D (i + Vector3Int.left, size)];
						var dy = 
							volume [Util.Index3DTo1D (i + Vector3Int.up, size)] -
							volume [Util.Index3DTo1D (i + Vector3Int.down, size)];
						var dz = 
							volume [Util.Index3DTo1D (i + forward, size)] -
							volume [Util.Index3DTo1D (i + back, size)];
						gradient [Util.Index3DTo1D (i, size)] = new Vector3 (dx, dy, dz);
					}
				}
			}
			return gradient;
		}
	}
}
