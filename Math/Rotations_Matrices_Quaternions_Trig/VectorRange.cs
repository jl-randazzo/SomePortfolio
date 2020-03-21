
using System;
using Assets.Code.GameCode.Physics.Collision;
using UnityEngine;

namespace Assets.Code.Physics.Collision {
    /*
     * Range is interpreted counter clockwise
     */
    public struct VectorRange {
        public static VectorRange Any = new VectorRange(0);
        private readonly Vector2 _least;
        private readonly Vector2 _greatest;
        private readonly Matrix4x4 _rotMat;
        private readonly float _cos_th, _sin_th;

        private delegate bool RoutineFunc(VectorRange range, Vector2 vect);
        private static readonly RoutineFunc[] funcs = new RoutineFunc[] { FirstQuadrantCheck, SecondQuadrantCheck, ThirdQuadrantCheck, FourthQuadrantCheck, AnyCheck };

        private readonly int _routineIndex;
        public readonly string name;

        private VectorRange(int i = 0) {
            name = "";
            _rotMat = Matrix4x4.identity;
            _routineIndex = 4;
            _least = Vector2.zero;
            _greatest = Vector2.zero;
            CalculateCosAndSinTheta(out _cos_th, out _sin_th, _greatest);
        }

        public VectorRange(Vector2 least, Vector2 greatest, string name = "") {
            least = least.normalized;
            greatest = greatest.normalized;

            CalculateCosAndSinTheta(out float cos_th, out float sin_th, least);
            _rotMat = new Matrix4x4(new Vector4(cos_th, -sin_th), new Vector4(sin_th, cos_th), Vector4.zero, Vector4.zero);

            _least = _rotMat.MultiplyVector(least);
            _greatest = _rotMat.MultiplyVector(greatest);

            // designate which check routine will be used
            CalculateCosAndSinTheta(out _cos_th, out _sin_th, _greatest);
            if (_cos_th >= 0) {
                _routineIndex = _sin_th >= 0 ? 0 : 3;
            } else {
                _routineIndex = _sin_th >= 0 ? 1 : 2;
            }
            this.name = name;
        }

        public bool Contains(Vector2 vector) {
            vector = _rotMat.MultiplyVector(vector.normalized);
            return funcs[_routineIndex].Invoke(this, vector);
        }

        public static void CalculateCosAndSinTheta(out float cos_th, out float sin_th, Vector2 vector) {
            cos_th = Mathf.Round(10000 * Vector2.Dot(vector, Vector2.right)) / 10000f;
            sin_th = Mathf.Round(Vector2.Dot(vector, Vector2.up) * 10000) / 10000;
        }

        private static bool FirstQuadrantCheck(VectorRange range, Vector2 vector) {
            CalculateCosAndSinTheta(out float cos_phi, out float sin_phi, vector);
            return vector.y >= 0 && cos_phi >= range._cos_th && sin_phi <= range._sin_th;
        }

        private static bool SecondQuadrantCheck(VectorRange range, Vector2 vector) {
            CalculateCosAndSinTheta(out float cos_phi, out float sin_phi, vector);
            return vector.y >= 0 && cos_phi >= range._cos_th;
        }

        private static bool ThirdQuadrantCheck(VectorRange range, Vector2 vector) {
            CalculateCosAndSinTheta(out float cos_phi, out float sin_phi, vector);
            return vector.y >= 0 || cos_phi <= range._cos_th;
        }

        private static bool FourthQuadrantCheck(VectorRange range, Vector2 vector) {
            CalculateCosAndSinTheta(out float cos_phi, out float sin_phi, vector);
            return vector.y >= 0 || cos_phi <= range._cos_th;
        }

        private static bool AnyCheck(VectorRange range, Vector2 vector2) {
            return true;
        }

        public override bool Equals(object obj) {
            if (obj.GetType() != typeof(VectorRange))
                return false;
            VectorRange other = (VectorRange)obj;
            return other._least == _least && other._greatest == _greatest && other._routineIndex == _routineIndex;
        }
    }
}
