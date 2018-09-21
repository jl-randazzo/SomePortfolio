package JLR;

import java.nio.FloatBuffer;

public class Color implements Bufferable
{
	private float r;
	private float g;
	private float b;
	private float a;

	public Color(float r, float g, float b, float a)
	{
		this.r = r;
		this.g = g;
		this.b = b;
		this.a = a;
	}

	@Override
	public void sendData(FloatBuffer fb)
	{
		fb.put(r);
		fb.put(g);
		fb.put(b);
		fb.put(a);
	}
}
