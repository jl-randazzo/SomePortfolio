package JLR;

import java.nio.FloatBuffer;

public class Tri implements Bufferable
{
	private Bufferable v1;
	private Bufferable v2;
	private Bufferable v3;

	public Tri(Bufferable v1, Bufferable v2, Bufferable v3)
	{
		this.v1 = v1;
		this.v2 = v2;
		this.v3 = v3;
	}

	@Override
	public void sendData(FloatBuffer fb)
	{
		v1.sendData(fb);
		v2.sendData(fb);
		v3.sendData(fb);
	}
}
