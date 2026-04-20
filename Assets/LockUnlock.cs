using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockUnlock : MonoBehaviour {
	void Start(){
		Application.targetFrameRate = 240;
		DontDestroyOnLoad(base.gameObject);
	}
	void Update () {
					if (Screen.lockCursor == true)
			{
				Cursor.visible = true;
				Screen.lockCursor = false;
			}
	}
}
