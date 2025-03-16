using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DoorInteractable : SimpleHingeInteractable
{
    [SerializeField] Transform doorObject;
    [SerializeField] float maxYRotation; // 최대 y축 회전 각도
    [SerializeField] Collider closedCollider;
    private bool isClosed;
    private Vector3 startRotation;
    [SerializeField] Collider openCollider;
    private bool isOpen;
    [SerializeField] private Vector3 endRotation;
    private float startAngleY;

    protected override void Start()
    {
        base.Start();
        startRotation = transform.localEulerAngles;
        startAngleY = startRotation.y;
        if (startAngleY >= 180)
        {
            startAngleY -= 360;
        }

        // 초기 회전 강제 적용
        transform.localEulerAngles = new Vector3(0f, startAngleY, 0f);
        if (doorObject != null)
        {
            doorObject.localEulerAngles = new Vector3(0f, startAngleY, 0f);
        }
    }

    protected override void Update()
    {
        base.Update();

        if (doorObject != null)
        {
            // 현재 y축 회전값만 가져오기
            float currentY = transform.localEulerAngles.y;
            if (currentY >= 180) currentY -= 360;

            // y축 회전만 적용, x와 z는 0으로 강제 고정
            Vector3 clampedRotation = new Vector3(0f, currentY, 0f);
            doorObject.localEulerAngles = clampedRotation;
            transform.localEulerAngles = clampedRotation;
        }

        if (isSelected)
        {
            CheckLimits();
        }
    }

    private void CheckLimits()
    {
        isClosed = false;
        isOpen = false;
        float localAngleY = transform.localEulerAngles.y;

        if (localAngleY >= 180)
        {
            localAngleY -= 360;
        }

        // y축 회전 제한
        if (localAngleY > startAngleY + maxYRotation ||
            localAngleY < startAngleY - maxYRotation)
        {
            ReleaseHinge();
        }
    }

    protected override void ResetHinge()
    {
        Vector3 targetRotation;

        if (isClosed)
        {
            targetRotation = new Vector3(0f, startRotation.y, 0f);
        }
        else if (isOpen)
        {
            targetRotation = new Vector3(0f, endRotation.y, 0f);
        }
        else
        {
            float currentY = transform.localEulerAngles.y;
            if (currentY >= 180) currentY -= 360;
            targetRotation = new Vector3(0f, currentY, 0f);
        }

        transform.localEulerAngles = targetRotation;
        if (doorObject != null)
        {
            doorObject.localEulerAngles = targetRotation;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == closedCollider)
        {
            isClosed = true;
            ReleaseHinge();
        }
        else if (other == openCollider)
        {
            isOpen = true;
            ReleaseHinge();
        }
    }

    // 회전 강제 고정용 LateUpdate
    private void LateUpdate()
    {
        // Update 후에도 강제로 x와 z 회전을 0으로 유지
        if (doorObject != null)
        {
            Vector3 currentRotation = doorObject.localEulerAngles;
            doorObject.localEulerAngles = new Vector3(0f, currentRotation.y, 0f);
        }
        Vector3 currentTransformRotation = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(0f, currentTransformRotation.y, 0f);
    }
}