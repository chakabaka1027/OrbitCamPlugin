﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAt : MonoBehaviour {


	// Update is called once per frame
	void Update () {
		gameObject.transform.LookAt(Camera.main.transform);
	}
}
