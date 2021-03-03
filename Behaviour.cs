using System;
using UnityEngine;

namespace GrassyKnight
{
	// For when you just want a MonoBehavior... We use this as a simple means
	// of getting access to Unity's coroutine scheduler.
	class Behaviour : MonoBehaviour {
		public event EventHandler OnUpdate;

		public void Update() {
			OnUpdate?.Invoke(this, EventArgs.Empty);
		}

		public static Behaviour CreateBehaviour() {
			// A game object that will do nothing quietly in the corner until
			// the end of time.
			GameObject dummy = new GameObject(
				"Behavior Container",
				typeof(Behaviour));
			UnityEngine.Object.DontDestroyOnLoad(dummy);

			return dummy.GetComponent<Behaviour>();
		}
	}
}
