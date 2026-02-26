using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePoolManager : MonoBehaviour
{
    // 싱글톤 패턴으로 어디서든 쉽게 접근 가능하도록 설정
    public static ParticlePoolManager Instance;

    [Header("풀링 설정")]
    public GameObject particlePrefab;
    public int initialPoolSize = 10;

    [Header("파티클 이펙트 설정")]
    public float particleScaleMultiplier = 2.0f; // 초기 크기 배율 (2배)
    public float particleWaitTime = 1.0f;        // 줄어들기 전 대기 시간 (1초)
    public float particleShrinkTime = 0.5f;      // 크기가 줄어드는데 걸리는 시간 (0.5초)

    // 파티클 오브젝트와 자식들의 원래 스케일을 저장하는 데이터 클래스
    private class ParticleData
    {
        public GameObject ParticleObj;
        public Transform[] Children;
        public Vector3[] OriginalScales;
    }

    private Queue<ParticleData> poolQueue = new Queue<ParticleData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            poolQueue.Enqueue(CreateNewParticle());
        }
    }

    ParticleData CreateNewParticle()
    {
        GameObject obj = Instantiate(particlePrefab, transform);
        obj.SetActive(false); // 처음에는 비활성화 상태로 둡니다.

        int childCount = obj.transform.childCount;
        ParticleData data = new ParticleData
        {
            ParticleObj = obj,
            Children = new Transform[childCount],
            OriginalScales = new Vector3[childCount]
        };

        // 자식들의 원래 스케일을 기억해 둡니다.
        for (int i = 0; i < childCount; i++)
        {
            data.Children[i] = obj.transform.GetChild(i);
            data.OriginalScales[i] = data.Children[i].localScale;
        }

        return data;
    }

    // 외부(플레이어 스크립트 등)에서 호출할 파티클 소환 함수
    public void SpawnParticle(Vector3 position, Quaternion rotation)
    {
        if (poolQueue.Count == 0)
        {
            poolQueue.Enqueue(CreateNewParticle()); // 풀이 비어있으면 추가로 생성
        }

        ParticleData data = poolQueue.Dequeue();

        // 위치와 회전을 설정하고 활성화
        data.ParticleObj.transform.position = position;
        data.ParticleObj.transform.rotation = rotation;
        data.ParticleObj.SetActive(true);

        // 연출 및 회수 코루틴 시작
        StartCoroutine(ShrinkAndReturnParticle(data));
    }

    IEnumerator ShrinkAndReturnParticle(ParticleData data)
    {
        // 1. 배율 적용해서 스케일 키우기
        for (int i = 0; i < data.Children.Length; i++)
        {
            data.Children[i].localScale = data.OriginalScales[i] * particleScaleMultiplier;
        }

        // 2. 지정된 시간만큼 대기
        yield return new WaitForSeconds(particleWaitTime);

        float timeElapsed = 0f;
        Vector3[] startScales = new Vector3[data.Children.Length];
        for (int i = 0; i < data.Children.Length; i++)
        {
            startScales[i] = data.Children[i].localScale;
        }

        // 3. 서서히 크기를 0으로 줄이기
        while (timeElapsed < particleShrinkTime)
        {
            timeElapsed += Time.deltaTime;
            float progress = timeElapsed / particleShrinkTime;

            for (int i = 0; i < data.Children.Length; i++)
            {
                data.Children[i].localScale = Vector3.Lerp(startScales[i], Vector3.zero, progress);
            }

            yield return null;
        }

        // 4. 원래 크기로 복구 (다음에 풀에서 꺼내 쓸 때를 대비)
        for (int i = 0; i < data.Children.Length; i++)
        {
            data.Children[i].localScale = data.OriginalScales[i];
        }

        // 5. 비활성화 후 큐에 반환 (Destroy를 하지 않습니다!)
        data.ParticleObj.SetActive(false);
        poolQueue.Enqueue(data);
    }
}