using System;
using System.Numerics;
using Assets.Code.Physics.Collision;

using NUnit.Framework;

namespace Assets.Code.Tests.Physics.Collision {
    public class VectorRangeTest
    {
        [Datapoint] private float _zero = 0;
        [Datapoint] private float _one = 1f;
        [Datapoint] private float _negOne = -1f;
        [Datapoint] private float _pointOne = .1f;
        [Datapoint] private float _negPointOne = -.1f;
        [Datapoint] private float _maxFloat = 10;
        [Datapoint] private float _minFloat = -10;

        [Theory]
        public void TestFirstQuadrantSuccess(float x, float y)
        {
            VectorRange range = new VectorRange(Vector2.up, Vector2.left);
            Assume.That(!(x == 0 && y == 0));
            Assume.That(x <= 0 && x >= _minFloat && y >= 0 && y <= _maxFloat);
            Assert.True(range.Contains(new Vector2(x, y)));
        }

        [Theory]
        public void TestFirstQuadrantFailure(float x, float y)
        {
            VectorRange range = new VectorRange(Vector2.up, Vector2.left);
            Assume.That(!(x == 0 && y == 0));
            Assume.That((x > 0 && x <= _maxFloat) || (y < 0 && y >= _minFloat));
            Assert.False(range.Contains(new Vector2(x, y)));
        }

        [Theory]
        public void TestSecondQuadrantSuccess(float x, float y)
        {
            VectorRange range = new VectorRange(new Vector2(-1, 1), new Vector2(1, -1));
            Assume.That(!(x == 0 && y == 0));
            Assume.That((y >= 0 && x <= 0 && y <= Mathf.Abs(x)) || (x <= 0 && y <= 0) || (x >= 0 && y <= 0 && x <= Mathf.Abs(y)));
            Assert.True(range.Contains(new Vector2(x, y)));
        }

        [Theory]
        public void TestSecondQuadrantFailure(float x, float y)
        {
            VectorRange range = new VectorRange(new Vector2(-1, 1), new Vector2(1, -1));
            Assume.That(!(x == 0 && y == 0));
            Assume.That((y <= 0 && x >= 0 && x > Mathf.Abs(y)) || (x >= 0 && y >= 0) || (x <= 0 && y >= 0 && y > Mathf.Abs(x)));
            Assert.False(range.Contains(new Vector2(x, y)));
        }

        [Theory]
        public void TestThirdQuadrantSuccess(float x, float y)
        {
            VectorRange range = new VectorRange(new Vector2(1, .1f), new Vector2(0, -1));
            Assume.That(!(x == 0 && y == 0));
            Assume.That((y >= 0 && x >= 0 && y >= .1f * x) || (x <= 0 && y >= 0) || (y <= 0 && x <= 0));
            Assert.True(range.Contains(new Vector2(x, y)));
        }

        [Theory]
        public void TestThirdQuadrantFailure(float x, float y)
        {
            VectorRange range = new VectorRange(new Vector2(1, .1f), new Vector2(0, -1));
            Assume.That(!(x == 0 && y == 0));
            Assume.That((y <= 0 && x > 0) || (x >= 0 && y >= 0 && y < .1f * x));
            Assert.False(range.Contains(new Vector2(x, y)));
        }

        [Theory]
        public void TestFourthQuadrantSuccess(float x, float y)
        {
            VectorRange range = new VectorRange(new Vector2(1, 1f), new Vector2(1, 0));
            Assume.That(!(x == 0 && y == 0));
            Assume.That((x <= 0) || (x >= 0 && y <= 0) || (x >= 0 && y >= 0 && y >= x));
            Assert.True(range.Contains(new Vector2(x, y)));
        }

        [Theory]
        public void TestFourthQuadrantFailure(float x, float y)
        {
            VectorRange range = new VectorRange(new Vector2(1, 1f), new Vector2(1, 0));
            Assume.That(!(x == 0 && y == 0));
            Assume.That((x >= 0 && y > 0 && y < x) );
            Assert.False(range.Contains(new Vector2(x, y)));
        }
    }
}
