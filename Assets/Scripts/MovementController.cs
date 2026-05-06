using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    public const float CollisionPadding = 0.015f;

    [Range(2, 100)] public int NumOfHorizontalRays = 4;
    [Range(2, 100)] public int NumOfVerticalRays = 4;

    private float horiztalRaySpace;
    private float verticalRaySpace;

    private BoxCollider2D coll;
    public RaycastCorners RayCastCorners;
    private PlayerMovmentStats moveStats;

    public bool IsCollidingAbove { get; private set; }

    public bool IsCollidingBelow { get; private set; }

    public bool IsCollidingLeft { get; private set; }

    public bool IsCollidingRight { get; private set; }

    public int HeadBumpSlideDirection { get; private set; }
    public bool IsHittingCeilCenter { get; private set; }
    public bool IsHittingBothCorners { get; private set; }

    public bool  IsClimbingSlope { get; private set; }
    public bool  WasClimbingSlopeLastFrame { get; private set; }
    public bool IsDescendingSlope { get; private set; }
    public float  SlopeAngle{ get; private set; }
    public  Vector2 SlopeNormal { get; private set; }
    public float WallAngle { get; private set; }
    public bool IsSliding { get; private set; }
    public bool  IsOnSlideableSlope { get; private set; }
    public int FaceDirection { get; private set; }
    public float CeilingAngle  { get; private set; }
    public Vector2 CeilingNormal  { get; private set; }



    private PlayerMovement playerMovement;
    private Rigidbody2D rb;

    public struct RaycastCorners
    {
        public Vector2 topLeft;
        public Vector2 topRight;
        public Vector2 bottomLeft;
        public Vector2 bottomRight;
    }


    private void Awake()
    {
        coll = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        moveStats = playerMovement.MoveStats;
        
        FaceDirection = 1;

    }
    private void Start()
    {
        CalculateRaySpacing();

    }

    public void Move(Vector2 velocity)
    {
        UpdateRaycastCorners();
        ResetCollisionStates();
        CheckCeilingBoxCast(velocity);



        ResolveHorizontalMovement(ref velocity);
        ResolveVerticalMovement(ref velocity);



        rb.MovePosition(rb.position + velocity);
    
    }

    private void CheckCeilingBoxCast(Vector2 velocity)
    {
        if (velocity.y < 0) return;
        if (!moveStats.UseHeadBumpedSlide) return;

        float boxCastDistance = Mathf.Abs(velocity.y) + CollisionPadding;
        Vector2 boxSize = new Vector2(coll.bounds.size.x * moveStats.HeadBumpBoxWidth, moveStats.HeadBumpBoxHeight);
        Vector2 boxOrigin = new Vector2(coll.bounds.center.x + velocity.x, coll.bounds.max.y);

        RaycastHit2D hit = Physics2D.BoxCast(boxOrigin, boxSize, 0f, Vector2.up, boxCastDistance, moveStats.GroundLayer);

        if (hit)
        {
            IsHittingCeilCenter = true;


        }


    }




    private void ResetCollisionStates() 
    
    {
        IsCollidingAbove = false;
        IsCollidingBelow = false;
        IsCollidingLeft = false;
        IsCollidingRight = false;

        HeadBumpSlideDirection = 0;
        IsHittingBothCorners = false;
        IsHittingCeilCenter = false;

        WasClimbingSlopeLastFrame = IsClimbingSlope;
        IsClimbingSlope = false;
        IsDescendingSlope = false;
        SlopeAngle = 0f;
        SlopeNormal = Vector2.zero;
        WallAngle = 0f;
        IsSliding = false;
        IsOnSlideableSlope = false;
        CeilingAngle = 0f;
        CeilingNormal = Vector2.zero;


    }

    private void ResolveHorizontalMovement(ref Vector2 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        float rayLength = Mathf.Abs(velocity.x) + CollisionPadding;
       






        for (int i = 0; i < NumOfHorizontalRays; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? RayCastCorners.bottomLeft : RayCastCorners.bottomRight;
            rayOrigin += Vector2.up * (horiztalRaySpace * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, moveStats.GroundLayer);

            if (hit)
            {
                velocity.x = (hit.distance - CollisionPadding) * directionX;
                rayLength = hit.distance;

                if (directionX == -1)
                {
                    IsCollidingLeft = true;

                }
                
                
                else if (directionX == 1)
                {
                    IsCollidingRight = true;

                }

            }

            #region debug visualizer

            if (moveStats.DebugShowWallHit)
            {
                float debugRayLength = moveStats.ExtraRayDeBugDistance;
                Vector2 debugRayOrigin = (directionX == -1) ? RayCastCorners.bottomLeft : RayCastCorners.bottomRight;
                debugRayOrigin += Vector2.up * (horiztalRaySpace * i);

                bool didHit = Physics2D.Raycast(debugRayOrigin, Vector2.right * directionX, debugRayLength, moveStats.GroundLayer);
                Color rayColor = didHit ? Color.cyan : Color.red;
                Debug.DrawRay(debugRayOrigin, Vector2.right * directionX * debugRayLength, rayColor);




            }


            
            
            
            #endregion
        }




    }


    private void ResolveVerticalMovement(ref Vector2 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + CollisionPadding;

        bool hitLeftCorner = false;
        bool hitRightCorner = false;


        #region Ceiling Check



      

        #endregion


        for (int i = 0; i < NumOfVerticalRays; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? RayCastCorners.bottomLeft : RayCastCorners.topLeft;
           
            rayOrigin += Vector2.right * (verticalRaySpace * i + velocity.x);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, moveStats.GroundLayer);


            if (hit)
            {
                velocity.y = (hit.distance - CollisionPadding) * directionY;
                rayLength = hit.distance;

                if (directionY == -1)
                {
                    IsCollidingBelow = true;

                }
                else
                {
                    IsCollidingAbove = true;
                    if (i == 0) hitLeftCorner = true;
                    if (i == NumOfVerticalRays - 1) hitRightCorner = true;

                    if (moveStats.UseHeadBumpedSlide)
                    {
                        int slideDir = 0;
                        if (i == 0) slideDir = 1;
                        else if (i == NumOfVerticalRays - 1) slideDir = -1;

                        if (slideDir != 0)
                        {
                            Vector2 slideCheckRayOrigin = hit.point + (Vector2.down * CollisionPadding * 2);
                            float slideCheckRayLength = CollisionPadding * 2;
                            RaycastHit2D slideCheckHit = Physics2D.Raycast(slideCheckRayOrigin, Vector2.right * slideDir, slideCheckRayLength, moveStats.GroundLayer);

                            if (!slideCheckHit)
                            {
                                HeadBumpSlideDirection = slideDir;

                            }
                        }
                    }
                }
            }


            #region debug visulizaer

            if (moveStats.DebugShowIsGrounded)
            {
                float debugRayLength = moveStats.ExtraRayDeBugDistance;
                Vector2 debugRayOrigin = RayCastCorners.bottomLeft + Vector2.right * (verticalRaySpace * i);
                bool didHit = Physics2D.Raycast(debugRayOrigin, Vector2.down, debugRayLength, moveStats.GroundLayer);
                Color rayColor = didHit ? Color.cyan : Color.red;
                Debug.DrawRay(debugRayOrigin, Vector2.down * debugRayLength, rayColor);
            }

            if (moveStats.DebugShowHeadRays)
            {
                float debugRayLength = moveStats.ExtraRayDeBugDistance;
                Vector2 debugRayOrigin = RayCastCorners.topLeft + Vector2.right * (verticalRaySpace * i);
                bool didHit = Physics2D.Raycast(debugRayOrigin, Vector2.up, debugRayLength, moveStats.GroundLayer);
                Color rayColor = didHit ? Color.cyan : Color.red;

                if (i == 0 || i == NumOfVerticalRays -1)
                {
                    rayColor = didHit ? Color.green : Color.magenta;

                }

                Debug.DrawRay(debugRayOrigin, Vector2.up * debugRayLength, rayColor);
            }





            #endregion

        } //for loop end
        
        
        IsHittingBothCorners = hitLeftCorner && hitRightCorner;





    }




    private void UpdateRaycastCorners()
    {
        Bounds bounds = coll.bounds;
        bounds.Expand(CollisionPadding * -2);
        RayCastCorners.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        RayCastCorners.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        RayCastCorners.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        RayCastCorners.topRight = new Vector2(bounds.max.x, bounds.max.y);

    }


    private void CalculateRaySpacing()
    {
        Bounds bounds = coll.bounds;
        bounds.Expand(CollisionPadding * -2);

        horiztalRaySpace = bounds.size.y / (NumOfHorizontalRays - 1);
        verticalRaySpace = bounds.size.x / (NumOfVerticalRays - 1);

    }
    #region Helpers Methods

    public bool IsGround() => IsCollidingBelow;

    public bool BumpedHead() => IsCollidingAbove;
    public bool IsTouchingWall(bool isFacingRight) => (isFacingRight && IsCollidingRight) || (!isFacingRight && IsCollidingLeft);
    public int GetWallDirection()
    {
        if (IsCollidingLeft) return -1;
        if (IsCollidingRight) return 1;
        return 0;




    }
    
    
    
    
    
    
    
    #endregion



}
