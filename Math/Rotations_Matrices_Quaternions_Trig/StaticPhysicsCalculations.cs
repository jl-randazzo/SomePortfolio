using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.Physics.Utilities {
    public static class StaticPhysicsCalculations {
        public static void RotateAround(GameObject gameObject, Quaternion rotation, Vector3 point) {
            Vector3 v = gameObject.transform.position - point;
            Vector3 w = rotation * v;

            gameObject.transform.position += (-v + w);
            gameObject.transform.rotation = rotation * gameObject.transform.rotation;
        }

        public static Vector3 ProjectOnInDirection(Vector3 v, Vector3 w, Vector3 inDir) {
            Vector3 n = Vector3.Cross(v, w);
            Vector3 wOrth = Vector3.Cross(n, w);
            float lambda = -(Vector3.Dot(wOrth, v) / Vector3.Dot(wOrth, inDir));
            lambda = Single.IsNaN(lambda) ? 0 : lambda;
            return v + lambda * inDir;
        }

        public static void QuadraticFormula(float a, float b, float c, out float sol_a, out float sol_b) {
            float b_sq_min_4_a_c = Mathf.Pow(b, 2) - 4 * a * c;
            float root = Mathf.Sqrt(b_sq_min_4_a_c);
            sol_a = (-b + root) / (2 * a);
            sol_b = (-b - root) / (2 * a);
        }

        public static Vector3 InterpolateNormalized(Vector3 a, Vector3 b, float percentB) {
            return ((b * percentB) + (a * (1 - percentB))).normalized;
        }

        public static Vector2 Project(Vector2 u, Vector2 v) {
            return (Vector2.Dot(u, v) / Vector2.Dot(v, v)) * v;
        }
        public static Vector2 LocalUp(Transform trasform) {
            return trasform.rotation * Vector2.up;
        }
        public static Vector2 LocalRight(Transform transform) {
            return transform.rotation * Vector2.right;
        }
        public static Vector2 LocalLeft(Transform transform) {
            return transform.rotation * Vector2.left;
        }
        public static Vector2 LocalDown(Transform transform) {
            return transform.rotation * Vector2.down;
        }
        public static Quaternion GetQuaternionBetweenNormalizedVectors(Vector3 from, Vector3 to) {
            Vector3 avVector = ((from + to) / 2).normalized;
            double cosTh_2 = Vector3.Dot(avVector, from);
            double sinTh_2 = Math.Sqrt(1 - Math.Pow(cosTh_2, 2));
            sinTh_2 = Double.IsNaN(sinTh_2) ? 0 : sinTh_2;
            Vector3 ijk = Vector3.Cross(from, to).normalized * (float)sinTh_2;
            return new Quaternion(ijk.x, ijk.y, ijk.z, (float)cosTh_2);
        }

        /*
        * The x and y components are calculated as a function of initial velocity (_xVelocity or _yVelocity) multiplied by time summed with the impact of the scalar accelerators in delta time.
        * If the termination conditions for any of the accelerators are satisfied, then the accumulated velocity during its entire interval is added to _x or _y Velocity, becoming the new V0 for the corresponding component, and the component is reset.
        */
        public static float microCorrection = .02f;//used to correct physics and put casters on the correct side of boxes
        public static Vector2 CalculateKinematicTransform(IEnumerable<Accelerator> xAccelerators, IEnumerable<Accelerator> yAccelerators, ref float xVelocity, ref float yVelocity) {
            Vector2 updateTransform;
            updateTransform.x = 0;
            updateTransform.x += xVelocity * Time.deltaTime;
            updateTransform.y = yVelocity * Time.deltaTime;

            foreach (var y in yAccelerators) {
                if (y.Active) {
                    //UnityEngine.Debug.Log("yVelocity before: " + yVelocity + "\n" + y.statedata); //helpful debugging line
                    updateTransform.y += y.Position();
                    if (y.Terminated) {
                        yVelocity = yVelocity + y.Velocity();
                        //updateTransform.y += y.Velocity() * (y.LowerBound - y.TermUpperBound);
                        y.Reset();
                    }
                    //UnityEngine.Debug.Log("yVelocity: " + yVelocity + "\n" + y.statedata);
                }
            }

            foreach (var x in xAccelerators) {
                if (x.Active) {
                    updateTransform.x += x.Position();
                    if (x.Terminated) {
                        xVelocity = xVelocity + x.Velocity();
                        updateTransform.x += x.Velocity() * (x.LowerBound - x.TermUpperBound);
                        x.Reset();
                    }
                }
            }
            return updateTransform;
        }
    }
}
