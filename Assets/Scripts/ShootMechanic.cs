using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

public class ShootMechanic : MonoBehaviour
{
    // Attributes for gun
    public GameObject nozzle;
    public GameObject player;
    
    // Attributes for line rendering
    private LineRenderer laserLine;
    public float laserLength = 10f;
    public float laserDuration = 0.05f;
    
    // Attributes for handling shoot
    private bool _take;
    private bool _give;

    private long timestampOfLastGunHit;
    // Storing GameComponents
    private PlayerController playerControllerComp;
    private TimeBank playerTimeBank;
    private Transform playerTransform;
    public GameObject analytics;
    private AnalyticManager analyticManager;

    private LayerMask interactableMasks;
    
    void Awake()
    {
        interactableMasks = LayerMask.GetMask("Ground", "Object");
        
        laserLine = GetComponent<LineRenderer>();
        timestampOfLastGunHit = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        playerControllerComp = player.GetComponent<PlayerController>();
        playerTimeBank = player.GetComponent<TimeBank>();
        playerTransform = player.GetComponent<Transform>();
        analyticManager = analytics.GetComponent<AnalyticManager>();
    }

    void Update()
    {

        Vector3 nozzlePosition = nozzle.transform.position;
        this._take = Input.GetButtonDown("Fire2");
        this._give = Input.GetButtonDown("Fire1");

        // If any of the buttons clicked
        if (_take || _give)
        {
            string clickType;
            if (_give)
            {
                clickType = "Give";
            }
            else
            {
                clickType = "Take";
            }
            // Shoot a raycast first
            RaycastHit2D hit = Physics2D.Raycast(nozzle.transform.position, transform.TransformDirection(Vector2.right), laserLength, interactableMasks);
            // If no collider hit then show laser yellow till laser length
            AlterColor(laserLine, Color.gray);
            
            // Shot analysis
            var x = Mathf.RoundToInt(transform.position.x);
            var y = Mathf.RoundToInt(transform.position.y);
            var timeStored = playerTimeBank.GetTimeStore();
            var playerAge = playerControllerComp.getAge();
            var currentHealth = playerControllerComp.getHP().GetHP();

            // If raycast hits a collider
            if (hit.collider != null)
            {
                // Track last time player hit a collider 
                timestampOfLastGunHit = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

                if (hit.collider.gameObject.CompareTag("TimeObject"))
                {
                    HandleTimeObject(_take, x, y, timeStored, playerAge, currentHealth,hit.collider.gameObject);
                }
                else if (hit.collider.gameObject.CompareTag("Mirror"))
                {
                    HandleMirrorHit(_take, x, y, timeStored, playerAge, currentHealth,hit.collider.gameObject.name);
                }
                else if (hit.collider.gameObject.CompareTag("BossOneShield"))
                {
                    HandleBossOne(_take, x, y, timeStored, playerAge, currentHealth,hit.collider.gameObject);
                }
                else if (hit.collider.gameObject.CompareTag("Enemy"))
                {
                    HandleEnemy(_take, x, y, timeStored, playerAge, currentHealth,hit.collider.gameObject);
                }
                /*// If Collider hits for subtraction
                if (_take)
                {
                    if (hit.collider.gameObject.CompareTag("TimeObject") && hit.collider.gameObject.GetComponent<TimeObject>().CheckSubtraction() && hit.collider.gameObject.GetComponent<TimeObject>().isActiveAndEnabled)
                    {
                        if (hit.collider.gameObject.GetComponent<CocoonController>() != null)
                        {
                            if (hit.collider.gameObject.GetComponent<CocoonController>().isOpening())
                            {
                                // Don't allow player to shoot case that is opening
                                return;
                            } 
                        }
                        hit.collider.gameObject.GetComponent<TimeObject>().SubtractTime(1);
                        playerTimeBank.AddTime(1);
                        AlterColor(laserLine, Color.red); // Show laser only if it is a time object
                        analyticManager.SendShootInfo(x,y, 0, timeStored, playerAge, currentHealth, clickType, "Take", hit.collider.gameObject.name);
                    }
                    else if (hit.collider.gameObject.CompareTag("Mirror") && playerControllerComp.getAge() > 0 )
                    {
                        playerControllerComp.decreaseAge();
                            playerTimeBank.AddTime(1);
                            AlterColor(laserLine, Color.red);

                            // Shrink the player
                            playerTransform.localScale = playerControllerComp.getPlayerSize();
                            
                            // Shoot Analysis
                            analyticManager.SendShootInfo(x,y, 0, timeStored, playerAge, currentHealth, clickType, "AgeSelf", hit.collider.gameObject.name);
                    }
                    else if (hit.collider.gameObject.CompareTag("BossOneShield"))
                    {
                        playerTimeBank.AddTime(1);
                        AlterColor(laserLine, Color.red); // Show laser only if it is a time object
                        analyticManager.SendShootInfo(x,y, 0, timeStored, playerAge, currentHealth, clickType, "Take", hit.collider.gameObject.name);
                    }
                }
                // If Collider hits for addition
                else if (_give)
                {
                    if (hit.collider.gameObject.CompareTag("TimeObject") && playerTimeBank.CheckSubtract() && hit.collider.gameObject.GetComponent<TimeObject>().CheckAddition() && hit.collider.gameObject.GetComponent<TimeObject>().isActiveAndEnabled )
                    {
                        if ( hit.collider.gameObject.GetComponent<CocoonController>() != null )
                        {
                            if ( hit.collider.gameObject.GetComponent<CocoonController>().isOpening() )
                            {
                                // Don't allow player to shoot case that is opening
                                return;
                            }
                        } 
                        hit.collider.gameObject.GetComponent<TimeObject>().AddTime(1);
                        playerTimeBank.SubtractTime(1);
                        AlterColor(laserLine, Color.green); // Show laser only if it is a time object
                        analyticManager.SendShootInfo(x,y, 0, timeStored, playerAge, currentHealth, clickType, "Give", hit.collider.gameObject.name);
                    }
                    else if (hit.collider.gameObject.CompareTag("Mirror") && playerTimeBank.CheckSubtract() && playerControllerComp.getAge() < 2)
                    {
                            playerControllerComp.increaseAge();
                            playerTimeBank.SubtractTime(1);
                            AlterColor(laserLine, Color.green);

                            // Grow the player
                            playerTransform.localScale = playerControllerComp.getPlayerSize();
                            analyticManager.SendShootInfo(x,y, 0, timeStored, playerAge, currentHealth, clickType, "AgeSelf", hit.collider.gameObject.name);
                    }
                    else if (hit.collider.gameObject.CompareTag("Enemy") && playerTimeBank.CheckSubtract())
                    {
                        hit.collider.gameObject.GetComponent<EnemyController>().Die();
                        playerTimeBank.SubtractTime(1);
                        AlterColor(laserLine, Color.green);
                        analyticManager.SendShootInfo(x,y, 0, timeStored, playerAge, currentHealth, clickType, "AgeEnemy", hit.collider.gameObject.name);
                    }
                }*/
                if (PlayerStatus.rewindUnlocked)
                {
                    
                    // If raycast hits rewind object
                    if (hit.collider.gameObject.CompareTag("RewindObject") && !PlayerStatus.isRewinding)
                    {
                        if (hit.collider.gameObject.GetComponent<FallingRewindObject>() != null)
                        {
                            FallingRewindObject fro = hit.collider.gameObject.GetComponent<FallingRewindObject>();

                            if (fro.isActiveAndEnabled)
                            {
                                fro.Rewind();
                                AlterColor(laserLine, Color.yellow);
                                analyticManager.SendShootInfo(x,y, 0, timeStored, playerAge, currentHealth, clickType, "Rewind", hit.collider.gameObject.name);
                            }
                        }
                        else if (hit.collider.gameObject.GetComponent<SlidingRewindObject>() != null)
                        {
                            SlidingRewindObject sro = hit.collider.gameObject.GetComponent<SlidingRewindObject>();
                            if (sro.isActiveAndEnabled)
                            {
                                sro.Rewind();
                                AlterColor(laserLine, Color.yellow);
                                analyticManager.SendShootInfo(x,y, 0, timeStored, playerAge, currentHealth, clickType, "Rewind", hit.collider.gameObject.name);
                            }
                        }
                        else if (hit.collider.gameObject.GetComponent<RotatingRewindObject>() != null)
                        {
                            RotatingRewindObject rro = hit.collider.gameObject.GetComponent<RotatingRewindObject>();

                            if (rro.isActiveAndEnabled)
                            {
                                rro.Rewind();
                                AlterColor(laserLine, Color.yellow);
                                analyticManager.SendShootInfo(x,y, 0, timeStored, playerAge, currentHealth, clickType,"Rewind", hit.collider.gameObject.name);
                            }
                        }
                    }
                }
                _ShowLaser(nozzlePosition, hit.point);
            }
            else
            { 
                _ShowLaser(nozzlePosition, nozzlePosition + nozzle.transform.right * laserLength);
                analyticManager.SendShootInfo(x,y, 0, timeStored, playerAge, currentHealth, clickType,"Missed", "Nothing");
            }
        }
    }

    public void HandleBossOne(bool take, int xLoc, int yLoc, int storedTime, int agePlayer, int healthPlayer, GameObject hitItem)
    {
        if (take)
        {
            // TODO: IF WE ADD AGE BOSSES TURN THIS INTO A SIMILAR STRUCTURE AS MIRROR AND TIME OBJECT
            playerTimeBank.AddTime(1);
            AlterColor(laserLine, Color.red); // Show laser only if it is a time object
            analyticManager.SendShootInfo(xLoc,yLoc, 0, storedTime, agePlayer, healthPlayer, "Take", "Take", hitItem.name);
        }
    }

    private void HandleEnemy(bool take, int xLoc, int yLoc, int storedTime, int agePlayer, int healthPlayer, GameObject hitItem)
    {
        if (!take && playerTimeBank.CheckSubtract())
        {
            // TODO: IF WE ADD DEAGEING ENEMIES TURN THIS INTO A SIMILAR STRUCTURE AS MIRROR AND TIME OBJECT
            hitItem.GetComponent<EnemyController>().Die();
            playerTimeBank.SubtractTime(1);
            AlterColor(laserLine, Color.green);
            analyticManager.SendShootInfo(xLoc,yLoc, 0, storedTime, agePlayer, healthPlayer, "Give", "AgeEnemy", hitItem.name);
        }
    }

    /*
     * Handling Time Objects
     */
    private void HandleTimeObject(bool take, int xLoc, int yLoc, int storedTime, int agePlayer, int healthPlayer, GameObject hitItem)
    {
        if (hitItem.GetComponent<CocoonController>() != null)
        {
            if (hitItem.GetComponent<CocoonController>().isOpening())
            {
                // Don't allow player to shoot case that is opening
                return;
            } 
        }
        // Shoot Analysis
        TimeObject hitTimeObj = hitItem.GetComponent<TimeObject>();
        string clickType = "Give";
        string interactionType = "Give";
        int deltaTimeStored = -1;
        Color mirrorLaserColor = Color.green;
        bool passedCriteria = this.playerTimeBank.CheckSubtract() && hitTimeObj.CheckAddition() && hitTimeObj.isActiveAndEnabled;

        if (take)
        {
            clickType = "Take";
            interactionType = "Take";
            deltaTimeStored = 1;
            mirrorLaserColor = Color.red;
            passedCriteria = hitTimeObj.CheckSubtraction() && hitTimeObj.isActiveAndEnabled;
        }
        if (passedCriteria)
        {
            hitTimeObj.AlterTime(-deltaTimeStored);
            this.playerTimeBank.AlterTimeStored(deltaTimeStored);
            AlterColor(laserLine, mirrorLaserColor);
            analyticManager.SendShootInfo(xLoc,yLoc, 0, storedTime, agePlayer, healthPlayer, clickType, interactionType, hitItem.name);
        }
    }

    /*
     * If take is false then it has to be give since that is the only option
     */
    private void HandleMirrorHit(bool take, int xLoc, int yLoc, int storedTime, int agePlayer, int healthPlayer, string hitName)
    {
        // Shoot Analysis
        string clickType = "Give";
        int ageChange = 1;
        int deltaTimeStored = -1;
        Color mirrorLaserColor = Color.green;
        bool passedCriteria = (this.playerControllerComp.getAge() > 0);
        if (take)
        {
            clickType = "Take";
            ageChange = -1;
            deltaTimeStored = 1;
            mirrorLaserColor = Color.red;
            passedCriteria = (playerTimeBank.CheckSubtract()) && (playerControllerComp.getAge() < 2);
        }
        if (passedCriteria)
        {
            playerControllerComp.AlterAge(ageChange);
            playerTimeBank.AlterTimeStored(deltaTimeStored);
            AlterColor(laserLine, mirrorLaserColor);
            playerTransform.localScale = playerControllerComp.getPlayerSize();
            analyticManager.SendShootInfo(xLoc,yLoc, 0,storedTime, agePlayer, healthPlayer, clickType, "AgeSelf", hitName);
        }
    }

    private void _ShowLaser(Vector3 startPosition, Vector3 endPosition)
    {
        laserLine.SetPosition(0, startPosition);
        laserLine.SetPosition(1, endPosition);
        StartCoroutine(LaserShow());
    } 

    void AlterColor(LineRenderer line, Color color)
    {
        line.startColor = color;
        line.endColor = color;
    }

    public long getLastTimePlayerHitObjectWithGun()
    {
        return timestampOfLastGunHit;
    }

    public void setLastTimePlayerHitObjectWithGun(long time)
    {
        this.timestampOfLastGunHit = time;
    }

    IEnumerator LaserShow()
    {
        laserLine.enabled = true;
        yield return new WaitForSeconds(laserDuration);
        laserLine.enabled = false;
    }
}