using UnityEngine;

public class SharkBehavior : MonoBehaviour
{
    [Header("行为状态")]
    public bool isChasing = false;
    public float currentSpeed = 0f;
    public SharkMovementType movementType = SharkMovementType.AutoDetect;

    [Header("移动设置")]
    public float speed = 3f;
    public float chaseDistance = 100f;
    public float wanderSpeed = 1f;
    public float rotationSpeed = 2f;
    public float maxChaseAngle = 30f; // 最大追逐角度偏移

    [Header("音频设置")]
    public AudioClip chaseSound;
    public AudioClip attackSound;
    public bool loopChaseSound = true;
    public float maxSoundDistance = 50f;
    public float minSoundVolume = 0.1f;
    public float maxSoundVolume = 1f;

    [Header("销毁设置")]
    public Camera targetCamera;
    public float destroyMargin = 2f; // 屏幕外多少距离后销毁

    public enum SharkMovementType
    {
        AutoDetect,
        Horizontal,
        Vertical,
        Free
    }

    private Transform player;
    private Vector3 wanderDirection;
    private float wanderChangeTime;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool hasPlayedAttackSound = false;
    private Vector3 initialForward;

    void Start()
    {
        player = FindObjectOfType<RipplePushEffect>()?.transform;
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D音效
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        InitializeMovementType();
        InitializeWander();
    }

    void InitializeMovementType()
    {
        if (movementType == SharkMovementType.AutoDetect)
        {
            // 根据贴图朝向自动检测移动类型
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                // 检查贴图的宽高比来判断朝向
                float aspectRatio = spriteRenderer.sprite.rect.width / spriteRenderer.sprite.rect.height;
                if (aspectRatio > 1.2f)
                {
                    movementType = SharkMovementType.Horizontal;
                }
                else if (aspectRatio < 0.8f)
                {
                    movementType = SharkMovementType.Vertical;
                }
                else
                {
                    movementType = SharkMovementType.Free;
                }
            }
            else
            {
                movementType = SharkMovementType.Free;
            }
        }

        // 设置初始朝向
        initialForward = transform.up;
    }

    void InitializeWander()
    {
        switch (movementType)
        {
            case SharkMovementType.Horizontal:
                wanderDirection = Random.value > 0.5f ? Vector3.right : Vector3.left;
                break;
            case SharkMovementType.Vertical:
                wanderDirection = Random.value > 0.5f ? Vector3.up : Vector3.down;
                break;
            case SharkMovementType.Free:
                wanderDirection = Random.insideUnitCircle.normalized;
                break;
        }
        
        wanderChangeTime = Time.time + Random.Range(2f, 5f);
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 检查是否在摄像机视野外
        if (IsOutsideCameraView())
        {
            DestroyShark();
            return;
        }

        if (distanceToPlayer <= chaseDistance)
        {
            if (!isChasing)
            {
                StartChasing();
            }
            ChasePlayer();
        }
        else
        {
            if (isChasing)
            {
                StopChasing();
            }
            Wander();
        }

        UpdateVisuals();
        UpdateAudio();
    }

    void StartChasing()
    {
        isChasing = true;
        currentSpeed = speed;
        
        // 播放追逐音效
        if (chaseSound != null)
        {
            if (loopChaseSound)
            {
                audioSource.clip = chaseSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            else
            {
                audioSource.PlayOneShot(chaseSound);
            }
        }
    }

    void StopChasing()
    {
        isChasing = false;
        currentSpeed = wanderSpeed;
        
        // 停止循环音效
        if (loopChaseSound)
        {
            audioSource.Stop();
        }
        
        hasPlayedAttackSound = false;
    }

    void ChasePlayer()
    {
        Vector3 toPlayer = (player.position - transform.position).normalized;
        
        // 根据移动类型限制追逐方向
        Vector3 chaseDirection = GetConstrainedChaseDirection(toPlayer);
        
        transform.position += chaseDirection * speed * Time.deltaTime;
        
        // 平滑面向移动方向
        float angle = Mathf.Atan2(chaseDirection.y, chaseDirection.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle - 90f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // 检查攻击距离
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < 10f && !hasPlayedAttackSound && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
            hasPlayedAttackSound = true;
        }
    }

    Vector3 GetConstrainedChaseDirection(Vector3 desiredDirection)
    {
        switch (movementType)
        {
            case SharkMovementType.Horizontal:
                return new Vector3(Mathf.Clamp(desiredDirection.x, -1f, 1f), 0f, 0f).normalized;
                
            case SharkMovementType.Vertical:
                return new Vector3(0f, Mathf.Clamp(desiredDirection.y, -1f, 1f), 0f).normalized;
                
            case SharkMovementType.Free:
                // 限制角度偏移
                float angle = Vector3.SignedAngle(initialForward, desiredDirection, Vector3.forward);
                float constrainedAngle = Mathf.Clamp(angle, -maxChaseAngle, maxChaseAngle);
                return Quaternion.Euler(0, 0, constrainedAngle) * initialForward;
                
            default:
                return desiredDirection;
        }
    }

    void Wander()
    {
        // 定期改变漫游方向
        if (Time.time >= wanderChangeTime)
        {
            switch (movementType)
            {
                case SharkMovementType.Horizontal:
                    wanderDirection = Random.value > 0.5f ? Vector3.right : Vector3.left;
                    break;
                case SharkMovementType.Vertical:
                    wanderDirection = Random.value > 0.5f ? Vector3.up : Vector3.down;
                    break;
                case SharkMovementType.Free:
                    wanderDirection = Random.insideUnitCircle.normalized;
                    break;
            }
            wanderChangeTime = Time.time + Random.Range(2f, 5f);
        }

        transform.position += wanderDirection * wanderSpeed * Time.deltaTime;

        // 缓慢旋转
        float wanderAngle = Mathf.Atan2(wanderDirection.y, wanderDirection.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, wanderAngle - 90f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 0.5f * Time.deltaTime);
    }

    void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            // 追逐时颜色变红
            spriteRenderer.color = isChasing ? Color.Lerp(Color.white, Color.red, 0.3f) : Color.white;
        }
    }

    void UpdateAudio()
    {
        if (isChasing && loopChaseSound && audioSource.isPlaying)
        {
            // 根据距离调整音量
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            float volume = Mathf.Lerp(maxSoundVolume, minSoundVolume, distanceToPlayer / maxSoundDistance);
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }

    bool IsOutsideCameraView()
    {
        if (targetCamera == null) return false;

        Vector3 viewportPoint = targetCamera.WorldToViewportPoint(transform.position);
        
        // 检查是否在屏幕外（加上边距）
        return viewportPoint.x < -destroyMargin || viewportPoint.x > 1 + destroyMargin ||
               viewportPoint.y < -destroyMargin || viewportPoint.y > 1 + destroyMargin;
    }

    void DestroyShark()
    {
        Debug.Log($"🦈 鲨鱼离开视野，销毁: {gameObject.name}");
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            RipplePushEffect player = collision.GetComponent<RipplePushEffect>();
            if (player != null)
            {
                Debug.Log("💀 玩家被鲨鱼攻击！游戏结束");
                
                // 播放攻击音效
                if (attackSound != null)
                {
                    audioSource.PlayOneShot(attackSound);
                }
                
                // 触发游戏结束逻辑
                SceneTransitionManager transitionManager = FindObjectOfType<SceneTransitionManager>();
                if (transitionManager != null)
                {
                    transitionManager.ForceTransition();
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                }
            }
        }
    }

    // 调试信息
    void OnDrawGizmosSelected()
    {
        // 绘制追逐范围
        Gizmos.color = isChasing ? Color.red : Color.gray;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        // 绘制移动方向
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.up * 3f);

        // 绘制移动限制
        if (movementType != SharkMovementType.Free)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, initialForward * 5f);
            
            if (movementType == SharkMovementType.Horizontal)
            {
                Gizmos.DrawLine(transform.position - Vector3.up * 2f, transform.position + Vector3.up * 2f);
            }
            else if (movementType == SharkMovementType.Vertical)
            {
                Gizmos.DrawLine(transform.position - Vector3.right * 2f, transform.position + Vector3.right * 2f);
            }
        }
    }
}