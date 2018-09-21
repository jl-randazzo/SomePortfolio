package JLR;

import java.nio.FloatBuffer;

import static org.junit.jupiter.api.Assertions.*;

public class Assertions
{

	public interface Assertable<T>
	{
		void test(T a, T b);
	}

	public static final Assertable<Float> ft = (Float a, Float b) -> assertEquals(a, b, 1e-7f); 
	public static final Assertable<Triple> tp = (Triple a, Triple b) -> assertEquals(a, b);
	public static final Assertable<FloatBuffer> fbt = (FloatBuffer a, FloatBuffer b) -> assertEquals(a, b);
}
