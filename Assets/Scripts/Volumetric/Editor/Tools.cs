using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Volumetric
{
	public class Tools
	{
		[MenuItem ("Assets/Generate Volume Gradient")]
		static void GenerateVolumeGradient()
		{
			// double check even if we provide a validation method
			if (!GenerateVolumeGradientValidation ())
			{
				Debug.LogError ("Gradient generation aborted, no Texture3D selected.");
				return;
			}

			Texture3D src = (Texture3D)Selection.activeObject;

			Texture3D dst = NumericalMethods.Gradient (src);

			var path = EditorUtility.SaveFilePanelInProject("Save gradient texture", "gradient", "asset", "Select Export Location");
			AssetDatabase.CreateAsset (dst, path);
		}

		[MenuItem ("Assets/Generate Volume Gradient", true)]
		static bool GenerateVolumeGradientValidation ()
		{
			return Selection.activeObject.GetType () == typeof(Texture3D);
		}
	}
}
