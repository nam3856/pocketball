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
    }
    private void OnCollisionEnter(Collision collision)
    {
        float volume = Mathf.Clamp(BallRigidbody.velocity.magnitude / 5.0f, 0.1f, 1.0f);

        if (collision.gameObject.CompareTag("SolidBall") || collision.gameObject.CompareTag("StripedBall") || collision.gameObject.CompareTag("CueBall"))
        {
            int index = UnityEngine.Random.Range(0, BallHitBall.Length);
            BallClip = BallHitBall[index];
            audioSource.clip = BallClip;
            audioSource.volume = volume;
            audioSource.Play();
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            audioSource.clip = BallHitWall[0];
            audioSource.volume = volume;
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
        if (other.CompareTag("Floor") && isOnTable)
        {
            Debug.Log($"{name}ÀÌ ¹Ù´Ú¿¡ ¶³¾îÁü");
            if (!CompareTag("CueBall"))
            {
                transform.position = new(0f, 0.5f, 0f);
            }
            else
            {
                gameManager.freeBall = true;
                gameManager.hasExtraTurn = false;
            }
        }
    }

    public bool IsBallStopped()
    {
        if (transform.position.y <= -1f) return true;
        if (BallRigidbody.angularVelocity.magnitude < 1f && BallRigidbody.velocity.magnitude < 0.1f) BallRigidbody.angularVelocity = Vector2.zero;
        return BallRigidbody.velocity.magnitude < 0.1f && BallRigidbody.angularVelocity.magnitude < 0.1f;
    }

    private IEnumerator ByeBye(Collider other)
    {
        int index = UnityEngine.Random.Range(0, BallFall.Length);
        BallClip = BallFall[index];
        audioSource.clip = BallClip;
        audioSource.Play();
        yield return new WaitForSeconds(0.3f);
        transform.position = new(5.475f, -1, 0.805f);
        if(CompareTag("CueBall")) isOnTable = true;
    }

}
