package JLR;

import org.junit.jupiter.api.Test;

import static JLR.Assertions.*;

public class CameraTest
{
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
