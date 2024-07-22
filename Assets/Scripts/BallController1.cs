using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BallController : MonoBehaviour
{
    public AudioClip[] BallHitBall;
    public AudioClip[] BallHitWall;
    public AudioClip[] BallFall;
    private AudioClip BallClip;
    public Rigidbody BallRigidbody;
    private GameManager gameManager;
    private bool isOnTable = true;
    public AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        gameManager = FindObjectOfType<GameManager>();
        Debug.Log((int)UnityEngine.Random.Range(0, BallHitBall.Length));

    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("SolidBall") || collision.gameObject.CompareTag("StripedBall") || collision.gameObject.CompareTag("CueBall"))
        {
            int index = UnityEngine.Random.Range(0, BallHitBall.Length);
            BallClip = BallHitBall[index];
            audioSource.clip = BallClip;
            audioSource.Play();
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            int index = 0;
            audioSource.clip = BallHitWall[0];
            audioSource.Play();
        }
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
        int index = UnityEngine.Random.Range(0, BallFall.Length);
        BallClip = BallFall[index];
        audioSource.clip = BallClip;
        audioSource.Play();
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
        if (transform.position.y <= -10f)
        {
            transform.position = new(0, -1, 0);
            gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }
}
