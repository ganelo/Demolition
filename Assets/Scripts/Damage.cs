using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Damage : MonoBehaviour {

	public float max_hp = 100;
	public float hp;
	public Vector3 original_scale;
	public Color original_color;

	Mesh mesh;
	Vector3[] vertices;

	// Want Start, not Awake b/c e.g. scale can change depending on use of half-bricks
	void Start () {
		hp = max_hp;
		original_scale = transform.localScale;
		mesh = GetComponent<MeshFilter> ().mesh;
		vertices = mesh.vertices;
		original_color = mesh.colors32 [0];
	}

	public void Reset() {
		hp = max_hp;
		transform.localScale = original_scale;
		for (int i = 0; i < vertices.Length; i++) {
			mesh.colors32[i] = original_color;
		}
	}
	
	public void TakeDamage(float d) {
		Construct.AddScore ((int)d);

		hp -= d;
		if (hp <= 0) {
			GetComponent<Brick>().Die(counts:true);
		}

		// Don't shrink if we're attached to something, because we'll just be floating in mid-air otherwise
		IEnumerable<FixedJoint> all_fjs = from brick in GameObject.Find("Building").transform.Cast<Transform>()
				                              from fj in brick.gameObject.GetComponents<FixedJoint> ()
											      select fj;
		bool used = false;

		foreach (FixedJoint fj in all_fjs) {
			if (fj != null && fj.connectedBody != null &&
			    (fj.connectedBody.Equals(rigidbody) || fj.gameObject.Equals (gameObject))) {
				used = true;
				break;
			}
		}

		// (Unless attached to something,) shrink in proportion to damage
		if (!used) {
			transform.localScale = original_scale * Mathf.Max (hp/max_hp, 0.4f);
		}

		// Arbitrarily, each brick is responsible for the strength of the FixedJoints it owns (rather than all of the ones that connect to it)
		//   so don't change the strength of all joints touching us - just the ones in our components.
		foreach (FixedJoint fj in GetComponents<FixedJoint>()) {
			// A FixedJoint may be removed due to a brick Die()ing, so make sure we have a valid fj first
			if (fj != null) continue;
			// That being said, we might still get unlucky and get errors due to a race condition :(
			fj.breakForce = Construct.strength * hp/max_hp;
			fj.breakTorque = Construct.strength * hp/max_hp;
		}

		// Darken in proportion to damage
		Color c = mesh.colors32[0];
		c.b = original_color.b * hp / (max_hp * 1.75f);
		c.g = original_color.g * hp / (max_hp * 1.75f);
		c.r = original_color.r * hp / (max_hp * 1.75f);
		Color32[] cols = new Color32[vertices.Length];
		for (int i = 0; i < vertices.Length; i++ ) {
			cols[i] = c;
		}
		mesh.colors32 = cols;
	}
}
