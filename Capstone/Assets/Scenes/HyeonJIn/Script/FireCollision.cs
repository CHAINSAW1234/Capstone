using UnityEngine;

public class FireCollision : MonoBehaviour
{
    private ParticleSystem particle;
    private ParticleSystem.Particle[] particleArray;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        particle = GetComponent<ParticleSystem>();
        particle.GetParticles(particleArray);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnParticleCollision(GameObject other)
    {
        var startSize = particle.main.startSize;
        startSize.constantMin -= Time.deltaTime * 0.5f;
        startSize.constantMax -= Time.deltaTime;

        int aliveCount = particle.GetParticles(particleArray);
        for(int i=0;i<aliveCount; ++i)
        {
            particleArray[i].startSize -= Time.deltaTime;
        }

        if(startSize.constantMax < 0.01)
        {
            gameObject.SetActive(false);
        }
    }
}
