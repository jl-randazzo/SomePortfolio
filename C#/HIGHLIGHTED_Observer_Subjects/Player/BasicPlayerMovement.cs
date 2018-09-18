using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.Animations;
using UnityEngine;
using Assets.Code.Physics;
/*
 * Basic Architecture:
 * The Enumeration State is used to define which routines are accessed in a given frame. On each Update(), the _state variable is used to slot into the _stateRoutineVector and invoke the corresponding routines.
 * During those routines, relevant player inputs are examined and converted into Accelerator objects which are stored in the _xAccelerators and _yAccelerators arrays; additionally, the new _state value is set in those routines.
 * As a result, for each corresponding state routine, the most important input detection must come last, because whichever state is saved last is the one that will be adhered to in the following frame.
 * After the state routine is finished, Update() calls CalculateKinematicTransform() to determine the character's movement based on the accelerators. The accumulated velocity of terminated accelerators is added to the scalar velocity.
 * LateUpdate() is used for collision detection, and it has final say in determining the transform and state to be used in the following frame.
 * 
 * Termination conditions for accelerators are checked every frame (See AcceleratorData.cs for more information)
 */

public class BasicPlayerMovement : MonoBehaviour {

    enum Face //Direction
    {
        Left, Right
    }
    
    enum State //Player routine
    {
        Idle, StartTrot, Trot, StartGallop, Gallop, Airborne, Landing, AirborneMoving, 
        /*For array initialization, must be last*/SIZE
    }

    delegate void StateMethod();
    StateMethod[] _stateRoutineVector = new StateMethod[(int)State.SIZE];
    Face _facing;
    State _state;

    State PlayerState { get { return _state; } set { _state = value; } }
    Face Facing { get { return _facing; } //used instead of _facing. The only place where left/right animation triggers are set
        set
        {
            if(value == Face.Left)
            {
                Animations.SetTrigger("Leftward");
                Animations.ResetTrigger("Rightward");
                _facing = Face.Left;
            }
            else
            {
                Animations.SetTrigger("Rightward");
                Animations.ResetTrigger("Leftward");
                _facing = Face.Right;
            }
        }
    }
    
    //Resets animation triggers in bulk. Prevents repetition.
    void ResetTriggers()
    {
        Animations.ResetTrigger("Trot");
        Animations.ResetTrigger("Idle");
        Animations.ResetTrigger("StandardJump");
        Animations.ResetTrigger("Gallop");
    }

    void Update()
    {
        ResetTriggers();
        _stateRoutineVector[(int)_state].Invoke(); //Run routines associated with state
        _updateTransform = StaticPhysicsCalculations.CalculateKinematicTransform(_xAccelerators, _yAccelerators, ref _xVelocity, ref _yVelocity);
    }

    void LateUpdate() //Player movement is handled in LateUpdate so that world elements have finished moving and Collision Detection can be more precise.
    {
        CollisionDetection();
        transform.Translate(_updateTransform, Space.World);
    }

    void IdleRoutine() //State.Idle
    {
        Animations.SetTrigger("Idle");
        UnityEngine.Debug.Log("Successful slot into Idle");
        GroundOrAir();
        GroundedStandardMovement();
        StandardJump();
    }

    void StartTrotRoutine() //State.StartTrot -- Sprint routine is not exposed until we enter the Trot routine
    {
        Animations.SetTrigger("Trot");
        GroundOrAir();
        GroundedStopTrot();
        StandardJump();
        UnityEngine.Debug.Log("Successful slot into StartTrot");
    }

    public bool automationTesting = false;
    void TrotRoutine() //State.Trot
    {
        if(automationTesting) UnityEngine.Debug.Log("trot cycle complete");
        automationTesting = false;
        Animations.SetTrigger("Trot");
        GroundOrAir();
        GroundedStopTrot();
        StartGallop();
        StandardJump();
        UnityEngine.Debug.Log("Successful slot into Trot");
    }

    void StartGallopRoutine() //State.StartGallop
    {
        Animations.SetTrigger("Gallop");
        GroundOrAir();
        GroundedStopTrot();
    }

    void GallopRoutine() //State.Gallop
    {
        Animations.SetTrigger("Gallop");
        GroundOrAir();
        GroundedStopTrot();
    }

    void StartGallop()
    {
        if(GallopButton)
        {
            UnityEngine.Debug.Log(_xVelocity);
            Animations.SetTrigger("Gallop");
            _state = State.StartGallop;
            _windup.Reset();
            _windup.Start();
            int LRscale = Facing == Face.Left ? -1 : 1;
            UnityEngine.Debug.Log((LRscale * _gallopA) + ": acceleration");
            _xAccelerators[PLAYER].Mutate(LRscale * _gallopA, 0, 0, GallopWindupt, ConstAccelToPosition, ConstAccelToVelocity, () => (float)_windup.Elapsed.TotalSeconds > GallopWindupt || _state == State.Airborne,
                delegate ()
                {
                    if((float)_windup.Elapsed.TotalSeconds <= GallopWindupt)
                        _xAccelerators[PLAYER].TermUpperBound = _xAccelerators[PLAYER].LowerBound;
                    else
                        _state = State.Gallop;
                });
        }
    }

    void AirborneRoutine() //State.Airborne
    {
        UnityEngine.Debug.Log("Successful slot into Airborne");
        AirborneMovementDetection();
        if(_xVelocity < 0) Facing = Face.Left;
        else if(_xVelocity > 0) Facing = Face.Right;
        //GroundOrAir();
    }

    void AirborneMovingRoutine() //State.AirborneMoving
    {
        UnityEngine.Debug.Log("Successful slot into Airborne moving routine");
    }

    void GroundOrAir()
    {
        if(_boxCollider.IsTouching(_ground)) // on the ground?
        {
            _yVelocity = 0;
            _yAccelerators[GRAVITY].Reset();
        }
        else
        {
            Animations.ResetTrigger("Grounded");
            Animations.SetTrigger("AirPeaked");
            _elapsedJumpTime.Reset();
            _elapsedJumpTime.Start();
            _yVelocity = 0;
            _state = State.Airborne;
            if(!_yAccelerators[GRAVITY].Active) _yAccelerators[GRAVITY].Mutate(-g, 0, 0, 4f,  ConstAccelToPosition, ConstAccelToVelocity, () => false, null);
        }
    }

    //Calculations for standard-speed movement, accounts for changes in midair
    Stopwatch _windup = new Stopwatch();
    void GroundedStandardMovement()
    {
        if(RightInput)
        {
            Facing = Face.Right;
            Trot(); 
        }
        else if(LeftInput)
        {
            Facing = Face.Left;
            Trot(); 
        }
    }

    void Trot()
    {
        Animations.SetTrigger("Trot");
        _state = State.StartTrot;
        _windup.Reset(); _windup.Start();

        int LRscale = Facing == Face.Left ? -1 : 1;
        float adjustedA;
        if(Mathf.Abs(_xVelocity) > TrotWindUpV)
        {
            adjustedA = (LRscale * TrotV - _xVelocity) / TrotWindUpt;
        }
        else
        {
            adjustedA = LRscale * _trotA;
            _xVelocity = LRscale * TrotWindUpV;
        }
        _xAccelerators[PLAYER].Mutate(adjustedA, 0, 0,  TrotWindUpt, ConstAccelToPosition, ConstAccelToVelocity, () => (float)_windup.Elapsed.TotalSeconds > TrotWindUpt || _state == State.Airborne,
            delegate ()
            {
                if(_windup.Elapsed.TotalSeconds <= TrotWindUpt)
                    _xAccelerators[PLAYER].TermUpperBound = _xAccelerators[PLAYER].LowerBound;
                else
                    _state = State.Trot;
            });
    }

    void GroundedStopTrot()
    {
        if((Facing == Face.Left && !LeftInput) || (Facing == Face.Right && !RightInput))
        {
            Animations.SetTrigger("Idle");
            _state = State.Idle;
            _xAccelerators[PLAYER].Reset();
            _xVelocity = 0;
        }
    }

    public float AirborneAccel;
    void AirborneMovementDetection()
    {
        if(RightInput) AirborneMove(Face.Right);
        else if(LeftInput) AirborneMove(Face.Left);
    }

    void AirborneMove(Face dir)
    {
        int LRscale = dir == Face.Left ? -1 : 1;
        Func<bool> inp = (dir == Face.Left) ? (Func<bool>)(() => LeftInput) : () => RightInput;
        float windupTime = (LRscale * TrotV - _xVelocity) / (LRscale * AirborneAccel);
        _windup.Reset(); _windup.Start();
        _state = State.AirborneMoving;
        _xAccelerators[PLAYER].Mutate(LRscale * AirborneAccel, 0, 0, windupTime, ConstAccelToPosition, ConstAccelToVelocity, () => !inp() || _windup.Elapsed.TotalSeconds >= windupTime,
            delegate ()
            {
                if(_windup.Elapsed.TotalSeconds < windupTime)
                    _xAccelerators[PLAYER].TermUpperBound = _xAccelerators[PLAYER].LowerBound;
                _state = State.Airborne;
            }
        );
    }

    bool LeftInput { get { return Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D); } }
    bool RightInput { get { return Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A); } }
    bool JumpButton { get { return Input.GetKey(KeyCode.Space); } }
    bool JumpButtonDown { get { return Input.GetKeyDown(KeyCode.Space); } }
    bool JumpButtonUp { get { return Input.GetKeyUp(KeyCode.Space); } }
    bool GallopButton { get { return Input.GetKey(KeyCode.LeftShift); } }

    void StandardJump()
    {
        if(JumpButtonDown)
        {
            _elapsedJumpTime.Reset();
            _elapsedJumpTime.Start();
            _yVelocity += StJumpVNaught;
            _yAccelerators[PLAYER].Mutate(CounterActGravity, 0, 0,  MaxJumpS, ConstAccelToPosition, ConstAccelToVelocity, () => !JumpButton || _elapsedJumpTime.Elapsed.TotalSeconds >= MaxJumpS,
                delegate ()
                {
                    if(_elapsedJumpTime.Elapsed.TotalSeconds < MinJumpS)
                        _yAccelerators[PLAYER].Mutate(CounterActGravity, 0, 0, MinJumpS, ConstAccelToPosition, ConstAccelToVelocity, () => _elapsedJumpTime.Elapsed.TotalSeconds >= MinJumpS,
                            delegate ()
                            {
                                Animations.ResetTrigger("StandardJump");
                                Animations.SetTrigger("AirPeaked");
                            });
                    else if(_elapsedJumpTime.Elapsed.TotalSeconds < MaxJumpS)
                    {
                        _yAccelerators[PLAYER].TermUpperBound = (float)_elapsedJumpTime.Elapsed.TotalSeconds;
                        Animations.ResetTrigger("StandardJump");
                        Animations.SetTrigger("AirPeaked");
                    }
                    else
                    {
                        Animations.ResetTrigger("StandardJump");
                        Animations.SetTrigger("AirPeaked");
                    }
                });
            _yAccelerators[GRAVITY].Mutate(-g, 0, 0, 4f,  ConstAccelToPosition, ConstAccelToVelocity, () => false, null);
            _state = State.Airborne;
            ResetTriggers();
            Animations.SetTrigger("StandardJump");
        }
    }

    RaycastHit2D _blHit;
    RaycastHit2D _brHit;

    //Physics calculations including in the UnityEngine can be imprecise and lead to objects overlapping. This method is designed to cross reference the available world space in the direction
    //the player-character is heading with the actual magnitude of their calculated transform. Currently this is only implemented on the y-axis with a low resolution (the raycasters are positioned
    //on the front and back of the character); eventually this will be modified to include collision detection and state transitions on the x-axis as well at a higher resolution.
    void CollisionDetection()
    {
        Vector2 oldTransform = _updateTransform;
        if(_updateTransform.y < 0)
        {
            _blHit = Physics2D.Raycast(BottomLeft.transform.position, _updateTransform.normalized, _updateTransform.magnitude, 512);
            _brHit = Physics2D.Raycast(BottomRight.transform.position, _updateTransform.normalized, _updateTransform.magnitude, 512);
            if(_blHit || _brHit)
            {
                UnityEngine.Debug.Log("hit");
                _stateRoutineVector[(int)State.Landing].Invoke();
                if(_brHit.point.y > _blHit.point.y)
                {
                    _updateTransform.y = oldTransform.normalized.y * _brHit.distance;
                }
                else
                {
                    _updateTransform.y = oldTransform.normalized.y * _blHit.distance;
                }
            }
            else UnityEngine.Debug.Log("nothit");
        }
    }

    void LandingRoutine()
    {
        Animations.ResetTrigger("AirPeaked");
        Animations.SetTrigger("Grounded");
        GroundedStopTrot(); GroundedStandardMovement(); //Checks input to determine if we're going into an idle state or a moving state
    }

    /*
     * General setup, variables, and Awake() function follow
     */
    //readonly variables

    readonly Func<float, float, float, float> ConstAccelToPosition = (a, lowerBound, upperBound) => .5f * a * (Mathf.Pow(upperBound, 2) - Mathf.Pow(lowerBound, 2));
    readonly Func<float, float, float, float> ConstAccelToVelocity = (a, lowerBound, upperBound) => a * (upperBound - lowerBound);

    readonly int WORLD = 0;
    readonly int PLAYER = 1;
    readonly int GRAVITY = 2;

    Accelerator[] _xAccelerators;
    Accelerator[] _yAccelerators;

    //High-level Editor variables 
    public Animator Animations;
    public float AirborneAcceleration = 16f;
    [Header("Trot variables")]
    public float TrotV = 40.8f; // units per s
    public float TrotWindUpV = 25f;
    public float TrotWindUpt = .2f;
    [Header("Gallop variables")]
    public float GallopWindupt = .4f;
    public float GallopV = 65f;
    [Header("Standard Jump variables")]
    public float StJumpVNaught = 48f;
    [Tooltip("Max and min determine how long the CounterActGravity acceleration is active in response to holding the jump button")]
    public float MaxJumpS = .3f; //Amount of time that Standard jump has linear velocity
    [Tooltip("Max and min determine how long the CounterActGravity acceleration is active in response to holding the jump button")]
    public float MinJumpS = .10f;
    [Tooltip("Max and min determine how long the CounterActGravity acceleration is active in response to holding the jump button")]
    public float CounterActGravity = 115f;
    public float g = 60f; // units per s^2 / 2
    Stopwatch _elapsedJumpTime;
    public LayerMask GroundMask;

    float _gallopA;
    float _trotA;
    float _stJumpA;

    //Movement variables updated every frame
    float _yVelocity;
    float _xVelocity;

    //Collision-detection
    Vector2 _updateTransform;
    BoxCollider2D _boxCollider;
    ContactFilter2D _ground;
    [Header("Particle-firing objects")]
    public GameObject BottomLeft;
    public GameObject BottomRight;

    void Awake()
    {
        _facing = Face.Right;
        Animations.SetTrigger("Rightward");
        Animations.ResetTrigger("Leftward");
        _state = State.Idle;
        //_stJumpA = (StJumpVFinal - StJumpVNaught) / StJumpWindupt;

        _xAccelerators = new Accelerator[3];
        _yAccelerators = new Accelerator[3];

        for(int i = 0; i < _xAccelerators.Length; i++)
        {
            _xAccelerators[i] = new Accelerator();
            _yAccelerators[i] = new Accelerator();
        }

        _stateRoutineVector[(int)State.Idle] = IdleRoutine;
        _stateRoutineVector[(int)State.StartTrot] = StartTrotRoutine;
        _stateRoutineVector[(int)State.Trot] = TrotRoutine;
        _stateRoutineVector[(int)State.StartGallop] = StartGallopRoutine;
        _stateRoutineVector[(int)State.Gallop] = GallopRoutine;
        _stateRoutineVector[(int)State.Airborne] = AirborneRoutine;
        _stateRoutineVector[(int)State.Landing] = LandingRoutine;
        _stateRoutineVector[(int)State.AirborneMoving] = AirborneMovingRoutine;

        _trotA = (TrotV - TrotWindUpV) / TrotWindUpt;
        _gallopA = (GallopV - TrotV) / GallopWindupt;
        _boxCollider = (BoxCollider2D)gameObject.GetComponent("BoxCollider2D");
        _elapsedJumpTime = new Stopwatch();
        _updateTransform = new Vector2(0, 0);
        _ground = new ContactFilter2D();
        _ground.layerMask = GroundMask;
        _ground.useLayerMask = true;
        _yVelocity = 0;
        _xVelocity = 0;
        GroundOrAir();
    }
}
