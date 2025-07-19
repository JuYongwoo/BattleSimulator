using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Add_PA : MonoBehaviour {

	private Area_tmp tmp;

	void Start(){

		tmp = GameObject.FindGameObjectWithTag("Areas").GetComponent<Area_tmp>();
		tmp.areas.Add(this.gameObject);
	}
}
