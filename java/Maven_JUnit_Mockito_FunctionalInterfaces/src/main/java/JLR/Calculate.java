package JLR;

import java.util.Scanner;
import java.io.InputStream;
import java.io.File;

public class Calculate
{
	public static void main(String[] args)
	{
		try
		{
			InputStream is = Calculate.class.getClassLoader().getResourceAsStream("input.txt");
			Scanner sc = new Scanner(is);
			System.out.println("File accessed");
			Camera cam = new Camera(sc);
			System.out.println("E: " + cam.getE().toString() + "\n" +
					   "A: " + cam.getA().toString() + "\n" +
					   "B: " + cam.getB().toString() + "\n" +
					   "C: " + cam.getC().toString() + "\n" +
					   "E-A: " + cam.getEMA().toString() + "\n" +
					   "C-A: " + cam.getCMA().toString() + "\n" +
					   "B-A: " + cam.getBMA().toString() + "\n" +
					   "(E-A).(E-A): " + cam.getEMA().dot(cam.getEMA()) + "\n" +
					   "(B-A).(B-A): " + cam.getBMA().dot(cam.getBMA()) + "\n" +
					   "(C-A).(C-A): " + cam.getCMA().dot(cam.getCMA()) + "\n" +
					   "(E-A).(B-A): " + cam.getEMA().dot(cam.getBMA()) + "\n" +
					   "(E-A).(C-A): " + cam.getEMA().dot(cam.getCMA()) + "\n" +
					   "(B-A).(C-A): " + cam.getBMA().dot(cam.getCMA()) + "\n" 
					   );
			int count = 1;
			Triple ret = Triple.zeroTriple;
			while(sc.hasNext())
			{
				ret = new Triple(sc);
				ret = cam.render(ret);
				System.out.println("For point P" + count + ": \n" +
						   "P = " + cam.getP().toString() + "\n" +
						   "PME = " + cam.getPME().toString() + "\n" +
					  	   "(E-A).(P-E): " + cam.getEMA().dot(cam.getPME()) + "\n" +
					  	   "(B-A).(P-E): " + cam.getBMA().dot(cam.getPME()) + "\n" +
					  	   "(C-A).(P-E): " + cam.getEMA().dot(cam.getPME()) + "\n" +
						   "beta = " + ret.x + "\n" +
						   "gamma = " + ret.y + "\n" +
						   "lamda = " + ret.z + "\n"
						   );
				count++;
			}
			sc.close();
		}
		catch(IllegalArgumentException e)//not terribly concerned about Exception type for this
		{
			System.out.println("Error reading file");
			System.exit(1);
		}
	}
}
