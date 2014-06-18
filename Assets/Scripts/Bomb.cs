using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bomb : MonoBehaviour {

	public bool placed=false, alive=false;

	private static Stack<GameObject> Pool = new Stack<GameObject>();
	private static GameObject Bombs;
	
	void Awake () {
		Bombs = GameObject.Find ("Bombs");
		if (Bombs == null)
			Bombs = new GameObject ("Bombs");
	}
	
	void Update () {
		if (alive == false || placed == true) return;
		// The bomb follows the cursor, just in front of the wall (which is ~40 units from the camera)
		var v3 = Input.mousePosition;
		v3.z = 39.4f;
		transform.position = Camera.main.ScreenToWorldPoint(v3);

		// Bombs shouldn't be clickable or visible below the ground
		if ((transform.position.y - renderer.bounds.extents.y) < GameObject.Find ("Ground").transform.position.y) {
			renderer.enabled = false;
			collider.enabled = false;
		} else {
			renderer.enabled = true;
			collider.enabled = true;
		}

		// Rotate - mostly relevant for shaped and vert
		var r = transform.rotation.eulerAngles;
		if (Input.GetKey (KeyCode.LeftArrow)) {
			r.z += 2;
		} else if (Input.GetKey (KeyCode.RightArrow)) {
			r.z -= 2;
		}
		r.z += Input.GetAxis ("Mouse ScrollWheel")*50;
		transform.rotation = Quaternion.Euler (r);
	}

	void OnMouseDown() {
		// Not clickable below the ground
		if (alive == false || renderer.enabled == false) return;

		// Delete placed bombs if clicked when no bomb is selected
		if (placed == true && BombSelector.current == BombSelector.bombs.none) Die ();
		// Place bombs when a bomb type is selected
		else if (placed == false) {
			Bomb b_placed = Get (GetComponent<SpriteRenderer>().sprite);
			b_placed.placed = true;
			b_placed.transform.position = transform.position;
			b_placed.transform.rotation = transform.rotation;
			if (GetComponent<SpriteRenderer>().sprite.name.Equals("shaped")) {
				// Shaped charges require a helper object for exploding
				Cone c = Cone.Get ();
				c.transform.parent = b_placed.transform;
				c.transform.localPosition = c.transform.position;
				c.transform.localRotation = c.transform.rotation;
			}
			BombSelector.clicks--;
		}
	}

	public static Bomb Get(Sprite s) {
		Bomb bomb;
		if (Pool.Count > 0) {
			bomb = Pool.Pop ().GetComponent<Bomb>();
		} else {
			// Easier than trying to find a way to reference a prefab from a static method . . .
			bomb = (Instantiate (GameObject.Find ("imploder")) as GameObject).GetComponent<Bomb>();
			bomb.transform.parent = Bombs.transform;
			bomb.tag = "Bomb";
		}
		bomb.name = s.name;
		bomb.alive = true;
		bomb.placed = false;
		bomb.gameObject.SetActive (true);
		// Resize the box collider to fit the shape of the specified bomb - makes deleting shaped and vert more reliable
		bomb.GetComponent<BoxCollider> ().size = s.bounds.extents*2;
		// Bombs shouldn't inherently collide with bricks - only on Go, and only in the designed way
		bomb.collider.isTrigger = true;
		bomb.gameObject.GetComponent<SpriteRenderer>().sprite = s;
		// Don't need a selector on our actual bomb instance
		Destroy (bomb.GetComponent<BombSelector> ());
		// Imploders should be a little bigger because they're not super useful if they're limited to removing a single brick
		if (s.name == "imploder")
			bomb.transform.localScale = new Vector3 (4.0f/7, 4.0f/7, 1);
		else
			bomb.transform.localScale = new Vector3 (1.0f/2, 1.0f/2, 1);
		return bomb;
	}

	public void Die() {
		transform.rotation = Quaternion.identity;
		renderer.enabled = true;
		collider.enabled = true;
		alive = false;
		placed = false;
		gameObject.SetActive (false);
		var c = transform.FindChild ("Cone");
		if (c != null) {
			c.GetComponent<Cone>().Die ();
		}
		Pool.Push (gameObject);
	}

	public IEnumerator Explode() {
		collider.enabled = true;
		collider.isTrigger = true;
		SendMessage (name);
		yield return new WaitForSeconds (1.5f);
		Die ();
	}

	public void imploder() {
		var p = transform.position;
		p.z = 0.2f;
		transform.position = p;
		Transform building = GameObject.Find ("Building").transform;
		// Not the most efficient method, but ultimately even sphere explosions on big walls are slow, and spheres are much more efficient . . .
		foreach (Transform brick in building) {
			if (brick.collider.bounds.Intersects(collider.bounds)) {
				// Imploder removes bricks entirely
				brick.GetComponent<Damage>().TakeDamage(brick.GetComponent<Damage>().max_hp);
			}
		}

		// Hide since Expode waits for some seconds
		renderer.enabled = false;
	}

	public IEnumerator sphere() {
		// Spherical explosion
		var p = transform.position;
		p.z = 0.2f;
		transform.position = p;
		Collider[] colliders = Physics.OverlapSphere(transform.position, transform.localScale.x*3);
		foreach (Collider hit in colliders) {;
			if (hit && hit.rigidbody) {
				hit.rigidbody.AddExplosionForce(10000, transform.position, transform.localScale.x*3, 0.0F);
				hit.gameObject.GetComponent<Damage>().TakeDamage(50/(hit.transform.position - transform.position).magnitude);
			}
			
		}
		yield return new WaitForSeconds (1.5f);
	}

	public IEnumerator shaped() {
		// Send bricks away from the center of the explosion in the path they already lie on
		var p = transform.position;
		p.z = 0.2f;
		transform.position = p;
		Cone c = transform.FindChild ("Cone").GetComponent<Cone> ();
		c.collider.isTrigger = true;
		c.collider.enabled = true;
		Transform building = GameObject.Find ("Building").transform;
		Ray diff;
		Vector3 force;
		Vector3 size;
		foreach (Transform brick in building) {
			// Minimize edge effects
			size = brick.GetComponent<BoxCollider>().size;
			brick.GetComponent<BoxCollider>().size = new Vector3(1/10f, 1/10f, 1/10f);
			if (brick.collider.bounds.Intersects(c.collider.bounds)) {
				diff = new Ray(transform.position, brick.transform.position-transform.position);
				force = diff.GetPoint (100)/(brick.transform.position - transform.position).magnitude;
				brick.rigidbody.AddForce(force, ForceMode.Impulse);
				brick.GetComponent<Damage>().TakeDamage(force.magnitude);
			}
			// Restore original size
			brick.GetComponent<BoxCollider>().size = size;
		}
		yield return new WaitForSeconds (1.5f);
	}

	public IEnumerator vert() {
		// Explode up/down, throwing bricks away parallel to each other
		var p = transform.position;
		p.z = 0.2f;
		transform.position = p;
		var s = transform.lossyScale;
		s.y *= 2;
		transform.localScale = s;
		Transform building = GameObject.Find ("Building").transform;
		Quaternion theta;
		float dist;
		int dir;
		foreach (Transform brick in building) {
			if (brick.collider.bounds.Intersects(collider.bounds)) {
				// Keep original rotation so we can restore it later
				theta = brick.transform.rotation;
				// Don't collide due to rotation/position changing during this calculatoin
				brick.collider.enabled = false;
				// Rotate to be in the same coordinate system as the bomb
				brick.transform.rotation = transform.rotation;
				// Further away means weaker
				dist = Vector3.Distance(brick.transform.position, transform.position);
				// Also, difference between original distance . . .
				brick.transform.Translate(Vector3.up, Space.Self);
				// . . . and distance when brick is moved up tells us which direction we should throw the brick
				if (dist < Vector3.Distance(brick.transform.position, transform.position)) dir = 1;
				else dir = -1;
				// We don't care which side is up - only the angle of rotation matters
				if (transform.rotation.eulerAngles.z > 90 && transform.rotation.eulerAngles.z < 360-90) dir *= -1;
				// Restore the brick's position
				brick.transform.Translate(Vector3.down, Space.Self);
				// Throw the brick
				brick.rigidbody.AddForce((30-dist)*brick.transform.up*dir, ForceMode.Impulse);
				// Restore the brick's rotation
				brick.transform.rotation = theta;
				// Re-enable collisions
				brick.collider.enabled = true;
				// Apply damage
				brick.GetComponent<Damage>().TakeDamage(30-dist);
			}
		}
		yield return new WaitForSeconds (1.5f);
	}
}
