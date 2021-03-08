using System;
using UnityEngine;

namespace GrassPls
{
    // A handy box to store some grass in. Used to store a reference to the
    // grass that ShouldCut is getting called for because ShouldCut is a
    // static function.
    class GrassyBox : IDisposable {
        private static GameObject _value = null;
        private static bool _hasValue = false;

        public static GameObject GetValue() {
            if (_hasValue) {
                return _value;
            } else {
                throw new InvalidOperationException("Nothing in box");
            }
        }

        public GrassyBox(GameObject value) {
            if (_hasValue) {
                GrassPls.Instance.LogError(
                    $"Already have value in box (current value is {_value}, " +
                    $"trying to store value {value}).");
            } else {
                _value = value;
                _hasValue = true;
            }
        }

        public void Dispose() {
            _value = null;
            _hasValue = false;
        }
    }
}
