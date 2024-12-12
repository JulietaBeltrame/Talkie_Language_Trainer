/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Oculus.Interaction.HandGrab;
using System;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// The <see cref="DistanceGrabInteractable"/> class allows an object to be grabbed from a distance.
    /// It leverages a configurable <see cref="IMovementProvider"/> to control how the grab interactable aligns with
    /// the <see cref="DistanceGrabInteractor"/> when selected. This functionality facilitates the grabbing and manipulation of objects remotely.
    /// </summary>
    /// <remarks>
    /// A reference to an <see cref="IPointableElement"/> must be set in the Unity Inspector.
    /// The <see cref="IPointableElement"/> serves as a target for <see cref="PointerEvent"/>s,
    /// which are generated by the <see cref="DistanceGrabInteractor"/>. These events are needed to trigger the distance grab interaction.
    /// </remarks>
    public class DistanceGrabInteractable : PointerInteractable<DistanceGrabInteractor, DistanceGrabInteractable>,
        IRigidbodyRef, IRelativeToRef, ICollidersRef
    {
        private Collider[] _colliders;
        /// <summary>
        /// The colliders associated with this interactable, used for interaction detection.
        /// These colliders define the physical boundaries of the interactable object,
        /// allowing it to be detected and interacted with by the <see cref="DistanceGrabInteractor"/>.
        /// </summary>
        public Collider[] Colliders => _colliders;

        /// <summary>
        /// The RigidBody of the interactable.
        /// </summary>
        [Tooltip("The RigidBody of the interactable.")]
        [SerializeField]
        Rigidbody _rigidbody;
        /// <summary>
        /// The [Rigidbody](https://docs.unity3d.com/ScriptReference/Rigidbody.html) component of the interactable object.
        /// The rigidbody defines the physical behavior of the interactable, such as its movement and collision responses.
        /// </summary>
        public Rigidbody Rigidbody => _rigidbody;

        /// <summary>
        /// An optional origin point for the grab, determining the position from which the grab action is performed.
        /// The grab source is used as the reference point for generating movement and positioning the object relative to the user’s hand or controller.
        /// </summary>
        [Tooltip("An optional origin point for the grab.")]
        [SerializeField, Optional]
        private Transform _grabSource;

        /// <summary>
        /// Forces a release on all other grabbing interactors when grabbed by a new interactor.
        /// This feature ensures that the object is only held by one interactor at a time, preventing
        /// multiple controllers or hands from interacting with the same object simultaneously.
        /// </summary>
        [Tooltip("Forces a release on all other grabbing interactors when grabbed by a new interactor.")]
        [SerializeField]
        private bool _resetGrabOnGrabsUpdated = true;

        /// <summary>
        /// Reference to the <see cref="PhysicsGrabbable"/> component used when the interactable object is grabbed.
        /// This property is marked as obsolete and should no longer be used. Consider using <see cref="Grabbable"/> or
        /// <see cref="RigidbodyKinematicLocker"/>.
        /// </summary>
        [Tooltip("PhysicsGrabbable used when you grab the interactable.")]
        [SerializeField, Optional(OptionalAttribute.Flag.Obsolete)]
        [Obsolete("Use " + nameof(Grabbable) + " and/or " + nameof(RigidbodyKinematicLocker) + " instead")]
        private PhysicsGrabbable _physicsGrabbable = null;

        /// <summary>
        /// The <see cref="IMovementProvider" /> specifies how the interactable will align with the grabber when selected.
        /// If no <see cref="IMovementProvider" /> is set, the <see cref="MoveTowardsTargetProvider" /> is created and used as the provider.
        /// </summary>
        [Tooltip("The IMovementProvider specifies how the interactable will align with the grabber when selected. If no IMovementProvider is set, the MoveTowardsTargetProvider is created and used as the provider.")]
        [Header("Snap")]
        [SerializeField, Optional, Interface(typeof(IMovementProvider))]
        private UnityEngine.Object _movementProvider;
        private IMovementProvider MovementProvider { get; set; }

        #region Properties
        /// <summary>
        /// Forces a release on all other grabbing interactors when grabbed by a new interactor.
        /// This feature ensures that the object is only held by one interactor at a time, preventing multiple controllers or hands from interacting with
        /// the same object simultaneously.
        /// </summary>
        public bool ResetGrabOnGrabsUpdated
        {
            get
            {
                return _resetGrabOnGrabsUpdated;
            }
            set
            {
                _resetGrabOnGrabsUpdated = value;
            }
        }

        /// <summary>
        /// Gets the [Transform](https://docs.unity3d.com/ScriptReference/Transform.html) that acts as the reference point for the interactable.
        /// This property returns the transform that defines the origin or "relative to" point for the interactable's movement and positioning.
        /// </summary>
        public Transform RelativeTo => _grabSource;

        #endregion

        #region Editor Events

        protected virtual void Reset()
        {
            _rigidbody = this.GetComponentInParent<Rigidbody>();
        }

        #endregion

        protected override void Awake()
        {
            base.Awake();
            MovementProvider = _movementProvider as IMovementProvider;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Rigidbody, nameof(Rigidbody));
            _colliders = Rigidbody.GetComponentsInChildren<Collider>();
            if (MovementProvider == null)
            {
                MoveTowardsTargetProvider movementProvider = this.gameObject.AddComponent<MoveTowardsTargetProvider>();
                InjectOptionalMovementProvider(movementProvider);
            }
            if (_grabSource == null)
            {
                _grabSource = Rigidbody.transform;
            }
            this.EndStart(ref _started);
        }

        /// <summary>
        /// Moves the interactable to the provided position.
        /// </summary>
        /// <param name="to">The target <see cref="Pose"/> to which the interactable will be moved.</param>
        /// <returns>
        /// An instance of <see cref="IMovement"/> that defines how the interactable should move to the target position.
        /// </returns>
        public IMovement GenerateMovement(in Pose to)
        {
            Pose source = _grabSource.GetPose();
            IMovement movement = MovementProvider.CreateMovement();
            movement.StopAndSetPose(source);
            movement.MoveTo(to);
            return movement;
        }

        /// <summary>
        /// Applies velocities to the interactable's <see cref="PhysicsGrabbable" /> component, if one is present.
        /// This method allows the interactable to be thrown or moved with a specified angular and linear velocity after being released.
        /// This method is marked as obsolete and should no longer be used. Use <see cref="Grabbable"/> instead.
        /// </summary>
        [Obsolete("Use " + nameof(Grabbable) + " instead")]
        public void ApplyVelocities(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            if (_physicsGrabbable == null)
            {
                return;
            }
            _physicsGrabbable.ApplyVelocities(linearVelocity, angularVelocity);
        }

        #region Inject

        /// <summary>
        /// Injects a [Rigidbody](https://docs.unity3d.com/ScriptReference/Rigidbody.html) component into a dynamically instantiated GameObject, allowing for physics-based interactions.
        /// This method is a wrapper that calls <see cref="InjectRigidbody"/> internally.
        /// </summary>
        /// <param name="rigidbody">The Rigidbody component to be assigned.</param>
        public void InjectAllGrabInteractable(Rigidbody rigidbody)
        {
            InjectRigidbody(rigidbody);
        }

        /// <summary>
        /// Directly assigns a [Rigidbody](https://docs.unity3d.com/ScriptReference/Rigidbody.html) to the private field of a dynamically instantiated GameObject.
        /// </summary>
        /// <param name="rigidbody">The Rigidbody component to be assigned.</param>
        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        /// <summary>
        /// Adds a grab source to a dynamically instantiated GameObject, defining the point of interaction for grabbing.
        /// </summary>
        /// <param name="grabSource">The Transform that represents the grab source.</param>
        public void InjectOptionalGrabSource(Transform grabSource)
        {
            _grabSource = grabSource;
        }

        /// <summary>
        /// Adds a <see cref="PhysicsGrabbable" /> to a dynamically instantiated GameObject.
        /// This method is marked as obsolete and should no longer be used. Use <see cref="Grabbable"/> instead.
        /// </summary>
        /// <param name="physicsGrabbable">The PhysicsGrabbable component to add.</param>
        [Obsolete("Use " + nameof(Grabbable) + " instead")]
        public void InjectOptionalPhysicsGrabbable(PhysicsGrabbable physicsGrabbable)
        {
            _physicsGrabbable = physicsGrabbable;
        }

        /// <summary>
        /// Adds a <see cref="IMovementProvider" /> to a dynamically instantiated GameObject, allowing you to add custom movement logic.
        /// </summary>
        /// <param name="provider">The movement provider to inject, which controls the movement logic.</param>
        public void InjectOptionalMovementProvider(IMovementProvider provider)
        {
            _movementProvider = provider as UnityEngine.Object;
            MovementProvider = provider;
        }
        #endregion
    }
}
