using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Collections;


[RequireComponent(typeof(SphereCollider))]

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] private InputActionReference triggerActionReference;
    [SerializeField] private SphereCollider sphereCollider;


    private void OnEnable()
    {
        triggerActionReference.action.performed += OnActionPerformed;
        triggerActionReference.action.canceled += OnActionCanceled;

    }

    private void OnActionPerformed(InputAction.CallbackContext obj) => sphereCollider.enabled = true;

    private void OnActionCanceled(InputAction.CallbackContext obj) => sphereCollider.enabled = false;

    private void OnDisable()
    {
        triggerActionReference.action.performed -= OnActionPerformed;
        triggerActionReference.action.canceled -= OnActionCanceled;
    }

}
