using UnityEngine;

public class MovementParticle : MonoBehaviour
{
    [Header("Settings")]
    public ParticleSystem trailParticles; // 파티클 시스템 연결
    public float minVelocity = 0.1f;       // 0으로 하면 미세한 움직임에도 반응하므로 약간 올리는 것 추천

    private Rigidbody rb;
    private Vector3 lastPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastPosition = transform.position;

        if (trailParticles != null)
        {
            // 1. 파티클이 사라지지 않도록 수명을 무한으로 설정
            var main = trailParticles.main;
            main.startLifetime = Mathf.Infinity; // 혹은 99999f 같이 아주 큰 수

            // 2. 파티클이 플레이어를 따라오지 않고 월드에 남도록 설정 (중요!)
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            // 시작 시 방출 멈춤
            var emission = trailParticles.emission;
            emission.enabled = false;
        }
    }

    void Update()
    {
        if (trailParticles == null) return;

        // 속도 계산 (Unity 버전에 따라 velocity 또는 linearVelocity 사용)
        float speed = 0f;
        if (rb != null)
        {
            // Unity 6 이상에서는 linearVelocity, 이전 버전은 velocity
            // 오류가 나면 rb.velocity로 변경하세요.
            speed = rb.linearVelocity.magnitude;
        }
        else
        {
            speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
            lastPosition = transform.position;
        }

        var emission = trailParticles.emission;

        // 움직일 때만 파티클 생성
        if (speed > minVelocity)
        {
            emission.enabled = true;
        }
        else
        {
            // 중요: 멈추면 생성을 꺼야 합니다. 
            // 안 끄면 제자리에 무한한 파티클이 쌓여서 금방 끊깁니다.
            emission.enabled = false;
        }
    }
}