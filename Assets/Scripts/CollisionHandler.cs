using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
 
public class CollisionHandler : MonoBehaviour
{   
  [SerializeField] float levelLoadDelay = 2f;
  // Raketa shu balandlikdan pastga tushib ketsa (platformadan tushib ketdi), game over bo'ladi
  [SerializeField] float fallLimitY = -8f;
  [SerializeField] AudioClip successSFX;
  [SerializeField] AudioClip crashSFX;
  [SerializeField] ParticleSystem successParticles;
  [SerializeField] ParticleSystem crashParticles;
  
  AudioSource audioSource;
 
  bool isControllable = true;
  bool isCollidable = true;
 
  private void Start() 
  {
    audioSource = GetComponent<AudioSource>();
  }
  
  private void Update() 
  {
    RespondToDebugKeys();
    CheckFall();
  }
 
  void CheckFall()
  {
    if (isControllable && transform.position.y < fallLimitY)
    {
      StartCrashSequence();
    }
  }

  void RespondToDebugKeys()
  {
    if (Keyboard.current == null) { return; } // telefonda klaviatura yo'q
    if (Keyboard.current.lKey.wasPressedThisFrame)
    {
      LoadNextLevel();
    }
    else if (Keyboard.current.cKey.wasPressedThisFrame)
    {
      isCollidable = !isCollidable;
    }
  }
 
  private void OnCollisionEnter(Collision other) 
  {
    if (!isControllable || !isCollidable) { return; }
    
    switch (other.gameObject.tag)
    {
      case "Friendly":
        Debug.Log("Everything is looking good!");
        break;
      case "Finish":
        StartSuccessSequence();
        break;
      default:
        StartCrashSequence();
        break;
    }
  }
 
  void StartSuccessSequence()
  {
    isControllable = false;
    audioSource.Stop();
    audioSource.PlayOneShot(successSFX);
    successParticles.Play();
    GetComponent<Movement>().enabled = false;
    Invoke("LoadNextLevel", levelLoadDelay);
  }
 
  void StartCrashSequence()
  {
    isControllable = false;
    audioSource.Stop();
    audioSource.PlayOneShot(crashSFX);
    crashParticles.Play();
    GetComponent<Movement>().enabled = false;
    Invoke("ShowGameOver", levelLoadDelay);
  }

  void ShowGameOver()
  {
    var ads = AdsManager.Instance;
    bool isFirstLevel = SceneManager.GetActiveScene().buildIndex == 0;

    if (isFirstLevel)
    {
      GameOverUI.Show("GAME OVER", "Level 1 da urildingiz",
        ("Reklama ko'rib keyingi levelga o'tish", () => ads.ShowRewarded(LoadNextLevel)),
        ("Boshidan boshlash", ReloadLevel));
    }
    else
    {
      GameOverUI.Show("GAME OVER", "Qayta o'ynash uchun reklama ko'ring",
        ("Reklama ko'rib qayta o'ynash", () => ads.ShowRewarded(ReloadLevel)));
    }
  }
 
  void LoadNextLevel()
  {
    int currentScene = SceneManager.GetActiveScene().buildIndex;
    int nextScene = currentScene + 1;
    
    if (nextScene == SceneManager.sceneCountInBuildSettings)
    {
      nextScene = 0;
    }
    
    SceneManager.LoadScene(nextScene);
  }
 
  void ReloadLevel()
  {
    int currentScene = SceneManager.GetActiveScene().buildIndex;
    SceneManager.LoadScene(currentScene);
  }
 
}