using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Brick : MonoBehaviour {

	private static Stack<GameObject> Pool = new Stack<GameObject>();
	private static GameObject Ground, Background;
	
	void Awake () {
		Ground = GameObject.Find ("Ground");
		Background = GameObject.Find ("Background");
	}
	
	void Update () {
		// Handle run-away bricks
		if (transform.position.y <= Ground.transform.position.y ||
		    transform.position.y > Background.transform.localScale.z*20 ||
		    Mathf.Abs (transform.position.x) > (Mathf.Abs (Ground.transform.localScale.x/2) + 5))
			// Background is in the x-z plane; use scale * 10 b/c 10 units tall;
			//   multiply that by 2 to allow some of the bricks to make it back
			Die (counts:true);
	}

	void OnCollisionEnter(Collision col) {
		if (col.relativeVelocity.magnitude > 2) {
			GetComponent<Damage> ().TakeDamage (Mathf.Sqrt(col.relativeVelocity.magnitude));
		}
	}

	public static Brick Get() {
		Brick brick;
		if (Pool.Count > 0) {
			brick = Pool.Pop ().GetComponent<Brick>();
		} else {
			brick = (Instantiate (Resources.Load ("BrickPrefab")) as GameObject).GetComponent<Brick>();
		}
		brick.name = "Brick";
		brick.gameObject.SetActive (true);
		brick.gameObject.tag = "Brick";
		return brick;
	}
	
	public void Die(bool counts) {
		// Get rid of our fixed joints
		foreach (FixedJoint fj in GetComponents<FixedJoint>()) {
			Destroy (fj);
		}
		IEnumerable<FixedJoint> all_fjs = from brick in GameObject.Find("Building").transform.Cast<Transform>()
											from fj in brick.gameObject.GetComponents<FixedJoint> ()
												select fj;
		// Get rid of fixed joints that touch us
		foreach (FixedJoint fj in all_fjs) {
			if (fj != null && (fj.connectedBody == null || fj.connectedBody.Equals(rigidbody))) {
				Destroy(fj);
			}
		}

		transform.rotation = Quaternion.identity;
		rigidbody.velocity = Vector3.zero;
		rigidbody.angularVelocity = Vector3.zero;
		GetComponent<Damage> ().Reset ();
		gameObject.tag = "Untagged";
		gameObject.SetActive (false);
		Pool.Push (gameObject);

		if (counts) {
			Construct.AddScore((int)GetComponent<Damage> ().max_hp);
			Construct.achieved += 1;
		}
	}
}
