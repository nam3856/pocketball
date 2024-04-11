using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BallController : MonoBehaviour
{
    public Rigidbody BallRigidbody;
    private GameManager gameManager;
    private bool isOnTable = true;
    
    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hole") && isOnTable)
        {
            isOnTable = false;
            Debug.Log($"{name}ÀÌ ±¸¸Û¿¡ ºüÁü");
            StartCoroutine(ByeBye(other));
            gameManager.BallFell(gameObject);
        }
    }

    private IEnumerator ByeBye(Collider other)
    {
        yield return new WaitForSeconds(0.3f);
        gameObject.GetComponent<SphereCollider>().enabled = false;
        gameObject.GetComponent<Rigidbody>().mass = 0.1f;
        yield return new WaitForSeconds(0.3f);
        gameObject.GetComponent<SphereCollider>().enabled = true;
        gameObject.GetComponent<Rigidbody>().mass = 1f;
        //Destroy(other.gameObject);

    }

    private void Update()
    {
        if(transform.position.y<=-10f) 
        {
            transform.position = new (0, -1, 0);
            gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }
}
