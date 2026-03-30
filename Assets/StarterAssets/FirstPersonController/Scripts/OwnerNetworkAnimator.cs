using Unity.Netcode.Components;
using UnityEngine;

namespace StarterAssets.FirstPersonController.Scripts
{
    /// <summary>
    /// A custom NetworkAnimator that allows client authority.
    /// Used to sync Animator from the Owner (client) to the Server and out to other clients.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class OwnerNetworkAnimator : NetworkAnimator
    {
        /// <summary>
        /// Determine if the server is authoritative.
        /// Returning false means the Owner is authoritative.
        /// </summary>
        /// <returns>False to allow Owner authority over the Animator</returns>
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}

