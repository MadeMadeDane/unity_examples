using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {

	private Rigidbody rb;
	private int count;
	public float moveForce = 50;
    public float maxVelocity = 10;
	public Text countText;
	public Text winText;

	void Start ()
	{
		rb = GetComponent<Rigidbody>();
		count = 0;
		changeCountText();
		winText.text = "";
	}
	void FixedUpdate ()
	{
		float moveHorizontal = Input.GetAxis("Horizontal");
		float moveVerticle = Input.GetAxis ("Vertical");
		Vector3 movement = new Vector3 (moveHorizontal, 0.0f, moveVerticle);
        Vector3 newVelocity = rb.velocity + (movement * (moveForce * Time.deltaTime / rb.mass));
        if (newVelocity.magnitude < maxVelocity) {
            rb.AddForce(movement * moveForce);
        } else {
            rb.velocity = newVelocity.normalized * maxVelocity;
        } 
	}
	void OnTriggerEnter(Collider other) {
		if (other.gameObject.CompareTag ("collectable")) {
			other.gameObject.SetActive (false);
			count++;
			changeCountText();
		}
	}
	void changeCountText () {
		countText.text = "Count: " + count.ToString();
		if (count >= 4) {
			winText.text = "You Win";
		}
	}
}
