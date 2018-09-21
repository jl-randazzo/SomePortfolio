package JLR;

import java.nio.FloatBuffer;
import java.util.Scanner;

public class Camera implements Bufferable
{
	private Triple a;
	private Triple b;
	private Triple c;
	private Triple e;
	private Triple p;

	private Triple ema;
	private Triple bma;
	private Triple cma;
	private Triple pme;

	public Camera(Triple a, Triple b, Triple c, Triple e)
	{
		this.a = a == null ? Triple.zeroTriple : a;
		this.b = b == null ? Triple.zeroTriple : b;
		this.c = c == null ? Triple.zeroTriple : c;
		this.e = e == null ? Triple.zeroTriple : e;
		this.p = Triple.zeroTriple; //non-null
		calculateMainVectors();
	}

	public Camera(Scanner sc)
	{
		a = new Triple(sc);
		b = new Triple(sc);
		c = new Triple(sc);
		e = new Triple(sc);
		p = Triple.zeroTriple;
		calculateMainVectors();
	}

	private void calculateMainVectors()
	{
		ema = e.minus(a);
		bma = b.minus(a);
		cma = c.minus(a);
		pme = p.minus(e);
	}

	//external mutator for target point p, semantically designed to support cam.setP(p).sendData(fb);
	public Camera setP(Triple p)
	{
		this.p = p;
		return this;
	}

	@Override
	public void sendData(FloatBuffer fb)
	{
		render().sendData(fb);
	}

	public Triple render(Triple p)//convert point p in world space into screen-space
	{
		this.p = p;
		return render();
	}
	
	//converts coordinates of point p in world-space to the camera's screen-space, beta (x, [-1, 1]),
	//gamma (y, [-1, 1]), and lambda(z, [-1, 1], used for depth buffering in render pipeline)
	public Triple render()
	{
		pme = p.minus(e);
		float lambda = -ema.dot(ema)/ema.dot(pme);
		float beta = lambda * bma.dot(pme)/bma.dot(bma);
		float gamma = lambda * cma.dot(pme)/cma.dot(cma);
		return new Triple(beta, gamma, lambda);
	}
	
	public Triple getA()
	{
		return a;
	}

	public Triple getB()
	{
		return b;
	}

	public Triple getC()
	{
		return c;
	}

	public Triple getE()
	{
		return e;
	}

	public Triple getP()
	{
		return p;
	}

	public Triple getEMA()
	{
		return ema;
	}

	public Triple getCMA()
	{
		return cma;
	}

	public Triple getBMA()
	{
		return bma;
	}

	public Triple getPME()
	{
		return pme;
	}
}
