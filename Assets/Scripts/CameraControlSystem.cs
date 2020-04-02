using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

[DisableAutoCreation]
public class CameraControlSystem : ComponentSystem
{
    private bool autoHeight = true;
    private LayerMask groundMask = -1; //layermask of ground or other objects that affect height

    private float maxHeight = 150f; //maximal height
    private float minHeight = 15f; //minimnal height
    private float heightDampening = 5f;
    private float keyboardZoomingSensitivity = 2f;
    private float scrollWheelZoomingSensitivity = 25f;

    private float zoomPos = 0f; //value in range (0, 1) used as t in Matf.Lerp

    private bool useKeyboardInput = true;
    private string horizontalAxis = "Horizontal";
    private string verticalAxis = "Vertical";

    private float keyboardMovementSpeed = 5f;
    private float rotationSpeed = 20f;
    private float mouseRotationSpeed = 30f;

    private bool useKeyboardRotation = true;
    private KeyCode rotateRightKey = KeyCode.X;
    private KeyCode rotateLeftKey = KeyCode.Z;

    private KeyCode rotateUpKey = KeyCode.C;
    private KeyCode rotateDownKey = KeyCode.V;

    private bool useMouseRotation = true;
    private KeyCode mouseRotationKey = KeyCode.Mouse1;

    private bool useKeyboardZooming = true;
    private KeyCode zoomInKey = KeyCode.E;
    private KeyCode zoomOutKey = KeyCode.Q;

    private bool useScrollwheelZooming = true;
    private string zoomingAxis = "Mouse ScrollWheel";

    private int RotationDirection
    {
        get
        {
            bool rotateRight = Input.GetKey(rotateRightKey);
            bool rotateLeft = Input.GetKey(rotateLeftKey);
            if (rotateLeft && rotateRight)
                return 0;
            else if (rotateLeft && !rotateRight)
                return -1;
            else if (!rotateLeft && rotateRight)
                return 1;
            else
                return 0;
        }
    }

    private int OrbitDirection
    {
        get
        {
            bool rotateUp = Input.GetKey(rotateUpKey);
            bool rotateDown = Input.GetKey(rotateDownKey);
            if (rotateUp && rotateDown)
                return 0;
            else if (rotateDown && !rotateUp)
                return -1;
            else if (!rotateDown && rotateUp)
                return 1;
            else
                return 0;
        }
    }

    private float2 KeyboardInput
    {
        get { return useKeyboardInput ? new Vector2(Input.GetAxis(horizontalAxis), Input.GetAxis(verticalAxis)) : Vector2.zero; }
    }

    private Vector2 MouseInput
    {
        get { return Input.mousePosition; }
    }

    private Vector2 MouseAxis
    {
        get { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); }
    }

    private float ScrollWheel
    {
        get { return Input.GetAxis(zoomingAxis); }
    }

    private int ZoomDirection
    {
        get
        {
            bool zoomIn = Input.GetKey(zoomInKey);
            bool zoomOut = Input.GetKey(zoomOutKey);
            if (zoomIn && zoomOut)
                return 0;
            else if (!zoomIn && zoomOut)
                return 1;
            else if (zoomIn && !zoomOut)
                return -1;
            else
                return 0;
        }
    }

    private void Rotation(Camera mainCamera)
    {
        if (useKeyboardRotation)
        {
            mainCamera.transform.Rotate(Vector3.up, RotationDirection * Time.DeltaTime * rotationSpeed, Space.World);
            mainCamera.transform.Rotate(mainCamera.transform.right, OrbitDirection * Time.DeltaTime * rotationSpeed, Space.World);
        }


        if (useMouseRotation && Input.GetKey(mouseRotationKey))
            mainCamera.transform.Rotate(Vector3.up, -MouseAxis.x * Time.DeltaTime * mouseRotationSpeed, Space.World);


    }

    private void move(Camera mainCamera)
    {
        if (useKeyboardInput)
        {
            Vector3 desiredMove = new Vector3(KeyboardInput.x, 0, KeyboardInput.y);

            desiredMove *= keyboardMovementSpeed;
            desiredMove *= Time.DeltaTime;
            desiredMove = Quaternion.Euler(new Vector3(0f, mainCamera.transform.eulerAngles.y, 0f)) * desiredMove;
            desiredMove = mainCamera.transform.InverseTransformDirection(desiredMove);

            mainCamera.transform.Translate(desiredMove, Space.Self);
        }

      
    }

    private float DistanceToGround(Camera mainCamera)
    {

        var physicsWorldSystem = World.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
        var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
        RaycastInput input = new RaycastInput()
        {
            Start = mainCamera.transform.position,
            End = new float3(mainCamera.transform.position.x, 0, mainCamera.transform.position.z),
            Filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u, // all 1s, so all layers, collide with everything
                GroupIndex = 0
            }
        };

        Unity.Physics.RaycastHit hit = new Unity.Physics.RaycastHit();
        bool haveHit = collisionWorld.CastRay(input, out hit);
        if (haveHit)
        {
            return math.length(hit.Position - new float3(mainCamera.transform.position));
        }          

        return 0f;
    }

    private Entity currentChunk(Camera mainCamera)
    {
        var physicsWorldSystem = World.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
        var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
        RaycastInput input = new RaycastInput()
        {
            Start = mainCamera.transform.position,
            End = new float3(mainCamera.transform.position.x, 0, mainCamera.transform.position.z),
            Filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u, // all 1s, so all layers, collide with everything
                GroupIndex = 0
            }
        };

        Unity.Physics.RaycastHit hit = new Unity.Physics.RaycastHit();
        bool haveHit = collisionWorld.CastRay(input, out hit);
        if (haveHit)
        {
            // see hit.Position
            // see hit.SurfaceNormal
            Entity e = physicsWorldSystem.PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
            return e;
        }

        return Entity.Null;
    }

    private void HeightCalculation(Camera mainCamera)
    {
        //float distanceToGround = DistanceToGround(mainCamera);
        if (useScrollwheelZooming)
            zoomPos += ScrollWheel * Time.DeltaTime * scrollWheelZoomingSensitivity;
        if (useKeyboardZooming)
            zoomPos += ZoomDirection * Time.DeltaTime * keyboardZoomingSensitivity;

        zoomPos = Mathf.Clamp01(zoomPos);

        float targetHeight = Mathf.Lerp(minHeight, maxHeight, zoomPos);
        float difference = 0;

        //if (distanceToGround != targetHeight)
        //    difference = targetHeight - distanceToGround;

        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position,
            new float3(mainCamera.transform.position.x, targetHeight + difference, mainCamera.transform.position.z), Time.DeltaTime * heightDampening);
    }

    private Entity currentChunkEntity = Entity.Null;

    protected override void OnUpdate()
    {
        Camera mainCamera = Camera.main;

        move(mainCamera);
        //HeightCalculation(mainCamera);
        Rotation(mainCamera);

        //Entity e = currentChunk(mainCamera);
        //if(!e.Equals(Entity.Null) && !e.Equals(currentChunkEntity) && EntityManager.HasComponent(e, typeof(WorldChunk)))
        //{
        //    PostUpdateCommands.AddComponent(e, new CurrentChunkFlag { });
        //    PostUpdateCommands.RemoveComponent(currentChunkEntity, typeof(CurrentChunkFlag));
        //    currentChunkEntity = e;
        //}   

    }
}
