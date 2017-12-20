using UnityEngine;
using System.Collections;

namespace Volumetric
{
	public class Util
	{
		public static bool IsPowerOfTwo (int x)
		{
			return x > 0 && (x & (x - 1)) == 0;
		}

		// Returns the bounding box (axis aligned) of a points set
		public static Bounds PointCloudBounds(Vector3[] points)
		{
			Vector3 minVert = Vector3.one * float.MaxValue;
			Vector3 maxVert = Vector3.one * float.MinValue;

			for (int i = 0; i != points.Length; ++i)
			{
				minVert.x = Mathf.Min(minVert.x, points[i].x);
				minVert.y = Mathf.Min(minVert.y, points[i].y);
				minVert.z = Mathf.Min(minVert.z, points[i].z);

				maxVert.x = Mathf.Max(maxVert.x, points[i].x);
				maxVert.y = Mathf.Max(maxVert.y, points[i].y);
				maxVert.z = Mathf.Max(maxVert.z, points[i].z);
			}
			Bounds rv = new Bounds();
			rv.SetMinMax(minVert, maxVert);
			return rv;
		}

		public static Mesh ReverseMeshNormals (Mesh mesh)
		{
			Vector3[] normals = mesh.normals;
			for (int i = 0; i < normals.Length; i++)
				normals [i] = -normals [i];
			mesh.normals = normals;

			for (int m = 0; m < mesh.subMeshCount; m++)
			{
				int[] triangles = mesh.GetTriangles (m);
				for (int i = 0; i < triangles.Length; i += 3)
				{
					int temp = triangles [i + 0];
					triangles [i + 0] = triangles [i + 1];
					triangles [i + 1] = temp;
				}
				mesh.SetTriangles (triangles, m);
			}
			return mesh;
		}

		public static int Index3DTo1D (Vector3Int index, Vector3Int size)
		{
			return index.z * size.y * size.x + index.y * size.x + index.x;
		}

		public static Vector3Int Index1DTo3D (int index, Vector3Int size)
		{
			int ix = index % size.x;
			int iy = ((index - ix) / size.x) % size.y;
			int iz = (index - ix - iy * size.x) / (size.x * size.y);
			return new Vector3Int (ix, iy, iz);
		}
	}
}
