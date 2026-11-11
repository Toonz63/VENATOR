using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Grim behavior: wander near home -> chase the Venator once seen -> stop and TELEGRAPH for 0.5 s
/// (red light + rear-back animation) -> ATTACK -> recover. A whip lash during the telegraph
/// interrupts it and staggers the Grim.
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Wander, Chase, Telegraph, Recover, Stagger, Banished }

    [Header("Senses")]
    public float sightRange = 16f;
    public float loseInterestRange = 24f;
    public float eyeHeight = 1.4f;

    [Header("Movement")]
    public float wanderRadius = 6f;
    public float wanderSpeed = 1.6f;
    public float chaseSpeed = 4.2f;

    [Header("Attack")]
    public float attackRange = 2.2f;
    public float hitRange = 2.9f;
    [Range(0f, 180f)] public float hitAngle = 70f;
    public float telegraphTime = 0.5f;
    public float recoverTime = 0.7f;
    public float attackCooldown = 1.2f;
    public float damage = 15f;
    public float staggerTime = 0.8f;

    [Header("Telegraph cue")]
    public Light telegraphLight;
    public float telegraphLightIntensity = 4f;

    public State CurrentState { get; private set; } = State.Wander;

    NavMeshAgent agent;
    EnemyHealth health;
    Animator anim;
    Transform player;
    HealthSystem playerHealth;
    Vector3 home;
    float stateTimer, nextAttackTime;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int TelegraphHash = Animator.StringToHash("Telegraph");
    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int StaggerHash = Animator.StringToHash("Stagger");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        anim = GetComponentInChildren<Animator>();
        health.OnBanished += () => Enter(State.Banished);
        if (telegraphLight != null) telegraphLight.intensity = 0f;
    }

    void Start()
    {
        home = transform.position;
        var fpc = FindFirstObjectByType<FirstPersonController>();
        if (fpc != null) { player = fpc.transform; playerHealth = fpc.GetComponent<HealthSystem>(); }

        // The NavMesh is built at runtime in Awake (NavMeshBaker), so the agent starts disabled
        // in the prefab and is switched on here, after every Awake has run.
        agent.enabled = true;
        if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas))
            agent.Warp(hit.position);
        PickWanderPoint();
    }

    bool AgentReady => agent.enabled && agent.isOnNavMesh;
    bool AnimReady => anim != null && anim.runtimeAnimatorController != null;

    void Update()
    {
        if (CurrentState == State.Banished) return;
        if (AnimReady) anim.SetFloat(SpeedHash, AgentReady ? agent.velocity.magnitude : 0f);
        if (!AgentReady || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool playerDead = playerHealth != null && playerHealth.IsDead;
        stateTimer -= Time.deltaTime;

        switch (CurrentState)
        {
            case State.Wander:
                agent.speed = wanderSpeed;
                if (!playerDead && CanSeePlayer(dist)) { Enter(State.Chase); break; }
                if (!agent.pathPending && agent.remainingDistance < 0.5f && stateTimer <= 0f) PickWanderPoint();
                break;

            case State.Chase:
                agent.speed = chaseSpeed;
                if (playerDead || dist > loseInterestRange) { Enter(State.Wander); break; }
                agent.SetDestination(player.position);
                if (dist <= attackRange && Time.time >= nextAttackTime) Enter(State.Telegraph);
                break;

            case State.Telegraph:
                FacePlayer();
                if (stateTimer <= 0f) DoAttack(dist);
                break;

            case State.Recover:
            case State.Stagger:
                if (stateTimer <= 0f) Enter(State.Chase);
                break;
        }
    }

    void Enter(State s)
    {
        CurrentState = s;
        if (telegraphLight != null) telegraphLight.intensity = s == State.Telegraph ? telegraphLightIntensity : 0f;
        if (AgentReady) agent.isStopped = s == State.Telegraph || s == State.Recover || s == State.Stagger || s == State.Banished;

        switch (s)
        {
            case State.Telegraph:
                stateTimer = telegraphTime;
                if (AnimReady) anim.SetTrigger(TelegraphHash);
                break;
            case State.Recover:
                stateTimer = recoverTime;
                break;
            case State.Stagger:
                stateTimer = staggerTime;
                nextAttackTime = Time.time + staggerTime + 0.4f;
                if (AnimReady) anim.SetTrigger(StaggerHash);
                break;
            case State.Wander:
                stateTimer = 0f;
                PickWanderPoint();
                break;
            case State.Banished:
                if (AgentReady) agent.ResetPath();
                break;
        }
    }

    void DoAttack(float dist)
    {
        if (AnimReady) anim.SetTrigger(AttackHash);
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        bool inFront = Vector3.Angle(transform.forward, toPlayer) <= hitAngle;
        if (dist <= hitRange && inFront && playerHealth != null)
            playerHealth.TakeDamage(damage, transform.position + Vector3.up * eyeHeight);
        nextAttackTime = Time.time + attackCooldown;
        Enter(State.Recover);
    }

    /// <summary>Called by the whip. Cancels a telegraphed attack and staggers the Grim.</summary>
    public void Interrupt()
    {
        if (CurrentState == State.Banished) return;
        Enter(State.Stagger);
    }

    bool CanSeePlayer(float dist)
    {
        if (dist > sightRange) return false;
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * 1.2f;
        if (!VenatorPhysics.Raycast(new Ray(eye, (target - eye).normalized), dist + 1f, transform, out RaycastHit hit))
            return false;
        return hit.collider.transform.IsChildOf(player);
    }

    void FacePlayer()
    {
        Vector3 d = player.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 10f * Time.deltaTime);
    }

    void PickWanderPoint()
    {
        stateTimer = Random.Range(1f, 3f); // pause a moment at each point
        if (!AgentReady) return;
        Vector3 random = home + Random.insideUnitSphere * wanderRadius;
        if (NavMesh.SamplePosition(random, out var hit, 2f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
