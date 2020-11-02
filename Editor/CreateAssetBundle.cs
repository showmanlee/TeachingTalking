using UnityEditor;
using System.IO;

// 애셋 번들 만들기
public class CreateAssetBundle {
	[MenuItem("Assets/Build AssetBundles")]
	static void BuildAllAssetBundles() {
		string assetBundleDirectory = "Assets/AssetBundles";
		if (!Directory.Exists(assetBundleDirectory)) {
			Directory.CreateDirectory(assetBundleDirectory);
		}
		BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
	}
}