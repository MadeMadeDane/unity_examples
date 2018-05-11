using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {

	private Rigidbody rb;
	private int count;
    private int jumpCount;
    public float moveForce = 50;
    public float maxVelocity = 10;
    public float jumpForce = 200;
    public int maxJumpCount = 3;
    public bool bouncyBoi = false;
    public bool seeYa = false;

    public Text countText;
	public Text winText;

	void Start ()
	{
		rb = GetComponent<Rigidbody>();
		count = 0;
        jumpCount = maxJumpCount;
		changeCountText();
		winText.text = "";
        rb.freezeRotation = true;
	}
	void FixedUpdate ()
	{
		float moveHorizontal = Input.GetAxis("Horizontal");
		float moveVerticle = Input.GetAxis ("Vertical");
		Vector3 movement = new Vector3 (moveHorizontal, 0.0f, moveVerticle);
        Vector3 ignore = new Vector3(0, rb.velocity.y, 0);
        Vector3 newXZVelocity = rb.velocity + (movement * (moveForce * Time.deltaTime / rb.mass)) - ignore;
        Vector3 newVelocity = rb.velocity + (movement * (moveForce * Time.deltaTime / rb.mass));

        if (newXZVelocity.magnitude < maxVelocity) {
            rb.AddForce(movement * moveForce);
        } else {
        }

    }

    private void Update()
    {
        Debug.DrawRay(transform.position,transform.up*2,Color.blue,0,false);
        if (Input.GetKeyDown(KeyCode.Space) && jumpCount > 0) {
            Vector3 jumpVector = jumpForce * transform.up;
            rb.AddForce(jumpVector, ForceMode.VelocityChange);
            jumpCount--;
        }
    }
    void OnTriggerEnter(Collider other) {
		if (other.gameObject.CompareTag ("collectable")) {
			other.gameObject.SetActive (false);
			count++;
			changeCountText();
		}
	}
    private void OnCollisionEnter(Collision other)
    {
        if (other.contacts.Length > 0) {
            var norm = other.contacts[0].normal;
            print(norm.y);
            Debug.DrawRay(other.contacts[0].point, norm * 2,Color.green,20);
            if(bouncyBoi) rb.AddForce(norm*jumpForce,ForceMode.Impulse);
            if (seeYa) rb.AddForce(Random.rotation * norm * Random.Range(0, 9000));
             if (other.contacts[0].normal.y > 1 / 2) {
                jumpCount = maxJumpCount;

            }
        }
    }
    void changeCountText () {
		countText.text = "Count: " + count.ToString();
		if (count >= 4) {
			winText.text = "You Win";
		}
	}
}
