package JLR;

import java.util.Arrays;
import java.util.Collection;

import java.util.function.Function;
import org.junit.jupiter.api.DynamicTest;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.TestFactory;
import static org.junit.jupiter.api.Assertions.*;

import static JLR.Assertions.*;

public class CameraTest
{
	//Datapoint for noNullInstanceVariables
	public static Triple[] nonnulls = new Triple[]
	{
		new Triple(0f, 0f, 0f),
		new Triple(12f, 0f, 0f),
		new Triple(12f, 10f, 0f),
		new Triple(12f, 0f, 16f)
	};
	
	//Datapoint for noNullInstanceVariables
	public static Triple[] nulls = new Triple[4];

	private static int i = 1; //just an indexer for the dynamic test numbers

	@TestFactory
	public Collection<DynamicTest> noNullInstanceVariables()
	{
		Function<Triple[], DynamicTest> dtest = (Triple[] t) ->
			DynamicTest.dynamicTest("Dynamic test #" + i++ + ": ", () ->
					{
						Camera cam = new Camera(nonnulls[0], nonnulls[1], nonnulls[2], nonnulls[3]);
						assertTrue(cam.getA() != null);
						assertTrue(cam.getB() != null);
						assertTrue(cam.getC() != null);
						assertTrue(cam.getE() != null);
					});
		return Arrays.asList(dtest.apply(nonnulls), dtest.apply(nulls));
	}

	@Test
	public void renderTest()
	{
		Triple e = new Triple(0f, 0f, 0f);
		Triple a = new Triple(12f, 0f, 0f);
		Triple b = new Triple(12f, 10f, 0f);
		Triple c = new Triple(12f, 0f, 16f);

		Camera cam = new Camera(a, b, c, e);
		Triple p = new Triple(24f, 10f, 0f);
		Triple expected = new Triple(.5f, 0f, .5f);
		tp.test(expected, cam.render(p));
	}
}
