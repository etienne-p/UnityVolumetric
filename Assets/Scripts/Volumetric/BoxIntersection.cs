using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Volumetric
{
public class BoxIntersection
{
	static public void OnDrawGizmos(Transform transform, Vector3[] intersectionVertices)
    {
        if (intersectionVertices == null)
            return;
        Gizmos.color = Color.magenta;
        for (var i = 0; i < intersectionVertices.Length; ++i)
        {
            Gizmos.DrawLine(
                transform.TransformPoint(intersectionVertices[i]), 
                transform.TransformPoint(intersectionVertices[(i + 1) % intersectionVertices.Length]));
        }
    }

	public static Vector3[] UpdateIntersectionVertices(Transform transform, Camera camera, float offset = .01f)
    {
        Vector3 camForward = camera.transform.forward;
		// add some offset to make sure the intersection in not culled or flickering
		Plane camNearPlane = new Plane(
			camForward,
			camera.transform.position + camForward * (camera.nearClipPlane + offset));

		Vector3[] unitVolumeEdges = new Vector3[UnitVolumeEdges.Length];
		System.Array.Copy (UnitVolumeEdges, unitVolumeEdges, UnitVolumeEdges.Length);
		for(int i = 0; i != unitVolumeEdges.Length; ++i)
		{
			unitVolumeEdges [i] = transform.TransformPoint (unitVolumeEdges [i]);
		}

		Vector3[] intersectionVertices = IntersectPlaneEdges(camNearPlane, unitVolumeEdges);
        SortIntersectionVertices(intersectionVertices, camNearPlane);

		// transform intersection to local space
		for(int i = 0; i != intersectionVertices.Length; ++i)
		{
			intersectionVertices [i] = transform.InverseTransformPoint (intersectionVertices [i]);
		}
		return intersectionVertices;
    }

	static readonly Vector3[] UnitVolumeEdges = BoxIntersection.ComputeUnitVolumeEdges ();

	public static void UpdateMesh(Mesh mesh, Vector3[] intersectionVertices)
    {
        mesh.Clear();

        if (intersectionVertices.Length < 3)
            return;

        var vertices = new Vector3[intersectionVertices.Length + 1];
        var centroid = ComputeCentroid(intersectionVertices);
        vertices[0] = centroid;
        System.Array.Copy(intersectionVertices, 0, vertices, 1, intersectionVertices.Length);

        mesh.vertices = vertices;

        var indices = new int[intersectionVertices.Length * 3];
        for (var i = 0; i < intersectionVertices.Length; ++i)
        {
            indices[i * 3] = ((i + 1) % intersectionVertices.Length) + 1;
            indices[i * 3 + 1] = i + 1;
            indices[i * 3 + 2] = 0; // centroid
        }
        mesh.triangles = indices;
        mesh.RecalculateNormals();
    }

	public static Vector3[] ComputeUnitVolumeEdges()
    {
		Vector3[] baseEdges = new Vector3[]
        { 
            new Vector3(-.5f, -.5f, -.5f),
            new Vector3(.5f, -.5f, -.5f),
            new Vector3(-.5f, -.5f, -.5f),
            new Vector3(-.5f, .5f, -.5f),
            new Vector3(-.5f, -.5f, -.5f),
            new Vector3(-.5f, -.5f, .5f),
        };

		Vector3[] axes = new Vector3[]
        { 
            Vector3.right,
            Vector3.up,
            Vector3.forward
        };

		Vector3[] edges = new Vector3[24];
        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < 4; ++j)
            {
				Quaternion q = Quaternion.AngleAxis(90 * j, axes[i]);
                edges[(i * 4 + j) * 2] = (q * baseEdges[i * 2]);
                edges[(i * 4 + j) * 2 + 1] = (q * baseEdges[i * 2 + 1]);
            }
        }
		return edges;
    }

    static bool RayToPlane(Ray ray, Plane plane, out float distance, out Vector3 position)
    {
        if (plane.Raycast(ray, out distance))
        {
            position = ray.GetPoint(distance);
            return true;
        }
        position = Vector3.zero;
        distance = -1;
        return false;
    }

    static Vector3[] IntersectPlaneEdges(Plane plane, Vector3[] edges)
    {
        int nEdges = edges.Length / 2;
		List<Vector3> intersectionPoints = new List<Vector3>();

        for (var i = 0; i < nEdges; ++i)
        {
            var edge = edges[i * 2 + 1] - edges[i * 2];
            var ray = new Ray(edges[i * 2], edge);
            var distance = .0f;
            var position = Vector3.zero;
            if (RayToPlane(ray, plane, out distance, out position))
            {
                if (distance <= edge.magnitude)
                {
                    intersectionPoints.Add(position);
                }
            }
        }
        return intersectionPoints.ToArray();
    }

    static void SortIntersectionVertices(Vector3[] vertices, Plane plane)
    {
        if (vertices.Length < 2)
            return;

        Vector3 centroid = ComputeCentroid(vertices);

        System.Array.Sort(vertices, (a, b) =>
            {
                var v = Vector3.Cross(a - centroid, b - centroid);
                var d = Vector3.Dot(v, plane.normal);
                return (d == .0f ? 0 : (d < .0f ? 1 : -1));
            });
    }

    static Vector3 ComputeCentroid(Vector3[] vertices)
    {
        var sum = Vector3.zero;
        for (var i = 0; i < vertices.Length; ++i)
        {
            sum += vertices[i];
        }
        return sum / (float)vertices.Length;
    }
}
}
