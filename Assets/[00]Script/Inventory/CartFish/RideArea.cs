using UnityEngine;
using KinematicCharacterController.Examples;
using UnityEngine.Video;
using Unity.Mathematics;

[RequireComponent(typeof(Collider))]
public class RideArea : MonoBehaviour, IInteractable
{
    [Header("References")]
    public Transform playerTransform;
    public ExamplePlayer examplePlayer;        // Player's ExamplePlayer
    public Transform ridingPoint;               // จุดที่ player จะยืนตอนขี่
    public Transform trolleyTransform;          // ตัวทอลลีย์ที่จะขยับตาม
    public float followSpeed = 50f;
    public float rotationSpeed = 50f;

    private bool isRiding;
    private Vector3 ridingPointOffset;

    public bool CanInteract() => true;

    public Transform GetTransform() => transform;

    public bool IsRiding => isRiding;


    public void Interact()
    {
        if (isRiding) { Unmount(); return; }
        Mount();
    }

    public void SetHighlighted(bool isHighlighted) { }


    private void Mount()
    {
        // 1. ปิด input
        examplePlayer.SetControlEnabled(false);

        // 2. หยุดแรง KCC
        var motor = examplePlayer.Character.Motor;
        motor.BaseVelocity = Vector3.zero;

        // 3. ย้าย player ไป ridingPoint (X,Z)
        Vector3 snap = new Vector3(ridingPoint.position.x, motor.transform.position.y, ridingPoint.position.z);
        motor.SetPosition(snap);

        // 4. คำนวณ offset ทอลลีย์
        ridingPointOffset = trolleyTransform.position - snap;

        // 5. ห้ามกระโดด
        examplePlayer.canJump = false;

        // 6. เปิด input กลับ
        isRiding = true;
        examplePlayer.SetControlEnabled(true);
    }

    private void Unmount()
    {
        examplePlayer.canJump = true;
        examplePlayer.SetControlEnabled(false);

        // ย้าย player กลับข้างนอกทอลลีย์ (ด้านข้าง ridingPoint)
        Vector3 exitPos = new Vector3(
            ridingPoint.position.x,
            examplePlayer.Character.Motor.transform.position.y,
            ridingPoint.position.z
        );
        examplePlayer.Character.Motor.SetPosition(exitPos);
        examplePlayer.Character.Motor.BaseVelocity = Vector3.zero;

        isRiding = false;
        examplePlayer.SetControlEnabled(true);
    }

    void Update()
    {
        if (!isRiding) return;

        Vector3 targetPos = new Vector3(
            playerTransform.position.x + ridingPointOffset.x,
            trolleyTransform.position.y,          // คงระดับพื้นเดิมของทอลลีย์
            playerTransform.position.z + ridingPointOffset.z
        );
        
        trolleyTransform.position = Vector3.Lerp(trolleyTransform.position, targetPos, followSpeed * Time.deltaTime);
        
        trolleyTransform.rotation = Quaternion.Slerp(
            trolleyTransform.rotation,
            playerTransform.rotation,
            rotationSpeed * Time.deltaTime
        );
    }
}
