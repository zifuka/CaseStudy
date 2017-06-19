﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateScript : MonoBehaviour {
    public GameObject[] prefab;

	// Use this for initialization
	void Start () {
        GameObject[] obj;
        obj = new GameObject[prefab.Length];

        for (int i = 0; i < prefab.Length; i++)
        {
            obj[i] = null;
            obj[i] = GameObject.Find(prefab[i].name);

            if (obj[i] == null)
            {
                // ないなら生成
                obj[i] = Instantiate(prefab[i]) as GameObject;
                obj[i].name = prefab[i].name;
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}