package JLR;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeAll;

import static JLR.Assertions.*; // calls to tp and ft reference static functions in the Assertions class

public class TripleTest
{
	private static Triple v, w;

	@BeforeAll
	public static void setup()
	{
		v = new Triple(1, 2, 3);
		w = new Triple(3, 2, 1);
	}

	@Test
	public void testDotProduct()
	{
		ft.test(10f, v.dot(w));
	}

	@Test
	public void testCrossProduct()
	{
		Triple expected = new Triple(-4, 8, -4);
		Triple result = v.cross(w);
		tp.test(expected, result);
	}

	@Test
	public void testMinus()
	{
		Triple expected = new Triple(-2, 0, 2);
		Triple result = v.minus(w);
		tp.test(expected, result);
	}

	@Test
	public void testPlus()
	{
		Triple expected = new Triple(4, 4, 4);
		Triple result = v.plus(w);
		tp.test(expected, result);
	}

	@Test
	public void testNorm()
	{
		float expected = 3.741657387f;
		ft.test(expected, v.norm());	
	}

	@Test
	public void testNormalize()
	{
		float norm = 3.741657387f;
		Triple expected = new Triple(1f/norm, 2f/norm, 3f/norm);
		tp.test(expected, v.normalize());	
	}
}
