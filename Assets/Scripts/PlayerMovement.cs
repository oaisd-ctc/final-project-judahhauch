using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
public class PlayerMovement : MonoBehaviour
{

    [Header("Refs")]
    public PlayerMovmentStats MoveStats;
    [SerializeField] private Collider2D coll;

    public Rigidbody2D rb;
    private Animator anim;

    //movement vars
   
    public bool isFacingRight { get; private set; }
    public MovementController Controller { get; private set; }
    [HideInInspector] public Vector2 Velocity;

    //input
    private Vector2 moveInput;
    private bool runHeld;
    private bool jumpPressed;
    private bool jumpReleased;
    private bool dashPressed;


    [Header("Jump power")]
    public float jumpPower = 10f;


    //jump vars
    private bool isJumping;
    private bool isFastFalling;
    private bool isFalling;
    private float fastFallTime;
    private float fastFallReleaseSpeed;
    private int numberOfAirJumpsUsed;

    //apex vars
    private float apexPoint;
    private float timePastApexThreshold;
    private bool isPastApexThreshold;

    //jump buffer vars
    private float jumpBufferTimer;
    private bool jumpReleasedDuringBuffer;

    //coyote time vars
    private float coyoteTimer;

    //Wall slide vars

    private bool isWallSliding;
    private bool isWallSlideFalling;

    //Wall Jump
    private bool useWallJumpMoveStats;
    private bool isWallJumping;
    private float wallJumpTime;
    private bool isWallJumpFastFalling;
    private bool isWallJumpFalling;
    private float wallJumpFastFallTime;
    private float wallJumpFastFallReleaseSpeed;

    private float wallJumpPostBufferTimer;

    private float wallJumpApexPoint;
    private float timePastWallJumpApexThreshold;
    private bool isPastWallJumpApexThreshold;

    private int lastWallDir;

    //dash vars
    private bool isDashing;
    private bool isAirDashing;
    private float dashTimer;
    private float dashOnGroundTimer;
    private int numberOfDashesUsed;
    private Vector2 dashDirection;
    private bool isDashFastFalling;
    private float dashFastFallTime;
    private float dashFastFallReleaseSpeed;
    private float dashBufferTimer;

    //head bump slide vars
    private float jumpStartY;
    private bool isHeadBumpSliding;
    private int headBumpSlideDirection;
    private bool justFinishedSlide;
    private bool slideFromDash;
    private float dashStartY;

    private void Awake()
    {
        isFacingRight = true;

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        Controller = GetComponent<MovementController>();

    }

    private void Update()
    {
        moveInput = InputManager.Movement;
        runHeld = InputManager.RunIsHeld;
        if (InputManager.JumpWasPressed) jumpPressed = true;
        if (InputManager.JumpWasReleased) jumpReleased = true;
        if (InputManager.DashWasPressed) dashPressed = true;


        



    }

    private void FixedUpdate()
    {
        justFinishedSlide = false;

        CountTimers(Time.fixedDeltaTime);

        JumpChecks();
        LandCheck();
        WallJumpCheck();
        WallSlideCheck();
        DashCheck();

        

        HandleHoriontalMovement(Time.fixedDeltaTime);
        HandleHeadBumpSlide();
        Jump(Time.fixedDeltaTime);
        WallSlide(Time.fixedDeltaTime);
        WallJump(Time.fixedDeltaTime);
        Dash(Time.fixedDeltaTime);
        Fall(Time.fixedDeltaTime);




        ClampVelocity();

        Controller.Move(Velocity * Time.fixedDeltaTime);

        //rest input bools
        jumpPressed = false;
        jumpReleased = false;
        dashPressed = false;



    }



    private void ClampVelocity()
    {
        //Clamp fall speed
        if (!isDashing)
        {
            Velocity.y = Mathf.Clamp(Velocity.y, -MoveStats.MaxFallSpeed, 50f);
        }
        else
        {
            Velocity.y = Mathf.Clamp(Velocity.y, -50f, 50f);

        }

  




    }



    #region Smovment

   
    private void HandleHoriontalMovement(float timeStep)
    {
        if (isHeadBumpSliding) return;

        if (!isDashing)
        {
            TurnCheck(moveInput);
            float targetVelocityX = 0f;
            if (Mathf.Abs(moveInput.x) >= MoveStats.MoveThreshold)
            {
                float moveDirection = Mathf.Sign(moveInput.x);
                targetVelocityX = runHeld ? moveDirection * MoveStats.MaxRunSpeed : moveDirection * MoveStats.MaxWalkSpeed;

            }

            float acceleration = Controller.IsGround() ? MoveStats.GroundAcceleration : MoveStats.AirAcceleration;
            float deceleration = Controller.IsGround() ? MoveStats.GroundDeceleration : MoveStats.AirDeceleration;

            if (useWallJumpMoveStats)
            {
                acceleration = MoveStats.WallJumpMoveAcceleration;
                deceleration = MoveStats.WallJumpMoveDeceleration;

            }
            if (Mathf.Abs(moveInput.x) >= MoveStats.MoveThreshold)
            {
                Velocity.x = Mathf.Lerp(Velocity.x, targetVelocityX, acceleration * timeStep);

            }
            else
            {
                Velocity.x = Mathf.Lerp(Velocity.x, 0, deceleration * timeStep);

            }





    
        }
    
    }


    private void TurnCheck(Vector2 moveInput)
    {
        if (isFacingRight && moveInput.x < 0)
        {
            turn(false);

        }
        else if (!isFacingRight && moveInput.x > 0)
        {
            turn(true);

        }


    }

    private void turn(bool turnRight)
    {
        if (turnRight)
        {
            isFacingRight = true;
            transform.Rotate(0f, 180f, 0f);

        }
        else
        {

            isFacingRight = false;
            transform.Rotate(0f, -180f, 0f);


        }

    }

    private void HandleHeadBumpSlide()
    { 
        if (!isHeadBumpSliding && (isJumping || isDashing || isWallJumping) && Controller.BumpedHead() && !Controller.IsHittingBothCorners && !Controller.IsHittingCeilCenter) 
        {
            isHeadBumpSliding = true;
            headBumpSlideDirection = Controller.HeadBumpSlideDirection;
        }

        if (isHeadBumpSliding)
        {
            Velocity.y = 0;
            if (Controller.HeadBumpSlideDirection == 0 || !Controller.BumpedHead() || Controller.IsHittingCeilCenter || Controller.IsHittingBothCorners)
            {
                isHeadBumpSliding = false;
                Velocity.x = 0;


             if (!slideFromDash)
                {
                    float compensationFactor = (1 - MoveStats.JumpHeightCompensationFactor) + 1;
                    float jumpPeakY = jumpStartY + (MoveStats.JumpHeight * compensationFactor);
                    float remainingHeight = jumpPeakY - rb.position.y;
                    if (remainingHeight <= 0f)
                    {
                        Velocity.y = 0;
                    }
                    else
                    {
                        float requiredVelocity = Mathf.Sqrt(2 * Mathf.Abs(MoveStats.Gravity) * remainingHeight);
                        Velocity.y = requiredVelocity;
                    }

                }

                else if (slideFromDash)
                {
                    float targetApexY = dashStartY + MoveStats.DashTargetApexHeight;
                    float remainingHeight = targetApexY - rb.position.y;

                    if (remainingHeight > 0)
                    {
                        float requiredVelocity = Mathf.Sqrt(2 * Mathf.Abs(MoveStats.Gravity) * remainingHeight);
                        Velocity.y = requiredVelocity;

                    }
                }

                slideFromDash = false;
                justFinishedSlide = false;

            }
            else
            {
                Velocity.x = headBumpSlideDirection * MoveStats.HeadBumpSlideSpeed;

            }



        }



            

        
        

    }




    #endregion



    #region Land/Fall

    private void LandCheck()
    {

        if (Controller.IsGround()) 
               
        {
            //landed


            if ((isJumping || isFalling || isWallJumpFalling || isWallJumping || isWallSliding || isWallSlideFalling || isDashFastFalling || isHeadBumpSliding) && Velocity.y <= 0f)
            {
                
                isHeadBumpSliding = false;


                ResetJumpValues();
                StopWallSlide();
                ResetWallJumpValues();
                ResetDashes();
                ResetDashValues();

                numberOfAirJumpsUsed = 0;
   
            }
          
            if(Velocity.y <= 0f)
            {
                Velocity.y = -2f;
            }


        }
      


    }

    private void Fall( float timeStep)
    {
        //normal grav for falling
        if (!Controller.IsGround() && !isJumping && !isWallSliding && !isWallJumping && !isDashFastFalling)
        {
            if (!isFalling)
            {
                isFalling = true;
            }

            Velocity.y += MoveStats.Gravity * timeStep;
        }
    }



    #endregion


    #region Jump

    private void ResetJumpValues()
    {
        isJumping = false;
        isFalling = false;
        isFastFalling = false;
        fastFallTime = 0f;
        isPastApexThreshold = false;

    }







    private void JumpChecks()
    {
        //when jump is pressed
        if (jumpPressed)
        {
            if (isWallSlideFalling && wallJumpPostBufferTimer >= 0f)
            {
                return;
            }
            else if (isWallSliding || (Controller.IsTouchingWall(isFacingRight) && !Controller.IsGround()))
            {
                return;
            }


                jumpBufferTimer = MoveStats.JumpBufferTime;
            jumpReleasedDuringBuffer = false;

        }

        //jump is let go of

        if (jumpReleased)
        {
             if (jumpBufferTimer > 0f)
            {
                jumpReleasedDuringBuffer = true;

            }

             if (isJumping && Velocity.y > 0f)
            {
                if (isPastApexThreshold) 
                {
                    isPastApexThreshold = false;
                    isFastFalling = true;
                    fastFallTime = MoveStats.TimeForUpwardsCancel;
                    Velocity.y = 0f;
                }
                else
                {
                    isFastFalling = true;
                    fastFallReleaseSpeed = Velocity.y;
                }

            }

        }

        //jumping with buffering and coyote time
        if (jumpBufferTimer > 0f && !isJumping && (Controller.IsGround() || coyoteTimer > 0f))
        {
            InitiateJump(0);

            if (jumpReleasedDuringBuffer)
            {
                isFastFalling = true;
                fastFallReleaseSpeed = Velocity.y;


            }

        }




        //double jump
        else if (jumpBufferTimer > 0f && (isJumping || isWallJumping || isWallSlideFalling || isAirDashing 
            || isDashFastFalling) && !Controller.IsTouchingWall(isFacingRight) &&  numberOfAirJumpsUsed < MoveStats.NumberOfAirJumpsAllowed)
        
        {
            isFastFalling = false;
            InitiateJump(1);

            if (isDashFastFalling)
            {
                isDashFastFalling = false;
            }



        }


        //Air jump after Coyote time laps
        else if (jumpBufferTimer > 0f && isFalling && !isWallSlideFalling && numberOfAirJumpsUsed < MoveStats.NumberOfAirJumpsAllowed )
        {
            InitiateJump(1);
            isFastFalling = false;
        }




    }

    private void InitiateJump(int _numberOfAirJumpsUsed)
    {
        if (!isJumping)
        {
            isJumping = true;

        }
        
        ResetWallJumpValues();

        jumpBufferTimer = 0f;
        numberOfAirJumpsUsed += _numberOfAirJumpsUsed;
        Velocity.y = MoveStats.InitialJumpVelocity;

        jumpStartY = rb.position.y;


    }



    private void Jump(float timeStep)
    {

        //apply grav while jumping
        if (isJumping)
        {


            //Check for HeadBump
            if (Controller.BumpedHead() &&  !isHeadBumpSliding)
            {
                if(Controller.HeadBumpSlideDirection != 0 && !Controller.IsHittingCeilCenter && !Controller.IsHittingBothCorners)
                {
                    slideFromDash = false;

                }
                else
                {
                    Velocity.y = 0f;
                    isFastFalling = true;


                }


            }
          
            if (isHeadBumpSliding)
            {
                Velocity.y = 0f;
                return;
            }


            if (!justFinishedSlide)
            {

                //grav on asceding
                if (Velocity.y >= 0f)
                {
                    //Apex Controls
                    apexPoint = Mathf.InverseLerp(MoveStats.InitialJumpVelocity, 0f, Velocity.y);

                    if (apexPoint > MoveStats.ApexThreshold)
                    {

                        if (!isPastApexThreshold)
                        {
                            isPastApexThreshold = true;
                            timePastApexThreshold = 0f;

                        }

                        if (isPastApexThreshold)
                        {
                            timePastApexThreshold += timeStep;

                            if (timePastApexThreshold < MoveStats.ApexHangTime)
                            {
                                Velocity.y = 0f;

                            }
                            else
                            {
                                Velocity.y = -0.01f;
                            }
                        }
                    }
                    //grav on \Ascending but not past apex
                    else if (!isFastFalling)
                    {
                        Velocity.y += MoveStats.Gravity * timeStep;
                        if (isPastApexThreshold)
                        {
                            isPastApexThreshold = false;

                        }
                    }

                }


                //grav on descending
                else if (!isFastFalling)
                {
                    Velocity.y += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * timeStep;
                }

                else if (Velocity.y < 0f)
                {

                    if (!isFalling)
                    {
                        isFalling = true;

                    }

                }
            }

        }





        
        //jump cut
        if (isFastFalling)
        {
            if (fastFallTime >= MoveStats.TimeForUpwardsCancel)
            {
                Velocity.y += MoveStats.Gravity * MoveStats.GravityOnReleaseMultiplier * Time.fixedDeltaTime;

            }
            else if (fastFallTime < MoveStats.TimeForUpwardsCancel)
            {
                Velocity.y = Mathf.Lerp( fastFallReleaseSpeed, 0f,(fastFallTime / MoveStats.TimeForUpwardsCancel));

            }
            fastFallTime += timeStep;
        }

    }





    #endregion


    #region Wall silde
    private void WallSlideCheck()
    {
        if (Controller.IsTouchingWall(isFacingRight) && !Controller.IsGround() && !isDashing)
        {
            if(Velocity.y < 0f && !isWallSliding)
            {
                ResetJumpValues();
                ResetWallJumpValues();
                ResetDashValues();

                if (MoveStats.ResetDashOnWallSlide)
                {
                    ResetDashes();
                }

                isWallJumpFalling = false;
                isWallSliding = true;

                if (MoveStats.ResetJumpOnWallSlide)
                {
                    numberOfAirJumpsUsed = 0;
                }
            }

        }

        else if (isWallSliding && !Controller.IsTouchingWall(isFacingRight) && !Controller.IsGround() && !isWallSlideFalling)
        {
            isWallSlideFalling = true;
            StopWallSlide();
        }
        else
        {
            StopWallSlide();
        }



    }

    private void StopWallSlide()
    {
        if(isWallSliding)
        {
         
            isWallSliding = false;

        }

    }

    private void WallSlide(float timeStep)
    {
        if (isWallSliding)
        {
            Velocity.y = Mathf.Lerp(Velocity.y, -MoveStats.WallSlideSpeed, MoveStats.WallSlideDecerationSpeed * timeStep);

        }

    }


    #endregion


    #region Wall Jump




    private void WallJumpCheck()
    {
        if (ShouldApplyPostWallJumpBuffer())
        {
            wallJumpPostBufferTimer = MoveStats.WallJumpPostBufferTime;

        }
        if (jumpReleased && !isWallSliding && !!Controller.IsTouchingWall(isFacingRight) && isWallJumping) 
        { 
             if (Velocity.y > 0f)
            {
                if (isPastWallJumpApexThreshold)
                {
                    isPastWallJumpApexThreshold = false;
                    isWallJumpFastFalling = true;
                    wallJumpFastFallTime = MoveStats.TimeForUpwardsCancel;

                    Velocity.y = 0f;
                }
                else
                {
                    isWallJumpFastFalling = true;
                    wallJumpFastFallReleaseSpeed = Velocity.y;

                }
            }


        }

        //actaul jump post wall jump buffer
        if(jumpPressed && wallJumpPostBufferTimer > 0f)
        {
            InitiateWallJump();

        }


    }

    private void InitiateWallJump()
    {
        if (!isWallJumping)
        {
            isWallJumping = true;
            useWallJumpMoveStats = true;
        }
        StopWallSlide();
        ResetJumpValues();
        wallJumpTime = 0f;

       Velocity.y = MoveStats.InitialWallJumpVelocity;
        Velocity.x = Mathf.Abs(MoveStats.WallJumpDirection.x) * -lastWallDir;

        jumpStartY = rb.position.y;

    }






    private void WallJump(float timeStep)
    {
        //apply wj grav
        if (isWallJumping)
        {
            //take over movement controls while wall jumping

            wallJumpTime += timeStep;
            if (wallJumpTime >= MoveStats.TimeTillJumpApex)
            {
                useWallJumpMoveStats = false;
            }



            //head hit
            if (Controller.BumpedHead() && !isHeadBumpSliding)
            {
               if (Controller.HeadBumpSlideDirection != 0 && !Controller.IsHittingCeilCenter && !Controller.IsHittingBothCorners)
                {
                    slideFromDash = false;
                }
                else
                {
                    Velocity.y = 0f;
                    isWallJumpFastFalling = true;
                    useWallJumpMoveStats = false;
                }
            }

            if (isHeadBumpSliding) 
            {
                Velocity.y = 0f;
                return;
            }

            if (!justFinishedSlide)
            {



                //grav in asedding

                if (Velocity.y >= 0f)
                {

                    //apex controls
                    wallJumpApexPoint = Mathf.InverseLerp(MoveStats.WallJumpDirection.y, 0f, Velocity.y);
                    if (wallJumpApexPoint > MoveStats.ApexThreshold)
                    {
                        if (!isPastWallJumpApexThreshold)
                        {
                            isPastWallJumpApexThreshold = true;
                            timePastWallJumpApexThreshold = 0f;

                        }

                        if (isPastWallJumpApexThreshold)
                        {
                            timePastWallJumpApexThreshold += timeStep;
                            if (timePastWallJumpApexThreshold < MoveStats.ApexHangTime)
                            {
                                Velocity.y = 0f;
                            }
                            else
                            {
                                Velocity.y = -0.01f;

                            }

                        }

                    }

                    //grav in assending not past Apex
                    else if (!isWallJumpFastFalling)
                    {
                        Velocity.y += MoveStats.WallJumpGravity * timeStep;

                        if (isPastWallJumpApexThreshold)
                        {
                            isPastWallJumpApexThreshold = false;

                        }


                    }



                }

                //grav on desending

                else if (!isWallJumpFastFalling)
                {
                    Velocity.y += MoveStats.WallJumpGravity * timeStep;

                }
                else if (Velocity.y < 0f)
                {
                    if (!isWallJumpFalling)
                    {
                        isWallJumpFalling = true;



                    }


                }



            }


        }





        //walljump cut

        if (isWallJumpFastFalling)
        {
            if(wallJumpFastFallTime >= MoveStats.TimeForUpwardsCancel)
            {
                Velocity.y += MoveStats.WallJumpGravity * MoveStats.WallJumpGravityOnReleaseMultiplier * timeStep;
            }
            else if (wallJumpFastFallTime < MoveStats.TimeForUpwardsCancel)
            {
                Velocity.y = Mathf.Lerp(wallJumpFastFallReleaseSpeed, 0f, (wallJumpFastFallTime / MoveStats.TimeForUpwardsCancel));


            }

            wallJumpFastFallTime += timeStep;



        }





    }


    private bool ShouldApplyPostWallJumpBuffer()
    {
        if( Controller.IsTouchingWall(isFacingRight) || isWallSliding)
        {
            lastWallDir = Controller.GetWallDirection();
            return true;

        }
        else { return false; }

    }




    private void ResetWallJumpValues()
    {
        isWallSlideFalling = false;
        useWallJumpMoveStats = false;
        isWallJumping = false;
        isWallJumpFastFalling = false;
        isWallJumpFalling = false;
        isPastWallJumpApexThreshold = false;

        wallJumpFastFallTime = 0f;
        wallJumpTime = 0f;


    }





    #endregion


    #region Dash

    private void DashCheck()
    {
        if (dashPressed)
        {
            dashBufferTimer = MoveStats.DashBufferTime;
        }




        if (dashBufferTimer > 0f)
        {
            //ground dash
            if (Controller.IsGround() && dashOnGroundTimer < 0 && !isDashing)
            {
                InitiateDash();
                dashBufferTimer = 0f;
            }

            //air dash
            else if (!Controller.IsGround() && !isDashing && numberOfDashesUsed < MoveStats.NumberOfDashes)
            {
                isAirDashing = true;
                InitiateDash();
                dashBufferTimer = 0f;


            }




        }


    }

    private void InitiateDash()
    {
        dashStartY = rb.position.y;



        dashDirection = moveInput;
        TurnCheck(dashDirection);

        Vector2 closestDirection = Vector2.zero;
        float minDistance = Vector2.Distance(dashDirection, MoveStats.DashDirections[0]);

        for (int i = 0; i < MoveStats.DashDirections.Length; i++)
        {
            if (dashDirection == MoveStats.DashDirections[i])
            {
                closestDirection = dashDirection;
                break;

            }

            float distance = Vector2.Distance(dashDirection, MoveStats.DashDirections[i]);

            bool isDiagonal = (Mathf.Abs(MoveStats.DashDirections[i].x) == 1 && Mathf.Abs(MoveStats.DashDirections[i].y) == 1);

            if (isDiagonal)
            {
                distance -= MoveStats.DashDiagonallyBias;
            }

            else if ( distance < minDistance)
            {
                minDistance = distance;
                closestDirection = MoveStats.DashDirections[i];
            }

        }
        //handle dir with no input
        if ( closestDirection == Vector2.zero)
        {
            if (isFacingRight)
            {
                closestDirection = Vector2.right;


            }
            else { closestDirection = Vector2.left; }
        }

        if (Controller.IsGround() && closestDirection.y < 0 && closestDirection.x != 0)
        {
            closestDirection = new Vector2(Mathf.Sign(closestDirection.x), 0);

        }


        dashDirection = closestDirection;
        numberOfDashesUsed++;
        isDashing = true;
        dashTimer = 0f;
        dashOnGroundTimer = MoveStats.TimeBtwDashesOnGround;
        ResetJumpValues();
        ResetWallJumpValues();
        StopWallSlide();

    }


    private void Dash(float timeStep)
    {

        if (justFinishedSlide) return;


        if(isDashing)
        {
            if(Controller.BumpedHead() && !isHeadBumpSliding)
            {
                if (Controller.HeadBumpSlideDirection != 0 && !Controller.IsHittingCeilCenter && !Controller.IsHittingBothCorners)
                {
                  
                    slideFromDash = true;
                    dashTimer = 0f;
                   
                }
                else
                {
                    Velocity.y = 0;
                    isDashing = false;
                    isAirDashing = false;
                    dashTimer = 0f;


                }
            }

            if (isHeadBumpSliding)
            {
                Velocity.y = 0f;
                return;

            }


            //stop the dash after timer
            dashTimer += timeStep;
            if(dashTimer >= MoveStats.DashTime)
            {
                if (Controller.IsGround())
                {
                    ResetDashes();
                }

                isAirDashing = false;
                isDashing = false;

                if (!isJumping && !isWallJumping)
                {
                    dashFastFallTime = 0f;
                    dashFastFallReleaseSpeed = Velocity.y;

                    if (!Controller.IsGround())
                    {

                        isDashFastFalling = true;

                    }


                }

                return;

            }
            
            Velocity.x = MoveStats.DashSpeed * dashDirection.x;

            if (dashDirection.y != 0f || isAirDashing)
            {
                Velocity.y = MoveStats.DashSpeed * dashDirection.y;

            }
            else if (!isJumping && dashDirection.y == 0f)
            {
                Velocity.y = -0.001f;


            }


        }

        //handle dash cut time

        else if (isDashFastFalling)
        {

            if (Velocity.y > 0f)
            {

                if (dashFastFallTime < MoveStats.DashTimeForUpwardsCancel)
                {
                    Velocity.y = Mathf.Lerp(dashFastFallReleaseSpeed, 0f, (dashFastFallTime / MoveStats.DashTimeForUpwardsCancel));

                }
                else if (dashFastFallTime >= MoveStats.DashTimeForUpwardsCancel)
                {
                    Velocity.y += MoveStats.Gravity * MoveStats.DashGravityOnReleaseMultiplier * timeStep;

                }

                dashFastFallTime += timeStep;

            }
            else
            {
                Velocity.y += MoveStats.Gravity * MoveStats.DashGravityOnReleaseMultiplier * timeStep;
            }

        }

    }





    private void ResetDashValues()
    {
       
        isDashFastFalling = false;
        dashOnGroundTimer = -0.01f;

        dashFastFallReleaseSpeed = 0f;
        dashFastFallTime = 0f;
        dashDirection = Vector2.zero;


    }
    private void ResetDashes()
    {
        numberOfDashesUsed = 0;


    }

    #endregion

   

   






    #region Timers
    
    
    private void CountTimers(float timeStep)
    {
        jumpBufferTimer -= timeStep;


        //coyote time

        if(!Controller.IsGround())
        {
            coyoteTimer -= timeStep;
        }
        else { coyoteTimer = MoveStats.JumpCoyoteTime; }

        //wall jump buffer timer
            wallJumpPostBufferTimer -= timeStep;

      

        //dash timer
        if (Controller.IsGround())
        {
            dashOnGroundTimer -= timeStep;
        }

        //dash buffer

        dashBufferTimer -= timeStep;




    }
    
    
    
    
    #endregion











}
