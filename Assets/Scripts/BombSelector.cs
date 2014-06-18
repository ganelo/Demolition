using UnityEngine;
using System.Collections;

public class BombSelector : MonoBehaviour {

	public enum bombs { imploder=0, shaped, sphere, vert, none };
	public static bombs current = bombs.none;
	public static Sprite[] bombSprites = new Sprite[4];
	public static int clicks;
	public static bool help = true;

	static Bomb current_bomb;
	static float rotAngle;
	static Vector2 pivot;

	void Awake () {
		bombSprites [0] = GameObject.Find ("imploder").GetComponent<SpriteRenderer>().sprite;
		bombSprites [1] = GameObject.Find ("shaped").GetComponent<SpriteRenderer>().sprite;
		bombSprites [2] = GameObject.Find ("sphere").GetComponent<SpriteRenderer>().sprite;
		bombSprites [3] = GameObject.Find ("vert").GetComponent<SpriteRenderer>().sprite;
	}

	void Update () {
		if (clicks <= 0 && Construct.strict) ClearSelection();
	}

	void OnMouseDown() {
		// Disable bomb selection while help screen is active
		if (help) return;
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hitInfo;
		
		if (collider.Raycast (ray, out hitInfo, Mathf.Infinity)) {
			// Click below ground to make/clear selection
			if (hitInfo.point.y < GameObject.Find ("Ground").transform.position.y) {
				// Click in blank space to clear selection
				if (hitInfo.collider.gameObject.name == "Background") {
					ClearSelection();
				} else {
					// Set current
					SendMessage("BS_"+hitInfo.collider.gameObject.name);

					// Swap which bomb is selected by killing the current one and the new one
					if (current_bomb && current_bomb.alive) {
						current_bomb.SendMessage ("Die");
					}
					if (current != bombs.none)
						current_bomb = Bomb.Get(bombSprites[(int)current]);
				}
			}
		}
	}

	void ClearSelection() {
		if (current_bomb && current_bomb.alive) {
			current_bomb.SendMessage ("Die");
		}
		current = bombs.none;
	}

	void BS_help() {
		help = true;
	}

	void BS_go() {
		// Trigger explosions
		GameObject[] all = GameObject.FindGameObjectsWithTag ("Bomb");
		foreach (GameObject b in all) {
			if (b.GetComponent<Bomb>().placed) {
				b.SendMessage ("Explode");
			}
		}
	}

	void BS_imploder() {
		current = bombs.imploder;
	}

	void BS_shaped() {
		current = bombs.shaped;
	}

	void BS_sphere() {
		current = bombs.sphere;
	}

	void BS_vert() {
		current = bombs.vert;
	}

	void OnGUI() {
		if (!help) return;

		GUI.Box (new Rect (Screen.width/2 - 250, Screen.height/4, 500, 245), "Demolition");
		GUILayout.BeginArea(new Rect (Screen.width/2 - 240, Screen.height/4 + 20, 480, 225));
		GUILayout.Label (" - Click on an icon to get a bomb of that kind\n" +
		                 " - Use Left/Right or the Mouse Scrollwheel to rotate the selected bomb\n" +
		                 " - Click above the bar to place a bomb\n" + 
		                 " - Click below the bar to clear the current selection\n" +
		                 " - Click on a placed bomb with no bomb selected to remove that bomb\n" +
		                 " - Click 'Go' to trigger explosions\n" +
		                 " - Bomb types are 'Imploder', 'Sphere', 'Shaped', and 'Vertical'\n" +
		                 "In leveling mode, try to clear the specified bricks before running out of bombs\n" + 
		                 "In Open-ended mode, clear all bricks with infinte bombs to get a new wall");
		if (GUILayout.Button ("Leveling Mode")) {
			help = false;
			if (Construct.level == 0 || !Construct.strict ) {;
				GameObject.Find ("Background").GetComponent<Construct>().StartStrict ();
			}
		}
		if (GUILayout.Button ("Open-ended Mode")) {
			help = false;
			if (Construct.level == 0 || Construct.strict) {
				GameObject.Find ("Background").GetComponent<Construct>().StartOpen ();
			}
		}
		if (GUILayout.Button ("Quit")) {
			Application.Quit();
		}

		GUILayout.EndArea ();
	}
}
