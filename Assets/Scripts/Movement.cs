using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{   
    [SerializeField] InputAction thrust;
    [SerializeField] InputAction rotation;
    [SerializeField] float thrustStrength = 1000f;
    [SerializeField] float rotationStrength = 100f;
    [SerializeField] AudioClip mainEngineSFX;
    [SerializeField] ParticleSystem mainEngineParticles;
    [SerializeField] ParticleSystem rightThrustParticles;
    [SerializeField] ParticleSystem leftThrustParticles;

    Rigidbody rb;
    AudioSource audioSource;

    // UI Buttons state
    bool isThrustingUI = false;
    float rotationUI = 0f;

    private void Start() 
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();    
    }

    private void OnEnable() 
    {
        thrust.Enable(); 
        rotation.Enable();
    }

    private void OnDisable()
    {
        // Crash/Finish da o'chirilganda gaz va burilish holatini tozalaymiz
        isThrustingUI = false;
        rotationUI = 0f;
        if (mainEngineParticles != null) mainEngineParticles.Stop();
        if (rightThrustParticles != null) rightThrustParticles.Stop();
        if (leftThrustParticles != null) leftThrustParticles.Stop();
    }

    private void FixedUpdate()
    {
        ProcessThrust();
        ProcessRotation();
    }

    // UI orqali chaqiriladigan metodlar
    public void SetThrustUI(bool state) => isThrustingUI = state;
    public void SetRotateLeftUI(bool state) => rotationUI = state ? -1f : (rotationUI < 0 ? 0f : rotationUI);
    public void SetRotateRightUI(bool state) => rotationUI = state ? 1f : (rotationUI > 0 ? 0f : rotationUI);

    private void ProcessThrust()
    {
        if (thrust.IsPressed() || isThrustingUI)
        {
            StartThrusting();
        }
        else
        {
            StopThrusting();
        }
    }

    private void StartThrusting()
    {
        rb.AddRelativeForce(Vector3.up * thrustStrength * Time.fixedDeltaTime);
        if (mainEngineSFX != null && !audioSource.isPlaying)
        {
            // Dvigatel ovozi uzluksiz aylanadi (bo'lak oxiri boshiga silliq ulanadi)
            audioSource.clip = mainEngineSFX;
            audioSource.loop = true;
            audioSource.Play();
        }
        if (mainEngineParticles != null && !mainEngineParticles.isPlaying)
        {
            mainEngineParticles.Play();
        }
    }

    private void StopThrusting()
    {
        audioSource.Stop();
        if (mainEngineParticles != null) mainEngineParticles.Stop();
    }

    private void ProcessRotation()
    {
        float rotationInput = rotation.ReadValue<float>();
        if (rotationUI != 0) rotationInput = rotationUI;

        if (rotationInput < 0)
        {
            RotateRight();
        }
        else if (rotationInput > 0)
        {
            RotateLeft();
        }
        else
        {
            StopRotating();
        }
    }

    private void RotateRight()
    {
        ApplyRotation(rotationStrength);
        if (rightThrustParticles != null && !rightThrustParticles.isPlaying)
        {
            if (leftThrustParticles != null) leftThrustParticles.Stop();
            rightThrustParticles.Play();
        }
    }

    private void RotateLeft()
    {
        ApplyRotation(-rotationStrength);
        if (leftThrustParticles != null && !leftThrustParticles.isPlaying)
        {
            if (rightThrustParticles != null) rightThrustParticles.Stop();
            leftThrustParticles.Play();
        }
    }

    private void StopRotating()
    {
        if (rightThrustParticles != null) rightThrustParticles.Stop();
        if (leftThrustParticles != null) leftThrustParticles.Stop();
    }

    private void ApplyRotation(float rotationThisFrame)
    {
        rb.freezeRotation = true;
        transform.Rotate(Vector3.forward * rotationThisFrame * Time.fixedDeltaTime);
        rb.freezeRotation = false;
    }
}
