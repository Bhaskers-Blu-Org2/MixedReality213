﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoloToolkit.Unity.Controllers;
using HoloToolkit.Unity.InputModule;
using UnityEngine;
using UnityEngine.XR.WSA.Input;

namespace HoloToolkit.Unity.ControllerExamples
{
    public class ColorPickerWheel : AttachToController, IPointerTarget
    {
        public bool Visible
        {
            get { return visible; }
            set
            {
                visible = value;
                if (value)
                {
                    lastTimeVisible = Time.unscaledTime;
                }
            }
        }

        public Color SelectedColor
        {
            get { return selectedColor; }
        }

        [SerializeField]
        private bool visible = false;
        [SerializeField]
        private Transform selectorTransform;
        [SerializeField]
        private Renderer selectorRenderer;
        [SerializeField]
        private float inputScale = 1.1f;
        [SerializeField]
        private Color selectedColor = Color.white;
        [SerializeField]
        private Texture2D colorWheelTexture;
        [SerializeField]
        private GameObject colorWheelObject;
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private float timeout = 2f;

        private Vector2 selectorPosition;
        private float lastTimeVisible;
        private bool visibleLastFrame = false;

        private void Update()
        {
            if (controller == null)
            {
                return;
            }

            if (Time.unscaledTime > lastTimeVisible + timeout)
            {
                visible = false;
            }

            if (visible != visibleLastFrame)
            {
                if (visible)
                {
                    animator.SetTrigger("Show");
                }
                else
                {
                    animator.SetTrigger("Hide");
                }
            }
            visibleLastFrame = visible;

            if (!visible)
            {
                return;
            }

            // clamp selector position to a radius of 1
            Vector3 localPosition = new Vector3(selectorPosition.x * inputScale, 0.15f, selectorPosition.y * inputScale);
            if (localPosition.magnitude > 1)
            {
                localPosition = localPosition.normalized;
            }
            selectorTransform.localPosition = localPosition;
            // Raycast the wheel mesh and get its UV coordinates
            Vector3 raycastStart = selectorTransform.position + selectorTransform.up * 0.15f;
            RaycastHit hit;
            Debug.DrawLine(raycastStart, raycastStart - (selectorTransform.up * 0.25f));
            if (Physics.Raycast(raycastStart, -selectorTransform.up, out hit, 0.25f, 1 << colorWheelObject.layer, QueryTriggerInteraction.Ignore))
            {
                Vector2 uv = hit.textureCoord;
                int pixelX = Mathf.FloorToInt(colorWheelTexture.width * uv.x);
                int pixelY = Mathf.FloorToInt(colorWheelTexture.height * uv.y);
                selectedColor = colorWheelTexture.GetPixel(pixelX, pixelY);
                selectedColor.a = 1f;
            }
            // Set the selector's color
            // Blend it with white to make it visible on top of the wheel
            selectorRenderer.material.color = Color.Lerp (selectedColor, Color.white, 0.5f);
        }

        protected override void OnAttachToController()
        {
            // Subscribe to input now that we're parented under the controller
            InteractionManager.InteractionSourceUpdated += InteractionSourceUpdated;
        }

        protected override void OnDetachFromController()
        {
            Visible = false;

            // Unsubscribe from input now that we've detached from the controller
            InteractionManager.InteractionSourceUpdated -= InteractionSourceUpdated;
        }

        public void OnPointerTarget(PhysicsPointer source)
        {
            Visible = true;

            // If we're opening or closing, don't set the color value
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Show") || stateInfo.IsName("Hide"))
            {
                return;
            }

            Vector3 localHitPoint = selectorTransform.parent.InverseTransformPoint(source.TargetPoint);
            selectorPosition = new Vector2(localHitPoint.x, localHitPoint.z);
        }

        private void InteractionSourceUpdated(InteractionSourceUpdatedEventArgs obj)
        {
            if (obj.state.source.handedness == handedness && obj.state.touchpadTouched)
            {
                Visible = true;
                selectorPosition = obj.state.touchpadPosition;
            }
        }
    }
}