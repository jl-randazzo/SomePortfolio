package JLR; 

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.BeforeAll;
import java.nio.FloatBuffer;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;

import static org.mockito.Mockito.*;
import org.mockito.stubbing.*;
import org.mockito.invocation.InvocationOnMock;

import static JLR.Assertions.*; // used for ft, fbt, and tp tests	

public class TriTest
{
	static Bufferable a;
	static Bufferable b;
	static Bufferable c;

	static Bufferable[] bufferables;

	static FloatBuffer expected;

	@BeforeAll
	public static void setup()
	{
		Answer<Object> answer = new Answer<Object>()
		{
			public Object answer(InvocationOnMock invocation) throws Throwable 
			{
				FloatBuffer fb = (FloatBuffer)invocation.getArguments()[0];
				fb.put(new float[]{ 1f, 2f, 3f});
				return null;
			}
		};

		a = mock(Bufferable.class);
		b = mock(Bufferable.class);
		c = mock(Bufferable.class);

		bufferables = new Bufferable[]{a, b, c};

		for(Bufferable x: bufferables)
		{
			doThrow(IllegalArgumentException.class).when(x).sendData(null);
			doAnswer(answer).when(x).sendData(any(FloatBuffer.class));
		}

		expected = ByteBuffer.allocateDirect(3 * 3 * 4).order(ByteOrder.nativeOrder()).asFloatBuffer();
		expected.put(new float[] { 1f, 2f, 3f, 1f, 2f, 3f, 1f, 2f, 3f } );
	}

	@Test
	public void testSendDataToChildren()
	{
		Tri tri = new Tri(a, b, c);
		FloatBuffer fb = ByteBuffer.allocateDirect(3 * 3 * 4).order(ByteOrder.nativeOrder()).asFloatBuffer();
		tri.sendData(fb);
		fb.rewind();
		expected.rewind();
		fbt.test(expected, fb);//Assertion
	}
}
