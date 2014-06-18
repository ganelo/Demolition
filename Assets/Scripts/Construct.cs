using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Construct : MonoBehaviour {

	public int width, height;
	public GameObject BrickPrefab;
	public static int score=0, hs=0, strength, level=0;
	public static float ratio, target, achieved;
	public static bool interlocked, strict;

	GUIText score_text, hs_text, click_text, status_text;
	float brick_w, brick_h, last_r = 150f, curr_r;
	List<Brick> bricks;
	Brick curr;
	GameObject building, ground, background;
	bool game_over = false;
	int MAX_FIXED_JOINTS = 150;
	
	void Awake () {  
		score_text = GameObject.Find ("Score").guiText;
		hs_text = GameObject.Find ("HighScore").guiText;
		click_text = GameObject.Find ("Clicks").guiText;
		status_text = GameObject.Find ("Status").guiText;
		building = GameObject.Find ("Building");
		if (building == null) {
			building = new GameObject("Building");
		}
		background = GameObject.Find ("Background");
		ground = GameObject.Find ("Ground");
	}

	void MakeLevel() {
		achieved = 0;
		BombSelector.clicks += Mathf.Max(10 - (int)Mathf.Log (level), 3);
		foreach (GameObject brick in GameObject.FindGameObjectsWithTag("Brick")) {
			brick.GetComponent<Brick>().Die (counts:false);
		}
		foreach (GameObject bomb in GameObject.FindGameObjectsWithTag ("Bomb")) {
			bomb.GetComponent<Bomb>().Die ();
		}
		strength = level;
		ratio = Mathf.Min (0.83f + level * 0.02f, 1f);
		target = 0.7f - 0.05f*Mathf.Log (level);
		interlocked = (level % 2 == 0);
		width = Mathf.Min (Random.Range (5 + level, 10 + level),
		                   (int)(background.transform.localScale.x * 10 / BrickPrefab.transform.localScale.x) - 10);
		height = Mathf.Min (Random.Range (5 + level, 10 + level),
		                    (int)(background.transform.localScale.z * 10 / BrickPrefab.transform.localScale.y) - Mathf.Abs ((int)ground.transform.position.y) - 5);
		Build (new Vector3(ground.transform.position.x,
		                   ground.transform.position.y + height/2f + 0.5f,
		                   0),
		       width,
		       height,
		       strength,
		       interlocked,
		       ratio);
	}

	void Build(Vector3 center, int w, int h, int strength, bool interlocked, float ratio) {
		// Reinitialize brick container
		bricks = new List<Brick> ();

		// Width/height
		brick_w = BrickPrefab.transform.localScale.x;
		brick_h = BrickPrefab.transform.localScale.y;

		// Left/Bottom edge of wall
		// (<w/h> - 1) * ... b/c we place the bricks using their centers, not their left/bottom edges
		float left_edge = center.x - ((w - 1) * brick_w) / 2f;
		float bottom_edge = center.y - ((h - 1) * brick_h) / 2f;

		// Create bricks, color them
		for (int y = 0; y < h; y++) {
			// Note x <= w to accommodate for rows with an extra brick
			for (int x = 0; x <= w; x++) {
				if (Random.value > ratio) continue;
				if (interlocked && y % 2 != 0) {
					curr = Brick.Get ();
					// Need a half-brick at front and back
					if (x == 0) {
						// First brick in the row - shift left by 1/4 width since we place by center
						curr.transform.position = new Vector3(left_edge + (x - 1/4f) * brick_w,
						                                      bottom_edge + y * brick_h,
						                                      0.1f);
						curr.transform.localScale = new Vector3(brick_w/2f,
						                                        brick_h,
						                                        1);
					} else if (x == w) {
						// Last brick in the row: shift left by 3/4 width
						//     b/c 1/4 width shift in the first half-brick + 1/2 width for the rest of the bricks
						curr.transform.position = new Vector3(left_edge + (x - 3/4f) * brick_w,
						                                      bottom_edge + y * brick_h,
						                                      0.1f);
						curr.transform.localScale = new Vector3(brick_w/2f,
						                                        brick_h,
						                                        1);
					} else {
						// Compensate for the half-brick in front by shifting left by half a brick width
						curr.transform.position = new Vector3(left_edge + (x - 1/2f) * brick_w,
						                                      bottom_edge + y * brick_h,
						                                      0.1f);
						curr.transform.localScale = new Vector3(brick_w,
						                                        brick_h,
						                                        1);
					}
				} else {
					// Only need the extra brick when we have 2 half-bricks instead of 1 full size brick
					if (x == w) break;
					curr = Brick.Get ();
					curr.transform.position = new Vector3(left_edge + x * brick_w,
					                                      bottom_edge + y * brick_h,
					                                      0.1f);
					curr.transform.localScale = new Vector3(brick_w,
					                                        brick_h,
					                                        1);
				}
				bricks.Add (curr);
				curr.transform.parent = building.transform;
				
				// Random shade of red - but not too close or far from the previous brick
				curr_r = Random.Range (100,200);
				while (Mathf.Abs (curr_r-last_r) < 25 || Mathf.Abs (curr_r-last_r) > 75)
					curr_r = Random.Range (100,200);
				last_r = curr_r;
				curr.name = "Brick"+y.ToString ("X")+x.ToString ("X");
				Color col = new Color(curr_r/255f,
				                      30/255f,
				                      30/255f);
				// Use vertex lighting to allow dynamic batching
				Mesh mesh = curr.GetComponent<MeshFilter>().mesh;
				Vector3[] vertices = mesh.vertices;
				Color32[] cols = new Color32[vertices.Length];
				for (int i = 0; i < vertices.Length; i++ ) {
					cols[i] = col;
				}
				mesh.colors32 = cols;
				curr.GetComponent<Damage>().original_color = col;
			}
		}

		// Randomize order of bricks so that when unity optimizes away reciprocal FixedJoints, we aren't left with a systematic bias
		// Fisher-Yates shuffle
		int n = bricks.Count;
		while (n > 1) {  
			n--;  
			int k = Random.Range(0, n + 1);  
			Brick tmp = bricks[k];  
			bricks[k] = bricks[n];  
			bricks[n] = tmp;  
		} 

		// Bind bricks together with FixedJoints of specified strength
		// Don't want to join with background - just with bricks and ground
		// Note that ground doesn't actually have a rigidbody, but that's OK
		//   because FixedJoints with no rigidbody just anchor to the world,
		//   which is basically what we want anyway
		background.collider.enabled = false;
		IEnumerable<Collider> colliders;
		Vector3[] dirs = new Vector3[] {Vector3.up, Vector3.left, Vector3.right, Vector3.down};
		FixedJoint fj;
		foreach (Brick brick in bricks) {
			// Don't want reflexive FixedJoints
			brick.collider.enabled = false;
			colliders = from cols in (from dir in dirs
			                          select Physics.OverlapSphere(brick.transform.position+dir, 0.1f))
				            from col in cols
						    select col;
			foreach (Collider col in colliders) {
				fj = brick.gameObject.AddComponent("FixedJoint") as FixedJoint;
				fj.connectedBody = col.gameObject.rigidbody;
				fj.breakForce = strength;
				fj.breakTorque = strength;
			}
			// Have to wait to re-enable until after query is actually exhausted . . .
			brick.collider.enabled = true;
		}
		background.collider.enabled = true;
		IEnumerable<FixedJoint> fjs = from js in (from brick in bricks
		                                          select brick.GetComponents<FixedJoint> ())
									      from j in js
										      select j;
		// For performance, limit the total number of fixed joints - otherwise, later levels get really laggy really fast
		// In fact, even with this change, late levels are really laggy due to the sheer number of bricks . . .
		//    I adjusted the Max allowed timestep, which helped a lot, but there's still a noticable difference
		// Bricks are already in random order, so we shouldn't have to re-randomize to spread around the removal
		int c = 0;
		foreach (FixedJoint j in fjs) {
			c++;
			if (c > MAX_FIXED_JOINTS)
				Destroy (j);
		}
	}

	public static void AddScore(int amt) {
		score += amt;
		if (score > hs)
			hs = score;
	}
	
	void Update () {
		if (BombSelector.help || game_over) return;
		hs_text.text = "Best Score: " + hs;
		score_text.text = "Score: " + score;
		if (strict) {
			click_text.text = "Remaining bombs: " + BombSelector.clicks;
			if (bricks != null)
				status_text.text = "Achieved: " + achieved + " / " + (int)(bricks.Count() * target);
		} else {
			click_text.text = "";
			status_text.text = "";
		}

		if (GameObject.FindGameObjectsWithTag("Brick").Length == 0) {
			// Open ended play is always on level 4 to avoid performance issues on ever-inreasing levels
			if (strict) level++;
			MakeLevel();
		} else if (strict) {
			float max_v = (from brick in bricks
			               select (brick.rigidbody.velocity + brick.rigidbody.angularVelocity).magnitude
			              ).Max();
			if (max_v < 2) {
				CheckGoal();
			}
		}
	}

	void CheckGoal () {
		// Never done if we still have bombs and haven't reached the goal
		if (BombSelector.clicks > 0 && achieved < (int)(bricks.Count()*target)) return;

		// Never done if there are placed, unexploded bombs
		foreach (Transform bomb in GameObject.Find ("Bombs").transform) {
			if (bomb.GetComponent<Bomb>().alive && bomb.GetComponent<Bomb>().placed) {
				return;
			}
		}

		if (achieved >= (int)(bricks.Count()*target)) {
			level++;
			MakeLevel ();
		} else {
			game_over = true;
		} 
	}

	public void StartStrict() {
		strict = true;
		game_over = false;
		level = 1;
		score = 0;
		BombSelector.clicks = 0;
		MakeLevel();
	}

	public void StartOpen() {
		strict = false;
		game_over = false;
		level = 4;
		score = 0;
		MakeLevel ();
	}

	void OnGUI() {
		if (!game_over) return;

		GUI.Box (new Rect (Screen.width / 2 - 100, Screen.height / 2 - 50, 200, 100), "");
		GUILayout.BeginArea (new Rect (Screen.width / 2 - 40, Screen.height / 2 - 50, 100, 20));
		GUILayout.Label ("Game Over :(");
		GUILayout.EndArea ();
		GUILayout.BeginArea(new Rect (Screen.width / 2 - 90, Screen.height / 2 - 30, 180, 80));
		if (GUILayout.Button ("Play leveling?")) {
			StartStrict ();
		}

		if (GUILayout.Button ("Play Open-ended?")) {
			StartOpen ();
		}

		if (GUILayout.Button ("Quit")) {
			Application.Quit ();
		}

		GUILayout.EndArea ();
	}
}