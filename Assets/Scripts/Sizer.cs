using UnityEngine;
using System.Collections;

public class Sizer : MonoBehaviour {
	// Resize components to fit screen by width or height or both

	public bool resizeWidth = false;
	public bool resizeHeight = false;
	
	void Awake () {
		Vector3 bottom_left = Camera.main.ViewportToWorldPoint (new Vector3 (0, 0, Mathf.Abs(Camera.main.transform.position.z - transform.position.z)));
		Vector3 top_right = Camera.main.ViewportToWorldPoint (new Vector3 (1, 1, Mathf.Abs(Camera.main.transform.position.z - transform.position.z)));
		float desiredWidth = top_right.x - bottom_left.x;
		float desiredHeight = top_right.y - bottom_left.y;
		desiredWidth /= renderer.bounds.extents.x*2;
		// Used for Background, which is in the x-z plane (hence x, 1, y rather than x, y, 1)
		desiredHeight /= renderer.bounds.extents.y*2;
		transform.localScale = new Vector3 (resizeWidth ? desiredWidth : transform.localScale.x,
		                                    1,
		                                    resizeHeight ? desiredHeight : transform.localScale.y);
	}
}
