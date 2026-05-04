using System.IO;
using UnityEngine;

public class LoadDir : MonoBehaviour {
	
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Init()
		{
			string path = "ux0:/data/comZombiesOffline/";

			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
		}
	}

