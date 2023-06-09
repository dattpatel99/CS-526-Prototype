using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurtleEnemyController : MonoBehaviour
{
    public float speed = -2.0f;
    public float range;
    public TurtleHeadController head;
    private float center;
    private float leftPoint;
    private float rightPoint;
    private Rigidbody2D rb2d;
    private float curRotation = 0;
    private bool turned;
    private bool goingLeft;

    void Start()
    {
        center = transform.position.x;
        leftPoint = center - range;
        rightPoint = center + range;
        rb2d = gameObject.GetComponent<Rigidbody2D>();
        rb2d.velocity = new Vector2(speed, 0);
        turned = false;
        goingLeft = true;
        curRotation = 0;
    }

    void Update()
    {
        // If at edge of left point and haven't turned turn
        if (transform.position.x < leftPoint && !turned || (head.isColliding() && goingLeft))
        {
            speed = -speed;
            curRotation = 180;
            turned = true;
            goingLeft = false;
            head.resetCollidingStatus();
        }
        // If at edge of right point and haven't turned turn
        else if (transform.position.x > rightPoint && !turned || (head.isColliding() && !goingLeft))
        {
            speed = -speed;
            curRotation = 0;
            turned = true;
            goingLeft = true;
            head.resetCollidingStatus();
        }
        // IF in between then turning is allowed
        else if(transform.position.x > leftPoint && transform.position.x < rightPoint && turned)
        {
            turned = false;
        }
        rb2d.velocity = new Vector2(speed, 0);
        transform.rotation = Quaternion.Euler(0, curRotation, 0);
    }
}
