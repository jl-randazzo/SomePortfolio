using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.Animations;
using UnityEngine;

namespace Assets.Code.Physics
{
    public static class StaticPhysicsCalculations
    {
        /*
        * The x and y components are calculated as a function of initial velocity (_xVelocity or _yVelocity) multiplied by time summed with the impact of the scalar accelerators in delta time.
        * If the termination conditions for any of the accelerators are satisfied, then the accumulated velocity during its entire interval is added to _x or _y Velocity, becoming the new V0 for the corresponding component, and the component is reset.
        */
        public static Vector2 CalculateKinematicTransform(IEnumerable<Accelerator> xAccelerators, IEnumerable<Accelerator> yAccelerators, ref float xVelocity, ref float yVelocity)
        {
            Vector2 updateTransform;
            updateTransform.x = xVelocity * Time.deltaTime;
            updateTransform.y = yVelocity * Time.deltaTime;

            foreach(var y in yAccelerators)
            {
                if(y.Active)
                {
                    updateTransform.y += y.Position();
                    if(y.Terminated)
                    {
                        yVelocity = yVelocity + y.Velocity();
                        updateTransform.y += y.Velocity() * (y.LowerBound - y.TermUpperBound);
                        y.Reset();
                    }
                }
            }

            foreach(var x in xAccelerators)
            {
                if(x.Active)
                {
                    updateTransform.x += x.Position();
                    if(x.Terminated)
                    {
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
