using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.Animations;
using UnityEngine;

namespace Assets.Code.Physics
{
    public delegate void Callback();

    public class Accelerator
    {
        //All values are massless, based on scalar acceleration
        readonly Callback _donothing = delegate () { return; };

        Func<float, float, float, float> _position;
        Func<float, float, float, float> _velocity;
        float _a;
        float _bias; //used for more accurate, time-dependant, first frame calculations
        float _lowerBound;
        float _initialLowerBound;
        Func<bool> _termConditions;
        Callback _callback;
        float _termUpperbound;
        bool _active;

        public bool Terminated { get; private set; }

        public Accelerator() { _active = false; }

        public void Mutate(float a, float bias, float lowerBound, float termUpperbound, Func<float, float, float, float> position, Func<float, float, float, float> velocity, Func<bool> termConditions, Callback callback)
        //Like a constructor, but prevents us from allocating more new memory than we need
        {
            _a = a;
            _bias = bias;
            _lowerBound = lowerBound;
            Terminated = false;
            _termConditions = termConditions;
            _termUpperbound = termUpperbound;
            _callback = callback == null ? _donothing : callback;
            _position = position;
            _velocity = velocity;
            _active = true;

            _initialLowerBound = lowerBound;
        }

        public void Reset()
        {
            _active = false;
        }

        // Properties
        public float TermUpperBound { get { return _termUpperbound; }  set { _termUpperbound = value; } } //special mutator, used in callbacks to change the terminating factor if we need to
        public float A { get { return _a; } }
        public bool Active { get { return _active; } }
        public float LowerBound { get { return _lowerBound; } }
        float UpperBound { get { return _lowerBound + Time.deltaTime + _bias; } }
        float UpdateAndUpper
        {
            get
            {
                Terminated = _termConditions();
                _lowerBound = _lowerBound + Time.deltaTime + _bias;
                _bias = 0;
                return _lowerBound;
            }
        }

        //Public methods
        public float Position()
        {
            float lower = _lowerBound;
            float upper = UpdateAndUpper;
            if(Terminated)
            {
                _callback();
            }
            upper = (upper <= _termUpperbound) ? upper : _termUpperbound;
            float retVal = _position(_a, lower, upper);
            return retVal;
        }

        public float Velocity()
        {
            return _velocity(_a, _initialLowerBound, _termUpperbound);
        }
    }
}
