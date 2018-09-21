package JLR;

import java.nio.ByteBuffer;
import java.nio.FloatBuffer;
import java.util.Scanner;

public class Triple implements Bufferable
{
	public static final Triple zeroTriple = new Triple(0,0,0);

	public final float x; //accessible but immutable
	public final float y; //accessible but immutable
	public final float z; //accessible but immutable
	
	public Triple(float x, float y, float z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public Triple(Scanner sc)
	{
		x = (float)sc.nextDouble();
		y = (float)sc.nextDouble();
		z = (float)sc.nextDouble();
	}


	public float dot(Triple v)
	{
		return (v.x * x + v.y * y + v.z * z);
	}

	public Triple cross(Triple v)
	{
		float rx = y * v.z - z * v.y;
		float ry = z * v.x - x * v.z;
		float rz = x * v.y - y * v.x;
		return new Triple(rx, ry, rz);
	}

	public Triple minus(Triple v)
	{
		float rx = x - v.x;
		float ry = y - v.y;
		float rz = z - v.z;
		return new Triple(rx, ry, rz);
	}

	public Triple plus(Triple v)
	{
		float rx = x + v.x;
		float ry = y + v.y;
		float rz = z + v.z;
		return new Triple(rx, ry, rz);
	}

	public float norm()
	{
		return (float)Math.sqrt(x*x + y*y + z*z);
	}

	public Triple normalize()
	{
		float norm = norm();
		return new Triple(x/norm, y/norm, z/norm);
	}

	private final float delta = 1e-7f; //accounting for floating point imprecision in equality testing
	@Override
	public boolean equals(Object o)
	{

		if(o == this) return true;
		else if (!(o instanceof Triple)) return false;
		Triple v = (Triple) o;
		if(Math.abs(v.x - x) <= delta && Math.abs(v.y - y) <= delta && Math.abs(v.z - z) <= delta) return true;
		return false;
	}

	@Override
	public String toString()
	{
		return (String)(x + " " + y + " " + z);
	}
	
	@Override
	public void sendData(FloatBuffer fb)
	{
		fb.put(x);
		fb.put(y);
		fb.put(z);
	}
}	
