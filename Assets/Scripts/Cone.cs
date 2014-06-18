using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Cone : MonoBehaviour {

	private static Stack<GameObject> Pool = new Stack<GameObject>();

	public static Cone Get() {
		Cone cone;
		if (Pool.Count > 0) {
			cone = Pool.Pop ().GetComponent<Cone>();
		} else {
			cone = (Instantiate (Resources.Load ("ConePrefab")) as GameObject).GetComponent<Cone>();
		}
		cone.name = "Cone";
		cone.gameObject.SetActive (true);
		cone.renderer.enabled = false;
		cone.collider.enabled = false;
		return cone;
	}
	
	public void Die() {
		transform.position = transform.localPosition;
		transform.rotation = transform.localRotation;
		renderer.enabled = true;
		collider.enabled = true;
		transform.parent = null;
		gameObject.SetActive (false);
		Pool.Push (gameObject);
	}

}
